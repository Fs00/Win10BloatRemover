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
            var sessionState = InitialSessionState.CreateDefault2();
            sessionState.ThreadOptions = PSThreadOptions.UseCurrentThread;
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
        public static void RunScript(this PowerShell psInstance, string script)
        {
            // Streams can be used by the caller to check for errors in the current script execution
            psInstance.Streams.ClearStreams();

            psInstance.AddScript(script);
            psInstance.Invoke();

            // Clear PowerShell pipeline to avoid the script being re-executed the next time we use this instance
            psInstance.Commands.Clear();
        }

        public static PowerShell WithOutput(this PowerShell psInstance, IMessagePrinter printer)
        {
            psInstance.Streams.Information.DataAdded +=
                (stream, eventArgs) => printer.PrintMessage(GetMessageToPrint<InformationRecord>(stream!, eventArgs));
            psInstance.Streams.Error.DataAdded +=
                (stream, eventArgs) => printer.PrintError(GetMessageToPrint<ErrorRecord>(stream!, eventArgs));
            psInstance.Streams.Warning.DataAdded +=
                (stream, eventArgs) => printer.PrintWarning(GetMessageToPrint<WarningRecord>(stream!, eventArgs));
            return psInstance;
        }

        private static string GetMessageToPrint<TRecord>(object psStream, DataAddedEventArgs eventArgs)
        {
            var powerShellCollection = (PSDataCollection<TRecord>) psStream;
            return powerShellCollection[eventArgs.Index]!.ToString()!;
        }
    }
}
