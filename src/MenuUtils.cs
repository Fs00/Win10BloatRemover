using System;

namespace Win10BloatRemover
{
    public enum MenuEntry
    {
        RemoveUWPApps,
        RemoveWinDefender,
        RemoveMSEdge,
        RemoveConnectApp,
        RemoveOneDrive,
        RemoveWindowsFeatures,
        RemoveServices,
        DisableAutoUpdates,
        DisableCortana,
        DisableScheduledTasks,
        DisableErrorReporting,
        DisableWindowsTips,
        Credits,
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
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("|    Windows 10 Bloat Remover and Tweaker   |");
            Console.WriteLine("|             for version 1809              |");
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine();
        }

        /**
         *  Prints the menu by looping on the MenuEntry enum values
         */
        public static void PrintMenu()
        {
            Console.WriteLine("-- MENU --");
            foreach (MenuEntry entry in Enum.GetValues(typeof(MenuEntry)))
                Console.WriteLine($"{(int)entry}: {GetMenuEntryDescription(entry)}");
            Console.WriteLine();
        }

        public static void PrintCredits()
        {
            Console.WriteLine("Developed by Fs00");
            //Console.WriteLine("Official GitHub repository: github.com/Fs00/Win10BloatRemover");
            Console.WriteLine("Based on Windows 10 de-botnet guide by Federico Dossena: fdossena.com");
        }

        /**
         *  Waits for user input and returns the MenuEntry corresponding to the number pressed
         *  If the number doesn't map to an existing entry, null is returned
         */
        public static MenuEntry? ProcessUserInput()
        {
            bool inputIsNumeric = int.TryParse(Console.ReadLine(), out int userInputNumber);
            if (inputIsNumeric && Enum.IsDefined(typeof(MenuEntry), userInputNumber))
                return (MenuEntry) userInputNumber;
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
                    description = "UWP apps removal";
                    break;
                case MenuEntry.RemoveWinDefender:
                    description = "Windows Defender removal";
                    break;
                case MenuEntry.RemoveConnectApp:
                    description = "Connect app removal";
                    break;
                case MenuEntry.RemoveMSEdge:
                    description = "Microsoft Edge removal";
                    break;
                case MenuEntry.RemoveOneDrive:
                    description = "OneDrive removal";
                    break;
                case MenuEntry.RemoveWindowsFeatures:
                    description = "Useless Windows features removal";
                    break;
                case MenuEntry.DisableAutoUpdates:
                    description = "Automatic Windows updates disabling";
                    break;
                case MenuEntry.DisableCortana:
                    description = "Cortana disabling";
                    break;
                case MenuEntry.RemoveServices:
                    description = "Diagnostic services removal";
                    break;
                case MenuEntry.DisableScheduledTasks:
                    description = "Useless scheduled tasks disabling";
                    break;
                case MenuEntry.DisableErrorReporting:
                    description = "Windows Error Reporting disabling";
                    break;
                case MenuEntry.DisableWindowsTips:
                    description = "Windows Tips disabling";
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

        public static string GetMenuEntryExplanation(MenuEntry entry)
        {
            string explanation;
            switch (entry)
            {
                case MenuEntry.RemoveUWPApps:
                    explanation = "The following UWP apps will be removed:\n";
                    foreach (string app in Configuration.UWPAppsToRemove)
                        explanation += $"  {app}\n";
                    break;
                case MenuEntry.RemoveWinDefender:
                    explanation = "Windows Defender menu icon will remain there, but the program won't start anymore.";
                    break;
                case MenuEntry.RemoveMSEdge:
                    explanation = "Remember to unpin Edge from your taskbar, otherwise you won't be able to do it!";
                    break;
                case MenuEntry.RemoveWindowsFeatures:
                    explanation = "The following features will be removed:\n";
                    foreach (string feature in Configuration.WindowsFeaturesToRemove)
                        explanation += $"  {feature}\n";
                    break;
                case MenuEntry.DisableCortana:
                    explanation = "This won't remove Cortana (otherwise the system would break), it will only be disabled using GP and blocked by the firewall.";
                    break;
                case MenuEntry.RemoveServices:
                    explanation = "The following services will be removed:\n";
                    break;
                case MenuEntry.DisableScheduledTasks:
                    explanation = "The following scheduled tasks will be disabled:\n";
                    foreach (string task in Configuration.ScheduledTasksToDisable)
                        explanation += $"  {task}\n";
                    break;
                case MenuEntry.Quit:
                    explanation = "Are you sure?";
                    break;
                default:
                    explanation = string.Empty;
                    break;
            }
            return explanation;
        }
    }
}
