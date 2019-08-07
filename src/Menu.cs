using System;

namespace Win10BloatRemover
{
    static class Menu
    {
        private static readonly MenuEntry[] orderedMenuEntries = {
            new SystemAppRemovalEnablingEntry(),
            new UWPAppRemovalEntry(),
            new WinDefenderRemovalEntry(),
            new EdgeRemovalEntry(),
            new OneDriveRemovalEntry(),
            new ServicesRemovalEntry(),
            new WindowsFeaturesRemovalEntry(),
            new TelemetryDisablingEntry(),
            new CortanaDisablingEntry(),
            new AutoUpdatesDisablingEntry(),
            new ScheduledTasksDisablingEntry(),
            new ErrorReportingDisablingEntry(),
            new TipsAndFeedbackDisablingEntry(),
            new NewGitHubIssueEntry(),
            new CreditsEntry(),
            new QuitEntry()
        };

        public static void PrintHeading()
        {
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("|    Windows 10 Bloat Remover and Tweaker   |");
            Console.WriteLine("|             for version " + Program.SUPPORTED_WINDOWS_RELEASE_ID + "              |");
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine();
        }

        public static void PrintMenu()
        {
            Console.WriteLine("-- MENU --");
            for (int i = 0; i < orderedMenuEntries.Length; i++)
                Console.WriteLine($"{i}: {orderedMenuEntries[i].Description}");
            Console.WriteLine();
        }

        /**
         *  Waits for user input and returns the MenuEntry corresponding to the number pressed
         *  If the number doesn't map to an existing entry, null is returned
         */
        public static MenuEntry ProcessUserInput()
        {
            bool inputIsNumeric = int.TryParse(Console.ReadLine(), out int userInputNumber);
            if (inputIsNumeric && userInputNumber >= 0 && userInputNumber < orderedMenuEntries.Length)
                return orderedMenuEntries[userInputNumber];
            else
                return null;
        }
    }
}
