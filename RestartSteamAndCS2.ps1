param(
    [string]$CS2AppID = "730"
)

$ErrorActionPreference = "Stop"

function Find-SteamPath {
    $steamPath = $null
    
    $registryPaths = @(
        "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam",
        "HKLM:\SOFTWARE\Valve\Steam",
        "HKCU:\SOFTWARE\Valve\Steam"
    )
    
    foreach ($regPath in $registryPaths) {
        if (Test-Path $regPath) {
            $installPath = (Get-ItemProperty -Path $regPath -Name "InstallPath" -EA SilentlyContinue).InstallPath
            if ($installPath -and (Test-Path "$installPath\steam.exe")) {
                $steamPath = "$installPath\steam.exe"
                break
            }
        }
    }
    
    if (-not $steamPath) {
        $commonPaths = @(
            "C:\Program Files (x86)\Steam\steam.exe",
            "C:\Program Files\Steam\steam.exe",
            "$env:LOCALAPPDATA\Steam\steam.exe",
            "$env:ProgramFiles(x86)\Steam\steam.exe"
        )
        
        foreach ($path in $commonPaths) {
            if (Test-Path $path) {
                $steamPath = $path
                break
            }
        }
    }
    
    return $steamPath
}

function Get-CdpErrors($port, $sec) {
    $err = @()
    try {
        $ws = New-Object System.Net.WebSockets.ClientWebSocket
        $ct = [Threading.CancellationToken]::None
        $ws.ConnectAsync((Invoke-RestMethod "http://localhost:$port/json" -TimeoutSec 3)[0].webSocketDebuggerUrl, $ct).Wait()
        '{"id":1,"method":"Runtime.enable"}','{"id":2,"method":"Log.enable"}' | % { $ws.SendAsync([ArraySegment[byte]][Text.Encoding]::UTF8.GetBytes($_), 'Text', $true, $ct).Wait() }
        $buf = [byte[]]::new(32768); $end = (Get-Date).AddSeconds($sec)
        while ((Get-Date) -lt $end -and $ws.State -eq 'Open') {
            $r = $ws.ReceiveAsync([ArraySegment[byte]]$buf, $ct)
            if ($r.Wait(500) -and $r.Result.Count -gt 0) {
                $j = [Text.Encoding]::UTF8.GetString($buf,0,$r.Result.Count) | ConvertFrom-Json -EA SilentlyContinue
                if ($j.method -match "exceptionThrown|consoleAPICalled|entryAdded" -and ($j.method -eq "Runtime.exceptionThrown" -or $j.params.type -eq "error" -or $j.params.entry.level -eq "error")) { $err += $j }
            }
        }
        $ws.CloseAsync('NormalClosure', "", $ct).Wait()
    } catch {}
    $err
}

Write-Host "=== Steam & CS2 Restart Script ===" -ForegroundColor Cyan

Write-Host "`n[1/4] Stopping Steam processes..." -ForegroundColor Yellow
$steamProcesses = @("steam", "steamwebhelper", "SteamService")
foreach ($proc in $steamProcesses) {
    Get-Process -Name $proc -EA SilentlyContinue | ForEach-Object {
        Write-Host "  -> Stopping $_" -ForegroundColor Gray
        Stop-Process -Id $_.Id -Force -EA SilentlyContinue
    }
}
Start-Sleep -Seconds 2
Write-Host "  -> All Steam processes stopped" -ForegroundColor Green

Write-Host "`n[2/4] Finding Steam installation..." -ForegroundColor Yellow
$SteamPath = Find-SteamPath

if (-not $SteamPath -or !(Test-Path $SteamPath)) {
    Write-Host "ERROR: Could not find Steam installation!" -ForegroundColor Red
    Write-Host "Please install Steam or manually specify the path" -ForegroundColor Red
    exit 1
}

Write-Host "  -> Found Steam at: $SteamPath" -ForegroundColor Green

Write-Host "`n[3/4] Starting Steam..." -ForegroundColor Yellow

$steamArgs = "-silent"
$steamProc = Start-Process -FilePath $SteamPath -ArgumentList $steamArgs -PassThru
Write-Host "  -> Steam started (PID: $($steamProc.Id))" -ForegroundColor Green

Write-Host "`n[4/4] Waiting for Steam to initialize..." -ForegroundColor Yellow
$timeout = 60
$elapsed = 0
$steamReady = $false

while ($elapsed -lt $timeout) {
    Start-Sleep -Seconds 2
    $elapsed += 2
    
    $webHelper = Get-Process -Name "steamwebhelper" -EA SilentlyContinue | Where-Object { $_.MainWindowHandle -ne [IntPtr]::Zero }
    if ($webHelper) {
        $steamReady = $true
        break
    }
    Write-Host "  -> Waiting... ($elapsed/$timeout seconds)" -ForegroundColor Gray
}

if (!$steamReady) {
    Write-Host "WARNING: Steam may not be fully ready, continuing anyway..." -ForegroundColor Yellow
} else {
    Write-Host "  -> Steam is ready" -ForegroundColor Green
}

Write-Host "`n[5/5] Starting CS2..." -ForegroundColor Yellow
$cs2LaunchUrl = "steam://run/$CS2AppID"

try {
    Start-Process -FilePath $cs2LaunchUrl
    Write-Host "  -> CS2 launch command sent!" -ForegroundColor Green
    Write-Host "`n=== DONE ===" -ForegroundColor Cyan
    Write-Host "CS2 should be starting now. Enjoy!" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Failed to launch CS2: $_" -ForegroundColor Red
    exit 1
}
