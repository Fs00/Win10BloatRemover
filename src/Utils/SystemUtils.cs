using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.ServiceProcess;
using Microsoft.Win32;
using Win10BloatRemover.Operations;
using Env = System.Environment;

namespace Win10BloatRemover.Utils
{
    static class SystemUtils
    {
        public static void StopServiceAndItsDependents(string name)
        {
            using var service = new ServiceController(name);
            if (service.Status != ServiceControllerStatus.Stopped)
            {
                if (service.Status != ServiceControllerStatus.StopPending)
                    service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
            }
        }

        public static void RebootSystem()
        {
            RunProcessBlocking("shutdown", "/r /t 3");
        }

        public static string GetProgramFilesFolder()
        {
            // See docs.microsoft.com/en-us/windows/win32/winprog64/wow64-implementation-details#environment-variables
            if (Env.Is64BitOperatingSystem)
                return Env.GetEnvironmentVariable("ProgramW6432")!;
            else
                return Env.GetFolderPath(Env.SpecialFolder.ProgramFiles);
        }

        public static void ExecuteWindowsPromptCommand(string command, IMessagePrinter printer)
        {
            Debug.WriteLine($"Command executed: {command}");
            RunProcessBlockingWithOutput("cmd.exe", $@"/c ""{command}""", printer);
        }

        public static void KillProcess(string processName)
        {
            foreach (var processToKill in Process.GetProcessesByName(processName))
            {
                processToKill.Kill();
                processToKill.WaitForExit();
            }
        }

        public static void KillChildProcesses(this Process process)
        {
            var searcher = new ManagementObjectSearcher(
                $"Select * From Win32_Process Where ParentProcessID={process.Id}"
            );
            foreach (var managementObject in searcher.Get())
            {
                using var child = Process.GetProcessById(Convert.ToInt32(managementObject["ProcessID"]));
                child.KillChildProcesses();
                child.Kill();
            }
        }

        public static ExitCode RunProcessBlocking(string name, string args)
        {
            using var process = CreateProcessInstance(name, args);
            process.Start();
            process.WaitForExit();
            return new ExitCode(process.ExitCode);
        }

        public static ExitCode RunProcessBlockingWithOutput(string name, string args, IMessagePrinter printer)
        {
            using var process = CreateProcessInstance(name, args);
            process.OutputDataReceived += (_, evt) => {
                if (!string.IsNullOrEmpty(evt.Data))
                    printer.PrintMessage(evt.Data);
            };
            process.ErrorDataReceived += (_, evt) => {
                if (!string.IsNullOrEmpty(evt.Data))
                    printer.PrintError(evt.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            return new ExitCode(process.ExitCode);
        }

        private static Process CreateProcessInstance(string name, string args)
        {
            return new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = name,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
        }

        public static void TryDeleteDirectoryIfExists(string path, IMessagePrinter printer)
        {
            try
            {
                DeleteDirectoryIfExists(path);
            }
            catch (Exception exc)
            {
                printer.PrintError($"An error occurred when deleting folder {path}: {exc.Message}");
            }
        }

        private static void DeleteDirectoryIfExists(string path)
        {
            var directoryToDelete = new DirectoryInfo(path);
            if (directoryToDelete.Exists)
            {
                // Reset attributes of the folder (avoid errors if a folder is marked as read-only)
                directoryToDelete.Attributes = FileAttributes.Directory;
                directoryToDelete.Delete(recursive: true);
            }
        }

        public static string? RetrieveWindowsReleaseId() =>
            Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "")?.ToString();
    }
}