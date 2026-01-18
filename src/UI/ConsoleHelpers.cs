namespace Win10BloatRemover.UI;

static class ConsoleHelpers
{
    extension(Console)
    {
        public static void WriteColored(string? text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }

        public static void WriteLineColored(string? text, ConsoleColor color)
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

    public static string BuildIndentedList<T>(IEnumerable<T> items)
    {
        return string.Join("\n", items.Select(item => $"    {item}"));
    }
}
