using Win10BloatRemover.Operations;
using Xunit;
using Xunit.Abstractions;

namespace Win10BloatRemover.Tests.Operations
{
    [Collection("ModifiesSystemState")]
    public class OneDriveRemoverTests
    {
        private readonly ITestOutputHelper output;
        public OneDriveRemoverTests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void ShouldNotEncounterErrors()
        {
            var ui = new TestUserInterface(output);
            var oneDriveRemover = new OneDriveRemover(ui);

            oneDriveRemover.Run();

            Assert.Equal(0, ui.ErrorMessagesCount);
        }
    }
}
