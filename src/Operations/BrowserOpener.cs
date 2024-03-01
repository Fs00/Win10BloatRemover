using System.Diagnostics;

namespace Win10BloatRemover.Operations;

public class BrowserOpener(string url) : IOperation
{
    public void Run()
    {
        var startInfo = new ProcessStartInfo {
            FileName = url,
            UseShellExecute = true
        };
        Process.Start(startInfo);
    }
}
