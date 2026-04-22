# SteamCS2Starter

A powerful tool for Steam and Epic Games management with CS2 auto-launch functionality.

### What does it do?
- **CS2 Mode**: Kills all running Steam processes, restarts Steam, and launches Counter-Strike 2
- **Games Browser Mode**: Browse and launch all your Steam and Epic Games from one interface
- **Auto-detection**: Automatically finds Steam and Epic Games installations
- **Game Management**: View game sizes, launch games directly from the interface

Very useful when CS2 is stuck, won't start, or you have connection issues. Also great as a unified game launcher!

---

### How to Use

#### 1. CS2 Auto-Launch (Original Mode)
Just run **`SteamCS2Starter.exe`**  
The program will automatically restart Steam and launch CS2.

#### 2. Games Browser Mode (NEW!)
Run **`SteamCS2Starter.exe --games`** to access the game browser:

- Browse all your Steam and Epic Games
- View game information (size, platform)
- Launch any game directly
- Beautiful console interface with color coding

#### 3. Using Commands (Advanced)

You can also run it from Command Prompt or PowerShell with these options:

| Command                                      | Description |
|---------------------------------------------|-----------|
| `SteamCS2Starter.exe`                       | Start Steam + CS2 (default) |
| `SteamCS2Starter.exe --restart`             | Restart Steam and then launch CS2 |
| `SteamCS2Starter.exe --steam-only`          | Restart Steam only (no CS2) |
| `SteamCS2Starter.exe --silent`              | Run without showing console window |
| `SteamCS2Starter.exe --games`               | **NEW!** Open games browser mode |
| `SteamCS2Starter.exe --help`                | Show all available commands |

**Examples:**
```cmd
# Launch CS2 automatically
SteamCS2Starter.exe

# Browse and launch any game
SteamCS2Starter.exe --games

# Restart Steam only
SteamCS2Starter.exe --steam-only
```

---

### Games Browser Features

When you run `--games` mode, you'll get:

- **Platform Selection**: Choose between Steam, Epic Games, or both
- **Game List**: All installed games with names, platforms, and sizes
- **One-Click Launch**: Select any game by number to launch it
- **Color Coding**: 
  - Green for Steam games
  - Magenta for Epic Games
  - Beautiful ASCII art interface

**Game Browser Controls:**
- `1` - Browse Steam games only
- `2` - Browse Epic Games only  
- `3` - Browse all games from both platforms
- `0` - Exit
- `Game Number` - Launch selected game
