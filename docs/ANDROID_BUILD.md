# Android + Windows crossplay — build va qurilmada sinash (PROMPT 19A)

## 1. Server (kompyuterda)

```powershell
$env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"
dotnet build AndijonSaltanatlar.Server -c Debug
cd AndijonSaltanatlar.Server\bin\Debug\net8.0
dotnet AndijonSaltanatlar.Server.dll          # http://0.0.0.0:5080 da tinglaydi
```

- Ma'lumotlar bazasi: standart holatda **SQLite** (`andijon_server.db`, server papkasida) — PostgreSQL o'rnatilmagan kompyuterda lokal sinov uchun. Server qayta ishga tushganda ma'lumot saqlanadi.
- PostgreSQL bilan ishlatish: `docker compose up -d`, so'ng serverni `--Database:Provider=Postgres` bilan ishga tushiring (ulanish satri `ConnectionStrings:Default`).
  **Diqqat:** `EnsureCreated` mavjud eski bazaga yangi jadvallarni qo'shmaydi — PROMPT 19A jadvallari uchun bo'sh baza kerak (yoki EF migratsiyalari).
- Tekshirish: brauzerda `http://<kompyuter-IP>:5080/health` → `{"ok":true,...}`.

### Windows Firewall (telefon ulanishi uchun bir marta, administrator PowerShell'da)

```powershell
New-NetFirewallRule -DisplayName "Andijon Saltanatlar server 5080" -Direction Inbound -Protocol TCP -LocalPort 5080 -Action Allow -Profile Private
```

Wi-Fi tarmog'i Windows'da **Private** (Shaxsiy) deb belgilangan bo'lishi kerak. Telefondan `http://<IP>:5080/health` brauzerda ochilsa — ulanish tayyor.

## 2. Windows client

```powershell
dotnet run --project AndijonSaltanatlar
```

O'yinda yuqori paneldagi **ONLINE** tugmasi (yoki `O` klavishi) → server manzili, login, parol → **KIRISH** / **RO'YXATDAN O'TISH**.

## 3. Android APK

Bir martalik tayyorgarlik (foydalanuvchi papkasiga, administrator huquqi shart emas):

```powershell
dotnet workload install android
dotnet build AndijonSaltanatlar.Android -t:InstallAndroidDependencies -f net10.0-android `
  "-p:AndroidSdkDirectory=$env:LOCALAPPDATA\Android\Sdk" "-p:JavaSdkDirectory=$env:LOCALAPPDATA\Microsoft\jdk-17" `
  -p:AcceptAndroidSDKLicenses=True
```

APK yig'ish:

```powershell
dotnet publish AndijonSaltanatlar.Android -c Release -f net10.0-android `
  "-p:AndroidSdkDirectory=$env:LOCALAPPDATA\Android\Sdk" "-p:JavaSdkDirectory=$env:LOCALAPPDATA\Microsoft\jdk-17"
```

Natija: `AndijonSaltanatlar.Android\bin\Release\net10.0-android\publish\uz.andijon.saltanatlar-Signed.apk`
(sinov uchun debug kalit bilan imzolangan; Play Store uchun alohida release kaliti kerak).

Telefonga o'rnatish: APK'ni telefonga ko'chirib oching ("Noma'lum manbalardan o'rnatish"ga ruxsat bering) yoki USB orqali:
`adb install -r <apk>` (`adb` — `%LOCALAPPDATA%\Android\Sdk\platform-tools`).

## 4. Qurilmada qo'lda sinov ssenariysi (PC → Android savdo)

| # | Qayerda | Qadam | Kutilgan natija |
|---|---------|-------|-----------------|
| 1 | PC | Server ishga tushdi, `/health` telefonda ham ochiladi | `ok: true` |
| 2 | Windows | ONLINE → server `http://<IP>:5080`, login `pc1`, parol → RO'YXATDAN O'TISH | GURUCH bo'limi, pastda "ULANGAN", ONLAYN: PC1 |
| 3 | Android | Ilova ochiladi → ONLINE panel. Server `http://<IP>:5080` (**localhost emas!**), login `tel1` → RO'YXATDAN O'TISH | ONLAYN ro'yxatida PC1 va TEL1 |
| 4 | Windows | MAYDON QURISH → GURUCH EKISH | "Ekildi", ~1 daqiqadan keyin "O'smoqda" |
| 5 | Windows | ~3 daqiqa kuting → HOSILNI YIG'ISH | GURUCH +120. Tugma qayta bosilsa hosil qo'shilmaydi |
| 6 | Windows | SOTISH: miqdor 100, narx 500 → SOTUVGA QO'YISH | "100 (+100 SOTUVDA)", taklif ro'yxatda |
| 7 | Android | SOTIB OLISH bo'limi (barmoq bilan tegish) | PC1 taklifi: 100 guruch — 500 tanga |
| 8 | Android | SOTIB OLISH | Telefon: tanga −500, guruch +100 |
| 9 | Windows | (hech narsa bosmasdan) | "Guruchingiz sotildi: +500 tanga" xabari, tanga +500 |
| 10 | Android | Xuddi shu taklifni yana bosish | Taklif ro'yxatda yo'q / "allaqachon sotilgan" |
| 11 | PC | Serverni to'xtatib qayta ishga tushirish, ikkala clientda qayta kirish | Tanga va guruch o'zgarmagan |
| 12 | Android | Xaritani bir barmoq bilan surish, ikki barmoq bilan zoom, tegib tanlash | Kamera suriladi/zoom bo'ladi, bino tanlanadi |

Har bir qadam natijasini `docs/PROMPT_19A_PROGRESS.md` dagi "Qurilmada sinov" jadvaliga yozing.

## 5. Internet (HTTPS) ga tayyorgarlik

- Server Kestrel'da HTTPS sertifikat bilan yoki reverse-proxy (nginx/Caddy) orqasida ishlatiladi; client'da server manziliga `https://domen` yoziladi — kod o'zgarmaydi.
- `AndroidManifest.xml` dagi `usesCleartextTraffic` ni `false` qiling.
- `Jwt:Key` (yoki `ANDIJON_JWT_KEY`) — kamida 32 belgili maxfiy kalit; standart dev kalit faqat lokal sinov uchun.
