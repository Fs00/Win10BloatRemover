using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    // ServiceRemover can remove services using sc or reg commands (see below)
    public enum ServiceRemovalMode
    {
        ServiceControl,
        Registry
    }

    /*
     *  Performs backup (export of registry keys) and removal of those services whose name starts with the service names
     *  passed into the constructor.
     *  This is made in order to include services that end with a random code.
     */
    class ServiceRemover : IOperation
    {
        private readonly string[] servicesToRemove;
        private readonly DirectoryInfo backupDirectory;
        private readonly IUserInterface ui;

        private const int SC_EXIT_CODE_MARKED_FOR_DELETION = 1072;

        public static void BackupAndRemove(string[] servicesToRemove,
                                           IUserInterface ui,
                                           ServiceRemovalMode removalMode = ServiceRemovalMode.ServiceControl)
        {
            var serviceRemover = new ServiceRemover(servicesToRemove, ui);
            string[] actualBackuppedServices = serviceRemover.PerformBackup();
            serviceRemover.PerformRemoval(actualBackuppedServices, removalMode);
        }

        public ServiceRemover(string[] servicesToRemove, IUserInterface ui)
        {
            this.servicesToRemove = servicesToRemove;
            this.ui = ui;
            backupDirectory = new DirectoryInfo($"servicesBackup_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}");
        }

        void IOperation.Run()
        {
            ui.PrintHeading("Backing up services...");
            string[] actualBackuppedServices = PerformBackup();
            ui.PrintHeading("Removing services...");
            PerformRemoval(actualBackuppedServices, ServiceRemovalMode.ServiceControl);
        }

        public string[] PerformBackup()
        {
            string[] existingServices = FindExistingServicesWithNames(servicesToRemove);
            foreach (string service in existingServices)
                BackupService(service);
            return existingServices;
        }

        private string[] FindExistingServicesWithNames(string[] servicesNames)
        {
            string[] allExistingServices = GetAllServicesNames();
            List<string> allMatchingServices = new List<string>();
            foreach (string serviceName in servicesNames)
            {
                var matchingServices = allExistingServices.Where(name => name.StartsWith(serviceName)).ToArray();
                if (matchingServices.Length == 0)
                    ui.PrintMessage($"No services found with name {serviceName}.");
                else
                    allMatchingServices.AddRange(matchingServices);
            }

            return allMatchingServices.ToArray();
        }

        private string[] GetAllServicesNames()
        {
            using RegistryKey servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            return servicesKey.GetSubKeyNames();
        }

        private void BackupService(string service)
        {
            EnsureBackupDirectoryExists();
            int regExportExitCode = SystemUtils.RunProcessBlocking(
                "reg", $@"export HKLM\SYSTEM\CurrentControlSet\Services\{service} {backupDirectory.FullName}\{service}.reg"
            );
            if (regExportExitCode == 0)
                ui.PrintMessage($"Service {service} backed up.");
            else
                throw new Exception($"Could not backup service {service}.");
        }

        private void EnsureBackupDirectoryExists()
        {
            if (!backupDirectory.Exists)
                backupDirectory.Create();
        }

        /*
         *  Performs the removal of the services by using either sc or reg command, according to the passed parameter
         *  (default is sc).
         *  reg command allows to remove unstoppable system services, like Windows Defender ones.
         */
        private void PerformRemoval(string[] backuppedServices, ServiceRemovalMode removalMode)
        {
            if (removalMode == ServiceRemovalMode.ServiceControl)
                RemoveServicesUsingSC(backuppedServices);
            else if (removalMode == ServiceRemovalMode.Registry)
                RemoveServicesByDeletingRegistryKeys(backuppedServices);
        }

        private void RemoveServicesUsingSC(string[] actualServicesToRemove)
        {
            foreach (string service in actualServicesToRemove)
            {
                int scExitCode = SystemUtils.RunProcessBlocking("sc", $"delete {service}");
                switch (scExitCode)
                {
                    case SystemUtils.EXIT_CODE_SUCCESS:
                        ui.PrintMessage($"Service {service} removed successfully.");
                        break;
                    case SC_EXIT_CODE_MARKED_FOR_DELETION:
                        ui.PrintMessage($"Service {service} will be removed after reboot.");
                        break;
                    default:
                        ui.PrintError($"Service {service} removal failed: sc exited with code {scExitCode}.");
                        break;
                }
            }
        }

        private void RemoveServicesByDeletingRegistryKeys(string[] actualServicesToRemove)
        {
            foreach (string service in actualServicesToRemove)
            {
                int regExitCode = SystemUtils.RunProcessBlocking(
                    "reg", $@"delete HKLM\SYSTEM\CurrentControlSet\Services\{service} /f"
                );
                if (regExitCode == SystemUtils.EXIT_CODE_SUCCESS)
                    ui.PrintMessage($"Service {service} removed, but it may continue to run until the next restart.");
                else
                    ui.PrintError($"Service {service} removal failed: couldn't delete its registry keys.");
            }
        }
    }
}
