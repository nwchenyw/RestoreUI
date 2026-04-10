using System.IO.Pipes;
using System.Text;
using RestoreSystem.Core;
using RestoreSystem.Shared;

namespace RestoreSystem.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly VhdManager _vhdManager = new();
    private readonly BootManager _bootManager = new();
    private readonly SnapshotManager _snapshotManager;
    private readonly DeferredDeletionQueueManager _deferredDeletionQueue = new();
    private readonly SemaphoreSlim _sync = new(1, 1);

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        _snapshotManager = new SnapshotManager(_vhdManager);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        OperationLogger.Info("RestoreSystem Service 啟動。");

        try
        {
            _vhdManager.EnsureBitLockerCompatibleVolume();
            _deferredDeletionQueue.ProcessQueue();
            OperationLogger.Info("已處理延遲刪除佇列。\n");

            var config = ConfigManager.Load();
            if (config.Enabled)
            {
                await EnableProtectionAsync(config, cancellationToken);
            }
            else
            {
                OperationLogger.Info("保護模式為停用狀態。\n");
            }
        }
        catch (Exception ex)
        {
            OperationLogger.Error("服務啟動流程失敗", ex);
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        OperationLogger.Info($"Named Pipe 啟動：\\\\.\\pipe\\{PipeConstants.PipeName}");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(PipeConstants.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await pipe.WaitForConnectionAsync(stoppingToken);

                using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
                using var writer = new StreamWriter(pipe, Encoding.UTF8, 1024, leaveOpen: true) { AutoFlush = true };

                var command = (await reader.ReadLineAsync())?.Trim() ?? string.Empty;
                var response = await HandleCommandAsync(command, stoppingToken);
                await writer.WriteLineAsync(response);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                OperationLogger.Error("Pipe 處理發生錯誤", ex);
                await Task.Delay(300, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            var config = ConfigManager.Load();
            if (config.Enabled)
            {
                _vhdManager.Unmount();
                OperationLogger.Info("Service 停止時已卸載 diff.vhdx。");
            }
        }
        catch (Exception ex)
        {
            OperationLogger.Error("服務停止流程失敗", ex);
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task<string> HandleCommandAsync(string command, CancellationToken ct)
    {
        await _sync.WaitAsync(ct);
        try
        {
            var config = ConfigManager.Load();
            if (!TryAuthorize(command, config, out var actualCommand))
            {
                OperationLogger.Info("拒絕未授權命令。\n");
                return "ERROR:UNAUTHORIZED";
            }

            OperationLogger.Info("授權命令：" + actualCommand);

            switch (actualCommand)
            {
                case nameof(PipeConstants.EnableProtection):
                case PipeConstants.EnableProtection:
                    config.Enabled = true;
                    ConfigManager.Save(config);
                    await EnableProtectionAsync(config, ct);
                    return "OK:ENABLED";

                case nameof(PipeConstants.DisableProtection):
                case PipeConstants.DisableProtection:
                    config.Enabled = false;
                    ConfigManager.Save(config);
                    DisableProtection(config);
                    return "OK:DISABLED";

                case nameof(PipeConstants.Status):
                case PipeConstants.Status:
                    return BuildStatus(config);

                case nameof(PipeConstants.ResetSystem):
                case PipeConstants.ResetSystem:
                    ResetSystem(config);
                    return "OK:RESET";

                case nameof(PipeConstants.CreateSnapshot):
                case PipeConstants.CreateSnapshot:
                    {
                        var created = _snapshotManager.CreateSnapshot();
                        config.CurrentSnapshotId = created.Id;
                        ConfigManager.Save(config);
                        OperationLogger.Info("已建立快照：" + created.Id);
                        return "OK:SNAPSHOT_CREATED:" + created.Id;
                    }

                case nameof(PipeConstants.ListSnapshots):
                case PipeConstants.ListSnapshots:
                    {
                        var list = _snapshotManager.ListSnapshots();
                        var payload = string.Join(";", list.Select(x => $"{x.Id}|{x.CreatedAt:yyyy-MM-dd HH:mm:ss}"));
                        return "OK:SNAPSHOT_LIST:" + payload;
                    }

                case "SET_BOOT_NORMAL":
                    _bootManager.SetDefaultTo(config.NormalBootEntryGuid);
                    return "OK:BOOT_NORMAL";

                case "SET_BOOT_RESTORE":
                    _bootManager.SetDefaultTo(config.BootEntryGuid);
                    return "OK:BOOT_RESTORE";

                case var x when x.StartsWith("SET_BOOT_TIMEOUT|", StringComparison.OrdinalIgnoreCase):
                    {
                        var raw = x[("SET_BOOT_TIMEOUT|".Length)..].Trim();
                        var timeout = int.TryParse(raw, out var t) ? t : config.BootTimeoutSeconds;
                        config.BootTimeoutSeconds = timeout;
                        _bootManager.SetBootTimeout(timeout);
                        ConfigManager.Save(config);
                        return "OK:BOOT_TIMEOUT_SET:" + timeout;
                    }

                default:
                    if (actualCommand.StartsWith(PipeConstants.RestoreSnapshot + "|", StringComparison.OrdinalIgnoreCase))
                    {
                        var id = actualCommand[(PipeConstants.RestoreSnapshot.Length + 1)..].Trim();
                        var restored = _snapshotManager.RestoreSnapshot(id);
                        config.CurrentSnapshotId = restored.Id;
                        ConfigManager.Save(config);
                        OperationLogger.Info("已還原快照：" + restored.Id);
                        return "OK:SNAPSHOT_RESTORED:" + restored.Id;
                    }

                    if (actualCommand.StartsWith(PipeConstants.DeleteSnapshot + "|", StringComparison.OrdinalIgnoreCase))
                    {
                        var id = actualCommand[(PipeConstants.DeleteSnapshot.Length + 1)..].Trim();
                        _snapshotManager.DeleteSnapshot(id, _deferredDeletionQueue);
                        if (string.Equals(config.CurrentSnapshotId, id, StringComparison.OrdinalIgnoreCase))
                        {
                            config.CurrentSnapshotId = string.Empty;
                            ConfigManager.Save(config);
                        }

                        OperationLogger.Info("已刪除快照：" + id);
                        return "OK:SNAPSHOT_DELETED:" + id;
                    }

                    return "ERROR:UNKNOWN_COMMAND";
            }
        }
        catch (Exception ex)
        {
            OperationLogger.Error("命令執行失敗：" + command, ex);
            return "ERROR:" + ex.Message;
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task EnableProtectionAsync(RestoreConfig config, CancellationToken ct)
    {
        if (!_vhdManager.BaseExists())
        {
            config.Enabled = false;
            ConfigManager.Save(config);
            OperationLogger.Error("base.vhdx 不存在，已自動停用保護以確保可復原。\n");
            return;
        }

        if (!_vhdManager.Exists())
            _vhdManager.CreateDiff();

        _vhdManager.Mount();

        config.NormalBootEntryGuid = _bootManager.EnsureNormalModeEntry(config.NormalBootEntryGuid);
        config.BootEntryGuid = _bootManager.EnsureRestoreModeEntry(config.BootEntryGuid);
        _bootManager.SetDefaultTo(config.BootEntryGuid);
        _bootManager.SetBootTimeout(config.BootTimeoutSeconds);
        ConfigManager.Save(config);

        OperationLogger.Info("保護模式已啟用。\n");
        await Task.CompletedTask;
    }

    private void DisableProtection(RestoreConfig config)
    {
        try { _vhdManager.Unmount(); } catch { }
        if (string.IsNullOrWhiteSpace(config.NormalBootEntryGuid))
            _bootManager.SetDefaultCurrent();
        else
            _bootManager.SetDefaultTo(config.NormalBootEntryGuid);
        _bootManager.SetBootTimeout(config.BootTimeoutSeconds);
        OperationLogger.Info("保護模式已停用。\n");
    }

    private void ResetSystem(RestoreConfig config)
    {
        try { _vhdManager.Unmount(); } catch { }
        if (!_vhdManager.Exists())
            _vhdManager.CreateDiff();

        if (config.Enabled)
        {
            _vhdManager.Mount();
        }

        OperationLogger.Info("已執行系統重置。\n");
    }

    private string BuildStatus(RestoreConfig config)
    {
        var baseState = _vhdManager.BaseExists() ? "BASE_OK" : "BASE_MISSING";
        var serviceState = config.Enabled ? "ENABLED" : "DISABLED";
        var snapshot = string.IsNullOrWhiteSpace(config.CurrentSnapshotId) ? "NONE" : config.CurrentSnapshotId;
        return $"STATUS:{serviceState}:{baseState}:{snapshot}";
    }

    private bool TryAuthorize(string payload, RestoreConfig config, out string command)
    {
        command = string.Empty;

        if (!payload.StartsWith("AUTH:", StringComparison.OrdinalIgnoreCase))
            return false;

        var sep = payload.IndexOf('|');
        if (sep <= 5)
            return false;

        var token = payload.Substring(5, sep - 5).Trim();
        command = payload[(sep + 1)..].Trim().ToUpperInvariant();

        return AuthTokenManager.ValidateToken(token, config);
    }
}
