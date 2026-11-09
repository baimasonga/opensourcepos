using System.IO;
using System.Windows;
using System.Windows.Threading;
using OsposDesktop.Core;

namespace OsposDesktop;

public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\OsposDesktop.SingleInstance";

    private Mutex? singleInstanceMutex;

    public static string DataDirectory { get; } = Path.GetDirectoryName(SettingsStore.DefaultFilePath)!;

    public static SettingsStore SettingsStore { get; } = new(SettingsStore.DefaultFilePath);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnUnhandledException;

        singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var isFirstInstance);

        if (!isFirstInstance)
        {
            MessageBox.Show(
                "OSPOS Desktop is already open. Use the window that is already running.",
                "OSPOS Desktop",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Keep running while the first-run settings dialog is the only window.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var settings = SettingsStore.Load();

        if (settings.ServerUrl is null)
        {
            var dialog = new SettingsWindow(settings, isFirstRun: true);

            if (dialog.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            settings = dialog.Result;
            SettingsStore.Save(settings);
        }

        var mainWindow = new MainWindow(settings);
        MainWindow = mainWindow;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var logPath = Path.Combine(DataDirectory, "error.log");

        try
        {
            Directory.CreateDirectory(DataDirectory);
            File.AppendAllText(logPath, $"[{DateTime.Now:O}] {e.Exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // Logging must never take the till down with it.
        }

        MessageBox.Show(
            $"Something went wrong: {e.Exception.Message}\n\nDetails were saved to {logPath}.",
            "OSPOS Desktop",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }
}
