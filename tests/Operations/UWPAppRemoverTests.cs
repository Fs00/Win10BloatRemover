using System.Linq;
using Win10BloatRemover.Operations;
using Win10BloatRemover.Utils;
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
            // UWPAppGroup.Maps,  // excluded because there is a test dedicated to it
            UWPAppGroup.Messaging,
            UWPAppGroup.MixedReality,
            UWPAppGroup.OfficeHub,
            UWPAppGroup.OneNote,
            UWPAppGroup.Paint3D,
            UWPAppGroup.Photos,
            UWPAppGroup.Skype,
            UWPAppGroup.SnipAndSketch,
            UWPAppGroup.SolitaireCollection,
            UWPAppGroup.SoundRecorder,
            UWPAppGroup.StickyNotes,
            UWPAppGroup.Store,
            UWPAppGroup.Zune
        };

        [Fact]
        public void ShouldRemoveAnAppForCurrentUser_WithoutRemovingItsProvisionedPackage()
        {
            var ui = new TestUserInterface(output);
            var appRemover = new UWPAppRemover(new[] { UWPAppGroup.Maps }, UWPAppRemovalMode.CurrentUser, ui, new MockInstallWimTweak());

            appRemover.Run();

            var mapsProvisionedPackage = GetProvisionedPackage("Microsoft.WindowsMaps");
            Assert.NotNull(mapsProvisionedPackage);
        }

        private dynamic? GetProvisionedPackage(string provisionedPackageName)
        {
            using var powerShell = PowerShellExtensions.CreateWithImportedModules("AppX");
            return powerShell.Run("Get-AppxProvisionedPackage -Online")
                .FirstOrDefault(package => package.DisplayName == provisionedPackageName);
        }

        [Theory]
        [Repeat(2)]
        public void ShouldRemoveGroupsWithNoSystemAppsWithoutErrors(int attempt)
        {
            var ui = new TestUserInterface(output);
            var appRemover = new UWPAppRemover(groupsWithNoSystemApps, UWPAppRemovalMode.AllUsers, ui, new MockInstallWimTweak());

            appRemover.Run();

            Assert.Equal(0, ui.ErrorMessagesCount);
        }
    }
}
