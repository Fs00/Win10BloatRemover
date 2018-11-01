using System;
using System.Diagnostics;
using System.IO;

namespace Win10BloatRemover
{
    static class SystemUtils
    {
        /**
         *  Extension method to be used on a running process
         *  Prints synchronously all the content of StandardOutput and StandardError of the process
         *  It locks the thread until the process stops writing on those streams (usually until its end)
         */
        public static void PrintOutputAndErrors(this Process process)
        {
            Console.Write(process.StandardOutput.ReadToEnd());

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(process.StandardError.ReadToEnd());
            Console.ResetColor();
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
                process.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
                process.ErrorDataReceived += (s, e) => {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.Data);
                    Console.ResetColor();
                };
            }

            process.Start();
            return process;
        }

        /**
         *  Executes a CMD command synchronously
         *  Output is printed after the command terminates its execution
         */
        public static void ExecuteWindowsCommand(string command)
        {
            using (var cmdProcess = RunProcess("cmd.exe", $"/c {command}"))
            {
                cmdProcess.PrintOutputAndErrors();
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
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"An error occurred when deleting folder {path}: {exc.Message}");
                    Console.ResetColor();
                }
            }
            else
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
        }

        public static string GetWindowsReleaseId()
        {
            return Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString();
        }
    }
}