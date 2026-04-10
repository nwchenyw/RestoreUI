namespace RestoreSystem.Shared;

public static class PipeConstants
{
    public const string PipeName = "RestoreSystemPipe";

    public const string EnableProtection = "ENABLE_PROTECTION";
    public const string DisableProtection = "DISABLE_PROTECTION";
    public const string Status = "STATUS";
    public const string ResetSystem = "RESET_SYSTEM";
    public const string CreateSnapshot = "CREATE_SNAPSHOT";
    public const string ListSnapshots = "LIST_SNAPSHOTS";
    public const string RestoreSnapshot = "RESTORE_SNAPSHOT";
    public const string DeleteSnapshot = "DELETE_SNAPSHOT";

    public static string ToPipeCommand(RestoreCommand command)
    {
        return command switch
        {
            RestoreCommand.Enable => EnableProtection,
            RestoreCommand.Disable => DisableProtection,
            RestoreCommand.Status => Status,
            RestoreCommand.Reset => ResetSystem,
            RestoreCommand.CreateSnapshot => CreateSnapshot,
            RestoreCommand.ListSnapshots => ListSnapshots,
            RestoreCommand.RestoreSnapshot => RestoreSnapshot,
            RestoreCommand.DeleteSnapshot => DeleteSnapshot,
            _ => Status
        };
    }
}
