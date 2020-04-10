namespace Win10BloatRemover.Operations
{
    public interface IMessagePrinter
    {
        void PrintMessage(string text);
        void PrintError(string text);
        void PrintWarning(string text);
        void PrintNotice(string text);
        void PrintHeading(string text);
        void PrintSubHeading(string text);
        void PrintEmptySpace();
    }

    public interface IUserInterface : IMessagePrinter
    {
        public enum UserChoice
        {
            Yes,
            No
        }

        UserChoice AskUserConsent(string text);
    }
}
