using System;
using System.Security.Principal;

namespace Win10BloatRemover
{
    static class Program
    {
        private static bool exit = false;

        static void Main()
        {
            if (!Program.HasAdministratorRights())
            {
                Console.WriteLine("This application needs to be run with administrator rights!");
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
        }

        /**
         *  Performs actions according to the MenuEntry chosen
         */
        private static void ProcessMenuEntry(MenuEntry entry)
        {
            switch (entry)
            {
                case MenuEntry.Quit:
                    exit = true;
                    break;
                case MenuEntry.RemoveUWPApps:
                case MenuEntry.RemoveWinDefender:
                case MenuEntry.RemoveMSEdge:
                case MenuEntry.RemoveOneDrive:
                case MenuEntry.DisableAutoUpdates:
                case MenuEntry.DisableCortana:
                case MenuEntry.DisableScheduledTasks:
                default:
                    Console.WriteLine($"Unimplemented function: {entry.ToString()}");
                    break;
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
