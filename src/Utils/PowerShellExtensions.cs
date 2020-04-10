using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell;
using Win10BloatRemover.Operations;

namespace Win10BloatRemover.Utils
{
    static class PowerShellExtensions
    {
        public static PowerShell CreateWithImportedModules(params string[] modules)
        {
            var sessionState = InitialSessionState.Create();
            sessionState.ExecutionPolicy = ExecutionPolicy.Unrestricted;
            sessionState.ImportPSModule(modules);
            return PowerShell.Create(sessionState);
        }

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
        public static void RunScriptAndPrintOutput(this PowerShell psInstance, string script, IMessagePrinter printer)
        {
            // Streams can be used by the caller to check for errors in the current script execution
            psInstance.Streams.ClearStreams();

            // By default PowerShell spawns one thread for each command, we must avoid it
            if (psInstance.Runspace.ThreadOptions != PSThreadOptions.UseCurrentThread)
                psInstance.CreateNewSingleThreadedRunspace();

            var handlers = psInstance.AddOutputStreamsEventHandlers(printer);
            psInstance.AddScript(script);
            psInstance.Invoke();
            psInstance.RemoveOutputStreamsEventHandlers(handlers);

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

        private static EventHandler<DataAddedEventArgs>[] AddOutputStreamsEventHandlers(this PowerShell psInstance, IMessagePrinter printer)
        {
            var handlers = new EventHandler<DataAddedEventArgs>[] {
                (sender, eventArgs) => printer.PrintMessage(GetMessageToPrint<InformationRecord>(sender!, eventArgs)),
                (sender, eventArgs) => printer.PrintWarning(GetMessageToPrint<ErrorRecord>(sender!, eventArgs)),
                (sender, eventArgs) => printer.PrintMessage(GetMessageToPrint<WarningRecord>(sender!, eventArgs))
            };
            psInstance.Streams.Information.DataAdded += handlers[0];
            psInstance.Streams.Error.DataAdded += handlers[1];
            psInstance.Streams.Warning.DataAdded += handlers[2];
            return handlers;
        }

        private static void RemoveOutputStreamsEventHandlers(this PowerShell psInstance, EventHandler<DataAddedEventArgs>[] handlers)
        {
            psInstance.Streams.Information.DataAdded -= handlers[0];
            psInstance.Streams.Error.DataAdded -= handlers[1];
            psInstance.Streams.Warning.DataAdded -= handlers[2];
        }

        private static string GetMessageToPrint<TRecord>(object sender, DataAddedEventArgs eventArgs)
        {
            var powerShellCollection = (PSDataCollection<TRecord>) sender;
            return powerShellCollection[eventArgs.Index]!.ToString()!;
        }
    }
}
