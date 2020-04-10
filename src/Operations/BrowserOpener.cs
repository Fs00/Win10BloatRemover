using System.Diagnostics;

namespace Win10BloatRemover.Operations
{
    public class BrowserOpener : IOperation
    {
        private readonly string url;

        public BrowserOpener(string url)
        {
            this.url = url;
        }

        public void Run()
        {
            var startInfo = new ProcessStartInfo {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
    }
}
