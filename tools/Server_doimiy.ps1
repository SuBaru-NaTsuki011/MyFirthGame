# ANDIJON: SALTANATLAR — serverni doimiy ishlatish (fon rejimi)
#
# Windows'ga kirganda "Andijon Saltanatlar Server" vazifasi shu skriptni yashirin ishga tushiradi.
#   * O'yin serveri (port 5081) to'xtab qolsa — 5 soniyada qayta ishga tushiriladi.
#   * Server ishlab turganda kompyuter o'zi uyquga ketmaydi (ekran o'chishi mumkin).
#   * release\tools\cloudflared.exe bo'lsa — internet tunneli ham ochiladi, manzil
#     ish stolidagi "Andijon_server_manzili.txt" va release\server_manzili.txt fayllariga yoziladi.
# Jurnallar: release\logs\
# To'xtatish: tools\Server_toxtatish.ps1 (yoki ish stolidagi Andijon_Server_toxtatish.bat).

$ErrorActionPreference = "Continue"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$releaseDir = Join-Path $root "release"
$serverDir = Join-Path $releaseDir "server-google"
$cf = Join-Path $releaseDir "tools\cloudflared.exe"
$logDir = Join-Path $releaseDir "logs"
$urlFile = Join-Path $releaseDir "server_manzili.txt"
$desktop = [Environment]::GetFolderPath("Desktop")
$port = 5081
New-Item -ItemType Directory -Force $logDir | Out-Null

# Faqat bitta nusxa ishlaydi.
$mutex = New-Object System.Threading.Mutex($false, "Local\AndijonSaltanatlarServerRunner")
if (-not $mutex.WaitOne(0)) { exit 0 }

function Log($text) {
    $line = (Get-Date -Format "yyyy-MM-dd HH:mm:ss") + "  " + $text
    Add-Content -Path (Join-Path $logDir "runner.log") -Value $line -Encoding UTF8
}

# Uyquga ketmaslik (ES_CONTINUOUS | ES_SYSTEM_REQUIRED) — skript ishlab turguncha amal qiladi.
Add-Type -Namespace Andijon -Name Power -MemberDefinition '[DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint esFlags);'
[void][Andijon.Power]::SetThreadExecutionState([uint32]"0x80000001")

function Healthy {
    try { return (Invoke-WebRequest -UseBasicParsing "http://localhost:$port/health" -TimeoutSec 3).StatusCode -eq 200 } catch { return $false }
}

function Start-Hidden($file, $arguments, $workDir) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $file
    $psi.Arguments = $arguments
    $psi.WorkingDirectory = $workDir
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    return [System.Diagnostics.Process]::Start($psi)
}

$dotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source }
$serverLog = Join-Path $logDir "server.log"
$tunnelLog = Join-Path $logDir "tunnel.log"

$server = $null; $tunnel = $null; $fails = 0; $lastUrl = $null
Log "Ishga tushdi."
while ($true) {
    # --- Server ---
    if (Healthy) { $fails = 0 }
    else {
        $fails++
        $ownAlive = $server -and -not $server.HasExited
        if (-not $ownAlive -and $fails -ge 2) {
            Log "Server javob bermayapti — ishga tushirilmoqda."
            $server = Start-Hidden "cmd.exe" "/c `"`"$dotnet`" AndijonSaltanatlar.Server.dll --urls http://0.0.0.0:$port >> `"$serverLog`" 2>&1`"" $serverDir
            $fails = -6   # ishga tushishi uchun ~1 daqiqa kutiladi
        }
    }

    # --- Internet tunneli (cloudflared o'rnatilgan bo'lsa) ---
    if ((Test-Path $cf) -and (-not $tunnel -or $tunnel.HasExited)) {
        if (Test-Path $tunnelLog) { Remove-Item $tunnelLog -Force -ErrorAction SilentlyContinue }
        Log "Internet tunneli ochilmoqda."
        $tunnel = Start-Hidden $cf "tunnel --no-autoupdate --url http://localhost:$port --logfile `"$tunnelLog`"" $releaseDir
        $lastUrl = $null
    }
    if ($tunnel -and -not $tunnel.HasExited -and -not $lastUrl -and (Test-Path $tunnelLog)) {
        $m = Select-String -Path $tunnelLog -Pattern "https://[a-z0-9-]+\.trycloudflare\.com" | Select-Object -First 1
        if ($m) {
            $lastUrl = $m.Matches[0].Value
            $info = "ANDIJON: SALTANATLAR — server manzili`r`n`r`nInternet (do'stingiz uchun): $lastUrl`r`nShu kompyuterda: http://localhost:$port`r`n`r`nYangilangan: $(Get-Date -Format 'yyyy-MM-dd HH:mm')`r`nEslatma: kompyuter qayta yoqilganda internet manzili o'zgaradi."
            Set-Content -Path $urlFile -Value $info -Encoding UTF8
            Set-Content -Path (Join-Path $desktop "Andijon_server_manzili.txt") -Value $info -Encoding UTF8
            Log "Internet manzili: $lastUrl"
        }
    }
    Start-Sleep -Seconds 10
}
