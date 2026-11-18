# Google login, doimiy hisob va VIP — bajarilgan ishlar

Oxirgi yangilanish: 2026-11-18

## 1. Umumiy holat

| Qism | Holat |
|---|---|
| Windows client (Debug + Release publish) | Bajarildi |
| Server (Release publish + live 5081) | Bajarildi |
| Avtomatik testlar | **161 / 161 o'tdi** |
| Google Cloud real login | Sozlangan, lekin brauzerda haqiqiy login sinalmagan |
| Android build | Bloklangan: JDK va `android` workload yo'q |

## 2. Google autentifikatsiya

- Kirish faqat Google Identity orqali: `openid`, `email`, `profile`.
- Windows: tizim brauzeri + PKCE + `state` + `nonce`; natija `/api/auth/google/poll` orqali olinadi.
- Android: Credential Manager (telefondagi Google akkauntlar ro'yxati) → serverda ID-token imzosi tekshiriladi. Ishlamasa brauzer oqimiga avtomatik qaytadi.
- Google `sub` — doimiy identifikator. Bir xil Google hisob qaysi qurilmada kirsa ham bir xil `AppUser`, `PlayerId`, saltanat va bulut saqlashga ulanadi.
- Email faqat shu qurilmada ko'rsatiladi; onlayn reytingda va boshqa o'yinchilarga faqat taxallus chiqadi.
- Client secret faqat serverda (`google.local.json` yoki muhit o'zgaruvchilari). APK/exe ichida secret yo'q.

### Sessiyalar

- Qisqa muddatli JWT access token (1 soat) + serverdagi yangilash tokeni (60 kun).
- Yangilash tokeni har ishlatishda almashtiriladi (rotation); eski token qayta ishlatilsa sessiya bekor qilinadi.
- Chiqishda serverdagi sessiya bekor qilinadi va bekor qilingan JWT darhol rad etiladi.
- Windows: tokenlar DPAPI (CurrentUser) bilan; Android: Keystore AES-256-GCM kaliti bilan shifrlanadi. Ochiq JSON'da token saqlanmaydi.
- Token muddati tugashi, internet uzilishi va sessiya yo'qolishi uchun alohida UI holatlari bor.

## 3. YANGI HUKMDOR → DUNYONI TANLASH → YURTNI TANLASH

Yangi o'yinchi profilni (taxallus, avatar) saqlagach yangi ekran ochiladi:

1. `GET /api/online/worlds` — dunyolar, o'yinchilar soni, bo'sh hududlar soni va o'z saltanati qaysi dunyoda ekani.
2. `POST /api/online/join` — tanlangan dunyoga qo'shilish (yangi hisob uchun saltanat yaratiladi).
3. `GET /api/online/start-regions` — bo'sh hududlar.
4. `POST /api/online/start` — hudud, saltanat nomi va bayroqni bir marta saqlash.

Qaytgan o'yinchi o'z dunyosini va saltanatini ko'radi; bir hisobda bitta saltanat modeli saqlanib qolgan. Boshqa dunyo kartasi bosilmaydi. Hudud band bo'lib qolsa (409), ro'yxat avtomatik yangilanadi va o'yinchi boshqa bo'sh hudud tanlaydi.

## 4. VIP hisoblar

### Qanday aniqlanadi

Serverdagi `Vip:Accounts` ro'yxati bo'yicha, Google emailning to'liq manzili yoki `@` oldidagi qismi bilan:

```json
"Vip": {
  "Accounts": [
    "anvarovmuhammadamin011",
    "anvarovmuhammadamin2011"
  ]
}
```

- `AppUser.IsVip` bazada saqlanadi; har Google kirishida qayta tekshiriladi.
- Bir xil hisob Windows, Android yoki boshqa qurilmada kirsa — VIP holati bir xil.
- Ro'yxatdan olib tashlansa, keyingi kirishda VIP bekor bo'ladi.
- Production'da ro'yxatni `Vip__Accounts__0`, `Vip__Accounts__1` muhit o'zgaruvchilari orqali ham berish mumkin.

### VIP nimani beradi

Kirgandan keyin `ApplyFullUpgrade()` avtomatik bajariladi:

- barcha binolar oxirgi darajada va to'liq qurilgan;
- barcha texnologiyalar o'rganilgan;
- barcha sotib olinadigan yer halqalari ochilgan, bo'sh yerlar binolar bilan to'ldirilgan;
- barcha jangchilar **xos navkar (VIP/elite)**, harbiy binolarda qorovullar;
- xazina kamida 50 000 tanga, ombor 20 000 oziq-ovqat;
- dehqonchilik ombori to'la, sug'orish va eng yaxshi asboblar;
- aholi uylar sig'imigacha to'ldirilgan.

VIP kuchaytirishdan keyingi holat darhol serverga yuboriladi. Yangi o'yinda dastlabki saqlash avval yuborilib, keyin kuchaytirish ustidan yozilib qolishi oldini olingan (upload race tuzatildi).

Kirish va dunyo tanlash ekranlarida `VIP HUKMDOR` belgisi ko'rsatiladi. Hisob nomi yonida `VIP` yozuvi chiqadi.

## 5. Google Cloud sozlamalari

Kerakli sozlamalar (batafsil: `docs/GOOGLE_LOGIN.md`):

1. **OAuth consent screen:** External; scopes `openid`, `email`, `profile`; test users.
2. **Web application Client ID:** redirect URI
   - `https://<domen>/auth/google/callback`
   - lokal: `http://localhost:5081/auth/google/callback`
3. **Android Client ID:** package `uz.andijon.saltanatlar`,
   SHA-1 `27:93:43:AF:44:09:BC:BA:B3:15:F4:1F:86:B9:D7:AD:73:B7:C1:27`.
4. Serverda `Google:ClientId`, `Google:ClientSecret`, `Google:PublicBaseUrl`.
5. `Google:DevLogin` faqat lokal sinovda `true`; production'da `false`.

Live server `release/server-google` yangilandi: yangi binary, `Vip` sozlamasi va `IsVip` ustuni qo'shildi. Eski DB va Google kalitlari saqlab qolindi.

## 6. Sinov natijalari

### Avtomatik testlar

```
Passed! - Failed: 0, Passed: 161, Skipped: 0, Total: 161
```

Yangi testlar:
- `WorldSelectionTests` — dunyo ro'yxati, a'zolik, qaytgan o'yinchi bir xil saltanatni oladi, band hudud qayta olinmaydi.
- `VipAccountTests` — ikkala VIP akkaunt (local qism va to'liq email), VIP belgisi bazada saqlanishi, sessiya javobida qaytishi, ro'yxatdan olib tashlanganda bekor bo'lishi.
- `AuthSessionTests` — refresh rotation, logout/revocation, cross-device bir xil hisob.

### Windows client + server (dev-login bilan uchdan-uchiga)

Haqiqiy Google o'rniga `Google:DevLogin` orqali ikkala email sinaldi:

| Tekshiruv | Natija |
|---|---|
| `anvarovmuhammadamin011@gmail.com` → `isVip` | `true` |
| `anvarovmuhammadamin2011@gmail.com` → `isVip` | `true` |
| Qaytgan VIP hisob bulutdagi saqlashni tiklaydi | `true` |
| VIP kuchaytirish bulutga saqlanadi | `coins=50000, food=20000, eliteArmy=true` |
| Ochilgan yerlar / binolar / aholi | `20` halqa, `631` bino, `4080` aholi |

Skrinshotlar va vaqtinchalik ma'lumotlar `%TEMP%\andijon-vip-run-*` papkasida. Sinovdan keyin test serveri va test clienti to'xtatildi.

### Haqiqiy Google

Brauzerda haqiqiy Google hisobi bilan login **sinalmagan**: buning uchun Google Cloud test foydalanuvchisi va brauzerda tasdiqlash kerak. Live serverda Google sozlangan (`Google kirish sozlangan (ClientId ...nt.com)`), lekin bu "haqiqiy login o'tdi" degani emas.

## 7. O'zgargan asosiy fayllar

**Server**
- `AndijonSaltanatlar.Server/Authentication/GoogleLoginService.cs`
- `AndijonSaltanatlar.Server/Authentication/VipOptions.cs` (yangi)
- `AndijonSaltanatlar.Server/Authentication/SessionTokenService.cs`
- `AndijonSaltanatlar.Server/Controllers/GoogleAuthController.cs`
- `AndijonSaltanatlar.Server/Controllers/SessionController.cs`
- `AndijonSaltanatlar.Server/Controllers/OnlineController.cs`
- `AndijonSaltanatlar.Server/Services/OnlineGameService.cs`
- `AndijonSaltanatlar.Server/Data/Entities.cs`
- `AndijonSaltanatlar.Server/Program.cs`
- `AndijonSaltanatlar.Server/appsettings.json`

**Shared**
- `AndijonSaltanatlar.Shared/DTOs/GameDtos.cs`
- `AndijonSaltanatlar.Shared/DTOs/OnlineDtos.cs`

**Client**
- `AndijonSaltanatlar/Game1.cs`
- `AndijonSaltanatlar/Online/CloudSession.cs`
- `AndijonSaltanatlar/Online/SecureTokenStorage.cs`
- `AndijonSaltanatlar/UI/GoogleLoginScreen.cs`
- `AndijonSaltanatlar/UI/WorldSelectionScreen.cs` (yangi)
- `AndijonSaltanatlar/Online/OnlineClient.cs`
- `AndijonSaltanatlar/Core/LaunchOptions.cs`

**Testlar**
- `AndijonSaltanatlar.Tests/AuthSessionTests.cs`
- `AndijonSaltanatlar.Tests/WorldSelectionTests.cs` (yangi)
- `AndijonSaltanatlar.Tests/VipAccountTests.cs` (yangi)
- `AndijonSaltanatlar.Tests/GoogleLoginAndLandRingTests.cs`

**Deploy**
- `release/server-google` — yangi server binarlari, `Vip` sozlamasi, DB'ga `IsVip` ustuni.
- `release/windows-ultra` — yangi Windows client binarlari.

## 8. Qolgan bloklar

- **Android build:** `dotnet workload list` bo'sh, `java` yo'q. `NETSDK1139: The target platform identifier android was not recognized`.
- **Haqiqiy Google login:** brauzer/telefon va Google Cloud test foydalanuvchisi bilan qo'lda tasdiqlash kerak.
- **Eski Debug `bin`:** o'ldirilgan test serveridan qolgan `dotnet.exe` (PID 27680) Debug fayllarini ushlab turibdi; Release publish va live serverga ta'siri yo'q. Windows qayta ishga tushirilganda tozalanadi.
