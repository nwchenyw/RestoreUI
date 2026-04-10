namespace Restore.Core
{
    public class RestoreConfig
    {
        public bool Enabled { get; set; }
        public bool VmSafeMode { get; set; }
        public bool AutoDetectVm { get; set; }
        public string PasswordHash { get; set; }
        public string ProtectDrive { get; set; }
        public string DataDrive { get; set; }
        public string VmRestorePath { get; set; }
        public string BootEntryGuid { get; set; }

        public RestoreConfig()
        {
            Enabled = false;
            VmSafeMode = false;
            AutoDetectVm = true;
            PasswordHash = "";
            ProtectDrive = "C";
            DataDrive = "D";
            VmRestorePath = @"C:\RestoreSystem\VMProtected";
            BootEntryGuid = "";
        }
    }
}
