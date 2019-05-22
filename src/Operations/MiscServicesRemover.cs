using System;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    class MiscServicesRemover : IOperation
    {
        public void PerformTask()
        {
            var serviceRemover = new ServiceRemover(Configuration.Instance.ServicesToRemove);
            ConsoleUtils.WriteLine("Backing up services...", ConsoleColor.Green);
            serviceRemover.PerformBackup();
            ConsoleUtils.WriteLine("Removing services...", ConsoleColor.Green);
            serviceRemover.PerformRemoval();
        }
    }
}
