using Win10BloatRemover.Operations;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Tests
{
    class MockInstallWimTweak : InstallWimTweak
    {
        public void RemoveComponentIfAllowed(string component, IMessagePrinter printer)
        {
            printer.PrintNotice($"Requested removal of component {component} through install-wim-tweak.");
        }
    }

    class MockOperation : IOperation
    {
        private readonly string operationName;
        private readonly IUserInterface ui;

        public MockOperation(string operationName, IUserInterface ui)
        {
            this.operationName = operationName;
            this.ui = ui;
        }

        public void Run()
        {
            ui.PrintNotice($"Requested execution of operation {operationName}.");
        }
    }
}
