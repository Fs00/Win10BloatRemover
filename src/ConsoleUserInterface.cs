using System;
using Win10BloatRemover.Operations;
using static Win10BloatRemover.Operations.IUserInterface;

namespace Win10BloatRemover
{
    class ConsoleUserInterface : IUserInterface
    {
        public void PrintMessage(string text) => PrintConsoleMessage(text);

        public void PrintError(string text) => PrintConsoleMessage(text, ConsoleColor.Red);

        public void PrintWarning(string text) => PrintConsoleMessage(text, ConsoleColor.DarkYellow);

        public void PrintNotice(string text) => PrintConsoleMessage(text, ConsoleColor.Cyan);

        public void PrintHeading(string text) => PrintConsoleMessage(text, ConsoleColor.Green);

        public void PrintSubHeading(string text) => PrintConsoleMessage(text, ConsoleColor.DarkGreen);

        public void PrintEmptySpace() => Console.Write("\n");

        public UserChoice AskUserConsent(string text)
        {
            Console.Write(text + " (y/N) ");
            string userInput = Console.ReadLine()?.Trim() ?? "";
            PrintEmptySpace();
            bool userPressedY = userInput.Equals("y", StringComparison.InvariantCultureIgnoreCase);
            return userPressedY ? UserChoice.Yes : UserChoice.No;
        }

        private void PrintConsoleMessage(string text)
        {
            Console.WriteLine(text);
        }

        private void PrintConsoleMessage(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            PrintConsoleMessage(text);
            Console.ResetColor();
        }
    }
}
