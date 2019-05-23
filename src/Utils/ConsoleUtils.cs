using System;

namespace Win10BloatRemover.Utils
{
    static class ConsoleUtils
    {
        /**
         *  Helper methods to print a message with a specific color
         */
        public static void WriteLine(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void Write(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }
    }
}
