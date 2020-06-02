using System;
using System.Linq;
using Win10BloatRemover.Operations;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover
{
    class ConsoleMenu
    {
        private bool exitRequested = false;
        private readonly MenuEntry[] entries;

        private static readonly Version programVersion = typeof(ConsoleMenu).Assembly.GetName().Version!;

        public ConsoleMenu(MenuEntry[] entries)
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
            Console.WriteLine("┌────────────────────────────────────────────┐");
            Console.WriteLine("│    Windows 10 Bloat Remover and Tweaker    │");
            Console.WriteLine($"│                version {programVersion.Major}.{programVersion.Minor}                 │");
            Console.WriteLine("└────────────────────────────────────────────┘");
            Console.WriteLine();
        }

        private void PrintMenuEntries()
        {
            ConsoleHelpers.WriteLine("-- MENU --", ConsoleColor.Green);
            for (int i = 0; i < entries.Length; i++)
            {
                ConsoleHelpers.Write($"{i}: ", ConsoleColor.Green);
                Console.WriteLine(entries[i].FullName);
            }
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
                    ConsoleHelpers.WriteLine("Incorrect input. Must be a valid menu entry number.", ConsoleColor.Red);
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
            ConsoleHelpers.WriteLine($"-- {entry.FullName} --", ConsoleColor.Green);
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
                IOperation? operation = entry.CreateNewOperation(new ConsoleUserInterface());
                if (operation == null)
                    return;

                operation.Run();
                Console.Write("\nDone! ");
            }
            catch (Exception exc)
            {
                ConsoleHelpers.WriteLine($"Operation failed: {exc.Message}", ConsoleColor.Red);
                #if DEBUG
                ConsoleHelpers.WriteLine(exc.StackTrace, ConsoleColor.Red);
                #endif
                Console.WriteLine();
            }

            ConsoleHelpers.FlushStandardInput();
            Console.WriteLine("Press a key to return to the main menu");
            Console.ReadKey();
        }
    }
}
