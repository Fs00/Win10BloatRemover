using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Win10BloatRemover.UI;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover;

static class Program
{
    private const int MINIMUM_SUPPORTED_WINDOWS_BUILD = 19045;  // 22H2 build

    private static readonly RebootRecommendedFlag rebootFlag = new RebootRecommendedFlag();

    private static void Main(string[] args)
    {
        if (IsTraceOutputEnabled(args))
            Trace.Listeners.Add(new ConsoleTraceListener());

        Console.Title = "Windows 10 Bloat Remover and Tweaker";
        if (!HasAdministratorRights())
            Console.Title += " (unprivileged)";

        ShowWarningOnUnsupportedOS();

        using var registration = RegisterTerminationHandlers();
        var configuration = LoadConfigurationFromFileOrDefault();
        var menu = new ConsoleMenu(CreateMenuEntries(configuration), rebootFlag);
        menu.RunLoopUntilExitRequested();
    }

    private static bool IsTraceOutputEnabled(string[] args) => args.Contains("--show-trace-output");

    private static MenuEntry[] CreateMenuEntries(AppConfiguration configuration)
    {
        return [
            new UWPAppRemovalEntry(configuration),
            new OneDriveRemovalEntry(),
            new ServicesRemovalEntry(configuration),
            new WindowsFeaturesRemovalEntry(configuration),
            new PrivacySettingsTweakEntry(),
            new TelemetryDisablingEntry(),
            new DefenderDisablingEntry(),
            new AutoUpdatesDisablingEntry(),
            new ScheduledTasksDisablingEntry(configuration),
            new ErrorReportingDisablingEntry(),
            new ConsumerFeaturesDisablingEntry(),
            new SuggestionsDisablingEntry(),
            new NewGitHubIssueEntry(),
            new AboutEntry(),
            new QuitEntry(rebootFlag)
        ];
    }

    private static bool HasAdministratorRights()
    {
        var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void ShowWarningOnUnsupportedOS()
    {
        if (OS.IsWindows10() && OS.WindowsBuild >= MINIMUM_SUPPORTED_WINDOWS_BUILD)
            return;

        Console.Clear();
        ConsoleHelpers.WriteLine("-- UNSUPPORTED WINDOWS VERSION --\n", ConsoleColor.DarkYellow);
        if (!OS.IsWindows10())
            Console.WriteLine("This program was designed to work only on Windows 10.");
        else
        {
            Console.WriteLine(
                "You are running an older version of Windows 10 which is not supported by this version of the program.\n" +
                "You should update your system or download an older version of the program which is compatible with this\n" +
                $"Windows 10 version ({OS.GetWindowsVersionName()}) at the following page:"
            );
            ConsoleHelpers.WriteLine("  https://github.com/Fs00/Win10BloatRemover/releases/", ConsoleColor.Cyan);
        }

        Console.WriteLine(
            "\nYou can still continue using this program, but BE AWARE that some features might work badly or not at all\n" +
            "and could even have unintended effects on your system (including corruptions or instability)."
        );

        Console.WriteLine("\nPress enter to continue, or another key to quit.");
        if (Console.ReadKey().Key != ConsoleKey.Enter)
            Environment.Exit(0);
    }
    
    private static AppConfiguration LoadConfigurationFromFileOrDefault()
    {
        try
        {
            return AppConfiguration.LoadOrCreateFile();
        }
        catch (AppConfigurationException exc)
        {
            DisplayConfigurationErrorMessage(exc);
            return AppConfiguration.Default;
        }
    }

    private static void DisplayConfigurationErrorMessage(AppConfigurationException exc)
    {
        Console.Clear();
        if (exc is AppConfigurationLoadException)
        {
            ConsoleHelpers.WriteLine($"An error occurred while loading settings file:\n{exc.Message}\n", ConsoleColor.Red);
            Console.WriteLine("Default settings have been loaded instead.\n");
        }
        else if (exc is AppConfigurationWriteException)
            ConsoleHelpers.WriteLine($"Default settings file could not be created: {exc.Message}\n", ConsoleColor.DarkYellow);

        Console.WriteLine("Press a key to continue to the main menu.");
        Console.ReadKey();
    }

    private static PosixSignalRegistration RegisterTerminationHandlers()
    {
        bool cancelKeyPressedOnce = false;
        Console.CancelKeyPress += (_, args) => {
            if (cancelKeyPressedOnce)
                Environment.Exit(1); // ensures that ProcessExit events are executed
            else
            {
                ConsoleHelpers.WriteLine("Press Ctrl+C again to terminate the program.", ConsoleColor.Red);
                cancelKeyPressedOnce = true;
                args.Cancel = true;
            }
        };
        // Fired when the user closes the console window (CTRL_CLOSE_EVENT)
        return PosixSignalRegistration.Create(PosixSignal.SIGHUP, _ => Environment.Exit(1));
    }
}
