using Microsoft.Win32;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;

namespace SteamCS2Starter;

public class Program
{
    private const string Cs2AppId = "730";
    private const int WaitTimeoutSeconds = 60;
    private const int WaitIntervalMs = 1500;
    
    private const string UpdateUrl = "https://raw.githubusercontent.com/ivke-dev/SteamCS2Starter/main/version.json";

    private static string CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

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
}
