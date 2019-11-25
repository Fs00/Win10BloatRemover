using System;
using System.Diagnostics;
using System.Security.Principal;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover
{
    static class Program
    {
        public const string SUPPORTED_WINDOWS_RELEASE_ID = "1909";
        private const string SUPPORTED_WINDOWS_RELEASE_NAME = "November 2019 Update";

        private static void Main()
        {
            Console.Title = "Windows 10 Bloat Remover and Tweaker";

            EnsurePreliminaryChecksAreSuccessful();
            TryLoadConfiguration();
            RegisterExitEventHandlers();

            if (Configuration.Instance.AllowInstallWimTweak)
                TryExtractInstallWimTweak();

            Menu.RunLoopUntilExitRequested();

            TryDeleteExtractedInstallWimTweak();
        }

        private static void EnsurePreliminaryChecksAreSuccessful()
        {
            if (!Program.HasAdministratorRights())
            {
                ConsoleUtils.WriteLine("This application needs to be run with administrator rights!", ConsoleColor.Red);
                Console.ReadKey();
                Environment.Exit(-1);
            }

            #if !DEBUG
            if (!SystemUtils.IsWindowsReleaseId(SUPPORTED_WINDOWS_RELEASE_ID))
            {
                ConsoleUtils.WriteLine($"This application is compatible only with Windows 10 {SUPPORTED_WINDOWS_RELEASE_NAME}!", ConsoleColor.Red);
                Console.ReadKey();
                Environment.Exit(-1);
            }
            #endif
        }

        private static bool HasAdministratorRights()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        
        private static void TryLoadConfiguration()
        {
            try
            {
                Configuration.Load();
            }
            catch (ConfigurationException exc)
            {
                ConsoleUtils.WriteLine(exc.Message, ConsoleColor.DarkYellow);
                Console.WriteLine("Press a key to continue to the main menu.");
                Console.ReadKey();
            }
        }

        private static void RegisterExitEventHandlers()
        {
            #if !DEBUG
            bool cancelKeyPressedOnce = false;
            Console.CancelKeyPress += (sender, args) => {
                if (!cancelKeyPressedOnce)
                {
                    ConsoleUtils.WriteLine("Press Ctrl+C again to terminate the program.", ConsoleColor.Red);
                    cancelKeyPressedOnce = true;
                    args.Cancel = true;
                }
                else
                    Process.GetCurrentProcess().KillChildProcesses();
            };
            #endif

            // Executed when the user closes the window. This handler is not fired when process is terminated with Ctrl+C
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => Process.GetCurrentProcess().KillChildProcesses();
        }

        private static void TryExtractInstallWimTweak()
        {
            try
            {
                InstallWimTweak.ExtractToTempFolderAndLockFile();
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Unable to extract install_wim_tweak tool to your temporary directory: {exc.Message}");
                Console.WriteLine("The application will exit.");
                Console.ReadKey();
                Environment.Exit(-1);
            }
        }

        private static void TryDeleteExtractedInstallWimTweak()
        {
            try
            {
                InstallWimTweak.DeleteExtractedExecutableIfExists();
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Unable to delete previously extracted install-wim-tweak binary: {exc.Message}");
                Console.ReadKey();
            }
        }
    }
}
