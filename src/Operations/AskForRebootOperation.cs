using Win10BloatRemover.UI;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations;

class AskForRebootOperation(IUserInterface ui, RebootRecommendedFlag rebootFlag) : IOperation
{
    public void Run()
    {
        if (rebootFlag.IsRebootRecommended)
        {
            ui.PrintWarning("You have executed one or more operations that require a system reboot to take full effect.");
            var choice = ui.AskUserConsent("Do you want to reboot now?");
            if (choice == UserChoice.Yes)
                OS.RebootPC();
        }
    }
}
