namespace Restore.Core
{
    public class RestoreConfig
    {
        public bool Enabled { get; set; }
        public string PasswordHash { get; set; }
        public string ProtectDrive { get; set; }
        public string DataDrive { get; set; }
        public string BootEntryGuid { get; set; }

        public RestoreConfig()
        {
            Enabled = false;
            PasswordHash = "";
            ProtectDrive = "C";
            DataDrive = "D";
            BootEntryGuid = "";
        }
    }
}
