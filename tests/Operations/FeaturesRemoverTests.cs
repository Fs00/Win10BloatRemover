using Win10BloatRemover.Operations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Repeat;

namespace Win10BloatRemover.Tests.Operations
{
    [Collection("ModifiesSystemState")]
    public class FeaturesRemoverTests
    {
        private readonly ITestOutputHelper output;
        public FeaturesRemoverTests(ITestOutputHelper output) => this.output = output;

        [Theory]
        [Repeat(2)]
        public void ShouldNotEncounterErrors_WhenRemovingDefaultFeatures(int attempt)
        {
            var ui = new TestUserInterface(output);
            var featuresRemover = new FeaturesRemover(Configuration.Default.WindowsFeaturesToRemove, ui);

            featuresRemover.Run();

            Assert.Equal(0, ui.ErrorMessagesCount);
        }
    }
}
