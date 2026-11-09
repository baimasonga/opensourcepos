using OsposDesktop.Core;

namespace OsposDesktop.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "ospos-desktop-tests-" + Guid.NewGuid());

    private string FilePath => Path.Combine(directory, "nested", "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenFileIsMissing()
    {
        var settings = new SettingsStore(FilePath).Load();

        Assert.Null(settings.ServerUrl);
        Assert.False(settings.SilentPrinting);
        Assert.Equal(1.0, settings.ZoomFactor);
    }

    [Fact]
    public void Save_RoundTripsSettings()
    {
        var store = new SettingsStore(FilePath);
        var saved = new AppSettings
        {
            ServerUrl = "http://pos.local/",
            StartFullScreen = true,
            SilentPrinting = true,
            ReceiptPrinter = "EPSON TM-T20",
            ReceiptMarginMillimetres = 2,
            ZoomFactor = 1.25,
        };

        store.Save(saved);

        Assert.Equal(saved, store.Load());
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenFileIsCorrupt()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, "{ not json");

        Assert.Equal(new AppSettings(), new SettingsStore(FilePath).Load());
    }

    [Fact]
    public void Save_ClampsOutOfRangeValues()
    {
        var store = new SettingsStore(FilePath);

        store.Save(new AppSettings { ZoomFactor = 10, ReceiptMarginMillimetres = -3, ReceiptPrinter = "  " });
        var loaded = store.Load();

        Assert.Equal(AppSettings.MaxZoom, loaded.ZoomFactor);
        Assert.Equal(0, loaded.ReceiptMarginMillimetres);
        Assert.Null(loaded.ReceiptPrinter);
    }
}
