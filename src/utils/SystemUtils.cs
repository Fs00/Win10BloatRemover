using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security.Principal;
using Microsoft.Win32;

namespace Win10BloatRemover
{
    static class SystemUtils
    {
        // These counters keep track of the number of messages written for every PowerShell stream
        // That's a workaround to avoid messages being rewritten due to DataAdded event raised more than once
        // for the same message (which is likely a bug in the API)
        private static int psInformationMessagesRead = 0,
                           psWarningMessagesRead = 0,
                           psErrorMessagesRead = 0;

        /**
        *  Runs a script on the given PowerShell instance and prints the messages written to info,
        *  error and warning streams asynchronously.
        */
        public static void RunScriptAndPrintOutput(this PowerShell psInstance, string script)
        {
            psInformationMessagesRead = 0;
            psWarningMessagesRead = 0;
            psErrorMessagesRead = 0;

            // Needed to make counters match the actual number of messages in the streams
            psInstance.Streams.ClearStreams();

            // Make sure that the Runspace uses the current thread to execute commands (avoids wild thread spawning)
            if (psInstance.Runspace.ThreadOptions != PSThreadOptions.UseCurrentThread)
            {
                psInstance.Runspace.Dispose();
                psInstance.Runspace = RunspaceFactory.CreateRunspace();
                psInstance.Runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;
                psInstance.Runspace.Open();
            }

            psInstance.AddScript(script);
            psInstance.Streams.Information.DataAdded += (s, evtArgs) => {
                if (evtArgs.Index >= psInformationMessagesRead)
                {
                    Console.WriteLine(psInstance.Streams.Information[evtArgs.Index].ToString());
                    psInformationMessagesRead++;
                }
            };
            psInstance.Streams.Error.DataAdded += (s, evtArgs) => {
                if (evtArgs.Index >= psErrorMessagesRead)
                {
                    ConsoleUtils.WriteLine(psInstance.Streams.Error[evtArgs.Index].ToString(), ConsoleColor.Red);
                    psErrorMessagesRead++;
                }
            };
            psInstance.Streams.Warning.DataAdded += (s, evtArgs) => {
                if (evtArgs.Index >= psWarningMessagesRead)
                {
                    ConsoleUtils.WriteLine(psInstance.Streams.Warning[evtArgs.Index].ToString(), ConsoleColor.DarkYellow);
                    psWarningMessagesRead++;
                }
            };

            psInstance.Invoke();
            // Clear PowerShell pipeline to avoid the script being re-executed the next time we use this instance
            psInstance.Commands.Clear();
        }

        /**
         *  Extension method to be used on a running process
         *  Prints synchronously all the content of StandardOutput and StandardError of the process
         *  It locks the thread until the process stops writing on those streams (usually until its end)
         */
        public static void PrintOutputAndErrors(this Process process)
        {
            Console.Write(process.StandardOutput.ReadToEnd());
            ConsoleUtils.Write(process.StandardError.ReadToEnd(), ConsoleColor.Red);
        }

        /**
         *  Runs a given process in the same terminal, redirecting its standard output/error and returning its Process instance
         *  Can optionally add callbacks to the Process instance for printing its messages asynchronously;
         *  they won't be printed until Begin[Output/Error]ReadLine() is called on the returned instance.
         */
        public static Process RunProcess(string name, string args, bool asyncMessagePrinting = false)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo {
                FileName = name,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (asyncMessagePrinting)
            {
                process.OutputDataReceived += (s, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.WriteLine(e.Data);
                };
                process.ErrorDataReceived += (s, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        ConsoleUtils.WriteLine(e.Data, ConsoleColor.Red);
                };
            }

            process.Start();
            return process;
        }

        /**
         *  Executes a CMD command synchronously
         *  Output and errors are printed asynchronously
         */
        public static void ExecuteWindowsCommand(string command)
        {
            Debug.WriteLine($"Command executed: {command}");
            using (var cmdProcess = RunProcess("cmd.exe", $"/c \"{command}\"", true))
            {
                cmdProcess.BeginOutputReadLine();
                cmdProcess.BeginErrorReadLine();
                cmdProcess.WaitForExit();
            }
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