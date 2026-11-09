# OSPOS Desktop

A Windows desktop app for Open Source Point of Sale, built with **.NET 10 (LTS), WPF and Microsoft Edge WebView2**.

It runs the OSPOS web interface in its own window, connected to your OSPOS server, and adds what a
till needs and a browser can't do:

- **Silent receipt printing**: receipts print straight to the chosen receipt printer with no print dialog.
- **Dedicated till window**: starts on your OSPOS server, can start full screen (F11), and only shows
  pages from that server. Other links open in the normal web browser.
- **Clear offline screen**: if the server can't be reached, it says so and offers *Try again*.
- **Remembers settings** per computer: server address, printer, page margin, zoom level.
- **Shared tills**: saved passwords and form autofill are off.

## Why WPF + WebView2

| Option | Verdict |
|---|---|
| **WPF + WebView2** (chosen) | Mature, Windows-native, fully supported on .NET 10. WebView2 is the Edge engine already installed on Windows 10/11, so the existing OSPOS screens, themes and translations work unchanged, and the host can print silently. |
| .NET MAUI (Blazor Hybrid) | Built for phones and tablets as well as desktop. On a Windows-only till it adds complexity and gives nothing extra. |
| WinUI 3 | Modern look, but deployment is more involved (Windows App SDK runtime) for the same WebView2 result. |
| Rewrite every screen natively | Hundreds of screens to rebuild and keep in step with the web app. Not worth it when WebView2 renders them faithfully. |

OSPOS still needs a server (PHP + MySQL/MariaDB). That can be one computer in the shop, a computer
on the local network, or a hosted server. Each till runs this app and points at that server.

## Requirements

- Windows 10 or 11 (64-bit)
- Microsoft Edge WebView2 Runtime. It's already installed on Windows 11 and up-to-date Windows 10. If it's
  missing, the app offers the download page.
- An OSPOS server reachable from the till, for example `http://192.168.1.10`.

## Using it

1. Run `OsposDesktop.exe`. No installer is needed: it's a single self-contained file.
2. On first run, enter the server address and click **Test**, then **Save**.
3. To print receipts without a dialog, open settings (**Ctrl+,**), tick
   *Print receipts without showing the print dialog*, and pick the receipt printer.
   OSPOS's own *print after sale* option decides whether a receipt prints automatically.

| Key | Action |
|---|---|
| F11 | Full screen on/off |
| Ctrl+, | Settings |
| F5 | Reload |
| Ctrl + / Ctrl − / Ctrl+0 | Zoom in / out / reset |

Settings live in `%LOCALAPPDATA%\OSPOS Desktop\settings.json`. Browser data (login cookies) is in the
`WebView2` folder beside it, and any unexpected errors are logged to `error.log` there.

## Building

Needs the .NET 10 SDK (`global.json` pins the feature band).

```bash
dotnet test tests/OsposDesktop.Core.Tests
```

```bash
dotnet publish src/OsposDesktop -p:PublishProfile=win-x64
```

The second command writes a single `OsposDesktop.exe` (about 65 MB, includes .NET) to `artifacts/win-x64/`.
Building and publishing also work from Linux or macOS; running the app needs Windows.
The **Desktop app** GitHub Actions workflow builds, tests and uploads the `.exe` for every change under `desktop/`.

## Project layout

```
desktop/
  src/OsposDesktop.Core/        Platform-independent logic (server address, navigation rules,
                                settings storage, print bridge), unit tested on any OS
  src/OsposDesktop/             WPF app: main window with WebView2, settings window, theme
  tests/OsposDesktop.Core.Tests xUnit tests
```

### How silent printing works

With silent printing on, the app injects a small script into every OSPOS page that replaces
`window.print()` with a message to the app (`PrintBridge`). OSPOS's receipt page already calls
`window.print()` (directly, or via its *print after sale* option), so no OSPOS changes are needed.
The app then prints the page with WebView2's `PrintAsync` to the chosen printer, without headers or
footers and with the configured margin. If that printer isn't available, it says so and opens the
normal print dialog instead.
