using System;
using System.IO;
using System.Resources;
using System.Security.Principal;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover
{
    static class Program
    {
        public const string SUPPORTED_WINDOWS_RELEASE_ID = "1903";
        public static string InstallWimTweakPath { get; } = Path.Combine(Path.GetTempPath(), "install_wim_tweak.exe");

        private static void Main()
        {
            Console.Title = "Windows 10 Bloat Remover and Tweaker";

            EnsurePreliminaryChecksAreSuccessful();
            TryLoadConfiguration();

            if (Configuration.Instance.AllowInstallWimTweak)
                TryExtractInstallWimTweak();

            Menu.RunLoopUntilExitRequested();

            TryDeleteExtractedInstallWimTweak();
        }

        private static void EnsurePreliminaryChecksAreSuccessful()
        {
            if (!Program.HasAdministratorRights())
            {
                ConsoleUtils.WriteLine("This application needs to be run with administrator rights!", ConsoleColor.Red);
                Console.ReadKey();
                Environment.Exit(-1);
            }

            #if !DEBUG
            if (!SystemUtils.IsWindowsReleaseId(SUPPORTED_WINDOWS_RELEASE_ID))
            {
                ConsoleUtils.WriteLine("This application is compatible only with Windows 10 May 2019 Update!", ConsoleColor.Red);
                Console.ReadKey();
                Environment.Exit(-1);
            }
            #endif
        }

        private static bool HasAdministratorRights()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        
        private static void TryLoadConfiguration()
        {
            try
            {
                Configuration.Load();
            }
            catch (ConfigurationException exc)
            {
                ConsoleUtils.WriteLine(exc.Message, ConsoleColor.DarkYellow);
                Console.WriteLine("Press a key to continue to the main menu.");
                Console.ReadKey();
            }
        }

        private static void TryExtractInstallWimTweak()
        {
            try
            {
                var resources = new ResourceManager("Win10BloatRemover.resources.Resources", typeof(Program).Assembly);
                File.WriteAllBytes(InstallWimTweakPath, (byte[]) resources.GetObject("install_wim_tweak"));
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Unable to extract install_wim_tweak tool to your temporary directory: {exc.Message}");
                Console.WriteLine("The application will exit.");
                Console.ReadKey();
                Environment.Exit(-1);
            }
        }

        private static void TryDeleteExtractedInstallWimTweak()
        {
            try
            {
                if (File.Exists(InstallWimTweakPath))
                    File.Delete(InstallWimTweakPath);
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Unable to delete previously extracted install-wim-tweak binary: {exc.Message}");
                Console.ReadKey();
            }
        }
    }
}
