namespace OsposDesktop.Core;

public sealed record AppSettings
{
    public const double MinZoom = 0.5;
    public const double MaxZoom = 3.0;

    /// <summary>Normalized OSPOS server address, or null until the user has set one.</summary>
    public string? ServerUrl { get; init; }

    public bool StartFullScreen { get; init; }

    /// <summary>
    /// When true, receipts print straight to <see cref="ReceiptPrinter"/> without a print dialog.
    /// </summary>
    public bool SilentPrinting { get; init; }

    /// <summary>Windows printer name; null or empty means the system default printer.</summary>
    public string? ReceiptPrinter { get; init; }

    public double ReceiptMarginMillimetres { get; init; }

    public double ZoomFactor { get; init; } = 1.0;

    public AppSettings Sanitized() => this with
    {
        ReceiptMarginMillimetres = Math.Clamp(double.IsFinite(ReceiptMarginMillimetres) ? ReceiptMarginMillimetres : 0, 0, 50),
        ZoomFactor = Math.Clamp(double.IsFinite(ZoomFactor) ? ZoomFactor : 1.0, MinZoom, MaxZoom),
        ReceiptPrinter = string.IsNullOrWhiteSpace(ReceiptPrinter) ? null : ReceiptPrinter.Trim(),
    };
}
