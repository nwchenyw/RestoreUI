using System;
using System.IO;

namespace Restore.Engine
{
    public class VmRestoreManager
    {
        private readonly string _restoreFolder;
        private readonly string _targetPath;
        private readonly string _baselinePath;

        public VmRestoreManager(string restoreFolder, string vmRestorePath)
        {
            _restoreFolder = restoreFolder;
            _targetPath = string.IsNullOrWhiteSpace(vmRestorePath)
                ? Path.Combine(restoreFolder, "VMProtected")
                : vmRestorePath;
            _baselinePath = Path.Combine(restoreFolder, "VMBaseline");
        }

        public string TargetPath
        {
            get { return _targetPath; }
        }

        public void EnsurePaths()
        {
            if (!Directory.Exists(_restoreFolder))
                Directory.CreateDirectory(_restoreFolder);

            if (!Directory.Exists(_targetPath))
                Directory.CreateDirectory(_targetPath);

            if (!Directory.Exists(_baselinePath))
                Directory.CreateDirectory(_baselinePath);
        }

        public void CaptureBaselineIfMissing()
        {
            EnsurePaths();
            if (DirectoryHasAnyItem(_baselinePath))
                return;

            MirrorDirectory(_targetPath, _baselinePath);
        }

        public void RestoreBaseline()
        {
            EnsurePaths();
            if (!DirectoryHasAnyItem(_baselinePath))
                CaptureBaselineIfMissing();

            ClearDirectory(_targetPath);
            MirrorDirectory(_baselinePath, _targetPath);
        }

        private static bool DirectoryHasAnyItem(string path)
        {
            return Directory.Exists(path) &&
                   (Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length > 0 ||
                    Directory.GetDirectories(path, "*", SearchOption.AllDirectories).Length > 0);
        }

        private static void ClearDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }

            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);

            foreach (var dir in Directory.GetDirectories(path))
                Directory.Delete(dir, true);

            foreach (var file in Directory.GetFiles(path))
                File.Delete(file);
        }

        private static void MirrorDirectory(string source, string destination)
        {
            if (!Directory.Exists(source))
                Directory.CreateDirectory(source);

            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);

            foreach (var dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                var targetDir = dirPath.Replace(source, destination);
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);
            }

            foreach (var srcFile in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var destFile = srcFile.Replace(source, destination);
                var parent = Path.GetDirectoryName(destFile);
                if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
                    Directory.CreateDirectory(parent);

                File.Copy(srcFile, destFile, true);
            }
        }
    }
}
