using System.IO;

namespace Win10BloatRemover.Operations
{
    public class LicensePrinter : IOperation
    {
        private readonly IUserInterface ui;
        public LicensePrinter(IUserInterface ui) => this.ui = ui;

        public void Run()
        {
            Stream licenseFile = GetType().Assembly.GetManifestResourceStream("Win10BloatRemover.Resources.License.txt")!;
            string licenseText = new StreamReader(licenseFile).ReadToEnd();
            ui.PrintNotice(licenseText);
        }
    }
}
