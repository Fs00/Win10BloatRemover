using Win10BloatRemover.Utils;
using static Win10BloatRemover.Operations.IUserInterface;

namespace Win10BloatRemover.Operations
{
    class AskForRebootOperation : IOperation
    {
        private readonly IUserInterface ui;
        private readonly RebootRecommendedFlag rebootFlag;

        public AskForRebootOperation(IUserInterface ui, RebootRecommendedFlag rebootFlag)
        {
            this.ui = ui;
            this.rebootFlag = rebootFlag;
        }

        public void Run()
        {
            if (rebootFlag.IsRebootRecommended)
            {
                ui.PrintWarning("You have executed one or more operations that require a system reboot to take full effect.");
                var choice = ui.AskUserConsent("Do you want to reboot now?");
                if (choice == UserChoice.Yes)
                    SystemUtils.RebootSystem();
            }
        }
    }
}
