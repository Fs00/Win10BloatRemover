using System;
using Win10BloatRemover.Operations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Repeat;

namespace Win10BloatRemover.Tests.Operations
{
    [Collection("ModifiesSystemState")]
    public class TelemetryDisablerTests
    {
        private readonly ITestOutputHelper output;
        public TelemetryDisablerTests(ITestOutputHelper output) => this.output = output;

        [Theory]
        [Repeat(2)]
        public void ShouldNotEncounterErrors(int attempt)
        {
            var ui = new TestUserInterface(output);
            var telemetryDisabler = new TelemetryDisabler(ui, new ServiceRemover(ui, DateTime.Now));

            telemetryDisabler.Run();

            Assert.Equal(0, ui.ErrorMessagesCount);
        }
    }
}
