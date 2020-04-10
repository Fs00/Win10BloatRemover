using System.Resources;

namespace Win10BloatRemover.Operations
{
    public class LicensePrinter : IOperation
    {
        private readonly IUserInterface ui;
        public LicensePrinter(IUserInterface ui) => this.ui = ui;

        public void Run()
        {
            var resources = new ResourceManager("Win10BloatRemover.resources.Resources", typeof(Program).Assembly);
            ui.PrintNotice(resources.GetString("LicenseText")!);
        }
    }
}
