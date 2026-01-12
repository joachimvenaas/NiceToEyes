# Nice To Eyes 🌙

A Windows application that creates an adjustable, movable, transparent, click-through overlay to make apps without dark mode easier on the eyes.

![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)
![Windows](https://img.shields.io/badge/Platform-Windows-lightgrey)

## Features

- **Click-through overlay** - The overlay doesn't interfere with your work
- **Adjustable darkness** - Control the opacity from 0-100%
- **Color presets**:
  - **Black** - Standard dark overlay
  - **Sepia** - Warm brown tone for reduced eye strain
  - **Inverted** - Real-time color inversion of the screen region
- **Resizable & movable** - Drag the corners to adjust
- **System tray** - Minimize to tray, right-click menu for quick access
- **Portable** - Single executable, no installation required

## Screenshots

The app creates a dark overlay with interactive corner grips:
- 🔀 Top-left: Move the overlay
- ❌ Top-right: Hide the overlay
- ↘️ Bottom-right: Resize the overlay

## Installation

### Download
Download the latest `NiceToEyes.exe` from the [Releases](../../releases) page.

### Build from source
```bash
git clone https://github.com/YOUR_USERNAME/NiceToEyes.git
cd NiceToEyes
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

## Usage

1. Run `NiceToEyes.exe`
2. Position the overlay over any app you want to darken
3. Adjust the darkness and color preset as needed
4. Minimize the control panel - it goes to the system tray
5. Right-click the tray icon for quick show/hide toggle

## Requirements

- Windows 10/11 (64-bit)
- No .NET runtime required (self-contained executable)

## License

MIT License - Feel free to use, modify, and distribute.
