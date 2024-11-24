using System.IO;
using Win10BloatRemover.UI;

namespace Win10BloatRemover.Operations;

class LicensePrinter(IUserInterface ui) : IOperation
{
    public void Run()
    {
        Stream licenseFile = GetType().Assembly.GetManifestResourceStream("Win10BloatRemover.Resources.License.txt")!;
        using var licenseFileStream = new StreamReader(licenseFile);
        string licenseText = licenseFileStream.ReadToEnd();
        ui.PrintNotice(licenseText);
    }
}
