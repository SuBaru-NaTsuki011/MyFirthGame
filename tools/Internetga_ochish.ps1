# ANDIJON: SALTANATLAR — serverni internetga ochish (do'stingiz boshqa joyda/boshqa internetda bo'lsa ham o'ynash uchun)
#
# Server "Andijon Saltanatlar Server" vazifasi orqali doimiy ishlaydi (tools\Server_doimiy.ps1).
# Bu skript:
#   1) cloudflared (Cloudflare tunnel) yo'q bo'lsa — ruxsat so'rab, rasmiy GitHub sahifasidan yuklaydi
#      va Cloudflare raqamli imzosini tekshiradi (bir marta);
#   2) fon xizmatini qayta ishga tushiradi — u endi tunnelni ham o'zi ochadi va doim ushlab turadi;
#   3) internet manzilini ekranga chiqaradi va buferga nusxalaydi
#      (manzil ish stolidagi "Andijon_server_manzili.txt" faylida ham turadi).
# Eslatma: bepul "quick tunnel" manzili kompyuter qayta yoqilganda yangilanadi — faylga qarang.

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$releaseDir = Join-Path $root "release"
$toolsDir = Join-Path $releaseDir "tools"
$cf = Join-Path $toolsDir "cloudflared.exe"
$urlFile = Join-Path $releaseDir "server_manzili.txt"
$task = "Andijon Saltanatlar Server"

function Say($text, $color = "Gray") { Write-Host $text -ForegroundColor $color }

Say "==============================================" "DarkYellow"
Say "  ANDIJON: SALTANATLAR — serverni internetga ochish" "Yellow"
Say "==============================================" "DarkYellow"

# 1) cloudflared
if (-not (Test-Path $cf)) {
    Say "Internetga chiqish uchun Cloudflare'ning bepul 'cloudflared' dasturi kerak (rasmiy: github.com/cloudflare/cloudflared)." "Cyan"
    $yes = Read-Host "Yuklab olinsinmi? Ha bo'lsa h yozing, Enter bosing"
    if ($yes.Trim() -notmatch '^(h|ha)$') { Say "Bekor qilindi." "Gray"; Read-Host "Enter"; exit 0 }
    Say "cloudflared yuklanmoqda..." "Cyan"
    New-Item -ItemType Directory -Force $toolsDir | Out-Null
    $tmp = "$cf.download"
    Invoke-WebRequest -UseBasicParsing "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe" -OutFile $tmp
    $sig = Get-AuthenticodeSignature $tmp
    if ($sig.Status -ne "Valid" -or $sig.SignerCertificate.Subject -notmatch "Cloudflare") {
        Remove-Item $tmp -Force
        Say "Yuklangan fayl Cloudflare imzosiga ega emas — xavfsizlik uchun bekor qilindi." "Red"; Read-Host "Enter"; exit 1
    }
    Move-Item $tmp $cf -Force
    Say "cloudflared tayyor (imzo: Cloudflare, tekshirildi)." "Green"
}

# 2) Fon xizmatini qayta ishga tushirish (server + tunnel)
if (-not (Get-ScheduledTask -TaskName $task -ErrorAction SilentlyContinue)) {
    Say "'$task' vazifasi topilmadi. Avval serverni doimiy rejimga o'rnating (docs/GOOGLE_LOGIN.md)." "Red"; Read-Host "Enter"; exit 1
}
$before = if (Test-Path $urlFile) { (Get-Item $urlFile).LastWriteTime } else { [datetime]::MinValue }
$running = Get-CimInstance Win32_Process | Where-Object { $_.Name -eq "cloudflared.exe" -and $_.CommandLine -match "localhost:5081" }
if (-not $running) {
    Say "Fon xizmati qayta ishga tushirilmoqda (server + tunnel)..." "Cyan"
    & (Join-Path $PSScriptRoot "Server_toxtatish.ps1") | Out-Null
    Start-ScheduledTask -TaskName $task
} else { $before = [datetime]::MinValue }

# 3) Manzilni kutish
Say "Internet manzili olinmoqda..." "Cyan"
$url = $null
for ($i = 0; $i -lt 90 -and -not $url; $i++) {
    if ((Test-Path $urlFile) -and (Get-Item $urlFile).LastWriteTime -gt $before) {
        $m = Select-String -Path $urlFile -Pattern "https://[a-z0-9-]+\.trycloudflare\.com" | Select-Object -First 1
        if ($m) { $url = $m.Matches[0].Value; break }
    }
    Start-Sleep -Seconds 1
}
if (-not $url) { Say "Manzilni olib bo'lmadi (internetni tekshiring). Jurnal: $releaseDir\logs\" "Red"; Read-Host "Enter"; exit 1 }
try { Set-Clipboard -Value $url } catch {}

Say ""
Say "=================== TAYYOR ===================" "Green"
Say "Do'stingizga yuboring:" "Yellow"
Say "  Server manzili: $url" "White"
Say "  Kirish:         Google akkaunt (telefonda — ro'yxatdan tanlanadi)" "White"
Say "(manzil buferga nusxalandi va ish stolidagi Andijon_server_manzili.txt faylida turadi)" "DarkGray"
Say ""
Say "Server va tunnel fon rejimida DOIMIY ishlaydi — bu oynani yopsangiz ham bo'ladi." "Green"
Say "Kompyuter qayta yoqilganda manzil yangilanadi — yangisi o'sha faylda bo'ladi." "Gray"
Read-Host "Yopish uchun Enter"
