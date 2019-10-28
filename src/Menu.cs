using System;
using System.Linq;
using Win10BloatRemover.Operations;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover
{
    static class Menu
    {
        private static bool exitRequested = false;
        private static readonly Version programVersion = typeof(Menu).Assembly.GetName().Version;
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
            new AboutEntry(),
            new QuitEntry()
        };

        public static void RunLoopUntilExitRequested()
        {
            while (!exitRequested)
            {
                Console.Clear();
                PrintHeading();
                PrintMenuEntries();
                MenuEntry chosenEntry = RequestUserChoice();

                Console.Clear();
                PrintTitleAndExplanation(chosenEntry);
                if (UserWantsToProceed())
                    TryPerformEntryOperation(chosenEntry);
            }
        }

        private static void PrintHeading()
        {
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("|    Windows 10 Bloat Remover and Tweaker   |");
            Console.WriteLine($"|                version {programVersion.Major}.{programVersion.Minor}                |");
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine();
        }

        private static void PrintMenuEntries()
        {
            Console.WriteLine("-- MENU --");
            for (int i = 0; i < orderedMenuEntries.Length; i++)
                Console.WriteLine($"{i}: {orderedMenuEntries[i].FullName}");
            Console.WriteLine();
        }

        private static MenuEntry RequestUserChoice()
        {
            MenuEntry chosenEntry = null;
            bool isUserInputCorrect = false;
            while (!isUserInputCorrect)
            {
                Console.Write("Choose an operation: ");
                chosenEntry = GetEntryCorrespondingToUserInput(Console.ReadLine());
                if (chosenEntry == null)
                    Console.WriteLine("Incorrect input.");
                else
                    isUserInputCorrect = true;
            }
            return chosenEntry;
        }

        private static MenuEntry GetEntryCorrespondingToUserInput(string userInput)
        {
            bool inputIsNumeric = int.TryParse(userInput, out int entryIndex);
            if (inputIsNumeric)
                return orderedMenuEntries.ElementAtOrDefault(entryIndex);

            return null;
        }

        private static void PrintTitleAndExplanation(MenuEntry entry)
        {
            ConsoleUtils.WriteLine($"-- {entry.FullName} --", ConsoleColor.Green);
            Console.WriteLine(entry.GetExplanation());
        }

        private static bool UserWantsToProceed()
        {
            Console.WriteLine("Press enter to continue, or another key to go back to the menu.");
            if (Console.ReadKey().Key == ConsoleKey.Enter)
                return true;

            return false;
        }

        private static void TryPerformEntryOperation(MenuEntry entry)
        {
            if (entry is QuitEntry)
            {
                exitRequested = true;
                return;
            }

            try
            {
                Console.WriteLine();
                IOperation operation = entry.GetOperationInstance();
                if (operation == null)
                    return;

                operation.PerformTask();
                Console.Write("\nDone! ");
            }
            catch (Exception exc)
            {
                ConsoleUtils.WriteLine($"Operation failed: {exc.Message}", ConsoleColor.Red);
            }

            Console.WriteLine("Press a key to return to the main menu");
            ConsoleUtils.ReadKeyIgnoringBuffer();
        }
    }
}
