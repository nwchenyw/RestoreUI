using System.ServiceProcess;
using Restore.Core;
using Restore.Engine;

namespace Restore.Service
{
    public partial class RestoreService : ServiceBase
    {
        private RestoreEngine _engine;

        public RestoreService()
        {
            ServiceName = "RestoreService";
        }

        protected override void OnStart(string[] args)
        {
            var config = ConfigManager.Load();
            if (config.Enabled)
            {
                _engine = new RestoreEngine(config.ProtectDrive);
                _engine.CreateDiff();
                _engine.Mount();
            }
        }

        protected override void OnStop()
        {
            var config = ConfigManager.Load();
            if (config.Enabled && _engine != null)
            {
                _engine.DeleteDiff();
            }
        }
    }
}
