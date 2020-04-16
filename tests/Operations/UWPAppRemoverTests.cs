using Win10BloatRemover.Operations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Repeat;

namespace Win10BloatRemover.Tests.Operations
{
    [Collection("ModifiesSystemState")]
    public class UWPAppRemoverTests
    {
        private readonly ITestOutputHelper output;
        public UWPAppRemoverTests(ITestOutputHelper output) => this.output = output;

        private readonly UWPAppGroup[] groupsWithNoSystemApps = {
            UWPAppGroup.AlarmsAndClock,
            UWPAppGroup.Bing,
            UWPAppGroup.Calculator,
            UWPAppGroup.Camera,
            UWPAppGroup.CommunicationsApps,
            UWPAppGroup.HelpAndFeedback,
            UWPAppGroup.Maps,
            UWPAppGroup.Messaging,
            UWPAppGroup.MixedReality,
            UWPAppGroup.OfficeHub,
            UWPAppGroup.OneNote,
            UWPAppGroup.Paint3D,
            UWPAppGroup.Photos,
            UWPAppGroup.Skype,
            UWPAppGroup.SnipAndSketch,
            UWPAppGroup.SolitaireCollection,
            UWPAppGroup.StickyNotes,
            UWPAppGroup.Store,
            UWPAppGroup.Zune
        };

        [Theory]
        [Repeat(2)]
        public void ShouldRemoveGroupsWithNoSystemAppsWithoutErrors(int attempt)
        {
            var ui = new TestUserInterface(output);
            var appRemover = new UWPAppRemover(groupsWithNoSystemApps, UWPAppRemovalMode.KeepProvisionedPackages, ui, new MockInstallWimTweak());

            appRemover.Run();

            Assert.Equal(0, ui.ErrorMessagesCount);
        }
    }
}
