using System;
using System.Diagnostics;
using System.Security.Principal;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover
{
    static class Program
    {
        public const string SUPPORTED_WINDOWS_RELEASE_ID = "2009";
        private const string SUPPORTED_WINDOWS_RELEASE_NAME = "October 2020 Update";

        private static void Main()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            Console.Title = "Windows 10 Bloat Remover and Tweaker";

            EnsureProgramIsRunningAsAdmin();
            ShowWarningOnIncompatibleOS();
            RegisterExitEventHandlers();

            var configuration = LoadConfigurationFromFileOrDefault();
            var rebootFlag = new RebootRecommendedFlag();
            var menu = new ConsoleMenu(CreateMenuEntries(configuration, rebootFlag), rebootFlag);
            menu.RunLoopUntilExitRequested();
        }

        private static MenuEntry[] CreateMenuEntries(Configuration configuration, RebootRecommendedFlag rebootFlag)
        {
            return new MenuEntry[] {
                new SystemAppsRemovalEnablingEntry(),
                new UWPAppRemovalEntry(configuration),
                new EdgeRemovalEntry(),
                new OneDriveRemovalEntry(),
                new ServicesRemovalEntry(configuration),
                new WindowsFeaturesRemovalEntry(configuration),
                new PrivacySettingsTweakEntry(),
                new TelemetryDisablingEntry(),
                new DefenderDisablingEntry(),
                new AutoUpdatesDisablingEntry(),
                new ScheduledTasksDisablingEntry(configuration),
                new ErrorReportingDisablingEntry(),
                new SuggestionsDisablingEntry(),
                new NewGitHubIssueEntry(),
                new AboutEntry(),
                new QuitEntry(rebootFlag)
            };
        }

        private static void EnsureProgramIsRunningAsAdmin()
        {
            if (!Program.HasAdministratorRights())
            {
                ConsoleHelpers.WriteLine("This application needs to be run with administrator rights!", ConsoleColor.Red);
                Console.ReadKey();
                Environment.Exit(-1);
            }
        }

        private static void ShowWarningOnIncompatibleOS()
        {
            string? installedWindowsVersion = SystemUtils.RetrieveWindowsReleaseId();
            if (installedWindowsVersion != SUPPORTED_WINDOWS_RELEASE_ID)
            {
                ConsoleHelpers.WriteLine(
                    "-- INCOMPATIBILITY WARNING --\n\n" +
                    $"This version of the program only supports Windows 10 {SUPPORTED_WINDOWS_RELEASE_NAME} (version {SUPPORTED_WINDOWS_RELEASE_ID}).\n" +
                    $"You are running Windows 10 version {installedWindowsVersion}.\n\n" +
                    "You should download a version of the program which is compatible with this Windows 10 version at the following page:",
                    ConsoleColor.DarkYellow);
                Console.WriteLine("  https://github.com/Fs00/Win10BloatRemover/releases/\n");
                ConsoleHelpers.WriteLine(
                    "If a compatible version is not available yet, you can still continue using this program.\n" +
                    "However, BE AWARE that some features might work badly or not at all, and could even have unintended effects\n" +
                    "on your system (including corruptions or instability).\n" +
                    "PROCEED AT YOUR OWN RISK.", ConsoleColor.DarkYellow);
                
                Console.WriteLine("\nPress enter to continue, or another key to quit.");
                if (Console.ReadKey().Key != ConsoleKey.Enter)
                    Environment.Exit(-1);
            }
        }

        private static bool HasAdministratorRights()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        
        private static Configuration LoadConfigurationFromFileOrDefault()
        {
            try
            {
                return Configuration.LoadOrCreateFile();
            }
            catch (ConfigurationException exc)
            {
                PrintConfigurationErrorMessage(exc);
                return Configuration.Default;
            }
        }

        private static void PrintConfigurationErrorMessage(ConfigurationException exc)
        {
            string errorMessage = "";
            if (exc is ConfigurationLoadException)
                errorMessage = $"An error occurred while loading settings file: {exc.Message}\n" +
                                "Default settings have been loaded instead.\n";
            else if (exc is ConfigurationWriteException)
                errorMessage = $"Couldn't write default configuration to settings file: {exc.Message}\n";

            ConsoleHelpers.WriteLine(errorMessage, ConsoleColor.DarkYellow);
            Console.WriteLine("Press a key to continue to the main menu.");
            Console.ReadKey();
        }

        private static void RegisterExitEventHandlers()
        {
            #if !DEBUG
            bool cancelKeyPressedOnce = false;
            Console.CancelKeyPress += (sender, args) => {
                if (!cancelKeyPressedOnce)
                {
                    ConsoleHelpers.WriteLine("Press Ctrl+C again to terminate the program.", ConsoleColor.Red);
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
    }
}
