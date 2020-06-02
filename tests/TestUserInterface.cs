using System;
using System.IO;
using System.Runtime.Loader;
using Win10BloatRemover.Operations;
using Xunit.Abstractions;

namespace Win10BloatRemover.Tests
{
    class TestUserInterface : IUserInterface
    {
        private readonly ITestOutputHelper output;
        private readonly StreamWriter testLog;

        public int ErrorMessagesCount { private set; get; } = 0;

        public TestUserInterface(ITestOutputHelper output)
        {
            this.output = output;
            testLog = File.CreateText($"testRun_{DateTime.Now.ToFileTime()}.log");
            AssemblyLoadContext.Default.Unloading += _ => testLog.Close();
        }

        public void PrintMessage(string text) => WriteText(text);

        public void PrintError(string text)
        {
            ErrorMessagesCount++;
            WriteText($"ERROR: {text}");
        }

        public void PrintWarning(string text) => WriteText($"WARN: {text}");

        public void PrintNotice(string text) => WriteText($"NOTICE: {text}");

        public void PrintHeading(string text) => WriteText($"--- {text.ToUpper()} ---");

        public void PrintSubHeading(string text) => WriteText($"-- {text} --");

        private void WriteText(string text)
        {
            testLog.WriteLine(text);
            output.WriteLine(text);
        }

        public IUserInterface.UserChoice AskUserConsent(string text)
        {
            WriteText($@"PROMPT ""{text}"" ACCEPTED.");
            return IUserInterface.UserChoice.Yes;
        }
    }
}
