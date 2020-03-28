using System;
using System.IO;
using System.Resources;
using System.Runtime.Loader;

namespace Win10BloatRemover.Utils
{
    class InstallWimTweak
    {
        private static readonly string extractedFilePath = Path.Combine(Path.GetTempPath(), "install_wim_tweak.exe");

        private /*lateinit*/ FileStream executableFileStreamForLocking = default!;
        private readonly bool isAllowed;

        public InstallWimTweak(Configuration configuration)
        {
            isAllowed = configuration.AllowInstallWimTweak;
            AssemblyLoadContext.Default.Unloading += _ => CleanupExtractedExecutable();
        }

        private void ExtractAndLock()
        {
            var resources = new ResourceManager("Win10BloatRemover.resources.Resources", typeof(Program).Assembly);
            File.WriteAllBytes(extractedFilePath, (byte[]) resources.GetObject("install_wim_tweak")!);
            executableFileStreamForLocking = File.Open(extractedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public void RemoveComponentIfAllowed(string component)
        {
            if (!isAllowed)
            {
                ConsoleUtils.WriteLine($"Skipped removal of {component} component(s) using install-wim-tweak since " +
                                       @"option ""AllowInstallWimTweak"" is set to false.", ConsoleColor.DarkYellow);
                return;
            }

            if (!File.Exists(extractedFilePath))
                ExtractAndLock();

            Console.WriteLine($"Running install-wim-tweak to remove {component}...");
            int exitCode = SystemUtils.RunProcessSynchronouslyWithConsoleOutput(extractedFilePath, $"/o /c {component} /r");
            if (exitCode == SystemUtils.EXIT_CODE_SUCCESS)
                Console.WriteLine("Install-wim-tweak executed successfully!");
            else
                ConsoleUtils.WriteLine($"An error occurred during the removal of {component}: " +
                                        "install-wim-tweak exited with a non-zero status.", ConsoleColor.Red);
        }

        private void CleanupExtractedExecutable()
        {
            if (File.Exists(extractedFilePath))
            {
                executableFileStreamForLocking.Close();
                File.Delete(extractedFilePath);
            }
        }
    }
}
