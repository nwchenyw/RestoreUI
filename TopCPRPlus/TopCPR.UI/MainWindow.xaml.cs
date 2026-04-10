using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TopCPR.Core;

namespace TopCPR.UI;

public partial class MainWindow : Window
{
    private readonly List<Grid> _panels;
    private readonly DispatcherTimer _sessionTimer;
    private int _failCount;
    private bool _isAdmin;
    private bool _locked;
    private string _token = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        _panels = new List<Grid> { DashboardPanel, ProtectionPanel, SnapshotsPanel, BootPanel, SettingsPanel };
        _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(10) };
        _sessionTimer.Tick += (_, _) => LockUi("閒置逾時，請重新登入。\n");

        Loaded += (_, _) =>
        {
            ShowPanel(DashboardPanel);
            LockUi("請登入管理模式。\n");
        };

        PreviewMouseDown += (_, _) => ResetSessionTimer();
        PreviewKeyDown += (_, _) => ResetSessionTimer();
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        if (_locked) return;

        var cfg = ConfigManager.Load();
        var input = PwdLogin.Password;

        if (string.IsNullOrWhiteSpace(cfg.PasswordSalt))
            cfg.PasswordSalt = SecurityService.GenerateSalt();

        if (string.IsNullOrWhiteSpace(cfg.PasswordHash))
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                TxtLoginHint.Text = "首次登入請輸入新密碼。";
                return;
            }

            cfg.PasswordHash = SecurityService.HashPassword(input, cfg.PasswordSalt);
            cfg.AuthTokenHash = SecurityService.CreateAuthToken(input, cfg.PasswordSalt);
            ConfigManager.Save(cfg);
        }

        if (!SecurityService.VerifyPassword(input, cfg.PasswordSalt, cfg.PasswordHash))
        {
            _failCount++;
            TxtLoginHint.Text = $"密碼錯誤，剩餘次數: {Math.Max(0, 3 - _failCount)}";
            if (_failCount >= 3)
            {
                _locked = true;
                TxtLoginHint.Text = "UI 已鎖定，請重新啟動。";
            }
            return;
        }

        _token = SecurityService.CreateAuthToken(input, cfg.PasswordSalt);
        _isAdmin = true;
        LoginOverlay.Visibility = Visibility.Collapsed;
        StartSession(cfg.SessionTimeoutMinutes);
        RefreshStatus();
        RefreshSnapshots();
    }

    private void BtnDashboard_Click(object sender, RoutedEventArgs e) => ShowPanel(DashboardPanel);
    private void BtnProtection_Click(object sender, RoutedEventArgs e) => ShowPanel(ProtectionPanel);
    private void BtnSnapshots_Click(object sender, RoutedEventArgs e)
    {
        RefreshSnapshots();
        ShowPanel(SnapshotsPanel);
    }
    private void BtnBoot_Click(object sender, RoutedEventArgs e) => ShowPanel(BootPanel);

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            MessageBox.Show("需 Shift + Click 才能開啟 Settings。", "保護", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        ShowPanel(SettingsPanel);
    }

    private void BtnEnable_Click(object sender, RoutedEventArgs e) => Exec("ENABLE_RESTORE", "Restore mode 已啟用。");
    private void BtnDisable_Click(object sender, RoutedEventArgs e) => Exec("DISABLE_RESTORE", "Restore mode 已停用。");

    private void BtnCreateSnapshot_Click(object sender, RoutedEventArgs e) => Exec("CREATE_SNAPSHOT", "快照已建立。", true);

    private void BtnRestoreSnapshot_Click(object sender, RoutedEventArgs e)
    {
        var id = GetSnapshotId();
        if (string.IsNullOrWhiteSpace(id)) return;
        Exec($"RESTORE_SNAPSHOT|{id}", "快照已還原。", true);
    }

    private void BtnDeleteSnapshot_Click(object sender, RoutedEventArgs e)
    {
        var id = GetSnapshotId();
        if (string.IsNullOrWhiteSpace(id)) return;
        Exec($"DELETE_SNAPSHOT|{id}", "快照已加入延遲刪除佇列。", true);
    }

    private void BtnSetNormal_Click(object sender, RoutedEventArgs e) => Exec("SET_BOOT_NORMAL", "已切換為 Normal Mode。\n");
    private void BtnSetRestore_Click(object sender, RoutedEventArgs e) => Exec("SET_BOOT_RESTORE", "已切換為 Restore Mode。\n");

    private void BtnSetTimeout_Click(object sender, RoutedEventArgs e)
    {
        var raw = TxtTimeout.Text?.Trim() ?? "5";
        if (!int.TryParse(raw, out var sec))
        {
            MessageBox.Show("Timeout 必須是數字。", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Exec($"SET_BOOT_TIMEOUT|{sec}", "Boot timeout 已更新。\n");
    }

    private void BtnSetPassword_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureAdmin()) return;

        var pwd = PwdNew.Password;
        if (string.IsNullOrWhiteSpace(pwd))
        {
            MessageBox.Show("請輸入新密碼。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var cfg = ConfigManager.Load();
        cfg.PasswordSalt = SecurityService.GenerateSalt();
        cfg.PasswordHash = SecurityService.HashPassword(pwd, cfg.PasswordSalt);
        cfg.AuthTokenHash = SecurityService.CreateAuthToken(pwd, cfg.PasswordSalt);
        ConfigManager.Save(cfg);

        _token = cfg.AuthTokenHash;
        MessageBox.Show("密碼已更新。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Exec(string command, string okMessage, bool refreshSnapshots = false)
    {
        if (!EnsureAdmin()) return;

        try
        {
            var result = PipeClient.Send(_token, command);
            if (!result.StartsWith("OK:", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(result, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RefreshStatus();
            if (refreshSnapshots) RefreshSnapshots();
            MessageBox.Show(okMessage, "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("服務通訊失敗：" + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool EnsureAdmin()
    {
        if (_isAdmin && !string.IsNullOrWhiteSpace(_token)) return true;
        MessageBox.Show("請先登入管理模式。", "權限不足", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private void RefreshStatus()
    {
        if (string.IsNullOrWhiteSpace(_token)) return;

        try
        {
            var hb = PipeClient.Send(_token, PipeProtocol.Heartbeat);
            TxtHeartbeat.Text = hb.StartsWith("OK") ? "Service: Online" : "Service: Warning";

            var status = PipeClient.Send(_token, PipeProtocol.Status);
            var parts = status.Split(':');
            TxtStatus.Text = parts.Length > 3 ? $"Status: {parts[2]} / {parts[3]}" : "Status: Unknown";
            TxtSnapshot.Text = parts.Length > 4 ? "Current Snapshot: " + parts[4] : "Current Snapshot: NONE";
        }
        catch
        {
            TxtHeartbeat.Text = "Service: Offline";
        }
    }

    private void RefreshSnapshots()
    {
        if (string.IsNullOrWhiteSpace(_token)) return;

        LstSnapshots.Items.Clear();
        try
        {
            var result = PipeClient.Send(_token, PipeProtocol.ListSnapshots);
            if (!result.StartsWith("OK:SNAPSHOT_LIST:")) return;

            var payload = result.Substring("OK:SNAPSHOT_LIST:".Length);
            foreach (var row in payload.Split(';', StringSplitOptions.RemoveEmptyEntries))
                LstSnapshots.Items.Add(row);
        }
        catch { }
    }

    private string GetSnapshotId()
    {
        var raw = LstSnapshots.SelectedItem?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var idx = raw.IndexOf('|');
        return idx <= 0 ? raw : raw[..idx];
    }

    private void ShowPanel(Grid panel)
    {
        foreach (var p in _panels)
            p.Visibility = Visibility.Collapsed;

        panel.Visibility = Visibility.Visible;
        panel.Opacity = 0;
        panel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
    }

    private void StartSession(int minutes)
    {
        if (minutes <= 0) minutes = 10;
        _sessionTimer.Interval = TimeSpan.FromMinutes(minutes);
        _sessionTimer.Start();
    }

    private void ResetSessionTimer()
    {
        if (!_isAdmin) return;
        _sessionTimer.Stop();
        _sessionTimer.Start();
    }

    private void LockUi(string message)
    {
        _isAdmin = false;
        _token = string.Empty;
        LoginOverlay.Visibility = Visibility.Visible;
        TxtLoginHint.Text = message;
        _sessionTimer.Stop();
    }
}