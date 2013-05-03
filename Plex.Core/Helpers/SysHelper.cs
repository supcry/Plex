using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Plex.Helpers
{
    public class SysHelper
    {
        public static long GetFreeSpace(string dir)
        {
            return DriveInfo.GetDrives()
                .First(p => p.RootDirectory.ToString().ToLower()[0] == dir.ToLower()[0]).AvailableFreeSpace;
        }

        private static bool _isLibPathChecked = false;

        /// <summary>
        /// Добавляет в %PATH% путь к нативным библиотекам в x86/ || x64/
        /// </summary>
        public static void CheckArchitectureLibPath()
        {
            if (_isLibPathChecked)
                return;

            var arch = IntPtr.Size == 8 ? "x64" : "x86";
            var curDir = Directory.GetCurrentDirectory();
            var arcDir = Path.Combine(curDir, arch);
            if (Directory.Exists(arcDir))
            {
                var pathArch = String.Format("{0};{1}", arcDir, Environment.GetEnvironmentVariable("PATH"));
                Environment.SetEnvironmentVariable("PATH", pathArch, EnvironmentVariableTarget.Process);
            }
            _isLibPathChecked = true;
        }

    }
}
