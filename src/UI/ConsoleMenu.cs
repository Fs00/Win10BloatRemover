using Win10BloatRemover.Operations;

namespace Win10BloatRemover.UI;

class ConsoleMenu
{
    private const int FirstMenuEntryNumber = 1;

    private bool exitRequested = false;
    private readonly MenuEntry[] entries;
    private readonly RebootRecommendedFlag rebootFlag;

    private static readonly Version programVersion = typeof(ConsoleMenu).Assembly.GetName().Version!;

    public ConsoleMenu(MenuEntry[] entries, RebootRecommendedFlag rebootFlag)
    {
        this.entries = entries;
        this.rebootFlag = rebootFlag;
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
            ConsoleHelpers.Write($"{FirstMenuEntryNumber + i}: ", ConsoleColor.Green);
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

    private MenuEntry? GetEntryCorrespondingToUserInput(string? userInput)
    {
        bool inputIsNumeric = int.TryParse(userInput, out int entryNumber);
        if (inputIsNumeric)
        {
            int entryIndex = entryNumber - FirstMenuEntryNumber;
            return entries.ElementAtOrDefault(entryIndex);
        }

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
        try
        {
            Console.WriteLine();
            IOperation operation = entry.CreateNewOperation(new ConsoleUserInterface());
            operation.Run();
            if (operation.IsRebootRecommended)
            {
                ConsoleHelpers.WriteLine("\nA system reboot is recommended.", ConsoleColor.Cyan);
                rebootFlag.SetRebootRecommended();
            }

            if (entry.ShouldQuit)
            {
                exitRequested = true;
                return;
            }

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
