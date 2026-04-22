using Microsoft.Win32;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SteamCS2Starter;

public class Program
{
    private const string Cs2AppId = "730";
    private const int WaitTimeoutSeconds = 60;
    private const int WaitIntervalMs = 1500;
    
    private const string UpdateUrl = "https://raw.githubusercontent.com/ivke-dev/SteamCS2Starter/main/version.json";

    private static string CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

    public class GameInfo
    {
        public string Name { get; set; } = string.Empty;
        public string AppId { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string InstallPath { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public DateTime LastPlayed { get; set; }
        public long SizeOnDisk { get; set; }
    }

    public class EpicGameInfo
    {
        public string DisplayName { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string InstallLocation { get; set; } = string.Empty;
        public DateTime LastPlayed { get; set; }
    }

    private static string? FindSteamPath()
    {
        string[] registryPaths = [
            @"SOFTWARE\WOW6432Node\Valve\Steam",
            @"SOFTWARE\Valve\Steam"
        ];

        foreach (string regPath in registryPaths)
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(regPath);
            if (key != null)
            {
                string? installPath = key.GetValue("InstallPath") as string;
                if (!string.IsNullOrEmpty(installPath))
                {
                    string exePath = Path.Combine(installPath, "steam.exe");
                    if (File.Exists(exePath))
                        return exePath;
                }
            }
        }

        string[] commonPaths = [
            @"C:\Program Files (x86)\Steam\steam.exe",
            @"C:\Program Files\Steam\steam.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Steam", "steam.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steam.exe")
        ];

        foreach (string path in commonPaths)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static string? FindEpicPath()
    {
        string[] epicPaths = [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "Launcher", "Portal", "Binaries", "Win32", "EpicGamesLauncher.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Epic Games", "Launcher", "Portal", "Binaries", "Win32", "EpicGamesLauncher.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EpicGamesLauncher", "Portal", "Binaries", "Win32", "EpicGamesLauncher.exe")
        ];

        foreach (string path in epicPaths)
        {
            if (File.Exists(path))
                return path;
        }

        // Proveri registry
        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\EpicGames\Launcher");
        if (key != null)
        {
            string? installPath = key.GetValue("InstallPath") as string;
            if (!string.IsNullOrEmpty(installPath))
            {
                string exePath = Path.Combine(installPath, "Portal", "Binaries", "Win32", "EpicGamesLauncher.exe");
                if (File.Exists(exePath))
                    return exePath;
            }
        }

        return null;
    }

    private static List<GameInfo> GetSteamGames()
    {
        var games = new List<GameInfo>();
        string? steamPath = FindSteamPath();
        
        if (string.IsNullOrEmpty(steamPath))
            return games;

        string steamFolder = Path.GetDirectoryName(steamPath)!;
        
        // Proveri standardne lokacije
        var standardPaths = new[] { 
            Path.Combine(steamFolder, "steamapps"),
            @"C:\Program Files (x86)\Steam\steamapps",
            @"C:\Program Files\Steam\steamapps",
            @"D:\SteamLibrary\steamapps",
            @"E:\SteamLibrary\steamapps",
            @"F:\SteamLibrary\steamapps",
            @"G:\SteamLibrary\steamapps"
        };
        
        foreach (string path in standardPaths)
        {
            if (Directory.Exists(path))
            {
                ProcessSteamAppsFolder(path, games);
            }
        }

        return games.GroupBy(g => new { g.AppId, g.Platform })
                   .Select(g => g.First())
                   .OrderBy(g => g.Name)
                   .ToList();
    }

