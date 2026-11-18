# Reliz chiqarish va yangilash — qo'llanma

Ilova **GitHub Releases** orqali tarqatiladi. QR kod va ilova ichidagi "Yangilang"
oynasi doim eng so'nggi relizga ishora qiladi. Shuning uchun har relizda APK **aynan
bir xil nom** bilan yuklanishi shart: `AndijonSaltanatlar.apk`.

Repo: `anvarovmuhammadamin011-ops/MyFirthGame`

Doimiy havolalar (o'zgarmaydi, har doim eng so'nggi relizga olib boradi):

- Yuklab olish (QR shu yerga): `https://github.com/anvarovmuhammadamin011-ops/MyFirthGame/releases/latest/download/AndijonSaltanatlar.apk`
- Relizlar sahifasi: `https://github.com/anvarovmuhammadamin011-ops/MyFirthGame/releases/latest`

> **Muhim:** `releases/latest/download/...` havolasi va anonim yangilash tekshiruvi
> faqat **repo ochiq (public)** bo'lsa ishlaydi. Repo yopiq bo'lsa do'stlar APK'ni
> yuklay olmaydi. Settings → General → Danger Zone → "Change visibility" orqali public qiling.

---

## 1. APK'ni yig'ish

```powershell
$env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"
dotnet publish AndijonSaltanatlar.Android -c Release -f net10.0-android `
  "-p:AndroidSdkDirectory=$env:LOCALAPPDATA\Android\Sdk" `
  "-p:JavaSdkDirectory=$env:LOCALAPPDATA\Microsoft\jdk-17"
```

Natija: `AndijonSaltanatlar.Android\bin\Release\net10.0-android\publish\uz.andijon.saltanatlar-Signed.apk`

Uni relizga yuklashdan oldin **`AndijonSaltanatlar.apk`** deb nomlang.

## 2. GitHub'da reliz yaratish

Tag nomi ilova versiyasiga mos bo'lsin: `AndijonSaltanatlar.Shared/AppVersion.cs`
dagi `Current` qiymati bilan bir xil, oldiga `v` bilan. Hozir: **`v0.21.0`**.

### A varianti — brauzer (oson, tavsiya)

1. `https://github.com/anvarovmuhammadamin011-ops/MyFirthGame/releases/new`
2. **Choose a tag** → `v0.21.0` yozing → "Create new tag".
3. Sarlavha: `Andijon: Taxtlar Jangi 0.21.0`.
4. Pastga `AndijonSaltanatlar.apk` faylini sudrab tashlang (nomi aynan shunday bo'lsin).
5. **Publish release**.

### B varianti — `gh` CLI (agar o'rnatsangiz)

```powershell
winget install --id GitHub.cli
gh auth login
gh release create v0.21.0 `
  "AndijonSaltanatlar.Android\bin\Release\net10.0-android\publish\AndijonSaltanatlar.apk" `
  --repo anvarovmuhammadamin011-ops/MyFirthGame `
  --title "Andijon: Taxtlar Jangi 0.21.0" `
  --notes "Ilova ichidan yangilash, QR orqali tarqatish."
```

## 3. QR / yuklab olish sahifasi

`docs/yuklab-olish.html` — do'stlarga ulashish uchun QR + o'rnatish yo'riqnomasi.
Brauzerda oching yoki GitHub Pages / istalgan hostingga qo'ying. QR kod yuqoridagi
doimiy APK havolasini kodlaydi.

---

## 4. Keyingi yangilanishlar (muhim tartib)

Har safar yangilik qo'shib, yangi versiya chiqarmoqchi bo'lsangiz:

1. `AndijonSaltanatlar.Shared/AppVersion.cs` → `Current` ni oshiring (masalan `0.22.0`).
2. `AndijonSaltanatlar.Android/AndijonSaltanatlar.Android.csproj` →
   `ApplicationDisplayVersion` ni `0.22.0`, `ApplicationVersion` ni `+1` (masalan `4`).
3. APK'ni qayta yig'ing (1-bo'lim), `AndijonSaltanatlar.apk` deb nomlang.
4. GitHub'da **`v0.22.0`** tagli yangi reliz yarating, APK'ni yuklang (2-bo'lim).

Shundan keyin eski versiyadagi o'yinchilar ilovani ochganda **"Yangi versiya mavjud"**
oynasini ko'radi → **YUKLAB OLISH** → yangi APK o'rnatiladi. Akkaunt va o'yin holati
bulutda (`/api/save`) saqlangani uchun **o'sha joydan davom etadi**.

> Yangilash tekshiruvi versiyalarni raqam bo'yicha solishtiradi, shuning uchun tag
> har doim avvalgisidan katta bo'lsin (`0.20.0` → `0.21.0` → `0.21.1` ...).

---

## 5. Online o'yin — do'stlar turli joylarda (internet)

QR/yangilash GitHub orqali ishlaydi, lekin **birga online o'ynash uchun serveringiz
internetdan ko'rinadigan bo'lishi kerak** (hozir lokal `http://<IP>:5080`). Variantlar:

- Router'da **port-forward** (5080) + ochiq IP yoki DuckDNS kabi dinamik DNS.
- Serverni **bulutga** joylashtirish (masalan bir VPS yoki Azure/Render) + domen.

Keyin ilovadagi server manzilini o'sha ommaviy manzilga qo'ying. Bu alohida qadam —
tayyor bo'lganda yordam beraman.
