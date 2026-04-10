using System;
using System.Windows.Forms;
using Restore.Core;
using Restore.Engine;

namespace RestoreUI
{
    public partial class Form1 : Form
    {
        private RestoreConfig _config;
        private RestoreEngine _engine;

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
            _engine = new RestoreEngine(_config.ProtectDrive);
            UpdateStatusUI();
        }

        private void UpdateStatusUI()
        {
            if (_config.Enabled)
            {
                lblStatus.Text = "狀態：已啟用還原（保護 " + _config.ProtectDrive + ": 磁碟）";
                lblStatus.ForeColor = System.Drawing.Color.Green;
                btnEnable.Enabled = false;
                btnDisable.Enabled = true;
            }
            else
            {
                lblStatus.Text = "狀態：已停用還原";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                btnEnable.Enabled = true;
                btnDisable.Enabled = false;
            }
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

            _config.Enabled = true;
            ConfigManager.Save(_config);
            UpdateStatusUI();
            MessageBox.Show("還原已啟用，下次開機將自動還原。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnDisable_Click(object sender, EventArgs e)
        {
            if (!VerifyPassword()) return;

            _config.Enabled = false;
            ConfigManager.Save(_config);
            UpdateStatusUI();
            MessageBox.Show("還原已停用。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnRestoreNow_Click(object sender, EventArgs e)
        {
            if (!VerifyPassword()) return;

            var result = MessageBox.Show("確定要立即還原嗎？這將刪除所有變更。", "確認還原",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _engine.DeleteDiff();
                    MessageBox.Show("還原完成。建議重新開機。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("還原失敗：" + ex.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
    }
}
