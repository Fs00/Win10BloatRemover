using Win10BloatRemover.Operations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Repeat;

namespace Win10BloatRemover.Tests.Operations
{
    [Collection("ModifiesSystemState")]
    public class ScheduledTaskDisablerTests
    {
        private readonly ITestOutputHelper output;
        public ScheduledTaskDisablerTests(ITestOutputHelper output) => this.output = output;

        [Theory]
        [Repeat(2)]
        public void ShouldDisableDefaultScheduledTasksWithoutErrors(int attempt)
        {
            var ui = new TestUserInterface(output);
            var tasksDisabler = new ScheduledTasksDisabler(Configuration.Default.ScheduledTasksToDisable, ui);

            tasksDisabler.Run();

            Assert.Equal(0, ui.ErrorMessagesCount);
        }
    }
}
