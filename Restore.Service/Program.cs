using System;
using System.ServiceProcess;

namespace Restore.Service
{
    static class Program
    {
        static void Main(string[] args)
        {
            bool runAsConsole = Environment.UserInteractive;
            if (!runAsConsole && args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--console", StringComparison.OrdinalIgnoreCase))
                    {
                        runAsConsole = true;
                        break;
                    }
                }
            }

            if (runAsConsole)
            {
                var service = new RestoreService();
                service.StartDebug(args ?? new string[0]);
                Console.WriteLine("RestoreService 已在主控台模式啟動。按 Enter 停止...");
                Console.ReadLine();
                service.StopDebug();
                return;
            }

            ServiceBase[] ServicesToRun = new ServiceBase[]
            {
                new RestoreService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
