using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using Win10BloatRemover.UI;

namespace Win10BloatRemover.Utils
{
    static class OS
    {
        public static int WindowsBuild => Environment.OSVersion.Version.Build;

        public static string? GetWindowsVersionName()
        {
            string currentVersionKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            // Starting from 20H2, the ReleaseId value always contains "2009". DisplayVersion contains the
            // updated version name (e.g. "21H2") instead, but may not be present on older Windows versions
            return (string?) Registry.GetValue(currentVersionKey, "DisplayVersion", defaultValue: null) ??
                   (string?) Registry.GetValue(currentVersionKey, "ReleaseId", defaultValue: null);
        }

        public static void RebootPC()
        {
            RunProcessBlocking(SystemExecutablePath("shutdown"), "/r /t 3");
        }

        public static string GetProgramFilesFolder()
        {
            // See docs.microsoft.com/en-us/windows/win32/winprog64/wow64-implementation-details#environment-variables
            if (Environment.Is64BitOperatingSystem)
                return Environment.GetEnvironmentVariable("ProgramW6432")!;
            else
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        }

        public static void ExecuteWindowsPromptCommand(string command, IMessagePrinter printer)
        {
            Debug.WriteLine($"Command executed: {command}");
            RunProcessBlockingWithOutput(SystemExecutablePath("cmd"), $@"/c ""{command}""", printer);
        }

        public static string SystemExecutablePath(string executableName)
        {
            // SpecialFolder.SystemX86 returns SysWOW64 folder on 64-bit systems
            string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            return $@"{systemFolder}\{executableName}.exe";
        }

        public static void CloseExplorer()
        {
            // This is the same as doing Ctrl+Shift+Right click on the system tray -> Exit Explorer
            // Solution found at https://stackoverflow.com/a/5705965
            IntPtr trayWindow = FindWindow("Shell_TrayWnd", null);
            if (trayWindow != IntPtr.Zero)
            {
                PostMessage(trayWindow, 0x5B4, IntPtr.Zero, IntPtr.Zero);
                Thread.Sleep(TimeSpan.FromSeconds(2)); // wait for the process to gracefully exit
            }
            // If the Explorer option "Launch folder windows in a separate process" is enabled, that separate process
            // remains active even after the shell is closed. We want to shut down that one too.
            KillProcess("explorer");
        }

        public static void StartExplorer()
        {
            // We're launching the explorer.exe found in %windir% since that one starts up the shell
            // if it's not running, whereas the ones in System32/SysWOW64 always open a new folder window
            string windowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            Process.Start($@"{windowsFolder}\explorer.exe");
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
            using var searcher = new ManagementObjectSearcher(
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

        public static bool IsWindows10()
        {
            var windowsVersion = Environment.OSVersion.Version;
            return windowsVersion.Major == 10 && windowsVersion.Build < 21996;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern IntPtr FindWindow(string? className, string? windowName);

        [DllImport("user32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern bool PostMessage(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam);
    }
}