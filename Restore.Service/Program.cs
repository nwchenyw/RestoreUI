using System.ServiceProcess;

namespace Restore.Service
{
    static class Program
    {
        static void Main()
        {
            ServiceBase[] ServicesToRun = new ServiceBase[]
            {
                new RestoreService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
