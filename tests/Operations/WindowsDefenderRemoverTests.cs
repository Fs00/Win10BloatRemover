using Win10BloatRemover.Operations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Repeat;

namespace Win10BloatRemover.Tests.Operations
{
    [Collection("ModifiesSystemState")]
    public class WindowsDefenderRemoverTests
    {
        private readonly ITestOutputHelper output;
        public WindowsDefenderRemoverTests(ITestOutputHelper output) => this.output = output;

        [Theory]
        [Repeat(2)]
        public void ShouldNotEncounterErrors(int attempt)
        {
            var ui = new TestUserInterface(output);
            var defenderRemover = new WindowsDefenderRemover(ui, new MockInstallWimTweak(), new MockOperation("UWPAppRemover", ui));

            defenderRemover.Run();

            Assert.Equal(0, ui.ErrorMessagesCount);
        }
    }
}
