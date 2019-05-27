using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;

namespace Win10BloatRemover.Utils
{
    static class SystemUtils
    {
        // This method locks the thread until the process stops writing on those streams (usually until its end)
        public static void PrintSynchronouslyOutputAndErrors(this Process process)
        {
            Console.Write(process.StandardOutput.ReadToEnd());
            ConsoleUtils.Write(process.StandardError.ReadToEnd(), ConsoleColor.Red);
        }

        public static Process RunProcess(string name, string args)
        {
            var process = CreateProcessInstance(name, args);
            process.Start();
            return process;
        }

        public static Process RunProcessWithAsyncOutputPrinting(string name, string args)
        {
            var process = CreateProcessInstance(name, args);

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
            return process;
        }

        private static Process CreateProcessInstance(string name, string args)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = name,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
        }

        /**
         *  Deletes the folder passed recursively
         *  Can optionally handle exceptions inside its body to avoid propagating of errors
         */
        public static void DeleteDirectoryIfExists(string path, bool handleErrors = false)
        {
            if (handleErrors)
            {
                try
                {
                    if (Directory.Exists(path))
                        Directory.Delete(path, true);
                }
                catch (Exception exc)
                {
                    ConsoleUtils.WriteLine($"An error occurred when deleting folder {path}: {exc.Message}", ConsoleColor.Red);
                }
            }
            else
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
        }

        public static bool IsWindowsReleaseId(string expectedId)
        {
            string releaseId = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                                                  "ReleaseId", "").ToString();
            return releaseId == expectedId;
        }

        public static bool HasAdministratorRights()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}