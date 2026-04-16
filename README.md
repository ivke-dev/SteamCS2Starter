# SteamCS2Starter

A simple and fast tool to restart Steam and launch Counter-Strike 2.

### What does it do?
- Kills all running Steam processes
- Restarts Steam in silent mode
- Waits for Steam to fully load
- Automatically launches Counter-Strike 2

Very useful when CS2 is stuck, won't start, or you have connection issues.

---

### How to Use

#### 1. Easy way (Recommended)
Just run **`SteamCS2Starter.exe`**  
The program will do everything automatically.

#### 2. Using Commands (Advanced)

You can also run it from Command Prompt or PowerShell with these options:

| Command                                      | Description |
|---------------------------------------------|-----------|
| `SteamCS2Starter.exe`                       | Start Steam + CS2 (default) |
| `SteamCS2Starter.exe --restart`             | Restart Steam and then launch CS2 |
| `SteamCS2Starter.exe --steam-only`          | Restart Steam only (no CS2) |
| `SteamCS2Starter.exe --silent`              | Run without showing console window |
| `SteamCS2Starter.exe --help`                | Show all available commands |

**Example:**
```cmd
SteamCS2Starter.exe --restart
