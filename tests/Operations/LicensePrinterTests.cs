using Win10BloatRemover.Operations;
using Xunit;
using Xunit.Abstractions;

namespace Win10BloatRemover.Tests.Operations
{
    public class LicensePrinterTests
    {
        private readonly ITestOutputHelper output;
        public LicensePrinterTests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void ShouldNotRaiseErrors()
        {
            var ui = new TestUserInterface(output);
            var licensePrinter = new LicensePrinter(ui);

            licensePrinter.Run();
        }
    }
}
