# OverDraw

A lightweight screen overlay drawing tool for Windows. Hold a modifier key and draw over anything on your screen — lines fade away automatically. Perfect for presentations, screen recordings, and quick visual communication.

![Windows](https://img.shields.io/badge/platform-Windows-blue) ![.NET 9](https://img.shields.io/badge/.NET-9.0-purple) ![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Draw over anything** — transparent overlay sits on top of all windows and monitors
- **Fading ink** — strokes automatically fade away after a configurable duration
- **Stamps** — draw reusable stamps (checkmarks, arrows, symbols) and spawn them instantly at your cursor with a pop animation
- **System tray app** — runs silently in the background, no window clutter
- **Portable** — single `.exe`, no installer, no registry, settings saved as JSON next to the executable
- **Multi-monitor** — overlay spans the entire virtual desktop

## Download

Grab the latest `OverDraw.exe` from the [Releases](../../releases) page.

> **Note:** The portable release is fully self-contained (~68 MB) and requires no dependencies. Just download and run.

## Usage

### Drawing

1. Run `OverDraw.exe` — it goes straight to the system tray
2. **Hold Ctrl + Left Click + Drag** to draw on screen
3. Release to stop — your drawing fades away automatically

### Stamps

1. Open Settings (left-click the tray icon)
2. Click any of the 10 stamp slots to open the stamp editor
3. Draw your stamp (checkmark, arrow, text, etc.) and click Save
4. Press **Ctrl + 1** through **Ctrl + 0** to spawn the stamp at your cursor position

Stamps appear with an elastic pop-in animation and then fade out.

### Settings

Left-click the tray icon or right-click → Settings to configure:

| Setting | Description | Default |
|---|---|---|
| **Pen Color** | Drawing color (click swatch to cycle presets, or type a hex value) | Red |
| **Thickness** | Stroke width (1–20 px) | 3 |
| **Fade Duration** | How long before strokes disappear (0.5–10 s) | 2.0 s |
| **Modifier Key** | Key to hold while drawing (Ctrl, Shift, or Alt) | Ctrl |
| **Stamps 1–10** | Custom drawable stamps triggered by Ctrl+1 through Ctrl+0 | Empty |

Settings are saved to `overdraw-settings.json` next to the executable.

### Exiting

Right-click the tray icon → **Exit**.

## Building from Source

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Build

```bash
cd OverDraw
dotnet build
```

### Publish (self-contained portable exe)

```bash
cd OverDraw
dotnet publish -c Release -r win-x64 --self-contained true -o ../publish
```

The output is a single `OverDraw.exe` in the `publish/` folder.

## How It Works

- **Transparent WPF window** covers all monitors with `WS_EX_TRANSPARENT` (click-through when not drawing)
- **Low-level mouse hook** (`WH_MOUSE_LL`) captures input globally, even when other apps have focus
- **Low-level keyboard hook** (`WH_KEYBOARD_LL`) listens for Ctrl+1–0 to spawn stamps
- **StreamGeometry rendering** at ~60fps for smooth drawing and fade animations
- **System tray** via WinForms `NotifyIcon` for a minimal footprint

## License

MIT
