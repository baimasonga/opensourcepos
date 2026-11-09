using System.Globalization;
using System.Net.Http;
using System.Printing;
using System.Windows;
using System.Windows.Media;
using OsposDesktop.Core;

namespace OsposDesktop;

public partial class SettingsWindow : Window
{
    private const string DefaultPrinterLabel = "(Windows default printer)";

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    private readonly AppSettings original;

    public SettingsWindow(AppSettings settings, bool isFirstRun)
    {
        InitializeComponent();

        original = settings;
        Result = settings;

        if (isFirstRun)
        {
            HeadingText.Text = "Welcome to OSPOS Desktop";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        ServerUrlBox.Text = settings.ServerUrl ?? string.Empty;
        SilentPrintingBox.IsChecked = settings.SilentPrinting;
        FullScreenBox.IsChecked = settings.StartFullScreen;
        MarginBox.Text = settings.ReceiptMarginMillimetres.ToString(CultureInfo.CurrentCulture);

        LoadPrinters(settings.ReceiptPrinter);
        UpdatePrinterPanel();

        Loaded += (_, _) => ServerUrlBox.Focus();
    }

    public AppSettings Result { get; private set; }

    private void LoadPrinters(string? selectedPrinter)
    {
        PrinterBox.Items.Add(DefaultPrinterLabel);

        try
        {
            using var printServer = new LocalPrintServer();
            var queues = printServer.GetPrintQueues([EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections]);

            foreach (var name in queues.Select(queue => queue.FullName).Order(StringComparer.CurrentCultureIgnoreCase))
            {
                PrinterBox.Items.Add(name);
            }
        }
        catch (PrintSystemException)
        {
            // The spooler may be stopped; the default printer remains selectable.
        }

        // Keep a saved printer listed even when it is currently disconnected.
        if (selectedPrinter is not null && !PrinterBox.Items.Contains(selectedPrinter))
        {
            PrinterBox.Items.Add(selectedPrinter);
        }

        PrinterBox.SelectedItem = selectedPrinter ?? DefaultPrinterLabel;
    }

    private void OnSilentPrintingChanged(object sender, RoutedEventArgs e) => UpdatePrinterPanel();

    private void UpdatePrinterPanel()
    {
        if (PrinterPanel is not null)
        {
            PrinterPanel.IsEnabled = SilentPrintingBox.IsChecked == true;
        }
    }

    private void OnServerUrlChanged(object sender, RoutedEventArgs e) => HideError();

    private async void OnTestClick(object sender, RoutedEventArgs e)
    {
        if (!ServerAddress.TryNormalize(ServerUrlBox.Text, out var address, out var error))
        {
            ShowError(error!);
            return;
        }

        TestButton.IsEnabled = false;
        ShowMessage($"Contacting {address}…", isError: false);

        try
        {
            using var response = await HttpClient.GetAsync(new Uri(address!, "login"));

            if (response.IsSuccessStatusCode)
            {
                ShowMessage("Connected. OSPOS answered at this address.", isError: false);
            }
            else
            {
                ShowError($"The server answered but OSPOS was not found there (HTTP {(int)response.StatusCode}). Check the address.");
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ShowError($"Could not reach {address}. Check the server is on and the address is correct.");
        }
        finally
        {
            TestButton.IsEnabled = true;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (!ServerAddress.TryNormalize(ServerUrlBox.Text, out var address, out var error))
        {
            ShowError(error!);
            ServerUrlBox.Focus();
            return;
        }

        if (!double.TryParse(MarginBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var margin) || margin < 0 || margin > 50)
        {
            ShowError("Enter a page margin between 0 and 50 millimetres.");
            MarginBox.Focus();
            return;
        }

        var printer = PrinterBox.SelectedItem as string;

        Result = (original with
        {
            ServerUrl = address!.AbsoluteUri,
            SilentPrinting = SilentPrintingBox.IsChecked == true,
            ReceiptPrinter = printer == DefaultPrinterLabel ? null : printer,
            ReceiptMarginMillimetres = margin,
            StartFullScreen = FullScreenBox.IsChecked == true,
        }).Sanitized();

        DialogResult = true;
    }

    private void ShowError(string message) => ShowMessage(message, isError: true);

    private void ShowMessage(string message, bool isError)
    {
        ErrorText.Text = message;
        ErrorText.Foreground = isError
            ? (Brush)FindResource("DangerBrush")
            : (Brush)FindResource("MutedTextBrush");
        ErrorText.Visibility = Visibility.Visible;
    }

    private void HideError() => ErrorText.Visibility = Visibility.Collapsed;
}
