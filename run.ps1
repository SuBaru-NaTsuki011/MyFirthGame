# Andijon: Saltanatlar — tez ishga tushirish skripti.
# Foydalanish:  o'ng tugma -> "Run with PowerShell"  yoki terminalda:  ./run.ps1
$ErrorActionPreference = "Stop"
$proj = Join-Path $PSScriptRoot "AndijonSaltanatlar/AndijonSaltanatlar.csproj"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host ".NET SDK topilmadi." -ForegroundColor Red
    Write-Host "Avval .NET 8 SDK ni o'rnating:  https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    Write-Host "Yoki:  winget install Microsoft.DotNet.SDK.8" -ForegroundColor Yellow
    exit 1
}

Write-Host "Andijon: Saltanatlar ishga tushirilmoqda..." -ForegroundColor Cyan
dotnet run --project $proj -c Debug