    private static void ProcessSteamAppsFolder(string appsPath, List<GameInfo> games)
    {
        if (!Directory.Exists(appsPath))
            return;

        var appManifestFiles = Directory.GetFiles(appsPath, "appmanifest_*.acf");
        
        foreach (string manifestFile in appManifestFiles)
        {
            try
            {
                var manifestData = ParseAppManifest(manifestFile);
                if (manifestData != null)
                {
                    string appId = Path.GetFileName(manifestFile).Replace("appmanifest_", "").Replace(".acf", "");
                    string gameName = manifestData.GetValueOrDefault("name", appId);
                    string installDir = manifestData.GetValueOrDefault("installdir", "");
                    
                    if (!string.IsNullOrEmpty(installDir))
                    {
                        string gameInstallPath = Path.Combine(appsPath, "common", installDir);
                        
                        if (Directory.Exists(gameInstallPath))
                        {
                            games.Add(new GameInfo
                            {
                                Name = gameName,
                                AppId = appId,
                                Platform = "Steam",
                                InstallPath = gameInstallPath,
                                ExecutablePath = FindGameExecutable(gameInstallPath),
                                LastPlayed = DateTime.Now,
                                SizeOnDisk = GetDirectorySize(gameInstallPath)
                            });
                        }
                    }
                }
            }
            catch
            {
                // Ignoriçi greçke za pojedinaène igre
            }
        }
    }

    private static List<GameInfo> GetEpicGames()
    {
        var games = new List<GameInfo>();
        string? epicPath = FindEpicPath();
        
        if (string.IsNullOrEmpty(epicPath))
            return games;

        string epicDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EpicGamesLauncher", "Data");
        string manifestPath = Path.Combine(epicDataPath, "Manifests");
        
        if (!Directory.Exists(manifestPath))
            return games;

        var manifestFiles = Directory.GetFiles(manifestPath, "*.item");
        
        foreach (string manifestFile in manifestFiles)
        {
            try
            {
                var manifestData = ParseEpicManifest(manifestFile);
                if (manifestData != null)
                {
                    games.Add(new GameInfo
                    {
                        Name = manifestData.DisplayName,
                        AppId = manifestData.AppName,
                        Platform = "Epic Games",
                        InstallPath = manifestData.InstallLocation,
                        ExecutablePath = FindEpicGameExecutable(manifestData.InstallLocation),
                        LastPlayed = manifestData.LastPlayed,
                        SizeOnDisk = GetDirectorySize(manifestData.InstallLocation)
                    });
                }
            }
            catch
            {
                // Ignoriši greške za pojedinačne igre
            }
        }

        return games.OrderBy(g => g.Name).ToList();
    }

