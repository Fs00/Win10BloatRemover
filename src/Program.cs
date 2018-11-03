using System;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Security.Principal;

namespace Win10BloatRemover
{
    static class Program
    {
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

            while (!exit)
            {
                bool userInputIsCorrect = false;
                Console.Clear();
                MenuUtils.PrintHeading();
                MenuUtils.PrintMenu();

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
                Console.WriteLine($"-- {MenuUtils.GetMenuEntryDescription(entry)} --");
                Console.WriteLine(MenuUtils.GetMenuEntryExplanation(entry));
                Console.WriteLine("Press enter to continue, or another key to go back to the menu.");
                if (Console.ReadKey().Key != ConsoleKey.Enter)
                    return;

                switch (entry)
                {
                    case MenuEntry.RemoveUWPApps:
                        new UWPAppRemover(Configuration.UWPAppsToRemove).PerformRemoval();
                        break;
                    case MenuEntry.DisableAutoUpdates:
                        Operations.DisableAutomaticUpdates();
                        break;
                    case MenuEntry.DisableCortana:
                        Operations.DisableCortana();
                        Console.WriteLine("A system reboot is recommended.");
                        break;
                    case MenuEntry.RemoveWinDefender:
                        Operations.RemoveWindowsDefender();
                        break;
                    case MenuEntry.RemoveConnectApp:
                        Operations.RemoveComponentUsingInstallWimTweak("Microsoft-PPIProjection-Package");
                        Console.WriteLine("A system reboot is recommended.");
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
                        var serviceRemover = new ServiceRemover(Configuration.ServicesToRemove);
                        Console.WriteLine("Backing up services...");
                        serviceRemover.PerformBackup();
                        Console.WriteLine("Removing services...");
                        serviceRemover.PerformRemoval();
                        break;
                    case MenuEntry.DisableErrorReporting:
                        Operations.DisableWinErrorReporting();
                        break;
                    case MenuEntry.DisableScheduledTasks:
                        Operations.DisableScheduledTasks(Configuration.ScheduledTasksToDisable);
                        Console.WriteLine("Some commands may fail, it's normal.");
                        break;
                    case MenuEntry.DisableWindowsTipsAndFeedback:
                        Operations.DisableWindowsTipsAndFeedback();
                        break;
                    case MenuEntry.RemoveWindowsFeatures:
                        Operations.RemoveWindowsFeatures(Configuration.WindowsFeaturesToRemove);
                        Console.WriteLine("A system reboot is recommended.");
                        break;
                    case MenuEntry.Credits:
                        MenuUtils.PrintCredits();
                        break;
                    case MenuEntry.Quit:
                        exit = true;
                        break;
                    default:
                        Console.WriteLine($"Unimplemented function: {entry.ToString()}");
                        break;
                }

                Console.Write("Done! ");
            }
            catch (Exception exc)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Operation failed: {exc.Message}");
                Console.ResetColor();
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
            ResourceManager binResources = new ResourceManager("Win10BloatRemover.resources.Binaries", typeof(Operations).Assembly);
            File.WriteAllBytes(Configuration.InstallWimTweakPath, (byte[])binResources.GetObject("install_wim_tweak"));
        }

        public static void DeleteTempInstallWimTweak()
        {
            if (File.Exists(Configuration.InstallWimTweakPath))
                File.Delete(Configuration.InstallWimTweakPath);
        }
    }
}
