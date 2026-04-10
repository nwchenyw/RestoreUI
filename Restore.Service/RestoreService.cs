using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ServiceProcess;
using Restore.Core;
using Restore.Engine;

namespace Restore.Service
{
    public partial class RestoreService : ServiceBase
    {
        private RestoreEngine _engine;
        private BootIntegrationManager _bootManager;
        private string _bootEntryGuid;
        private CancellationTokenSource _pipeCts;
        private Task _pipeServerTask;
        private readonly object _sync = new object();

        public RestoreService()
        {
            ServiceName = "RestoreService";
        }

        protected override void OnStart(string[] args)
        {
            ServiceLogger.Info("服務啟動。");
            _pipeCts = new CancellationTokenSource();
            _pipeServerTask = Task.Run(() => RunPipeServer(_pipeCts.Token));

            try
            {
                var config = ConfigManager.Load();
                EnsureEngine(config);
                EnsureBaseVhdGuide(config);

                if (config.Enabled)
                    EnableProtection(config);
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("啟動時初始化失敗。", ex);
            }
        }

        protected override void OnStop()
        {
            ServiceLogger.Info("服務停止。");

            if (_pipeCts != null)
            {
                _pipeCts.Cancel();
                try { _pipeServerTask.Wait(2000); } catch { }
                _pipeCts.Dispose();
                _pipeCts = null;
            }

            try
            {
                var config = ConfigManager.Load();
                if (config.Enabled && _engine != null)
                    _engine.DeleteDiff();
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("停止時清理失敗。", ex);
            }
        }

        private void EnsureEngine(RestoreConfig config)
        {
            _engine = new RestoreEngine(config.ProtectDrive);
            _bootManager = new BootIntegrationManager();
            _engine.EnsureRestoreFolder();
        }

        private void EnsureBaseVhdGuide(RestoreConfig config)
        {
            EnsureEngine(config);

            bool created = _engine.CreateBaseIfMissing();
            if (!created)
                return;

            string guidePath = Path.Combine(_engine.RestoreFolder, "BASE_SETUP_README.txt");
            var lines = new[]
            {
                "RestoreSystem Base VHD Setup Guide",
                "1) 已自動建立 base.vhdx（首次安裝流程）。",
                "2) 請確認 base.vhdx 為可用的基底映像。",
                "3) 若要採用企業標準映像，請先停用保護後替換 base.vhdx。",
                "4) 完成後再於 UI 啟用保護模式。"
            };
            File.WriteAllLines(guidePath, lines, Encoding.UTF8);
            ServiceLogger.Info("首次流程：已建立 base.vhdx 並產生引導檔。" + guidePath);
        }

        private void EnableProtection(RestoreConfig config)
        {
            lock (_sync)
            {
                EnsureEngine(config);
                if (!_engine.BaseExists())
                {
                    config.Enabled = false;
                    ConfigManager.Save(config);
                    ServiceLogger.Error("base.vhdx 不存在，已自動停用保護模式。路徑：" + _engine.BasePath);
                    return;
                }

                if (_engine.DiffExists())
                    _engine.DeleteDiff();

                _engine.CreateDiff();
                _engine.Mount();

                _bootEntryGuid = _bootManager.EnsureBootEntry(config.BootEntryGuid);
                config.BootEntryGuid = _bootEntryGuid;
                ConfigManager.Save(config);
                _bootManager.SetDefaultBootEntry(_bootEntryGuid);

                ServiceLogger.Info("已啟用保護模式。BootEntry=" + _bootEntryGuid);
            }
        }

        private void DisableProtection(RestoreConfig config)
        {
            lock (_sync)
            {
                EnsureEngine(config);

                try { _engine.Unmount(); } catch { }
                _engine.DeleteDiff();

                _bootManager.SetDefaultToCurrent();
                _bootEntryGuid = null;
                config.BootEntryGuid = string.Empty;
                ConfigManager.Save(config);

                ServiceLogger.Info("已停用保護模式。");
            }
        }

        private string ResetSystem(RestoreConfig config)
        {
            lock (_sync)
            {
                EnsureEngine(config);
                _engine.DeleteDiff();
                if (config.Enabled)
                {
                    _engine.CreateDiff();
                    _engine.Mount();
                }

                ServiceLogger.Info("已執行系統重置。Enabled=" + config.Enabled);
                return "OK:RESET";
            }
        }

        private void RunPipeServer(CancellationToken token)
        {
            ServiceLogger.Info("Named Pipe 伺服器啟動。\\\\.\\pipe\\" + PipeConstants.PipeName);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using (var pipe = new NamedPipeServerStream(PipeConstants.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        var waitTask = pipe.WaitForConnectionAsync(token);
                        waitTask.Wait(token);

                        using (var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, true))
                        using (var writer = new StreamWriter(pipe, Encoding.UTF8, 1024, true))
                        {
                            writer.AutoFlush = true;
                            string command = (reader.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();
                            string response = HandleCommand(command);
                            writer.WriteLine(response);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ServiceLogger.Error("Pipe 處理失敗。", ex);
                    Thread.Sleep(300);
                }
            }
            ServiceLogger.Info("Named Pipe 伺服器停止。");
        }

        private string HandleCommand(string command)
        {
            try
            {
                var config = ConfigManager.Load();
                switch (command)
                {
                    case PipeConstants.EnableProtection:
                        config.Enabled = true;
                        ConfigManager.Save(config);
                        EnableProtection(config);
                        return "OK:ENABLED";

                    case PipeConstants.DisableProtection:
                        config.Enabled = false;
                        ConfigManager.Save(config);
                        DisableProtection(config);
                        return "OK:DISABLED";

                    case PipeConstants.Status:
                        return config.Enabled ? "STATUS:ENABLED" : "STATUS:DISABLED";

                    case PipeConstants.ResetSystem:
                        return ResetSystem(config);

                    default:
                        return "ERROR:UNKNOWN_COMMAND";
                }
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("命令執行失敗：" + command, ex);
                return "ERROR:" + ex.Message;
            }
        }
    }
}
