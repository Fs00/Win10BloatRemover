using System;

namespace Win10BloatRemover
{
    public enum MenuEntry
    {
        RemoveUWPApps,
        RemoveWinDefender,
        RemoveMSEdge,
        RemoveOneDrive,
        DisableAutoUpdates,
        DisableCortana,
        DisableScheduledTasks,
        Quit
    }

    /**
     *  MenuUtils
     *  Contains helper functions to display the menu
     */
    class MenuUtils
    {
        public static void PrintHeading()
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("|      Windows 10 Bloat Remover      |");
            Console.WriteLine("|         Developed by Fs00          |");
            Console.WriteLine("--------------------------------------");
        }

        /**
         *  Prints the menu by looping on the MenuEntry enum values
         */
        public static void PrintMenu()
        {
            Console.WriteLine("Menu:");
            foreach (MenuEntry entry in Enum.GetValues(typeof(MenuEntry)))
                Console.WriteLine($"{(int)entry}: {GetMenuEntryDescription(entry)}");
        }

        /**
         *  Waits for user input and returns the MenuEntry corresponding to the number pressed
         *  If the number doesn't map to an existing entry, null is returned
         */
        public static MenuEntry? ProcessUserInput()
        {
            int userInputNumber = Console.ReadKey().KeyChar - 48;
            if (Enum.IsDefined(typeof(MenuEntry), userInputNumber))
                return (MenuEntry)userInputNumber;
            else
                return null;
        }

        /**
         *  Given a MenuEntry enum value, gets the description corresponding to the entry
         */
        public static string GetMenuEntryDescription(MenuEntry entry)
        {
            string description;
            switch (entry)
            {
                case MenuEntry.RemoveUWPApps:
                    description = "Remove UWP apps";
                    break;
                case MenuEntry.RemoveWinDefender:
                    description = "Remove Windows Defender";
                    break;
                case MenuEntry.RemoveMSEdge:
                    description = "Remove Microsoft Edge";
                    break;
                case MenuEntry.RemoveOneDrive:
                    description = "Remove OneDrive";
                    break;
                case MenuEntry.DisableAutoUpdates:
                    description = "Disable automatic Windows updates";
                    break;
                case MenuEntry.DisableCortana:
                    description = "Disable Cortana";
                    break;
                case MenuEntry.DisableScheduledTasks:
                    description = "Disable useless scheduled tasks";
                    break;
                case MenuEntry.Quit:
                    description = "Exit the application";
                    break;
                default:
                    description = entry.ToString();
                    break;
            }
            return description;
        }
    }
}
