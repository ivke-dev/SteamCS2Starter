# SteamCS2Starter v2.0.0

**Universal Game Launcher for Steam and Epic Games**

---

## What this program enables:

### Game Browser Mode (Default)
- **Browse all games** from Steam and Epic Games libraries
- **Automatic detection** of installed games
- **Easy launching** of any game with one click
- **Display game information** (size, platform, name)
- **Beautiful console interface** with color-coded display

### CS2 Auto-Launch Mode
- **Automatic Steam restart**
- **Launch Counter-Strike 2**
- **Resolve issues** when CS2 won't start

---

## Usage

### 1. Game Browser Mode (Default)
Run `SteamCS2Starter.exe` without arguments:

```
SteamCS2Starter.exe
```

The program will open a platform selection menu:
- **[1]** Steam games
- **[2]** Epic Games games  
- **[3]** All games
- **[0]** Exit

After selecting a platform, you'll see a list of all games with numbers. Enter the number of the game you want to launch.

### 2. CS2 Auto-Launch Mode
For automatic CS2 launch:

```
SteamCS2Starter.exe --cs2
```

### 3. Other Options

| Command                              | Description |
|--------------------------------------|------------|
| `SteamCS2Starter.exe`                  | Game browser mode (default) |
| `SteamCS2Starter.exe --cs2`            | CS2 auto-launch |
| `SteamCS2Starter.exe --restart`        | Restart Steam then CS2 |
| `SteamCS2Starter.exe --steam-only`     | Restart Steam only |
| `SteamCS2Starter.exe --silent`         | Run without console display |

---

## Features

### Game Browser
- **Automatic detection** of Steam and Epic Games installations
- **Browse all games** with size information
- **Color-coded display**: 
  - Green for Steam games
  - Magenta for Epic Games games
- **Direct launching** of games without opening platforms

### CS2 Auto-Launch
- **Kill all Steam processes** to clear issues
- **Restart Steam** in silent mode
- **Wait for Steam** to load
- **Automatically launch** CS2

---

## System Requirements

- Windows 10/11 (x64)
- Steam (for Steam games)
- Epic Games Launcher (for Epic games)
- .NET Runtime 10.0 (included in .exe file)

---

## Installation

1. Download `SteamCS2Starter.exe` (73.5 MB)
2. Place it in any folder
3. Run with double click

**Note**: The program is self-contained - no additional files needed!

---

## Version 2.0.0 - What's New:

- &checkmark; **Epic Games support** - now supports Epic games too
- &checkmark; **Game browser mode** - default mode for browsing games
- &checkmark; **Single-file build** - everything in one .exe file
- &checkmark; **Better detection** of games with multiple library paths
- &checkmark; **Enhanced interface** with beautiful ASCII art

---

## Author

**IVKE** - ivke-dev

---

## Download

[https://github.com/ivke-dev/SteamCS2Starter](https://github.com/ivke-dev/SteamCS2Starter)
