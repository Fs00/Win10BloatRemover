using System;
using System.Linq;
using Win10BloatRemover.Operations;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover
{
    class Menu
    {
        private bool exitRequested = false;
        private readonly MenuEntry[] entries;

        private static readonly Version programVersion = typeof(Menu).Assembly.GetName().Version!;

        public Menu(MenuEntry[] entries)
        {
            this.entries = entries;
        }

        public void RunLoopUntilExitRequested()
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

        private void PrintHeading()
        {
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("|    Windows 10 Bloat Remover and Tweaker   |");
            Console.WriteLine($"|                version {programVersion.Major}.{programVersion.Minor}                |");
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine();
        }

        private void PrintMenuEntries()
        {
            Console.WriteLine("-- MENU --");
            for (int i = 0; i < entries.Length; i++)
                Console.WriteLine($"{i}: {entries[i].FullName}");
            Console.WriteLine();
        }

        private MenuEntry RequestUserChoice()
        {
            MenuEntry? chosenEntry = null;
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
            return chosenEntry!;
        }

        private MenuEntry? GetEntryCorrespondingToUserInput(string userInput)
        {
            bool inputIsNumeric = int.TryParse(userInput, out int entryIndex);
            if (inputIsNumeric)
                return entries.ElementAtOrDefault(entryIndex);

            return null;
        }

        private void PrintTitleAndExplanation(MenuEntry entry)
        {
            ConsoleUtils.WriteLine($"-- {entry.FullName} --", ConsoleColor.Green);
            Console.WriteLine(entry.GetExplanation());
        }

        private bool UserWantsToProceed()
        {
            Console.WriteLine("\nPress enter to continue, or another key to go back to the menu.");
            return Console.ReadKey().Key == ConsoleKey.Enter;
        }

        private void TryPerformEntryOperation(MenuEntry entry)
        {
            if (entry.ShouldQuit)
            {
                exitRequested = true;
                return;
            }

            try
            {
                Console.WriteLine();
                IOperation? operation = entry.GetOperationInstance();
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
