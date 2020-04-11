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
}
