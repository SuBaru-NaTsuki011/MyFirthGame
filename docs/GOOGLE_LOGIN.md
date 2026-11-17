# Google orqali kirish va doimiy onlayn o'yin

O'yinda kirish **faqat Google hisobi orqali**. Login/parol bilan ro'yxatdan o'tish o'chirilgan:
`/api/auth/register` va `/api/auth/login` so'rovlari 403 qaytaradi.
O'yin holati serverda saqlanadi. Har 30 soniyada, yer ochilganda, oyna yopilganda va telefon fonga o'tganda
avtomatik saqlanadi. Shuning uchun o'yinchi istalgan qurilmada qolgan joyidan davom etadi.

## Qanday ishlaydi

1. O'yin ochiladi va **"Google bilan kirish"** tugmasi ko'rinadi.
2. Tugma bosilganda server kirish seansini yaratadi va tizim brauzerida Google hisobini tanlash sahifasi ochiladi.
3. Google serverga qaytaradi. Server o'yinchini topadi yoki yaratadi va token beradi.
4. O'yin har 2 soniyada serverdan natijani so'raydi va tokenni oladi.
5. O'yin tokenni qurilmada eslab qoladi, shuning uchun keyingi safar avtomatik kiradi.
6. Serverdagi saltanat yuklanadi. Saltanat hali yo'q bo'lsa, yangisi boshlanadi.

Client secret faqat serverda saqlanadi. Bir hisob bilan ikki qurilmada o'ynalsa, eski qurilma yangi progress ustidan
yoza olmaydi (versiya nazorati, 409). Bunday holda o'yin eng so'nggi holatni o'zi yuklaydi.

## Google Client ID olish (bir martalik)

1. https://console.cloud.google.com saytida yangi loyiha yarating.
2. **APIs & Services → OAuth consent screen** bo'limida turini "External" qilib tanlang.
   Ilova nomi va emailni kiriting. Scopes: `openid`, `email`, `profile`.
3. **APIs & Services → Credentials → Create credentials → OAuth client ID** bo'limida turini **Web application** qilib tanlang.
4. **Authorized redirect URIs** ro'yxatiga quyidagini qo'shing:
   `https://<server-domeningiz>/auth/google/callback`
   Kompyuterda sinash uchun `http://localhost:5080/auth/google/callback` ham bo'ladi.
5. Client ID va Client Secret'ni serverga kiriting. Secret'ni kodga yozmang, muhit o'zgaruvchisidan foydalaning:

```powershell
$env:Google__ClientId = "xxxx.apps.googleusercontent.com"
$env:Google__ClientSecret = "GOCSPX-..."
$env:Google__PublicBaseUrl = "https://andijon.example.uz"   # telefonlar ulanadigan manzil
$env:Jwt__Key = "kamida-32-belgili-maxfiy-kalit-..."
dotnet AndijonSaltanatlar.Server.dll
```

**Muhim:** Google faqat `https://` yoki `http://localhost` redirect manzillarini qabul qiladi.
Telefonlar haqiqiy Google bilan kirishi uchun server internetda HTTPS domen bilan ishlashi kerak
(yoki Cloudflare Tunnel / ngrok kabi tunnel orqali). Bunda o'yindagi "Server" maydoniga shu `https://` manzil kiritiladi.

### Kalitlarni saqlash (`google.local.json`)

Server o'z papkasidagi `google.local.json` faylini o'qiydi. Bu fayl git'ga qo'shilmaydi:

```json
{ "Google": { "ClientId": "....apps.googleusercontent.com", "ClientSecret": "GOCSPX-..." } }
```

Joriy sozlama:

- **Loyiha:** "Andijon Saltanatlar" (Google Cloud Console).
- **Holat:** "Testing". Kira oladiganlar: faqat Audience → Test users ro'yxatidagilar.
- **Web client:** redirect `http://localhost:5081/auth/google/callback`.
- **Android client:** paket `uz.andijon.saltanatlar`, SHA-1 `27:93:43:AF:44:09:BC:BA:B3:15:F4:1F:86:B9:D7:AD:73:B7:C1:27`.

