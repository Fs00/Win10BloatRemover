using System.Diagnostics;

namespace Win10BloatRemover.Operations
{
    class BrowserOpener : IOperation
    {
        private static string GITHUB_NEW_ISSUE_URL = "https://github.com/Fs00/Win10BloatRemover/issues/new";

        public void PerformTask()
        {
            Process.Start(GITHUB_NEW_ISSUE_URL);
        }
    }
}
