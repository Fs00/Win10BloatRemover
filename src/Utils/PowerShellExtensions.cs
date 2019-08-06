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
            // Streams can be used by the caller to check for errors in the current script execution
            psInstance.Streams.ClearStreams();

            // By default PowerShell spawns one thread for each command, we must avoid it
            if (psInstance.Runspace.ThreadOptions != PSThreadOptions.UseCurrentThread)
                psInstance.CreateNewSingleThreadedRunspace();

            psInstance.AddOutputStreamsEventHandlers();
            psInstance.AddScript(script);
            psInstance.Invoke();
            psInstance.RemoveOutputStreamsEventHandlers();

            // Clear PowerShell pipeline to avoid the script being re-executed the next time we use this instance
            psInstance.Commands.Clear();
        }

        private static void CreateNewSingleThreadedRunspace(this PowerShell psInstance)
        {
            psInstance.Runspace.Dispose();
            psInstance.Runspace = RunspaceFactory.CreateRunspace();
            psInstance.Runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;
            psInstance.Runspace.Open();
        }

        private static void AddOutputStreamsEventHandlers(this PowerShell psInstance)
        {
            psInstance.Streams.Information.DataAdded += PrintInformationString;
            psInstance.Streams.Error.DataAdded += PrintErrorString;
            psInstance.Streams.Warning.DataAdded += PrintWarningString;
        }

        private static void RemoveOutputStreamsEventHandlers(this PowerShell psInstance)
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