Sinov sahifasi (DevLogin) orqali ochilgan akkaunt shu Gmail bilan haqiqiy Google'da birinchi marta kirilganda
avtomatik ulanadi (`GoogleSub` `dev:email` → haqiqiy `sub`). Saltanat va saqlash saqlanib qoladi.

## Telefondagi Google akkauntlar ro'yxati (Android)

Android'da "Google bilan kirish" bosilganda brauzer ochilmaydi. Buning o'rniga tizim oynasi chiqadi
(Android Credential Manager) va unda **telefondagi barcha Google akkauntlar** ro'yxati ko'rinadi.
O'yinchi bittasini tanlaydi. Telefon Google ID token beradi, server esa uning imzosini Google'ning ochiq
kalitlari bilan tekshiradi (`/api/auth/google/idtoken`). Bu oyna ishlamasa (Google Play xizmatlari yo'q,
yoki ilova Google Cloud'da ro'yxatdan o'tmagan), o'yin avtomatik ravishda brauzer orqali kirishga o'tadi.

Bu oyna ishlashi uchun Google Cloud'da **ikkita** OAuth Client ID kerak (bir loyiha ichida):

1. **Web application** turidagi Client ID. Bu serverdagi `Google:ClientId` va `Google:ClientSecret`.
   Telefon uni serverdan o'zi oladi (`/api/auth/google/config`), shuning uchun APK'ni qayta yig'ish shart emas.
2. **Android** turidagi Client ID. Uning sozlamalari:
   - Package name: `uz.andijon.saltanatlar`
   - SHA-1 certificate fingerprint: `27:93:43:AF:44:09:BC:BA:B3:15:F4:1F:86:B9:D7:AD:73:B7:C1:27`
     (bu kompyuterdagi debug kalit; APK shu kalit bilan imzolangan).
   Play Store uchun alohida release kaliti yaratilsa, uning SHA-1 qiymati ham qo'shiladi.

Android Client ID'ni o'yinga yozish kerak emas: Google uni paket nomi va imzo orqali o'zi aniqlaydi.

## Sinov rejimi (Google sozlanmaguncha)

`Google:DevLogin=true` yoqilsa (`appsettings.Development.json` da yoqilgan), haqiqiy Google o'rniga
**"SINOV REJIMI"** sahifasi ochiladi. Unda email va ism kiritiladi. Bu faqat lokal sinov uchun.
Bu rejimda istalgan email bilan kirish mumkin, shuning uchun **production'da hech qachon yoqmang**.
Haqiqiy `ClientId` va `ClientSecret` kiritilgach, `DevLogin` qiymatidan qat'i nazar haqiqiy Google ishlatiladi.

```powershell
dotnet AndijonSaltanatlar.Server.dll --Google:DevLogin=true
```

Eski avtomatik sinov skriptlari (`tools/e2e_*.ps1`) parol bilan ro'yxatdan o'tadi. Ular faqat server
`--Auth:AllowPasswordLogin=true` bilan ishga tushirilganda ishlaydi.

### Taklif kodi (`Google:DevLoginCode`)

Server internetga ochilganda begona odam sinov sahifasidan kira olmasligi uchun taklif kodi qo'shing:

```powershell
dotnet AndijonSaltanatlar.Server.dll --Google:DevLogin=true --Google:DevLoginCode=K7PX2M
```

Kod berilgan bo'lsa, sinov sahifasida "Taklif kodi" maydoni chiqadi. Noto'g'ri kod kiritilsa server
403 qaytaradi va akkaunt yaratilmaydi. Kodda katta-kichik harf farqi hisobga olinmaydi.

## Serverni doimiy ishlatish

Windows Task Scheduler'da **"Andijon Saltanatlar Server"** vazifasi bor (foydalanuvchi Windows'ga kirganda ishga tushadi,
administrator huquqi kerak emas). U `tools/Server_doimiy.ps1` ni yashirin ishga tushiradi:

