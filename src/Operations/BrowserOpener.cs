using System.Diagnostics;

namespace Win10BloatRemover.Operations
{
    class BrowserOpener : IOperation
    {
        private readonly string url;

        public BrowserOpener(string url)
        {
            this.url = url;
        }

        public void PerformTask()
        {
            Process.Start(url);
        }
    }
}
