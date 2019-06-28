using System;
using Win10BloatRemover.Utils;
using Env = System.Environment;

namespace Win10BloatRemover.Operations
{
    class EdgeRemover : IOperation
    {
        public void PerformTask()
        {
            OperationUtils.RemoveComponentUsingInstallWimTweak("Microsoft-Windows-Internet-Browser");

            Console.WriteLine("Removing old files...");
            SystemUtils.DeleteDirectoryIfExists(
                $@"{Env.GetFolderPath(Env.SpecialFolder.UserProfile)}\MicrosoftEdgeBackups",
                handleErrors: true
            );

            Console.WriteLine("A system reboot is recommended.");
        }
    }
}