- **Server:** port 5081. To'xtab qolsa, taxminan 20 soniyada qayta ishga tushadi.
- **Uyqu:** server ishlab turganda kompyuter o'zi uyquga ketmaydi. Ekran o'chishi mumkin.
  Noutbuk qopqog'i yopilganda nima bo'lishi Windows quvvat sozlamasiga bog'liq.
- **Internet tunneli:** `release/tools/cloudflared.exe` bo'lsa, tunnel ham ochiladi va ushlab turiladi.
  Manzil `release/server_manzili.txt` va ish stolidagi `Andijon_server_manzili.txt` ga yoziladi.
- **Jurnallar:** `release/logs/` (`runner.log`, `server.log`, `tunnel.log`).

To'xtatish uchun `tools/Server_toxtatish.ps1` yoki ish stolidagi `Andijon_Server_toxtatish.bat`. Avtomatik ishga tushishni
butunlay o'chirish uchun: `Server_toxtatish.ps1 -Butunlay`.

## Boshqa joydan (boshqa internetdan) o'ynash

Bir xil Wi-Fi shart emas. `tools/Internetga_ochish.ps1` (ish stolida `Andijon_Internet_Server.bat`) quyidagilarni qiladi:

1. `cloudflared` yo'q bo'lsa, ruxsat so'raydi. Ruxsat berilsa, rasmiy GitHub sahifasidan yuklaydi va
   Cloudflare raqamli imzosini tekshiradi.
2. `release/taklif_kodi.txt` dagi taklif kodi bilan serverni ishga tushiradi (yoki ishlab turgan serverdan foydalanadi).
3. Bepul Cloudflare "quick tunnel" ochadi va `https://....trycloudflare.com` manzilini ekranga,
   buferga va ish stolidagi `Andijon_server_manzili.txt` fayliga yozadi.

Do'stingiz telefonda kirish oynasida server qatoridagi "O'zgartirish"ni bosib shu manzilni yozadi.
Keyin kirish sahifasida taklif kodini kiritadi. Domen nomi sxemasiz yozilsa, o'yin avtomatik `https://`
qo'shadi. IP manzil yoki `localhost` uchun `http://` qo'shiladi.

Server `X-Forwarded-Proto` sarlavhasini faqat shu kompyuterdagi proksidan qabul qiladi
(`UseForwardedHeaders`, standart: loopback). Shuning uchun tunnel ortida kirish havolalari `https` bo'ladi.
Quick tunnel manzili har ishga tushirishda yangilanadi.

## Yer kengaytirish (halqalar)

- Xarita 64×64 dan **144×144** ga kattalashdi. Shahar xarita markazida turadi.
- **1-halqa:** shahar devori atrofidagi 4 mahalla (shimol, janub, g'arb, sharq).
- 1-halqaning **4 tomoni ham ochilgach**, 2-halqaning 4 tomoni ochiladi: shimol — yaylov (chorvachilik),
  janub — dalalar (dehqonchilik), g'arb — bog'lar, sharq — karvon yo'li (savdo). Shu tartibda 5-halqagacha davom etadi.
- Yerni bundan ham kattalashtirish keyingi bosqichda boshqa yerlarni bosib olish orqali bo'ladi.
- Eski saqlashlar yuklanganda binolar, yo'llar va kamera yangi xaritaga avtomatik ko'chiriladi.

## Sinov parametrlari (Windows client)

| Parametr | Vazifasi |
|---|---|
| `--login` | Kirish ekranini majburan ko'rsatish |
| `--server URL` | Standart server manzili |
| `--data-dir PAPKA` | Token va sozlamalar uchun alohida papka (sinov haqiqiy hisobga tegmaydi) |
| `--login-url-file F` | Brauzer ochilmaydi, login manzili faylga yoziladi (Playwright o'zi ochadi) |
| `--shot-after-login` | `--screenshot` kirib bo'lingandan keyin olinadi |
| `--unlock-parcel ID` | Kirgandan so'ng uchastkani ochib, serverga saqlash |
| `--phone` | PC'da telefon interfeysi (UI masshtabi, katta tugmalar) |
