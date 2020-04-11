using Win10BloatRemover.Operations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Repeat;

namespace Win10BloatRemover.Tests.Operations
{
    [Collection("ModifiesSystemState")]
    public class AutoUpdatesDisablerTests
    {
        private readonly ITestOutputHelper output;
        public AutoUpdatesDisablerTests(ITestOutputHelper output) => this.output = output;

        [Theory]
        [Repeat(2)]
        public void ShouldNotEncounterErrors(int attempt)
        {
            var ui = new TestUserInterface(output);
            var updatesDisabler = new AutoUpdatesDisabler(ui);

            updatesDisabler.Run();

            Assert.Equal(0, ui.ErrorMessagesCount);
        }
    }
}
