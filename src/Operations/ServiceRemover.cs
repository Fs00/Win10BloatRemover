using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    // ServiceRemover can remove services using sc or reg commands (see below)
    public enum ServiceRemovalMode {
        ServiceControl,
        Registry
    }

    /**
     *  ServiceRemover
     *  Performs backup (export of registry keys) and removal of those services whose name starts with the service names
     *  passed into the constructor.
     *  This is made in order to include services that end with a random code.
     */
    class ServiceRemover : IOperation
    {
        private bool backupPerformed;
        private readonly string[] servicesToRemove;
        private readonly DirectoryInfo backupDirectory;

        public ServiceRemover(string[] servicesToRemove)
        {
            this.servicesToRemove = servicesToRemove;
            backupDirectory = new DirectoryInfo($"servicesBackup_{DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss")}");
        }

        public void PerformTask()
        {
            ConsoleUtils.WriteLine("Backing up services...", ConsoleColor.Green);
            PerformBackup();
            ConsoleUtils.WriteLine("Removing services...", ConsoleColor.Green);
            PerformRemoval();
        }

        public ServiceRemover PerformBackup()
        {
            if (backupPerformed)
                throw new InvalidOperationException("Backup already done!");

            string[] servicesNames = GetAllServicesNames();
            foreach (string serviceToRemove in servicesToRemove)
            {
                var actualServicesToRemove = servicesNames.Where(name => name.StartsWith(serviceToRemove));
                if (!actualServicesToRemove.Any())
                    Console.WriteLine($"No services found with name {serviceToRemove}.");
                else
                    BackupServices(actualServicesToRemove);
            }

            backupPerformed = true;
            return this;    // allows chaining with PerformRemoval
        }

        private void BackupServices(IEnumerable<string> actualServicesToBackup)
        {
            EnsureBackupDirectoryExists();
            foreach (string service in actualServicesToBackup)
            {
                int regExportExitCode = SystemUtils.RunProcessSynchronously(
                    "reg", $@"export HKLM\SYSTEM\CurrentControlSet\Services\{service} " +
                    $@"{backupDirectory.FullName}\{service}.reg"
                );

                if (regExportExitCode == 0)
                    Console.WriteLine($"Service {service} backed up.");
                else
                    throw new Exception($"Could not backup service {service}.");
            }
        }

        private void EnsureBackupDirectoryExists()
        {
            if (!backupDirectory.Exists)
                backupDirectory.Create();
        }

        /**
         *  Performs the removal of the services by using either sc or reg command, according to the passed parameter
         *  (default is sc).
         *  reg command allows to remove unstoppable system services, like Windows Defender ones.
         */
        public void PerformRemoval(ServiceRemovalMode removalMode = ServiceRemovalMode.ServiceControl)
        {
            if (!backupPerformed)
                throw new InvalidOperationException("Backup services before removing them!");

            string[] servicesNames = GetAllServicesNames();
            foreach (string serviceToRemove in servicesToRemove)
            {
                var actualServicesToRemove = servicesNames.Where(name => name.StartsWith(serviceToRemove));
                if (removalMode == ServiceRemovalMode.ServiceControl)
                    RemoveServicesUsingSC(actualServicesToRemove);
                else if (removalMode == ServiceRemovalMode.Registry)
                    RemoveServicesByDeletingRegistryKeys(actualServicesToRemove);
            }
        }

        private string[] GetAllServicesNames()
        {
            using (RegistryKey servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
                return servicesKey.GetSubKeyNames();
        }

        private void RemoveServicesUsingSC(IEnumerable<string> actualServicesToRemove)
        {
            foreach (string service in actualServicesToRemove)
            {
                int scExitCode = SystemUtils.RunProcessSynchronously("sc", $"delete {service}");
                switch (scExitCode)
                {
                    case 0:
                        Console.WriteLine($"Service {service} removed successfully.");
                        break;
                    case 1072:
                        Console.WriteLine($"Service {service} will be removed after reboot.");
                        break;
                    default:
                        ConsoleUtils.WriteLine($"Service {service} removal failed: exit code {scExitCode}.", ConsoleColor.Red);
                        break;
                }
            }
        }

        private void RemoveServicesByDeletingRegistryKeys(IEnumerable<string> actualServicesToRemove)
        {
            foreach (string service in actualServicesToRemove)
            {
                int regExitCode = SystemUtils.RunProcessSynchronously(
                    "reg", $@"delete HKLM\SYSTEM\CurrentControlSet\Services\{service} /f"
                );
                if (regExitCode == 0)
                    Console.WriteLine($"Service {service} removed, but it may continue to run until the next restart.");
                else
                    ConsoleUtils.WriteLine($"Service {service} removal failed: couldn't delete its registry keys.", ConsoleColor.Red);
            }
        }
    }
}
