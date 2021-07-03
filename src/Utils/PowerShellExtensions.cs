using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell;
using Win10BloatRemover.Operations;

namespace Win10BloatRemover.Utils
{
    public static class PowerShellExtensions
    {
        public static PowerShell CreateWithImportedModules(params string[] modules)
        {
            Environment.SetEnvironmentVariable("PSModuleAutoLoadingPreference", "None");
            Environment.SetEnvironmentVariable("POWERSHELL_TELEMETRY_OPTOUT", "true");
            var sessionState = InitialSessionState.CreateDefault2();
            sessionState.ThreadOptions = PSThreadOptions.UseCurrentThread;
            sessionState.ExecutionPolicy = ExecutionPolicy.Unrestricted;
            sessionState.ImportPSModule(modules);
            return PowerShell.Create(sessionState);
        }

        public static dynamic[] Run(this PowerShell powerShell, string script)
        {
            // Since streams are used to check for errors in the current script execution,
            // they must not contain anything from previous executions
            powerShell.Streams.ClearStreams();

            powerShell.AddScript(script);
            Collection<PSObject> results = powerShell.Invoke();

            // Clear pipeline to avoid the script being re-executed the next time we use this instance
            powerShell.Commands.Clear();

            return UnwrapCommandResults(results);
        }

        private static dynamic[] UnwrapCommandResults(Collection<PSObject> results)
        {
            return results.Select(psObject => psObject.BaseObject).ToArray();
        }

        public static PowerShell WithOutput(this PowerShell powerShell, IMessagePrinter printer)
        {
            powerShell.Streams.Information.DataAdded +=
                (stream, eventArgs) => printer.PrintMessage(GetMessageToPrint<InformationRecord>(stream!, eventArgs));
            powerShell.Streams.Error.DataAdded +=
                (stream, eventArgs) => printer.PrintError(GetMessageToPrint<ErrorRecord>(stream!, eventArgs));
            powerShell.Streams.Warning.DataAdded +=
                (stream, eventArgs) => printer.PrintWarning(GetMessageToPrint<WarningRecord>(stream!, eventArgs));
            return powerShell;
        }

        private static string GetMessageToPrint<TRecord>(object psStream, DataAddedEventArgs eventArgs)
        {
            var powerShellCollection = (PSDataCollection<TRecord>) psStream;
            return powerShellCollection[eventArgs.Index]!.ToString()!;
        }
    }
}
