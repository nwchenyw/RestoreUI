using System.Management;

namespace RestoreSystem.Core;

public static class VirtualMachineDetector
{
    public static bool IsRunningInVirtualMachine()
    {
        try
        {
            // 方法 1: 檢查 WMI 硬體資訊
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var manufacturer = obj["Manufacturer"]?.ToString()?.ToLowerInvariant() ?? string.Empty;
                var model = obj["Model"]?.ToString()?.ToLowerInvariant() ?? string.Empty;

                if (manufacturer.Contains("vmware") || model.Contains("vmware") ||
                    manufacturer.Contains("microsoft corporation") && model.Contains("virtual") ||
                    manufacturer.Contains("xen") || model.Contains("virtualbox") ||
                    manufacturer.Contains("qemu") || manufacturer.Contains("kvm"))
                {
                    return true;
                }
            }

            // 方法 2: 檢查 BIOS 資訊
            using var biosSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
            foreach (ManagementObject bios in biosSearcher.Get())
            {
                var biosVersion = bios["Version"]?.ToString()?.ToLowerInvariant() ?? string.Empty;
                var smbiosVersion = bios["SMBIOSBIOSVersion"]?.ToString()?.ToLowerInvariant() ?? string.Empty;

                if (biosVersion.Contains("vbox") || biosVersion.Contains("vmware") ||
                    biosVersion.Contains("hyper-v") || biosVersion.Contains("virtual") ||
                    smbiosVersion.Contains("vbox") || smbiosVersion.Contains("vmware"))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            // 如果偵測失敗，保守起見假設不是 VM
            return false;
        }
    }

    public static string GetVirtualMachineType()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var manufacturer = obj["Manufacturer"]?.ToString()?.ToLowerInvariant() ?? string.Empty;
                var model = obj["Model"]?.ToString()?.ToLowerInvariant() ?? string.Empty;

                if (manufacturer.Contains("vmware") || model.Contains("vmware"))
                    return "VMware";
                if (manufacturer.Contains("microsoft corporation") && model.Contains("virtual"))
                    return "Hyper-V";
                if (manufacturer.Contains("xen"))
                    return "Xen";
                if (model.Contains("virtualbox"))
                    return "VirtualBox";
                if (manufacturer.Contains("qemu") || manufacturer.Contains("kvm"))
                    return "QEMU/KVM";
            }

            return "Unknown";
        }
        catch
        {
            return "Detection Failed";
        }
    }
}
