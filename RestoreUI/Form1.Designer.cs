namespace RestoreUI
{
    partial class Form1
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.btnEnable = new System.Windows.Forms.Button();
            this.btnDisable = new System.Windows.Forms.Button();
            this.btnRestoreNow = new System.Windows.Forms.Button();
            this.btnStartService = new System.Windows.Forms.Button();
            this.btnSetPassword = new System.Windows.Forms.Button();
            this.grpStatus = new System.Windows.Forms.GroupBox();
            this.grpPassword = new System.Windows.Forms.GroupBox();
            this.grpStatus.SuspendLayout();
            this.grpPassword.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpStatus
            // 
            this.grpStatus.Controls.Add(this.lblStatus);
            this.grpStatus.Controls.Add(this.btnEnable);
            this.grpStatus.Controls.Add(this.btnDisable);
            this.grpStatus.Controls.Add(this.btnRestoreNow);
            this.grpStatus.Controls.Add(this.btnStartService);
            this.grpStatus.Location = new System.Drawing.Point(20, 20);
            this.grpStatus.Name = "grpStatus";
            this.grpStatus.Size = new System.Drawing.Size(440, 160);
            this.grpStatus.TabIndex = 0;
            this.grpStatus.TabStop = false;
            this.grpStatus.Text = "還原狀態";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft JhengHei UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblStatus.Location = new System.Drawing.Point(20, 30);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(100, 25);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "狀態：載入中...";
            // 
            // btnEnable
            // 
            this.btnEnable.Location = new System.Drawing.Point(20, 70);
            this.btnEnable.Name = "btnEnable";
            this.btnEnable.Size = new System.Drawing.Size(120, 35);
            this.btnEnable.TabIndex = 1;
            this.btnEnable.Text = "啟用還原";
            this.btnEnable.UseVisualStyleBackColor = true;
            this.btnEnable.Click += new System.EventHandler(this.btnEnable_Click);
            // 
            // btnDisable
            // 
            this.btnDisable.Location = new System.Drawing.Point(160, 70);
            this.btnDisable.Name = "btnDisable";
            this.btnDisable.Size = new System.Drawing.Size(120, 35);
            this.btnDisable.TabIndex = 2;
            this.btnDisable.Text = "停用還原";
            this.btnDisable.UseVisualStyleBackColor = true;
            this.btnDisable.Click += new System.EventHandler(this.btnDisable_Click);
            // 
            // btnRestoreNow
            // 
            this.btnRestoreNow.Location = new System.Drawing.Point(300, 70);
            this.btnRestoreNow.Name = "btnRestoreNow";
            this.btnRestoreNow.Size = new System.Drawing.Size(120, 35);
            this.btnRestoreNow.TabIndex = 3;
            this.btnRestoreNow.Text = "立即還原";
            this.btnRestoreNow.UseVisualStyleBackColor = true;
            this.btnRestoreNow.Click += new System.EventHandler(this.btnRestoreNow_Click);
            // 
            // btnStartService
            // 
            this.btnStartService.Location = new System.Drawing.Point(20, 115);
            this.btnStartService.Name = "btnStartService";
            this.btnStartService.Size = new System.Drawing.Size(400, 30);
            this.btnStartService.TabIndex = 4;
            this.btnStartService.Text = "服務未啟動？一鍵啟動 RestoreService";
            this.btnStartService.UseVisualStyleBackColor = true;
            this.btnStartService.Click += new System.EventHandler(this.btnStartService_Click);
            // 
            // grpPassword
            // 
            this.grpPassword.Controls.Add(this.lblPassword);
            this.grpPassword.Controls.Add(this.txtPassword);
            this.grpPassword.Controls.Add(this.btnSetPassword);
            this.grpPassword.Location = new System.Drawing.Point(20, 200);
            this.grpPassword.Name = "grpPassword";
            this.grpPassword.Size = new System.Drawing.Size(440, 100);
            this.grpPassword.TabIndex = 1;
            this.grpPassword.TabStop = false;
            this.grpPassword.Text = "密碼設定";
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(20, 35);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(41, 12);
            this.lblPassword.TabIndex = 0;
            this.lblPassword.Text = "密碼：";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(80, 32);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(200, 22);
            this.txtPassword.TabIndex = 1;
            // 
            // btnSetPassword
            // 
            this.btnSetPassword.Location = new System.Drawing.Point(300, 28);
            this.btnSetPassword.Name = "btnSetPassword";
            this.btnSetPassword.Size = new System.Drawing.Size(120, 30);
            this.btnSetPassword.TabIndex = 2;
            this.btnSetPassword.Text = "設定密碼";
            this.btnSetPassword.UseVisualStyleBackColor = true;
            this.btnSetPassword.Click += new System.EventHandler(this.btnSetPassword_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 321);
            this.Controls.Add(this.grpStatus);
            this.Controls.Add(this.grpPassword);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RestoreSystem 還原系統控制台";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.grpStatus.ResumeLayout(false);
            this.grpStatus.PerformLayout();
            this.grpPassword.ResumeLayout(false);
            this.grpPassword.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox grpStatus;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnEnable;
        private System.Windows.Forms.Button btnDisable;
        private System.Windows.Forms.Button btnRestoreNow;
        private System.Windows.Forms.Button btnStartService;
        private System.Windows.Forms.GroupBox grpPassword;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Button btnSetPassword;
    }
}

