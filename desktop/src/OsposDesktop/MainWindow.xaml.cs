using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using OsposDesktop.Core;

namespace OsposDesktop;

public partial class MainWindow : Window
{
    private const string WebView2DownloadUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

    private AppSettings settings;
    private NavigationPolicy navigationPolicy;
    private CoreWebView2Environment? environment;
    private string? printBridgeScriptId;
    private bool hasLoadedServerPage;
    private WindowState stateBeforeFullScreen = WindowState.Normal;

    public MainWindow(AppSettings settings)
    {
        InitializeComponent();

        this.settings = settings;
        navigationPolicy = new NavigationPolicy(new Uri(settings.ServerUrl!));

        Loaded += async (_, _) => await InitializeWebViewAsync();
        PreviewKeyDown += (_, e) => e.Handled = HandleShortcut(e.Key, Keyboard.Modifiers);

        if (settings.StartFullScreen)
        {
            SetFullScreen(true);
        }
    }

    private bool IsFullScreen => WindowStyle == WindowStyle.None;

    private async Task InitializeWebViewAsync()
    {
        try
        {
            environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: Path.Combine(App.DataDirectory, "WebView2"));

            await WebView.EnsureCoreWebView2Async(environment);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            var answer = MessageBox.Show(
                "OSPOS Desktop needs the Microsoft Edge WebView2 Runtime, which is not installed on this computer.\n\n"
                + "Open the download page now?",
                "OSPOS Desktop",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (answer == MessageBoxResult.Yes)
            {
                OpenExternally(WebView2DownloadUrl);
            }

            Close();
            return;
        }

        var core = WebView.CoreWebView2;

        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;
#if !DEBUG
        core.Settings.AreDevToolsEnabled = false;
#endif

        core.NavigationStarting += OnNavigationStarting;
        core.NavigationCompleted += OnNavigationCompleted;
        core.NewWindowRequested += OnNewWindowRequested;
        core.DocumentTitleChanged += (_, _) => Title = string.IsNullOrWhiteSpace(core.DocumentTitle)
            ? "OSPOS Desktop"
            : core.DocumentTitle;
        core.WebMessageReceived += OnWebMessageReceived;

        WebView.ZoomFactor = settings.ZoomFactor;
        WebView.ZoomFactorChanged += (_, _) => SaveSettings(settings with { ZoomFactor = WebView.ZoomFactor });

        await ApplyPrintBridgeAsync();
        NavigateHome();
    }

    private void NavigateHome()
    {
        ShowStatus("Connecting to OSPOS…", navigationPolicy.ServerAddress.AbsoluteUri, showActions: false);
        WebView.CoreWebView2.Navigate(navigationPolicy.ServerAddress.AbsoluteUri);
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        switch (navigationPolicy.Decide(e.Uri))
        {
            case NavigationDecision.Allow:
                return;
            case NavigationDecision.OpenExternally:
                e.Cancel = true;
                OpenExternally(e.Uri);
                return;
            default:
                e.Cancel = true;
                return;
        }
    }

    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        // Keep everything in the one till window: server pages (receipts, invoices) open in place,
        // anything else goes to the default browser.
        e.Handled = true;

        switch (navigationPolicy.Decide(e.Uri))
        {
            case NavigationDecision.Allow:
                WebView.CoreWebView2.Navigate(e.Uri);
                break;
            case NavigationDecision.OpenExternally:
                OpenExternally(e.Uri);
                break;
        }
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            hasLoadedServerPage = true;
            StatusOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        if (e.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled)
        {
            return;
        }

