using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.ServiceProcess;
using Microsoft.Win32;

namespace Win10BloatRemover.Utils
{
    static class SystemUtils
    {
        public const int EXIT_CODE_SUCCESS = 0;

        public static void StartService(string name)
        {
            using var serviceController = new ServiceController(name);
            if (serviceController.Status == ServiceControllerStatus.Stopped)
                serviceController.Start();
        }

        public static void StopServiceAndItsDependents(string name)
        {
            using var serviceController = new ServiceController(name);
            if (serviceController.Status == ServiceControllerStatus.Running)
            {
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
            }
        }

        public static void ExecuteWindowsPromptCommand(string command)
        {
            Debug.WriteLine($"Command executed: {command}");
            RunProcessSynchronouslyWithConsoleOutput("cmd.exe", $"/c \"{command}\"");
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

        public static int RunProcessSynchronously(string name, string args)
        {
            using var process = CreateProcessInstance(name, args);
            process.Start();
            process.WaitForExit();
            return process.ExitCode;
        }

        public static int RunProcessSynchronouslyWithConsoleOutput(string name, string args)
        {
            using var process = CreateProcessInstance(name, args);
            process.OutputDataReceived += (_, evt) => {
                if (!string.IsNullOrEmpty(evt.Data))
                    Console.WriteLine(evt.Data);
            };
            process.ErrorDataReceived += (_, evt) => {
                if (!string.IsNullOrEmpty(evt.Data))
                    ConsoleUtils.WriteLine(evt.Data, ConsoleColor.Red);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            return process.ExitCode;
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

        public static void TryDeleteDirectoryIfExists(string path)
        {
            try
            {
                DeleteDirectoryIfExists(path);
            }
            catch (Exception exc)
            {
                ConsoleUtils.WriteLine($"An error occurred when deleting folder {path}: {exc.Message}", ConsoleColor.Red);
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

        public static bool IsWindowsReleaseId(string expectedId)
        {
            string? releaseId = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                                                  "ReleaseId", "").ToString();
            return releaseId == expectedId;
        }
    }
}