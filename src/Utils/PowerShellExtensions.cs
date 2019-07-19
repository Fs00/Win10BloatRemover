using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Win10BloatRemover.Utils
{
    static class PowerShellExtensions
    {
        public static PSVariable GetVariable(this PowerShell psInstance, string name)
        {
            return psInstance.Runspace.SessionStateProxy.PSVariable.Get(name);
        }

        public static bool IsNotEmpty(this PSVariable psVariable)
        {
            return psVariable.Value.ToString() != "";
        }

        /*
         *  Runs a script on the given PowerShell instance and prints the messages written to info,
         *  error and warning streams asynchronously.
         */
        public static void RunScriptAndPrintOutput(this PowerShell psInstance, string script)
        {
            psInstance.Streams.ClearStreams();

            // Make sure that the Runspace uses the current thread to execute commands (avoids wild thread spawning)
            if (psInstance.Runspace.ThreadOptions != PSThreadOptions.UseCurrentThread)
            {
                psInstance.Runspace.Dispose();
                psInstance.Runspace = RunspaceFactory.CreateRunspace();
                psInstance.Runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;
                psInstance.Runspace.Open();
            }

            AddOutputStreamsEventHandlers(psInstance);

            psInstance.AddScript(script);
            psInstance.Invoke();

            RemoveOutputStreamsEventHandlers(psInstance);
            // Clear PowerShell pipeline to avoid the script being re-executed the next time we use this instance
            psInstance.Commands.Clear();
        }

        private static void AddOutputStreamsEventHandlers(PowerShell psInstance)
        {
            psInstance.Streams.Information.DataAdded += PrintInformationString;
            psInstance.Streams.Error.DataAdded += PrintErrorString;
            psInstance.Streams.Warning.DataAdded += PrintWarningString;
        }

        private static void RemoveOutputStreamsEventHandlers(PowerShell psInstance)
        {
            psInstance.Streams.Information.DataAdded -= PrintInformationString;
            psInstance.Streams.Error.DataAdded -= PrintErrorString;
            psInstance.Streams.Warning.DataAdded -= PrintWarningString;
        }

        private static void PrintInformationString(object sender, DataAddedEventArgs eventArgs)
        {
            var powerShellStream = (PSDataCollection<InformationRecord>) sender;
            Console.WriteLine(powerShellStream[eventArgs.Index].ToString());
        }

        private static void PrintErrorString(object sender, DataAddedEventArgs eventArgs)
        {
            var powerShellStream = (PSDataCollection<ErrorRecord>) sender;
            ConsoleUtils.WriteLine(powerShellStream[eventArgs.Index].ToString(), ConsoleColor.Red);
        }

        private static void PrintWarningString(object sender, DataAddedEventArgs eventArgs)
        {
            var powerShellStream = (PSDataCollection<WarningRecord>) sender;
            ConsoleUtils.WriteLine(powerShellStream[eventArgs.Index].ToString(), ConsoleColor.DarkYellow);
        }
    }
}
