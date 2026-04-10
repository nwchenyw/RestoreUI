using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using RestoreSystem.Core;
using RestoreSystem.Shared;

namespace RestoreSystem.UI;

public partial class MainWindow : Window
{
    private readonly List<Grid> _panels;
    private readonly DispatcherTimer _sessionTimer;
    private int _loginFailCount;
    private bool _isLocked;
    private bool _isAdminMode;
    private string _authToken = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        _panels = new List<Grid> { DashboardPanel, ProtectionPanel, SnapshotsPanel, BootModePanel, SettingsPanel };
        _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(10) };
        _sessionTimer.Tick += SessionTimer_Tick;

        Loaded += MainWindow_Loaded;
        PreviewMouseDown += (_, _) => ResetSessionTimer();
        PreviewKeyDown += (_, _) => ResetSessionTimer();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ShowPanel(DashboardPanel);
        ForceLockUi("請先登入管理模式。");
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        if (_isLocked)
            return;

        var config = ConfigManager.Load();
        var input = PwdLogin.Password;

        if (string.IsNullOrWhiteSpace(config.PasswordHash))
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                TxtLoginHint.Text = "首次登入請輸入要設定的密碼。";
                return;
            }

            config.PasswordHash = PasswordHasher.Hash(input);
            config.AuthTokenHash = AuthTokenManager.BuildToken(input);
            ConfigManager.Save(config);
        }

        if (!PasswordHasher.Verify(input, config.PasswordHash))
        {
            _loginFailCount++;
            TxtLoginHint.Text = $"密碼錯誤，剩餘嘗試：{Math.Max(0, 3 - _loginFailCount)}";
            if (_loginFailCount >= 3)
            {
                _isLocked = true;
                BtnLogin.IsEnabled = false;
                TxtLoginHint.Text = "已鎖定，請重新啟動應用程式。";
            }
            return;
        }

        _authToken = string.IsNullOrWhiteSpace(config.AuthTokenHash)
            ? AuthTokenManager.BuildToken(input)
            : config.AuthTokenHash;

        _isAdminMode = true;
        ChkAdminMode.IsChecked = true;
        LoginOverlay.Visibility = Visibility.Collapsed;
        TxtLoginHint.Text = string.Empty;

        StartSession(config.SessionTimeoutMinutes);
        RefreshStatus();
        RefreshSnapshots();
    }

    private void BtnNavDashboard_Click(object sender, RoutedEventArgs e) => ShowPanel(DashboardPanel);
    private void BtnNavProtection_Click(object sender, RoutedEventArgs e) => ShowPanel(ProtectionPanel);
    private void BtnNavSnapshots_Click(object sender, RoutedEventArgs e)
    {
        RefreshSnapshots();
        ShowPanel(SnapshotsPanel);
    }
    private void BtnNavBootMode_Click(object sender, RoutedEventArgs e) => ShowPanel(BootModePanel);

    private void BtnNavSettings_Click(object sender, RoutedEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            MessageBox.Show("請使用 Shift + Click 解鎖 Settings。", "保護模式", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ShowPanel(SettingsPanel);
    }

    private void ShowPanel(Grid panel)
    {
        foreach (var p in _panels)
            p.Visibility = Visibility.Collapsed;

        panel.Opacity = 0;
        panel.Visibility = Visibility.Visible;

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220));
        panel.BeginAnimation(OpacityProperty, fade);
    }

    private void BtnEnable_Click(object sender, RoutedEventArgs e)
    {
        ExecuteAdminCommand(PipeConstants.EnableProtection, "已啟用保護模式。");
    }

    private void BtnDisable_Click(object sender, RoutedEventArgs e)
    {
        ExecuteAdminCommand(PipeConstants.DisableProtection, "已停用保護模式。");
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        ExecuteAdminCommand(PipeConstants.ResetSystem, "已完成系統重置。\n");
    }

    private void BtnCreateSnapshot_Click(object sender, RoutedEventArgs e)
    {
        ExecuteAdminCommand(PipeConstants.CreateSnapshot, "已建立快照。", refreshSnapshots: true);
    }

    private void BtnRestoreSnapshot_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureAdmin()) return;
        var id = GetSelectedSnapshotId();
        if (string.IsNullOrWhiteSpace(id))
        {
            MessageBox.Show("請先選擇快照。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ExecuteRawCommand($"{PipeConstants.RestoreSnapshot}|{id}", "已還原快照。", true);
    }

    private void BtnDeleteSnapshot_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureAdmin()) return;
        var id = GetSelectedSnapshotId();
        if (string.IsNullOrWhiteSpace(id))
        {
            MessageBox.Show("請先選擇快照。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ExecuteRawCommand($"{PipeConstants.DeleteSnapshot}|{id}", "已刪除快照。", true);
    }

    private void BtnSetPassword_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureAdmin()) return;

        var newPassword = PwdSet.Password;
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            MessageBox.Show("請輸入新密碼。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var config = ConfigManager.Load();
        config.PasswordHash = PasswordHasher.Hash(newPassword);
        config.AuthTokenHash = AuthTokenManager.BuildToken(newPassword);
        ConfigManager.Save(config);
        _authToken = config.AuthTokenHash;
        PwdSet.Clear();
        MessageBox.Show("密碼已更新。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnBootNormal_Click(object sender, RoutedEventArgs e)
    {
        ExecuteAdminCommand("SET_BOOT_NORMAL", "已切換預設開機為 Normal Mode。");
    }

    private void BtnBootRestore_Click(object sender, RoutedEventArgs e)
    {
        ExecuteAdminCommand("SET_BOOT_RESTORE", "已切換預設開機為 Restore Mode。");
    }

    private void BtnSetBootTimeout_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureAdmin()) return;
        var raw = (TxtBootTimeout.Text ?? string.Empty).Trim();
        if (!int.TryParse(raw, out var seconds))
        {
            MessageBox.Show("請輸入 0~30 的整數。", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ExecuteRawCommand($"SET_BOOT_TIMEOUT|{seconds}", "已更新 boot timeout。", false);
    }

    private void ChkAdminMode_Checked(object sender, RoutedEventArgs e)
    {
        _isAdminMode = ChkAdminMode.IsChecked == true && _isAdminMode;
    }

    private bool EnsureAdmin()
    {
        if (_isAdminMode && !string.IsNullOrWhiteSpace(_authToken))
            return true;

        MessageBox.Show("請先登入並啟用 Admin Mode。", "權限不足", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private void ExecuteAdminCommand(string command, string successMessage, bool refreshSnapshots = false, int timeoutMs = 30000)
    {
        if (!EnsureAdmin()) return;
        ExecuteRawCommand(command, successMessage, refreshSnapshots, timeoutMs);
    }

    private void ExecuteRawCommand(string command, string successMessage, bool refreshSnapshots, int timeoutMs = 30000)
    {
        try
        {
            var result = PipeClient.SendAuthenticatedRaw(_authToken, command, timeoutMs);
            if (!result.StartsWith("OK:", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(result, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RefreshStatus();
            if (refreshSnapshots)
                RefreshSnapshots();

            MessageBox.Show(successMessage, "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (TimeoutException)
        {
            MessageBox.Show("服務處理逾時，操作可能仍在進行中。\n請稍候後檢查服務狀態。", "逾時", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show("服務通訊失敗：" + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshStatus()
    {
        if (string.IsNullOrWhiteSpace(_authToken))
            return;

        try
        {
            var status = PipeClient.SendAuthenticated(_authToken, RestoreCommand.Status);
            var parts = status.Split(':');
            var protection = parts.Length > 1 ? parts[1] : "UNKNOWN";
            var baseState = parts.Length > 2 ? parts[2] : "UNKNOWN";
            var snapshot = parts.Length > 3 ? parts[3] : "NONE";

            TxtProtectionStatus.Text = "Protection: " + protection;
            TxtBaseStatus.Text = "Base Disk: " + baseState;
            TxtCurrentSnapshot.Text = "Current Snapshot: " + snapshot;

            StatusDot.Fill = protection == "ENABLED" ? Brushes.LimeGreen : Brushes.OrangeRed;
        }
        catch (Exception ex)
        {
            TxtProtectionStatus.Text = "Protection: SERVICE_OFFLINE";
            TxtBaseStatus.Text = "Base Disk: UNKNOWN";
            TxtCurrentSnapshot.Text = "Current Snapshot: NONE";
            StatusDot.Fill = Brushes.DarkOrange;
            if (_isAdminMode)
                MessageBox.Show("無法取得服務狀態：" + ex.Message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RefreshSnapshots()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_authToken))
                return;

            var result = PipeClient.SendAuthenticated(_authToken, RestoreCommand.ListSnapshots);
            LstSnapshots.Items.Clear();

            if (!result.StartsWith("OK:SNAPSHOT_LIST:", StringComparison.OrdinalIgnoreCase))
                return;

            var payload = result.Substring("OK:SNAPSHOT_LIST:".Length);
            if (string.IsNullOrWhiteSpace(payload))
                return;

            foreach (var item in payload.Split(';', StringSplitOptions.RemoveEmptyEntries))
                LstSnapshots.Items.Add(item);
        }
        catch
        {
            LstSnapshots.Items.Clear();
        }
    }

    private string GetSelectedSnapshotId()
    {
        if (LstSnapshots.SelectedItem is null)
            return string.Empty;

        var text = LstSnapshots.SelectedItem.ToString() ?? string.Empty;
        var index = text.IndexOf('|');
        return index > 0 ? text[..index] : text;
    }

    private void SessionTimer_Tick(object? sender, EventArgs e)
    {
        ForceLockUi("管理模式逾時，請重新登入。\n");
    }

    private void StartSession(int timeoutMinutes)
    {
        if (timeoutMinutes <= 0)
            timeoutMinutes = 10;

        _sessionTimer.Stop();
        _sessionTimer.Interval = TimeSpan.FromMinutes(timeoutMinutes);
        _sessionTimer.Start();
    }

    private void ResetSessionTimer()
    {
        if (!_isAdminMode)
            return;

        _sessionTimer.Stop();
        _sessionTimer.Start();
    }

    private void ForceLockUi(string message)
    {
        _isAdminMode = false;
        _authToken = string.Empty;
        ChkAdminMode.IsChecked = false;
        LoginOverlay.Visibility = Visibility.Visible;
        TxtLoginHint.Text = message;
        _sessionTimer.Stop();
    }
}