    private static List<string> ParseLibraryFolders(string vdfPath)
    {
        var folders = new List<string>();
        try
        {
            string content = File.ReadAllText(vdfPath);
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                
                // Trai "path" linije
                if (line.Contains("\"path\""))
                {
                    // Proveri sleæu liniju za vrednost
                    if (i + 1 < lines.Length)
                    {
                        string nextLine = lines[i + 1].Trim();
                        var match = System.Text.RegularExpressions.Regex.Match(nextLine, @"""([^""]+)""");
                        if (match.Success)
                        {
                            string path = match.Groups[1].Value;
                            // Konvertuj double backslashes u single backslashes
                            path = path.Replace("\\\\", "\\");
                            if (Directory.Exists(path) && !folders.Contains(path))
                            {
                                folders.Add(path);
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Ako ne uspe parser, vrati praznu listu
        }
        
        return folders.Distinct().ToList();
    }

    private static Dictionary<string, string>? ParseAppManifest(string manifestPath)
    {
        var data = new Dictionary<string, string>();
        try
        {
            string content = File.ReadAllText(manifestPath);
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                
                // Preskoçi komentare i zagrade
                if (line.StartsWith("//") || line == "{" || line == "}")
                    continue;
                
                // Parsuj kljuè-vrednost parove
                var match = System.Text.RegularExpressions.Regex.Match(line, @"""([^""]+)""\s*""([^""]+)""");
                if (match.Success)
                {
                    data[match.Groups[1].Value] = match.Groups[2].Value;
                }
                else
                {
                    // Proveri i drugi format
                    var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        data[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
            
            return data.Count > 0 ? data : null;
        }
        catch
        {
            return null;
        }
    }

    private static EpicGameInfo? ParseEpicManifest(string manifestPath)
    {
        try
        {
            var json = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<EpicManifest>(json);
            
            if (manifest != null)
            {
                return new EpicGameInfo
                {
                    DisplayName = manifest.DisplayName ?? manifest.AppName ?? "Unknown",
                    AppName = manifest.AppName ?? "",
                    InstallLocation = manifest.InstallLocation ?? "",
                    LastPlayed = DateTime.Now // Ovo bi trebalo dobiti iz Epic API-ja
                };
            }
        }
        catch
        {
            // Ignoriši greške
        }
        
        return null;
    }

    private static string FindGameExecutable(string gamePath)
    {
        var commonExes = new[] { "game.exe", "play.exe", "start.exe", "launcher.exe", "bin\\game.exe", "bin\\win64\\game.exe" };
        
        foreach (string exe in commonExes)
        {
            string fullPath = Path.Combine(gamePath, exe);
            if (File.Exists(fullPath))
                return fullPath;
        }
        
        // Pronađi prvi .exe fajl
        var exeFiles = Directory.GetFiles(gamePath, "*.exe", SearchOption.AllDirectories);
        return exeFiles.FirstOrDefault() ?? "";
    }

    private static string FindEpicGameExecutable(string gamePath)
    {
        // Epic igre često imaju .exe u root folderu ili podfolderima
        var exeFiles = Directory.GetFiles(gamePath, "*.exe", SearchOption.AllDirectories);
        return exeFiles.FirstOrDefault() ?? "";
    }

    private static long GetDirectorySize(string path)
    {
        try
        {
            return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .Sum(file => new FileInfo(file).Length);
        }
        catch
        {
            return 0;
        }
    }

    private static void KillSteamProcesses()
    {
        string[] processNames = ["steam", "steamwebhelper", "SteamService"];
        
        foreach (string name in processNames)
        {
            Process[] processes = Process.GetProcessesByName(name);
            foreach (Process p in processes)
            {
                try
                {
                    Console.WriteLine($"  -> Stopping {p.ProcessName}");
                    p.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  -> Could not stop {p.ProcessName}: {ex.Message}");
                }
            }
        }
        
        Thread.Sleep(WaitIntervalMs);
    }

    private static void PrintHeader()
    {
        Console.Clear();
        
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                                                                                                        ║");
        Console.ResetColor();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("║     ██████╗ ███████╗████████╗██████╗  ██████╗ ██╗  ██╗██╗   ██╗███████╗    ███████╗██████╗ ██████╗  █████╗ ██████╗  ██████╗███████╗██████╗ ███████╗     ║");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("║     ██╔══██╗██╔════╝╚══██╔══╝██╔══██╗██╔═══██╗██║  ██║╚██╗ ██╔╝██╔════╝    ██╔════╝██╔══██╗██╔══██╗██╔══██╗██╔══██╗██╔════╝██╔══██╗██╔════╝     ║");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("║     ██████╔╝█████╗     ██║   ██████╔╝██║   ██║███████║ ╚████╔╝ █████╗      █████╗  ██████╔╝██████╔╝███████║██████╔╝█████╗  ██████╔╝█████╗       ║");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("║     ██╔══██╗██╔══╝     ██║   ██╔══██╗██║   ██║██╔══██║  ╚██╔╝  ██╔══╝      ██╔══╝  ██╔══██╗██╔══██╗██╔══██║██╔══██╗██╔══╝  ██╔══██╗██╔══╝       ║");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("║     ██║  ██║███████╗   ██║   ██║  ██║╚██████╔╝██║  ██║   ██║   ███████╗    ███████╗██║  ██║██║  ██║██║  ██║██║  ██║███████╗██║  ██║███████╗     ║");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("║     ╚═╝  ╚═╝╚══════╝   ╚═╝   ╚═╝  ╚═╝ ╚═════╝ ╚═╝  ╚═╝   ╚═╝   ╚══════╝    ╚══════╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝╚══════╝     ║");
        
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("║                                                                                                                        ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("                              ╔═══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("                              ║");
        Console.Write("                              ║              ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Created by: ");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write("IVKE");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("              ║");
        Console.WriteLine("                              ║");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("                              ╚═══════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"                                              Version: {CurrentVersion}");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void PrintStep(int step, int total, string message, ConsoleColor color = ConsoleColor.White)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  [{step}/{total}] ");
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ {message}");
        Console.ResetColor();
    }

    private static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ✗ {message}");
        Console.ResetColor();
    }

    private static void PrintLoading(string message, int dotCount)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        string dots = new string('.', dotCount);
        Console.Write($"\r  {message}{dots}");
        Console.ResetColor();
    }

    private static void PrintDivider()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ─────────────────────────────────────────────────────────────");
        Console.ResetColor();
    }

    private static bool WaitForSteamReady()
    {
        int elapsed = 0;
        int dotCount = 0;
        
        while (elapsed < WaitTimeoutSeconds)
        {
            Thread.Sleep(WaitIntervalMs);
            elapsed += WaitIntervalMs / 1000;
            dotCount = (dotCount % 3) + 1;

            Process[] steamProcesses = Process.GetProcessesByName("steam");
            bool steamWindow = steamProcesses.Any(p => p.MainWindowHandle != IntPtr.Zero);

            Process[] webHelper = Process.GetProcessesByName("steamwebhelper");
            bool webHelperWindow = webHelper.Any(p => p.MainWindowHandle != IntPtr.Zero);

            if (steamWindow || webHelperWindow)
                return true;

            Console.Write("\r");
            PrintLoading("Waiting for Steam", dotCount);
        }
        
        Console.WriteLine();
        return false;
    }

    private static async Task<(bool available, string? version, string? downloadUrl)> CheckForUpdate()
    {
        try
        {
            using HttpClient client = new();
            client.Timeout = TimeSpan.FromSeconds(10);
            
            string json = await client.GetStringAsync(UpdateUrl);
            var versionInfo = System.Text.Json.JsonSerializer.Deserialize<VersionInfo>(json);
            
            if (versionInfo?.version == null)
                return (false, null, null);

            Version current = Version.Parse(CurrentVersion);
            Version latest = Version.Parse(versionInfo.version);

            if (latest > current)
            {
                return (true, versionInfo.version, versionInfo.downloadUrl);
            }

            return (false, versionInfo.version, null);
        }
        catch
        {
            return (false, null, null);
        }
    }

    private static async Task<bool> DownloadAndUpdate(string downloadUrl)
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine(@"
       ██████╗ ███████╗████████╗██████╗  ██████╗ ██╗  ██╗██╗   ██╗███████╗    ██████╗ ██╗   ██╗██████╗ ███████╗██████╗ ███████╗██████╗ 
       ██╔══██╗██╔════╝╚══██╔══╝██╔══██╗██╔═══██╗██║  ██║╚██╗ ██╔╝██╔════╝   ██╔══██╗██║   ██║██╔══██╗██╔════╝██╔══██╗██╔════╝██╔══██╗
       ██████╔╝█████╗     ██║   ██████╔╝██║   ██║███████║ ╚████╔╝ █████╗     ██████╔╝██║   ██║██████╔╝█████╗  ██████╔╝█████╗  
       ██╔══██╗██╔══╝     ██║   ██╔══██╗██║   ██║██╔══██║  ╚██╔╝  ██╔══╝     ██╔══██╗██║   ██║██╔══██╗██╔══╝  ██╔══██╗██╔══╝  
       ██║  ██║███████╗   ██║   ██║  ██║╚██████╔╝██║  ██║   ██║   ███████╗   ██║  ██║╚██████╔╝██████╔╝███████╗██║  ██║███████╗
       ╚═╝  ╚═╝╚══════╝   ╚═╝   ╚═╝  ╚═╝ ╚═════╝ ╚═╝  ╚═╝   ╚═╝   ╚══════╝   ╚═╝  ╚═╝ ╚═════╝ ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝
                                                                       
          ██████╗██████╗ ██╗   ██╗██████╗ ████████╗████████╗███████╗███╗   ███╗
         ██╔════╝██╔══██╗╚██╗ ██╔╝██╔══██╗╚══██╔══╝╚══██╔══╝██╔════╝████╗ ████║
         ██║     ██████╔╝ ╚████╔╝ ██████╔╝   ██║      ██║   █████╗  ██╔████╔██║
         ██║     ██╔══██╗  ╚██╔╝  ██╔═══╝    ██║      ██║   ██╔══╝  ██║╚██╔╝██║
         ╚██████╗██║  ██║   ██║   ██║        ██║      ██║   ███████╗██║ ╚═╝ ██║
          ╚═════╝╚═╝  ╚═╝   ╚═╝   ╚═╝        ╚═╝      ╚═╝   ╚══════╝╚═╝     ╚═╝
            ");
            Console.ResetColor();
            Console.WriteLine();

            string currentExePath = Environment.ProcessPath ?? "SteamCS2Starter.exe";
            string updateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SteamCS2Starter", "Updates");
            Directory.CreateDirectory(updateFolder);
            
            string newExePath = Path.Combine(updateFolder, "SteamCS2Starter_new.exe");
            string batchPath = Path.Combine(updateFolder, "update.bat");

            using HttpClient client = new();
            client.Timeout = TimeSpan.FromMinutes(5);

            var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? -1;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(newExePath, FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            int lastProgress = -1;

            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    int progress = (int)((totalRead * 100) / totalBytes);
                    if (progress != lastProgress)
                    {
                        lastProgress = progress;
                        ShowProgressBar(progress, "Downloading update");
                    }
                }
            }

            Console.WriteLine();
            PrintSuccess("Update downloaded!");

            string batchContent = $@"@echo off
setlocal enabledelayedexpansion
echo Waiting for process to close...
timeout /t 5 /nobreak >nul

echo Killing any remaining processes...
taskkill /F /IM SteamCS2Starter.exe 2>nul

timeout /t 2 /nobreak >nul

echo Updating...
set attempts=0
:retry
set /a attempts+=1
copy /y ""{newExePath}"" ""{currentExePath}"" 2>nul
if errorlevel 1 (
    if %attempts% LSS 10 (
        timeout /t 2 /nobreak >nul
        goto retry
    )
    echo Failed to update after 10 attempts
    pause
    exit /b 1
)

echo Cleaning up...
del ""{newExePath}"" 2>nul

echo Starting updated version...
start """" ""{currentExePath}""

echo Done!
del ""%~f0""
";

            await File.WriteAllTextAsync(batchPath, batchContent);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine(@"      ╔════════════════════════════════════════════════════════════╗
      ║                                                            ║
      ║         ██████╗ ██████╗  █████╗ ███╗   ██╗████████╗███████╗██╗         ║
      ║         ██╔══██╗██╔══██╗██╔══██╗████╗  ██║╚══██╔══╝██╔════╝██║         ║
      ║         ██████╔╝██████╔╝███████║██╔██╗ ██║   ██║   █████╗  ██║         ║
      ║         ██╔═══╝ ██╔══██╗██╔══██║██║╚██╗██║   ██║   ██╔══╝  ██║         ║
      ║         ██║     ██║  ██║██║  ██║██║ ╚████║   ██║   ███████╗███████╗    ║
      ║         ╚═╝     ╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═══╝   ╚═╝   ╚══════╝╚══════╝    ║
      ║                                                            ║
      ║              ██████╗██████╗ ██╗   ██╗██████╗ ████████╗      ║
      ║             ██╔════╝██╔══██╗╚██╗ ██╔╝██╔══██╗╚══██╔══╝      ║
      ║             ██║     ██████╔╝ ╚████╔╝ ██████╔╝   ██║         ║
      ║             ██║     ██╔══██╗  ╚██╔╝  ██╔═══╝    ██║         ║
      ║             ╚██████╗██║  ██║   ██║   ██║        ██║         ║
      ║              ╚═════╝╚═╝  ╚═╝   ╚═╝   ╚═╝        ╚═╝         ║
      ║                                                            ║
      ╚════════════════════════════════════════════════════════════╝
            ");
            Console.ResetColor();
            Console.WriteLine();

            Thread.Sleep(2000);

            ProcessStartInfo psi = new()
            {
                FileName = batchPath,
                UseShellExecute = true,
                CreateNoWindow = true
            };
            Process.Start(psi);

            Environment.Exit(0);

            return true;
        }
        catch (Exception ex)
        {
            PrintError($"Update failed: {ex.Message}");
            return false;
        }
    }

    private static void ShowProgressBar(int progress, string message)
    {
        int barWidth = 50;
        int filled = (int)((barWidth * progress) / 100);
        
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("\r       ");
        Console.ResetColor();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("█");
        for (int i = 0; i < filled - 1 && i < barWidth - 1; i++)
            Console.Write("█");
        
        Console.ForegroundColor = ConsoleColor.DarkGray;
        for (int i = filled; i < barWidth; i++)
            Console.Write("░");
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("█ ");
        Console.ResetColor();
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{message}: {progress}%");
        Console.ResetColor();
    }

    public static async Task Main(string[] args)
    {
        // Proveri da li je pozvan sa --games argumentom za novi mod
        if (args.Contains("--games"))
        {
            await RunGameLauncher();
            return;
        }
        
        // Inache pokreni originalnu CS2 starter funkcionalnost
        await RunOriginalCS2Starter(args);
    }

    private static async Task RunOriginalCS2Starter(string[] args)
    {
        PrintHeader();

        PrintStep(0, 5, "Checking for updates...", ConsoleColor.Cyan);
        var (hasUpdate, newVersion, downloadUrl) = await CheckForUpdate();

        if (hasUpdate && !string.IsNullOrEmpty(downloadUrl))
        {
            Console.WriteLine();
            PrintSuccess($"New version available: {newVersion}");
            bool updated = await DownloadAndUpdate(downloadUrl);
            if (updated)
                return;
        }
        else if (newVersion != null)
        {
            PrintSuccess($"You have the latest version ({newVersion})");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  (Update check failed - continuing anyway)");
            Console.ResetColor();
        }

        Console.WriteLine();

        PrintStep(1, 5, "Stopping Steam processes...", ConsoleColor.Yellow);
        KillSteamProcesses();
        PrintSuccess("All Steam processes stopped");
        Console.WriteLine();

        PrintStep(2, 5, "Finding Steam installation...", ConsoleColor.Yellow);
        string? steamPath = FindSteamPath();
        
        if (string.IsNullOrEmpty(steamPath) || !File.Exists(steamPath))
        {
            PrintError("Could not find Steam installation!");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Please install Steam or check installation path.");
            Console.ResetColor();
            return;
        }
        
        PrintSuccess($"Found at: {Path.GetDirectoryName(steamPath)}");
        Console.WriteLine();

        PrintStep(3, 5, "Starting Steam...", ConsoleColor.Yellow);
        
        ProcessStartInfo psi = new()
        {
            FileName = steamPath,
            Arguments = "",
            UseShellExecute = true
        };
        
        Process? steamProc = Process.Start(psi);
        
        if (steamProc != null)
        {
            Thread.Sleep(1000);
            try
            {
                steamProc.Refresh();
                if (steamProc.MainWindowHandle != IntPtr.Zero)
                {
                    SetForegroundWindow(steamProc.MainWindowHandle);
                    ShowWindow(steamProc.MainWindowHandle, 3);
                }
            }
            catch { }
        }
        
        PrintSuccess("Steam is launching...");
        Console.WriteLine();

        PrintStep(4, 5, "Waiting for Steam to initialize...", ConsoleColor.Yellow);
        bool steamReady = WaitForSteamReady();
        
        Console.WriteLine();
        if (!steamReady)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            PrintStep(4, 5, "WARNING: Steam may not be fully ready, continuing...", ConsoleColor.Yellow);
        }
        else
        {
            PrintSuccess("Steam is ready!");
        }
        Console.WriteLine();

        PrintStep(5, 5, "Starting CS2...", ConsoleColor.Yellow);
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"steam://run/{Cs2AppId}",
                UseShellExecute = true
            });
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
   ╔═══════════════════════════════════════════╗
   ║                                           ║
   ║     ██████╗ ██████╗ ███╗   ██╗███████╗███╗   ███╗██╗███╗   ██╗ █████╗ ██╗      ║
   ║     ██╔══██╗██╔══██╗████╗  ██║██╔════╝████╗ ████║██║████╗  ██║██╔══██╗██║      ║
   ║     ██║  ██║██████╔╝██╔██╗ ██║███████╗██╔████╔██║██║██╔██╗ ██║███████║██║      ║
   ║     ██║  ██║██╔══██╗██║╚██╗██║╚════██║██║╚██╔╝██║██║██║╚██╗██║██╔══██║██║      ║
   ║     ██████╔╝██║  ██║██║ ╚████║███████║██║ ╚═╝ ██║██║██║ ╚████║██║  ██║███████╗║
   ║     ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═══╝╚══════╝╚═╝     ╚═╝╚═╝╚═╝  ╚═══╝╚═╝  ╚═╝╚══════╝║
   ║                         ██████╗ ███████╗ █████╗ ██████╗                      ║
   ║                         ██╔══██╗██╔════╝██╔══██╗██╔══██╗                     ║
   ║                         ██████╔╝█████╗  ███████║██████╔╝                     ║
   ║                         ██╔══██╗██╔══╝  ██╔══██║██╔══██╗                     ║
   ║                         ██║  ██║███████╗██║  ██║██║  ██║                     ║
   ║                         ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝╚═╝  ╚═╝                     ║
   ║                                           ║
   ║            ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀            ║
   ╚═══════════════════════════════════════════╝
            ");
            Console.ResetColor();
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓");
            Console.WriteLine("  ▓▓  GLHF! Have fun playing CS2!  ▓▓");
            Console.WriteLine("  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            PrintError($"Failed to launch CS2: {ex.Message}");
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private class VersionInfo
    {
        public string? version { get; set; }
        public string? downloadUrl { get; set; }
    }

    private class EpicManifest
    {
        [JsonPropertyName("DisplayName")]
        public string? DisplayName { get; set; }
        
        [JsonPropertyName("AppName")]
        public string? AppName { get; set; }
        
        [JsonPropertyName("InstallLocation")]
        public string? InstallLocation { get; set; }
        
        [JsonPropertyName("LastPlayed")]
        public string? LastPlayed { get; set; }
    }

    private static void ShowPlatformSelection()
    {
        Console.Clear();
        PrintHeader();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                                                                                                        ║");
        Console.WriteLine("║                                          IZABERITE PLATFORMU ZA PREGLED IGARA                                              ║");
        Console.WriteLine("║                                                                                                                        ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        
        Console.WriteLine();
        
        bool steamAvailable = !string.IsNullOrEmpty(FindSteamPath());
        bool epicAvailable = !string.IsNullOrEmpty(FindEpicPath());
        
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  Dostupne platforme:");
        Console.WriteLine();
        
        if (steamAvailable)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  [1] Steam");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [1] Steam (nije dostupan)");
        }
        
        if (epicAvailable)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("  [2] Epic Games");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [2] Epic Games (nije dostupan)");
        }
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  [3] Obje platforme");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("  [0] Izlaz");
        Console.ResetColor();
        
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("  Vaš izbor: ");
        Console.ResetColor();
    }

    private static void ShowGamesList(List<GameInfo> games, string platformFilter = "")
    {
        Console.Clear();
        PrintHeader();
        
        var filteredGames = string.IsNullOrEmpty(platformFilter) 
            ? games 
            : games.Where(g => g.Platform.Equals(platformFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗");
        
        string title = string.IsNullOrEmpty(platformFilter) 
            ? "SPISAK SVIH IGARA" 
            : $"SPISAK IGARA - {platformFilter.ToUpper()}";
        
        Console.WriteLine($"║{title.PadRight(118)}║");
        Console.WriteLine("║                                                                                                                        ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        
        Console.WriteLine();
        
        if (filteredGames.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Nema pronađenih igara.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"  Ukupno pronađenih igara: {filteredGames.Count}");
            Console.WriteLine();
            
            for (int i = 0; i < filteredGames.Count; i++)
            {
                var game = filteredGames[i];
                
                Console.ForegroundColor = game.Platform == "Steam" ? ConsoleColor.Green : ConsoleColor.Magenta;
                Console.Write($"  [{i + 1:D2}] ");
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{game.Name,-50} ");
                
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"({game.Platform,-12}) ");
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{FormatSize(game.SizeOnDisk)}");
                
                Console.ResetColor();
                Console.WriteLine();
            }
        }
        
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  Unesite broj igre za pokretanje, ili '0' za nazad: ");
        Console.ResetColor();
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static void LaunchGame(GameInfo game)
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n  Pokretanje igre: {game.Name}...");
            Console.ResetColor();
            
            if (game.Platform == "Steam")
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"steam://run/{game.AppId}",
                    UseShellExecute = true
                });
            }
            else if (game.Platform == "Epic Games")
            {
                if (!string.IsNullOrEmpty(game.ExecutablePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = game.ExecutablePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  Nije moguće pronaći izvršni fajl za Epic igru.");
                    Console.ResetColor();
                    return;
                }
            }
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  Igra '{game.Name}' je pokrenuta!");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Greška pri pokretanju igre: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static async Task RunGameLauncher()
    {
        while (true)
        {
            ShowPlatformSelection();
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "0":
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  Doviđenja!");
                    Console.ResetColor();
                    return;
                    
                case "1":
                    await ShowPlatformGames("Steam");
                    break;
                    
                case "2":
                    await ShowPlatformGames("Epic Games");
                    break;
                    
                case "3":
                    await ShowPlatformGames("");
                    break;
                    
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  Nevalidan izbor. Pokušajte ponovo.");
                    Console.ResetColor();
                    Thread.Sleep(1500);
                    break;
            }
        }
    }

    private static async Task ShowPlatformGames(string platformFilter)
    {
        var allGames = new List<GameInfo>();
        
        if (string.IsNullOrEmpty(platformFilter) || platformFilter == "Steam")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Učitavanje Steam igara...");
            Console.ResetColor();
            allGames.AddRange(GetSteamGames());
        }
        
        if (string.IsNullOrEmpty(platformFilter) || platformFilter == "Epic Games")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Učitavanje Epic Games igara...");
            Console.ResetColor();
            allGames.AddRange(GetEpicGames());
        }
        
        while (true)
        {
            ShowGamesList(allGames, platformFilter);
            
            var input = Console.ReadLine();
            
            if (input == "0")
            {
                break;
            }
            
            if (int.TryParse(input, out int gameIndex) && gameIndex > 0 && gameIndex <= allGames.Count)
            {
                var filteredGames = string.IsNullOrEmpty(platformFilter) 
                    ? allGames 
                    : allGames.Where(g => g.Platform.Equals(platformFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                
                if (gameIndex <= filteredGames.Count)
                {
                    var game = filteredGames[gameIndex - 1];
                    LaunchGame(game);
                    
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("\n  Pritisnite bilo koju tipku za nastavak...");
                    Console.ResetColor();
                    Console.ReadKey();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  Nevalidan unos. Pokušajte ponovo.");
                Console.ResetColor();
                Thread.Sleep(1000);
            }
        }
    }
}
