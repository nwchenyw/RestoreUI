using System;
using System.IO;
using System.ServiceProcess;
using System.Windows.Forms;
using Restore.Core;

namespace RestoreUI
{
    public partial class Form1 : Form
    {
        private RestoreConfig _config;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                _config = ConfigManager.Load();
            }
            catch
            {
                _config = new RestoreConfig();
            }
            UpdateStatusUI();
        }

        private void UpdateStatusUI()
        {
            bool serviceOnline = IsServiceRunning("RestoreService");
            bool baseExists = IsBaseVhdExists();

            string statusText;
            if (serviceOnline)
            {
                string serviceStatus = QueryServiceStatus();
                bool vmSafe = serviceStatus.IndexOf("VM_SAFE", StringComparison.OrdinalIgnoreCase) >= 0;
                bool enabled = serviceStatus.StartsWith("STATUS:ENABLED", StringComparison.OrdinalIgnoreCase);
                if (!baseExists)
                {
                    statusText = "狀態：服務已啟動，但缺少 base.vhdx";
                }
                else
                {
                    statusText = enabled
                        ? "狀態：已啟用還原（Service）"
                        : "狀態：已停用還原（Service）";

                    if (vmSafe)
                        statusText += "（VM 安全模式）";
                }
                _config.Enabled = enabled;
            }
            else
            {
                statusText = baseExists
                    ? "狀態：服務未執行"
                    : "狀態：服務未執行，且缺少 base.vhdx";
            }

            if (_config.Enabled)
            {
                lblStatus.Text = statusText;
                lblStatus.ForeColor = System.Drawing.Color.Green;
                btnEnable.Enabled = false;
                btnDisable.Enabled = true;
            }
            else
            {
                lblStatus.Text = statusText;
                lblStatus.ForeColor = serviceOnline ? System.Drawing.Color.Red : System.Drawing.Color.DarkOrange;
                btnEnable.Enabled = serviceOnline;
                btnDisable.Enabled = serviceOnline;
            }

            btnRestoreNow.Enabled = serviceOnline;
            btnStartService.Enabled = !serviceOnline;

            if (!baseExists)
                lblStatus.ForeColor = System.Drawing.Color.DarkOrange;
        }

        private bool VerifyPassword()
        {
            if (string.IsNullOrEmpty(_config.PasswordHash))
                return true;

            string input = txtPassword.Text;
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("請輸入密碼。", "密碼驗證", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!AuthManager.Verify(input, _config.PasswordHash))
            {
                MessageBox.Show("密碼錯誤。", "密碼驗證", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private void btnEnable_Click(object sender, EventArgs e)
        {
            if (!VerifyPassword()) return;

            ExecuteServiceCommand(PipeConstants.EnableProtection, "還原已啟用，下次開機將自動還原。", "啟用還原失敗");
        }

        private void btnDisable_Click(object sender, EventArgs e)
        {
            if (!VerifyPassword()) return;

            ExecuteServiceCommand(PipeConstants.DisableProtection, "還原已停用。", "停用還原失敗");
        }

        private void btnRestoreNow_Click(object sender, EventArgs e)
        {
            if (!VerifyPassword()) return;

            var result = MessageBox.Show("確定要立即還原嗎？這將刪除所有變更。", "確認還原",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                ExecuteServiceCommand(PipeConstants.ResetSystem, "還原完成。建議重新開機。", "還原失敗");
            }
        }

        private void btnSetPassword_Click(object sender, EventArgs e)
        {
            string newPassword = txtPassword.Text;
            if (string.IsNullOrEmpty(newPassword))
            {
                MessageBox.Show("請輸入新密碼。", "設定密碼", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _config.PasswordHash = AuthManager.Hash(newPassword);
            ConfigManager.Save(_config);
            txtPassword.Clear();
            MessageBox.Show("密碼已設定。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string QueryServiceStatus()
        {
            try
            {
                return PipeClient.SendCommand(PipeConstants.Status);
            }
            catch
            {
                return "STATUS:DISABLED";
            }
        }

        private void ExecuteServiceCommand(string command, string successMessage, string errorTitle)
        {
            try
            {
                string result = PipeClient.SendCommand(command);
                if (result.StartsWith("OK:"))
                {
                    LoadConfig();
                    MessageBox.Show(successMessage, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(result, errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("無法與服務通訊：" + ex.Message, errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsServiceRunning(string serviceName)
        {
            try
            {
                using (var sc = new ServiceController(serviceName))
                {
                    return sc.Status == ServiceControllerStatus.Running;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool IsBaseVhdExists()
        {
            try
            {
                string protectDrive = string.IsNullOrWhiteSpace(_config.ProtectDrive) ? "C" : _config.ProtectDrive;
                string restoreFolder = string.Equals(protectDrive, "C", StringComparison.OrdinalIgnoreCase)
                    ? @"C:\RestoreSystem"
                    : protectDrive + @":\RestoreSystem";

                string basePath = Path.Combine(restoreFolder, "base.vhdx");
                return File.Exists(basePath);
            }
            catch
            {
                return false;
            }
        }

        private void btnStartService_Click(object sender, EventArgs e)
        {
            if (!VerifyPassword()) return;

            try
            {
                using (var sc = new ServiceController("RestoreService"))
                {
                    if (sc.Status != ServiceControllerStatus.Running &&
                        sc.Status != ServiceControllerStatus.StartPending)
                    {
                        sc.Start();
                    }

                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                }

                LoadConfig();
                MessageBox.Show("服務已啟動。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("啟動服務失敗：" + ex.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
