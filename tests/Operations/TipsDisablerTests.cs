using Win10BloatRemover.Operations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Repeat;

namespace Win10BloatRemover.Tests.Operations
{
    [Collection("ModifiesSystemState")]
    public class TipsDisablerTests
    {
        private readonly ITestOutputHelper output;
        public TipsDisablerTests(ITestOutputHelper output) => this.output = output;

        [Theory]
        [Repeat(2)]
        public void ShouldNotEncounterErrors(int attempt)
        {
            var ui = new TestUserInterface(output);
            var tipsDisabler = new TipsDisabler(ui);

            tipsDisabler.Run();

            Assert.Equal(0, ui.ErrorMessagesCount);
        }
    }
}
