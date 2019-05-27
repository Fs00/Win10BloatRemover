using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Win10BloatRemover.Utils
{
    static class ShellUtils
    {
        // These counters keep track of the number of messages written for every PowerShell stream
        // That's a workaround to avoid messages being rewritten due to DataAdded event raised more than once
        // for the same message (which is likely a bug in the API)
        private static int psInformationMessagesRead = 0,
                           psWarningMessagesRead = 0,
                           psErrorMessagesRead = 0;

        /**
        *  Runs a script on the given PowerShell instance and prints the messages written to info,
        *  error and warning streams asynchronously.
        */
        public static void RunScriptAndPrintOutput(this PowerShell psInstance, string script)
        {
            psInformationMessagesRead = 0;
            psWarningMessagesRead = 0;
            psErrorMessagesRead = 0;

            // Make our counters match the actual number of messages in the streams
            psInstance.Streams.ClearStreams();

            // Make sure that the Runspace uses the current thread to execute commands (avoids wild thread spawning)
            if (psInstance.Runspace.ThreadOptions != PSThreadOptions.UseCurrentThread)
            {
                psInstance.Runspace.Dispose();
                psInstance.Runspace = RunspaceFactory.CreateRunspace();
                psInstance.Runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;
                psInstance.Runspace.Open();
            }

            psInstance.AddScript(script);
            psInstance.Streams.Information.DataAdded += (_, evtArgs) => {
                if (evtArgs.Index >= psInformationMessagesRead)
                {
                    Console.WriteLine(psInstance.Streams.Information[evtArgs.Index].ToString());
                    psInformationMessagesRead++;
                }
            };
            psInstance.Streams.Error.DataAdded += (_, evtArgs) => {
                if (evtArgs.Index >= psErrorMessagesRead)
                {
                    ConsoleUtils.WriteLine(psInstance.Streams.Error[evtArgs.Index].ToString(), ConsoleColor.Red);
                    psErrorMessagesRead++;
                }
            };
            psInstance.Streams.Warning.DataAdded += (_, evtArgs) => {
                if (evtArgs.Index >= psWarningMessagesRead)
                {
                    ConsoleUtils.WriteLine(psInstance.Streams.Warning[evtArgs.Index].ToString(), ConsoleColor.DarkYellow);
                    psWarningMessagesRead++;
                }
            };

            psInstance.Invoke();
            // Clear PowerShell pipeline to avoid the script being re-executed the next time we use this instance
            psInstance.Commands.Clear();
        }

        public static PSVariable GetVariable(this PowerShell psInstance, string name)
        {
            return psInstance.Runspace.SessionStateProxy.PSVariable.Get(name);
        }

        public static bool IsNotEmpty(this PSVariable psVariable)
        {
            return psVariable.Value.ToString() != "";
        }

        public static void ExecuteWindowsCommand(string command)
        {
            Debug.WriteLine($"Command executed: {command}");
            using (var cmdProcess = SystemUtils.RunProcessWithAsyncOutputPrinting("cmd.exe", $"/c \"{command}\""))
                cmdProcess.WaitForExit();
        }
    }
}
