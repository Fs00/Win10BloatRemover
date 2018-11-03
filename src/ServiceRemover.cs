using System;
using System.IO;
using Microsoft.Win32;

namespace Win10BloatRemover
{
    class ServiceRemover
    {
        private string[] servicesToRemove;
        private bool backupPerformed,
                     removalPerformed;

        public ServiceRemover(string[] servicesToRemove)
        {
            this.servicesToRemove = servicesToRemove;
        }

        public void PerformBackup()
        {
            if (backupPerformed)
                throw new InvalidOperationException("Backup already done!");

            DirectoryInfo backupDirectory = Directory.CreateDirectory($"./servicesBackup_{DateTime.Now.ToString("yyyy-MM-dd_hh-mm")}");
            foreach (string service in servicesToRemove)
            {
                SystemUtils.ExecuteWindowsCommand($"for /f \"tokens=1\" %I in ('reg query \"HKLM\\SYSTEM\\CurrentControlSet\\Services\" /k /f \"{service}\" ^| find /i \"{service}\"') " +
                                                  $"do reg export %I {backupDirectory.FullName}\\%~nxI.reg");
            }

            backupPerformed = true;
        }

        private void PerformExtraTasks()
        {
            // TODO
        }
    }
}
