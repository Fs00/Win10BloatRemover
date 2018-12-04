using System;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Security.Principal;

namespace Win10BloatRemover
{
    static class Program
    {
        public static string InstallWimTweakPath { get; } = Path.Combine(Path.GetTempPath(), "install_wim_tweak.exe");
        private static bool exit = false;

        static void Main()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en");
            Console.Title = "Windows 10 Bloat Remover and Tweaker";

            if (!Program.HasAdministratorRights())
            {
                Console.WriteLine("This application needs to be run with administrator rights!");
                Console.ReadKey();
                Environment.Exit(-1);
            }
            
            if (SystemUtils.GetWindowsReleaseId() != "1809")
            {
                Console.WriteLine("This application is compatible only with Windows 10 October 2018 Update!");
                Console.ReadKey();
                Environment.Exit(-1);
            }

            try
            {
                ExtractInstallWimTweak();
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Unable to extract install_wim_tweak tool to your temporary directory: {exc.Message}");
                Console.WriteLine("The application will exit.");
                Console.ReadKey();
                Environment.Exit(-1);
            }

            string configurationLoadingError = Configuration.Load();
            while (!exit)
            {
                // Console screen mustn't be cleared if at the first iteration was detected an error when
                // loading options, so that the user can be warned of it
                if (configurationLoadingError != null)
                {
                    ConsoleUtils.WriteLine(configurationLoadingError, ConsoleColor.Red);
                    configurationLoadingError = null;
                }
                else
                    Console.Clear();

                MenuUtils.PrintHeading();
                MenuUtils.PrintMenu();

                bool userInputIsCorrect = false;
                MenuEntry? chosenEntry = null;
                while (!userInputIsCorrect)
                {
                    Console.Write("Choose an operation: ");
                    chosenEntry = MenuUtils.ProcessUserInput();
                    if (chosenEntry == null)
                        Console.WriteLine("Incorrect input.");
                    else
                        userInputIsCorrect = true;
                }

                Console.Clear();
                ProcessMenuEntry(chosenEntry.Value);
            }

            try
            {
                DeleteTempInstallWimTweak();
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Unable to perform exit cleanup: {exc.Message}");
            }
        }

        /**
         *  Performs actions according to the MenuEntry chosen
         */
        private static void ProcessMenuEntry(MenuEntry entry)
        {
            try
            {
                ConsoleUtils.WriteLine($"-- {MenuUtils.GetMenuEntryDescription(entry)} --", ConsoleColor.Green);
                Console.WriteLine(MenuUtils.GetMenuEntryExplanation(entry));
                Console.WriteLine("Press enter to continue, or another key to go back to the menu.");
                if (Console.ReadKey().Key != ConsoleKey.Enter)
                    return;

                Console.WriteLine();
                switch (entry)
                {
                    case MenuEntry.RemoveUWPApps:
                        new UWPAppRemover(Configuration.Instance.UWPAppsToRemove).PerformRemoval();
                        break;
                    case MenuEntry.DisableAutoUpdates:
                        Console.WriteLine("Writing values into the Registry...");
                        Operations.DisableAutomaticUpdates();
                        break;
                    case MenuEntry.DisableCortana:
                        Operations.DisableCortana();
                        Console.WriteLine("A system reboot is recommended.");
                        break;
                    case MenuEntry.RemoveWinDefender:
                        Operations.RemoveWindowsDefender();
                        break;
                    case MenuEntry.RemoveMSEdge:
                        Operations.RemoveComponentUsingInstallWimTweak("Microsoft-Windows-Internet-Browser");
                        Console.WriteLine("A system reboot is recommended.");
                        break;
                    case MenuEntry.RemoveOneDrive:
                        Operations.RemoveOneDrive();
                        Console.WriteLine("Some folders may not exist, it's normal.");
                        break;
                    case MenuEntry.RemoveServices:
                        var serviceRemover = new ServiceRemover(Configuration.Instance.ServicesToRemove);
                        ConsoleUtils.WriteLine("Backing up services...", ConsoleColor.Green);
                        serviceRemover.PerformBackup();
                        ConsoleUtils.WriteLine("Removing services...", ConsoleColor.Green);
                        serviceRemover.PerformRemoval();
                        ConsoleUtils.WriteLine("Performing additional tasks to disable telemetry-related features...", ConsoleColor.Green);
                        Operations.DisableTelemetryRelatedFeatures();
                        ConsoleUtils.WriteLine("You may also want to remove DPS, WdiSystemHost and WdiServiceHost services, " +
                                               "which can't be easily deleted programmatically due to their permissions.", ConsoleColor.Cyan);
                        ConsoleUtils.WriteLine("Follow this steps to do it: github.com/adolfintel/Windows10-Privacy/blob/master/data/delkey.gif", ConsoleColor.Cyan);
                        break;
                    case MenuEntry.DisableErrorReporting:
                        Console.WriteLine("Writing values into the Registry...");
                        Operations.DisableWinErrorReporting();
                        break;
                    case MenuEntry.DisableScheduledTasks:
                        Operations.DisableScheduledTasks(Configuration.Instance.ScheduledTasksToDisable);
                        Console.WriteLine("Some commands may fail, it's normal.");
                        break;
                    case MenuEntry.DisableWindowsTipsAndFeedback:
                        Console.WriteLine("Writing values into the Registry...");
                        Operations.DisableWindowsTipsAndFeedback();
                        break;
                    case MenuEntry.RemoveWindowsFeatures:
                        Operations.RemoveWindowsFeatures(Configuration.Instance.WindowsFeaturesToRemove);
                        Console.WriteLine("A system reboot is recommended.");
                        break;
                    case MenuEntry.Credits:
                        // Credits are printed through GetMenuEntryExplanation(). We want to skip "Done" message in this case.
                        return;
                    case MenuEntry.Quit:
                        exit = true;
                        break;
                    default:
                        Console.WriteLine($"Unimplemented function: {entry.ToString()}");
                        break;
                }

                Console.Write("\nDone! ");
            }
            catch (Exception exc)
            {
                ConsoleUtils.WriteLine($"Operation failed: {exc.Message}", ConsoleColor.Red);
            }

            if (entry != MenuEntry.Quit)
            {
                Console.WriteLine("Press a key to return to the main menu");
                Console.ReadKey();
            }
        }

        public static bool HasAdministratorRights()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void ExtractInstallWimTweak()
        {
            var resources = new ResourceManager("Win10BloatRemover.resources.Resources", typeof(Operations).Assembly);
            File.WriteAllBytes(InstallWimTweakPath, (byte[]) resources.GetObject("install_wim_tweak"));
        }

        public static void DeleteTempInstallWimTweak()
        {
            if (File.Exists(InstallWimTweakPath))
                File.Delete(InstallWimTweakPath);
        }
    }
}
