using System;

namespace Win10BloatRemover.UI
{
    class ConsoleUserInterface : IUserInterface
    {
        private int writtenMessages = 0;

        public void PrintMessage(string text) => PrintConsoleMessage(text);

        public void PrintError(string text) => PrintConsoleMessage(text, ConsoleColor.Red);

        public void PrintWarning(string text) => PrintConsoleMessage(text, ConsoleColor.DarkYellow);

        public void PrintNotice(string text) => PrintConsoleMessage(text, ConsoleColor.Cyan);

        public void PrintHeading(string text)
        {
            PrintEmptySpace();
            PrintConsoleMessage(text, ConsoleColor.Green);
        }

        public void PrintEmptySpace()
        {
            // Avoid a double empty line when called at the beginning of an operation
            if (writtenMessages > 0)
                Console.WriteLine();
        }

        public UserChoice AskUserConsent(string text)
        {
            Console.Write(text + " (y/N) ");
            string userInput = Console.ReadLine()?.Trim() ?? "";
            PrintEmptySpace();
            bool userDidConfirm = userInput.StartsWith("y", StringComparison.InvariantCultureIgnoreCase);
            return userDidConfirm ? UserChoice.Yes : UserChoice.No;
        }

        private void PrintConsoleMessage(string text, ConsoleColor? color = null)
        {
            if (color == null)
                Console.WriteLine(text);
            else
                ConsoleHelpers.WriteLine(text, color.Value);
            writtenMessages++;
        }
    }
}
