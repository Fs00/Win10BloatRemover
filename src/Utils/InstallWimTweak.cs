using System;
using System.IO;
using System.Resources;

namespace Win10BloatRemover.Utils
{
    static class InstallWimTweak
    {
        private static readonly string executablePath = Path.Combine(Path.GetTempPath(), "install_wim_tweak.exe");

        public static void ExtractToTempFolder()
        {
            var resources = new ResourceManager("Win10BloatRemover.resources.Resources", typeof(Program).Assembly);
            File.WriteAllBytes(executablePath, (byte[]) resources.GetObject("install_wim_tweak")!);
        }

        public static void RemoveComponentIfAllowed(string component)
        {
            if (!Configuration.Instance.AllowInstallWimTweak)
            {
                ConsoleUtils.WriteLine($"Skipped removal of component {component} using install-wim-tweak since " +
                                       @"option ""AllowInstallWimTweak"" is set to false.", ConsoleColor.DarkYellow);
                return;
            }

            Console.WriteLine($"Running install-wim-tweak to remove {component}...");
            int exitCode = SystemUtils.RunProcessSynchronouslyWithConsoleOutput(executablePath, $"/o /c {component} /r");
            if (exitCode == SystemUtils.EXIT_CODE_SUCCESS)
                Console.WriteLine("Install-wim-tweak executed successfully!");
            else
                ConsoleUtils.WriteLine($"An error occurred during the removal of {component}: " +
                                        "install-wim-tweak exited with a non-zero status.", ConsoleColor.Red);
        }

        public static void DeleteExtractedExecutableIfExists()
        {
            if (File.Exists(executablePath))
                File.Delete(executablePath);
        }
    }
}
