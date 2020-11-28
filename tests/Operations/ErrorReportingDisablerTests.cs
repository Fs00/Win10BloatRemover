using Win10BloatRemover.Operations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Repeat;

namespace Win10BloatRemover.Tests.Operations
{
    [Collection("ModifiesSystemState")]
    public class ErrorReportingDisablerTests
    {
        private readonly ITestOutputHelper output;
        public ErrorReportingDisablerTests(ITestOutputHelper output) => this.output = output;

        [Theory]
        [Repeat(2)]
        public void ShouldNotEncounterErrors(int attempt)
        {
            var ui = new TestUserInterface(output);
            var errorReportingDisabler = new ErrorReportingDisabler(ui, new ServiceRemover(ui));

            errorReportingDisabler.Run();

            Assert.Equal(0, ui.ErrorMessagesCount);
        }
    }
}
