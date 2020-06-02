using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    /*
     *  Performs backup (export of registry keys) and removal of those services whose name starts with the service names
     *  passed into the constructor.
     *  This is made in order to include services that end with a random code.
     */
    public class ServiceRemover : IOperation
    {
        private readonly string[] servicesToRemove;
        private readonly DirectoryInfo backupDirectory;
        private readonly IUserInterface ui;

        private const int SC_EXIT_CODE_MARKED_FOR_DELETION = 1072;

        public static void BackupAndRemove(string[] servicesToRemove, IUserInterface ui)
        {
            var serviceRemover = new ServiceRemover(servicesToRemove, ui);
            string[] actualBackuppedServices = serviceRemover.PerformBackup();
            serviceRemover.PerformRemoval(actualBackuppedServices);
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

            if (actualBackuppedServices.Length > 0)
            {
                ui.PrintHeading("Removing services...");
                PerformRemoval(actualBackuppedServices);
            }
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

        private void PerformRemoval(string[] backuppedServices)
        {
            foreach (string service in backuppedServices)
                RemoveService(service);
        }

        private void RemoveService(string service)
        {
            int scExitCode = SystemUtils.RunProcessBlocking("sc", $"delete {service}");
            if (IsScExitCodeSuccessful(scExitCode))
                PrintSuccessMessage(scExitCode, service);
            else
            {
                Debug.WriteLine($"SC removal failed with exit code {scExitCode} for service {service}.");
                DeleteServiceRegistryKey(service);
            }
        }

        private bool IsScExitCodeSuccessful(int exitCode)
        {
            return exitCode == SystemUtils.EXIT_CODE_SUCCESS ||
                   exitCode == SC_EXIT_CODE_MARKED_FOR_DELETION;
        }

        private void PrintSuccessMessage(int scExitCode, string service)
        {
            if (scExitCode == SystemUtils.EXIT_CODE_SUCCESS)
                ui.PrintMessage($"Service {service} removed successfully.");
            else if (scExitCode == SC_EXIT_CODE_MARKED_FOR_DELETION)
                ui.PrintMessage($"Service {service} will be removed after reboot.");
            else
                Debug.Fail($"There must be an error in {nameof(IsScExitCodeSuccessful)}: exit code {scExitCode}.");
        }

        // reg command with /f option allows to remove unstoppable system services, like Windows Defender ones
        private void DeleteServiceRegistryKey(string service)
        {
            int regExitCode = SystemUtils.RunProcessBlocking("reg", $@"delete HKLM\SYSTEM\CurrentControlSet\Services\{service} /f");
            if (regExitCode == SystemUtils.EXIT_CODE_SUCCESS)
                ui.PrintMessage($"Service {service} removed, but it will continue to run until the next restart.");
            else
                ui.PrintError($"Service {service} removal failed: couldn't delete its registry keys.");
        }
    }
}
