using System;
using System.IO;
using System.Resources;

namespace Win10BloatRemover.Utils
{
    static class InstallWimTweak
    {
        private static readonly string executableFilePath = Path.Combine(Path.GetTempPath(), "install_wim_tweak.exe");
        #nullable disable warnings
        private static /*lateinit*/ FileStream executableFileStreamForLocking;
        #nullable restore warnings

        public static void ExtractToTempFolderAndLockFile()
        {
            var resources = new ResourceManager("Win10BloatRemover.resources.Resources", typeof(Program).Assembly);
            File.WriteAllBytes(executableFilePath, (byte[]) resources.GetObject("install_wim_tweak")!);
            executableFileStreamForLocking = File.Open(executableFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public static void RemoveComponentIfAllowed(string component)
        {
            if (!Configuration.Instance.AllowInstallWimTweak)
            {
                ConsoleUtils.WriteLine($"Skipped removal of {component} component(s) using install-wim-tweak since " +
                                       @"option ""AllowInstallWimTweak"" is set to false.", ConsoleColor.DarkYellow);
                return;
            }

            Console.WriteLine($"Running install-wim-tweak to remove {component}...");
            int exitCode = SystemUtils.RunProcessSynchronouslyWithConsoleOutput(executableFilePath, $"/o /c {component} /r");
            if (exitCode == SystemUtils.EXIT_CODE_SUCCESS)
                Console.WriteLine("Install-wim-tweak executed successfully!");
            else
                ConsoleUtils.WriteLine($"An error occurred during the removal of {component}: " +
                                        "install-wim-tweak exited with a non-zero status.", ConsoleColor.Red);
        }

        public static void DeleteExtractedExecutableIfExists()
        {
            if (File.Exists(executableFilePath))
            {
                executableFileStreamForLocking.Close();
                File.Delete(executableFilePath);
            }
        }
    }
}
