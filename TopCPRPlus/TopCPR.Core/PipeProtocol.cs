namespace TopCPR.Core;

public static class PipeProtocol
{
    public const string PipeName = "TopCPRPipe";

    public const string Enable = "ENABLE_RESTORE";
    public const string Disable = "DISABLE_RESTORE";
    public const string Status = "STATUS";
    public const string CreateSnapshot = "CREATE_SNAPSHOT";
    public const string ListSnapshots = "LIST_SNAPSHOTS";
    public const string RestoreSnapshot = "RESTORE_SNAPSHOT";
    public const string DeleteSnapshot = "DELETE_SNAPSHOT";
    public const string SetBootNormal = "SET_BOOT_NORMAL";
    public const string SetBootRestore = "SET_BOOT_RESTORE";
    public const string SetBootTimeout = "SET_BOOT_TIMEOUT";
    public const string Heartbeat = "HEARTBEAT";
}
