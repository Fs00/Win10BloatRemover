using System;
using System.Globalization;
using System.Security.Principal;

namespace Win10BloatRemover
{
    static class Program
    {
        private static bool exit = false;

        static void Main()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en");
            Console.Title = "Windows 10 Bloat Remover";

            if (!Program.HasAdministratorRights())
            {
                Console.WriteLine("This application needs to be run with administrator rights!");
                Console.ReadKey();
                Environment.Exit(-1);
            }

            try
            {
                Utils.ExtractInstallWimTweak();
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
                    Console.Write("Select an entry: ");
                    chosenEntry = MenuUtils.ProcessUserInput();
                    if (chosenEntry == null)
                        Console.WriteLine("\nIncorrect input.");
                    else
                        userInputIsCorrect = true;
                }

                Console.Clear();
                ProcessMenuEntry(chosenEntry.Value);
            }

            try
            {
                Utils.DeleteTempInstallWimTweak();
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
                switch (entry)
                {
                    case MenuEntry.DisableAutoUpdates:
                        Console.WriteLine("-- Disabling automatic updates --");
                        Utils.DisableAutomaticUpdates();
                        break;
                    case MenuEntry.DisableCortana:
                        Console.WriteLine("-- Disabling Cortana --");
                        Utils.DisableCortana();
                        Console.WriteLine("A system reboot is recommended.");
                        break;
                    case MenuEntry.Quit:
                        exit = true;
                        break;
                    case MenuEntry.RemoveWinDefender:
                        Console.WriteLine("-- Removing Windows Defender --");
                        Utils.RemoveWindowsDefender();
                        break;
                    case MenuEntry.DisableScheduledTasks:
                        Console.WriteLine("-- Disabling useless scheduled tasks --");
                        Utils.DisableScheduledTasks(Configuration.ScheduledTasksToDisable);
                        break;
                    case MenuEntry.RemoveMSEdge:
                        Console.WriteLine("-- Removing Microsoft Edge --");
                        Utils.RemoveMicrosoftEdge();
                        break;
                    case MenuEntry.RemoveUWPApps:
                    case MenuEntry.RemoveOneDrive:
                    default:
                        Console.WriteLine($"Unimplemented function: {entry.ToString()}");
                        break;
                }

                Console.Write("Done! ");
            }
            catch (Exception exc)
            {
                Console.WriteLine($"An error occurred: {exc.Message}");
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
    }
}
