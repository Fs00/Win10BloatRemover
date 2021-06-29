using System;
using System.Diagnostics;
using System.Security.Principal;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover
{
    static class Program
    {
        public const string MINIMUM_SUPPORTED_WINDOWS_VERSION = "2009";

        private static void Main()
        {
            using var consoleListener = new ConsoleTraceListener();
            Trace.Listeners.Add(consoleListener);
            Console.Title = "Windows 10 Bloat Remover and Tweaker";

            EnsureProgramIsRunningAsAdmin();
            ShowWarningOnUnsupportedOS();
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

        private static bool HasAdministratorRights()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void ShowWarningOnUnsupportedOS()
        {
            bool isWindows10System = Environment.OSVersion.Version.Major == 10;
            string? installedWindows10Version = SystemUtils.RetrieveWindows10ReleaseId();
            if (!isWindows10System || IsLowerThanMinimumSupportedVersion(installedWindows10Version))
            {
                ConsoleHelpers.WriteLine("-- UNSUPPORTED WINDOWS VERSION --\n", ConsoleColor.DarkYellow);
                if (isWindows10System)
                {
                    Console.WriteLine(
                        "You are running an older version of Windows 10 which is not supported by this version of the program.\n" +
                        "You should update your system or download an older version of the program which is compatible with this\n" +
                        $"Windows 10 version ({installedWindows10Version}) at the following page:"
                    );
                    ConsoleHelpers.WriteLine("  https://github.com/Fs00/Win10BloatRemover/releases/", ConsoleColor.Cyan);
                }
                else
                    Console.WriteLine("This program was designed to work only on Windows 10.");

                Console.WriteLine(
                    "\nYou can still continue using this program, but BE AWARE that some features might work badly or not at all\n" +
                    "and could even have unintended effects on your system (including corruptions or instability)."
                );
                
                Console.WriteLine("\nPress enter to continue, or another key to quit.");
                if (Console.ReadKey().Key != ConsoleKey.Enter)
                    Environment.Exit(-1);
            }
        }

        private static bool IsLowerThanMinimumSupportedVersion(string? installedWindows10Version)
        {
            return string.Compare(installedWindows10Version, MINIMUM_SUPPORTED_WINDOWS_VERSION) < 0;
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
