using System.Text.Json;

namespace OsposDesktop.Core;

/// <summary>
/// Lets OSPOS pages print silently. With silent printing on, the host injects
/// <see cref="Script"/> into every page; it replaces window.print() with a message to the host,
/// which then prints the page to the configured receipt printer without a dialog.
/// </summary>
public static class PrintBridge
{
    public const string PrintMessageType = "ospos-desktop:print";

    public static readonly string Script = $$"""
        (() => {
            if (!window.chrome || !window.chrome.webview) {
                return;
            }
            window.print = () => window.chrome.webview.postMessage({ type: '{{PrintMessageType}}' });
        })();
        """;

    public static bool IsPrintRequest(string? messageJson)
    {
        if (string.IsNullOrWhiteSpace(messageJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(messageJson);

            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("type", out var type)
                && type.ValueKind == JsonValueKind.String
                && type.GetString() == PrintMessageType;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static double MillimetresToInches(double millimetres) => millimetres / 25.4;
}
