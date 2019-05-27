using System;

namespace Win10BloatRemover.Utils
{
    static class OperationUtils
    {
        /**
         *  Removes the specified component using install-wim-tweak synchronously
         *  Messages from install-wim-tweak process are printed asynchronously (as soon as they are written to stdout/stderr)
         */
        public static void RemoveComponentUsingInstallWimTweak(string component)
        {
            Console.WriteLine($"Running install-wim-tweak to remove {component}...");
            using (var installWimTweakProcess = SystemUtils.RunProcessWithAsyncOutputPrinting(Program.InstallWimTweakPath, $"/o /c {component} /r"))
            {
                installWimTweakProcess.WaitForExit();
                if (installWimTweakProcess.ExitCode == 0)
                    Console.WriteLine("Install-wim-tweak executed successfully!");
                else
                    ConsoleUtils.WriteLine($"An error occurred during the removal of {component}: " +
                                            "install-wim-tweak exited with a non-zero status.", ConsoleColor.Red);
            }
        }
    }
}
