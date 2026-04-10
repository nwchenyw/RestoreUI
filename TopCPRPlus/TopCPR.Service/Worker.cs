using System.IO.Pipes;
using System.Text;
using TopCPR.Core;

namespace TopCPR.Service;

public class Worker : BackgroundService
{
    private readonly VHDXManager _vhdx = new();
    private readonly DeferredDeletionQueue _queue = new();
    private readonly BootModeManager _boot = new();
    private readonly SemaphoreSlim _sync = new(1, 1);

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        ConfigManager.EnsureFolders();
        _vhdx.EnsureBitLockerSafeMode();
        _vhdx.CreateBaseIfMissing();
        _queue.ProcessOnBoot();

        var cfg = ConfigManager.Load();
        if (cfg.RestoreModeEnabled)
        {
            _vhdx.CreateDiff();
            _vhdx.Attach();
        }

        AuditLogger.Info("Service started.");
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(PipeProtocol.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await pipe.WaitForConnectionAsync(stoppingToken);

                using var reader = new StreamReader(pipe, Encoding.UTF8, true, 1024, true);
                using var writer = new StreamWriter(pipe, Encoding.UTF8, 1024, true) { AutoFlush = true };

                var payload = (await reader.ReadLineAsync()) ?? string.Empty;
                var response = await HandleAsync(payload, stoppingToken);
                await writer.WriteLineAsync(response);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                AuditLogger.Error("Pipe error", ex);
                await Task.Delay(200, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _vhdx.Detach();
            _vhdx.OptimizeIfAvailable();
        }
        catch { }

        AuditLogger.Info("Service stopped.");
        await base.StopAsync(cancellationToken);
    }

    private async Task<string> HandleAsync(string payload, CancellationToken ct)
    {
        await _sync.WaitAsync(ct);
        try
        {
            var cfg = ConfigManager.Load();
            if (!TryAuth(payload, cfg, out var command))
                return "ERROR:UNAUTHORIZED";

            AuditLogger.Info("Authorized command: " + command);

            if (command == PipeProtocol.Heartbeat)
                return "OK:HEARTBEAT";

            if (command == PipeProtocol.Enable)
            {
                cfg.RestoreModeEnabled = true;
                cfg.RestoreBootGuid = _boot.EnsureRestoreMode(cfg.RestoreBootGuid);
                cfg.NormalBootGuid = _boot.EnsureNormalMode(cfg.NormalBootGuid);
                _boot.SetDefault(cfg.RestoreBootGuid);
                _boot.SetTimeout(cfg.BootTimeoutSeconds);
                ConfigManager.Save(cfg);

                _vhdx.CreateDiff();
                _vhdx.Attach();
                return "OK:ENABLED";
            }

            if (command == PipeProtocol.Disable)
            {
                cfg.RestoreModeEnabled = false;
                ConfigManager.Save(cfg);
                _vhdx.Detach();
                _boot.SetDefault(string.IsNullOrWhiteSpace(cfg.NormalBootGuid) ? "{current}" : cfg.NormalBootGuid);
                return "OK:DISABLED";
            }

            if (command == PipeProtocol.Status)
            {
                var s1 = cfg.RestoreModeEnabled ? "ON" : "OFF";
                var s2 = _vhdx.BaseExists() ? "BASE_OK" : "BASE_MISSING";
                return $"OK:STATUS:{s1}:{s2}:{cfg.CurrentSnapshotId}";
            }

            if (command == PipeProtocol.CreateSnapshot)
            {
                var sm = new SnapshotManager(_vhdx, _queue);
                var item = sm.CreateSnapshot();
                cfg.CurrentSnapshotId = item.Id;
                ConfigManager.Save(cfg);
                return "OK:SNAPSHOT_CREATED:" + item.Id;
            }

            if (command == PipeProtocol.ListSnapshots)
            {
                var sm = new SnapshotManager(_vhdx, _queue);
                var payloadList = string.Join(';', sm.ListSnapshots().Select(x => $"{x.Id}|{x.CreatedAt:yyyy-MM-dd HH:mm:ss}"));
                return "OK:SNAPSHOT_LIST:" + payloadList;
            }

            if (command.StartsWith(PipeProtocol.RestoreSnapshot + "|", StringComparison.OrdinalIgnoreCase))
            {
                var id = command[(PipeProtocol.RestoreSnapshot.Length + 1)..];
                var sm = new SnapshotManager(_vhdx, _queue);
                var restored = sm.RestoreSnapshot(id);
                cfg.CurrentSnapshotId = restored.Id;
                ConfigManager.Save(cfg);
                return "OK:SNAPSHOT_RESTORED:" + restored.Id;
            }

            if (command.StartsWith(PipeProtocol.DeleteSnapshot + "|", StringComparison.OrdinalIgnoreCase))
            {
                var id = command[(PipeProtocol.DeleteSnapshot.Length + 1)..];
                var sm = new SnapshotManager(_vhdx, _queue);
                sm.DeleteSnapshot(id);
                return "OK:SNAPSHOT_DELETED:" + id;
            }

            if (command == PipeProtocol.SetBootNormal)
            {
                _boot.SetDefault(cfg.NormalBootGuid);
                return "OK:BOOT_NORMAL";
            }

            if (command == PipeProtocol.SetBootRestore)
            {
                _boot.SetDefault(cfg.RestoreBootGuid);
                return "OK:BOOT_RESTORE";
            }

            if (command.StartsWith(PipeProtocol.SetBootTimeout + "|", StringComparison.OrdinalIgnoreCase))
            {
                var raw = command[(PipeProtocol.SetBootTimeout.Length + 1)..];
                var sec = int.TryParse(raw, out var parsed) ? parsed : cfg.BootTimeoutSeconds;
                cfg.BootTimeoutSeconds = sec;
                ConfigManager.Save(cfg);
                _boot.SetTimeout(sec);
                return "OK:BOOT_TIMEOUT:" + sec;
            }

            return "ERROR:UNKNOWN_COMMAND";
        }
        catch (Exception ex)
        {
            AuditLogger.Error("Command error", ex);
            return "ERROR:" + ex.Message;
        }
        finally
        {
            _sync.Release();
        }
    }

    private static bool TryAuth(string payload, AppConfig cfg, out string command)
    {
        command = string.Empty;
        if (!payload.StartsWith("AUTH:", StringComparison.OrdinalIgnoreCase)) return false;

        var idx = payload.IndexOf('|');
        if (idx <= 5) return false;

        var token = payload.Substring(5, idx - 5);
        command = payload[(idx + 1)..].Trim();

        return !string.IsNullOrWhiteSpace(cfg.AuthTokenHash) && string.Equals(token, cfg.AuthTokenHash, StringComparison.Ordinal);
    }
}
