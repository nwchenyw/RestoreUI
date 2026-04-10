using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace Restore.Service
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller _processInstaller;
        private ServiceInstaller _serviceInstaller;

        public ProjectInstaller()
        {
            _processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };

            _serviceInstaller = new ServiceInstaller
            {
                ServiceName = "RestoreService",
                DisplayName = "Restore System Service",
                Description = "開機自動還原系統服務（登入前執行）",
                StartType = ServiceStartMode.Automatic,
                DelayedAutoStart = false
            };

            Installers.Add(_processInstaller);
            Installers.Add(_serviceInstaller);
        }

        protected override void OnAfterInstall(System.Collections.IDictionary savedState)
        {
            base.OnAfterInstall(savedState);
            // 設定服務失敗時自動重啟（第1/2/3次失敗都重啟，60秒後）
            System.Diagnostics.Process.Start("sc", "failure RestoreService reset= 86400 actions= restart/60000/restart/60000/restart/60000");
        }
    }
}