        ShowStatus(
            hasLoadedServerPage ? "Lost connection to OSPOS" : "Cannot reach OSPOS",
            $"{navigationPolicy.ServerAddress.AbsoluteUri} did not respond ({e.WebErrorStatus}). "
            + "Check that the server is switched on and this computer is on the same network, then try again.",
            showActions: true);
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (PrintBridge.IsPrintRequest(e.WebMessageAsJson))
        {
            await PrintSilentlyAsync();
        }
    }

    private async Task PrintSilentlyAsync()
    {
        var printSettings = environment!.CreatePrintSettings();
        var marginInches = PrintBridge.MillimetresToInches(settings.ReceiptMarginMillimetres);

        printSettings.ShouldPrintHeaderAndFooter = false;
        printSettings.ShouldPrintBackgrounds = true;
        printSettings.MarginTop = marginInches;
        printSettings.MarginBottom = marginInches;
        printSettings.MarginLeft = marginInches;
        printSettings.MarginRight = marginInches;

        if (settings.ReceiptPrinter is not null)
        {
            printSettings.PrinterName = settings.ReceiptPrinter;
        }

        var status = await WebView.CoreWebView2.PrintAsync(printSettings);

        if (status != CoreWebView2PrintStatus.Succeeded)
        {
            MessageBox.Show(
                status == CoreWebView2PrintStatus.PrinterUnavailable
                    ? $"The receipt printer \"{settings.ReceiptPrinter ?? "default"}\" is not available. Choose the printer to use."
                    : "The receipt could not be printed silently. Choose the printer to use.",
                "OSPOS Desktop",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            WebView.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);
        }
    }

    private async Task ApplyPrintBridgeAsync()
    {
        if (printBridgeScriptId is not null)
        {
            WebView.CoreWebView2.RemoveScriptToExecuteOnDocumentCreated(printBridgeScriptId);
            printBridgeScriptId = null;
        }

        if (settings.SilentPrinting)
        {
            printBridgeScriptId = await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(PrintBridge.Script);
        }
    }

    private bool HandleShortcut(Key key, ModifierKeys modifiers)
    {
        switch (key)
        {
            case Key.F11:
                SetFullScreen(!IsFullScreen);
                return true;
            case Key.OemComma when modifiers == ModifierKeys.Control:
                OpenSettings();
                return true;
            case Key.D0 or Key.NumPad0 when modifiers == ModifierKeys.Control && WebView.CoreWebView2 is not null:
                WebView.ZoomFactor = 1.0;
                return true;
            default:
                return false;
        }
    }

    private void SetFullScreen(bool fullScreen)
    {
        if (fullScreen == IsFullScreen)
        {
            return;
        }

        if (fullScreen)
        {
            stateBeforeFullScreen = WindowState;
            WindowState = WindowState.Normal; // Re-maximizing after the style change covers the taskbar.
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            WindowState = stateBeforeFullScreen;
        }
    }

    private async void OpenSettings()
    {
        var dialog = new SettingsWindow(settings, isFirstRun: false) { Owner = this };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var serverChanged = dialog.Result.ServerUrl != settings.ServerUrl;

        SaveSettings(dialog.Result);
        navigationPolicy = new NavigationPolicy(new Uri(settings.ServerUrl!));
        SetFullScreen(settings.StartFullScreen);

        if (WebView.CoreWebView2 is null)
        {
            return;
        }

        await ApplyPrintBridgeAsync();

        if (serverChanged)
        {
            hasLoadedServerPage = false;
            NavigateHome();
        }
        else
        {
            WebView.CoreWebView2.Reload();
        }
    }

    private void SaveSettings(AppSettings updated)
    {
        settings = updated.Sanitized();
        App.SettingsStore.Save(settings);
    }

    private void ShowStatus(string heading, string message, bool showActions)
    {
        StatusHeading.Text = heading;
        StatusMessage.Text = message;
        StatusActions.Visibility = showActions ? Visibility.Visible : Visibility.Collapsed;
        StatusOverlay.Visibility = Visibility.Visible;
    }

    private void OnRetryClick(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2 is not null)
        {
            NavigateHome();
        }
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e) => OpenSettings();

    private static void OpenExternally(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Win32Exception)
        {
            // No handler registered for this link type: nothing more useful to do at a till.
        }
    }
}
