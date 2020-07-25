using Win10BloatRemover.Operations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Repeat;

namespace Win10BloatRemover.Tests.Operations
{
    [Collection("ModifiesSystemState")]
    public class PrivacySettingsTweakerTests
    {
        private readonly ITestOutputHelper output;
        public PrivacySettingsTweakerTests(ITestOutputHelper output) => this.output = output;

        [Theory]
        [Repeat(2)]
        public void ShouldNotEncounterErrors(int attempt)
        {
            var ui = new TestUserInterface(output);
            var settingsTweaker = new PrivacySettingsTweaker(ui);

            settingsTweaker.Run();

            Assert.Equal(0, ui.ErrorMessagesCount);
        }
    }
}
