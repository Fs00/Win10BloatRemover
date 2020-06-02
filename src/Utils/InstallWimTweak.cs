using System.IO;
using System.Resources;
using System.Runtime.Loader;
using Win10BloatRemover.Operations;

namespace Win10BloatRemover.Utils
{
    public interface InstallWimTweak
    {
        void RemoveComponentIfAllowed(string component, IMessagePrinter printer);
    }

    public class DisabledInstallWimTweak : InstallWimTweak
    {
        public void RemoveComponentIfAllowed(string component, IMessagePrinter printer)
        {
            printer.PrintNotice($"Skipped removal of {component} component(s) using install-wim-tweak since " +
                                @"option ""AllowInstallWimTweak"" is set to false.");
        }
    }

    public class ExtractedInstallWimTweak : InstallWimTweak
    {
        private static readonly string extractedFilePath = Path.Combine(Path.GetTempPath(), "install_wim_tweak.exe");

        private /*lateinit*/ FileStream executableFileStreamForLocking = default!;

        private bool HasBeenExtracted()
        {
            // If we didn't check for the second condition, anyone could be able to replace install_wim_tweak.exe
            // with a custom executable (and potentially escalate privileges)
            return File.Exists(extractedFilePath) && executableFileStreamForLocking != null;
        }

        private void ExtractAndLock()
        {
            var resources = new ResourceManager("Win10BloatRemover.resources.Resources", typeof(Program).Assembly);
            File.WriteAllBytes(extractedFilePath, (byte[]) resources.GetObject("install_wim_tweak")!);
            executableFileStreamForLocking = File.Open(extractedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            AssemblyLoadContext.Default.Unloading += _ => RemoveExtractedExecutable();
        }

        public void RemoveComponentIfAllowed(string component, IMessagePrinter printer)
        {
            if (!HasBeenExtracted())
                ExtractAndLock();

            printer.PrintHeading($"Running install-wim-tweak to remove {component}...");
            int exitCode = SystemUtils.RunProcessBlockingWithOutput(extractedFilePath, $"/o /c {component} /r", printer);
            if (exitCode == SystemUtils.EXIT_CODE_SUCCESS)
                printer.PrintMessage("Install-wim-tweak executed successfully!");
            else
                printer.PrintError($"An error occurred during the removal of {component}: " +
                                    "install-wim-tweak exited with a non-zero status.");
        }

        private void RemoveExtractedExecutable()
        {
            executableFileStreamForLocking.Close();
            File.Delete(extractedFilePath);
        }
    }
}
