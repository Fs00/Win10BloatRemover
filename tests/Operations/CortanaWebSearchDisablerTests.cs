using Win10BloatRemover.Operations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Repeat;

namespace Win10BloatRemover.Tests.Operations
{
    [Collection("ModifiesSystemState")]
    public class CortanaWebSearchDisablerTests
    {
        private readonly ITestOutputHelper output;
        public CortanaWebSearchDisablerTests(ITestOutputHelper output) => this.output = output;

        [Theory]
        [Repeat(2)]
        public void ShouldNotEncounterErrors(int attempt)
        {
            var ui = new TestUserInterface(output);
            var cortanaDisabler = new CortanaWebSearchDisabler(ui);

            cortanaDisabler.Run();

            Assert.Equal(0, ui.ErrorMessagesCount);
        }
    }
}
