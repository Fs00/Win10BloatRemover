using System;

namespace Win10BloatRemover.Utils
{
    static class ConsoleHelpers
    {
        public static void Write(string? text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }

        public static void WriteLine(string? text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void FlushStandardInput()
        {
            while (Console.KeyAvailable)
                Console.ReadKey(intercept: true);
        }
    }
}
