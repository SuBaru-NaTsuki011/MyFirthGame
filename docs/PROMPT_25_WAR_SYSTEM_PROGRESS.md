# PROMPT 25 — Urush tizimi: holat

Sana: 2026-11-17

## 1. Repository audit (boshlanishdagi holat)

| Modul | Holat |
|---|---|
| `Military/ArmyManager`, `UnitType` (6 tur), `Commander` (Bobur) | Faqat offline (lokal shahar o'yini). Server bilan bog'lanmagan |
| `Battles/BattleSimulationService` | Bir zumda hisoblanadigan, raundsiz jang. Faqat lokal |
| Server: hududlar, `AnnexAsync` (diplomatik qo'shib olish), diplomatiya, ittifoqlar, `TerritoryTransferred` | Ishlaydi. Shu kod qayta ishlatildi |
| Server: qo'shin, jang, PvP | **Yo'q edi**. Yangidan yaratildi |

## 2. Nima qilindi (haqiqiy, ishlaydigan kod)

### Umumiy qoidalar — `AndijonSaltanatlar.Shared/War/WarRules.cs`
- 10 ta birlik: piyoda (L1–L3: oddiy, tajribali, saroy qo'riqchisi), otliq (L1–L3: oddiy, tajribali, elita),
  masofaviy (L1–L2), ta'minot aravasi, lager xodimlari. Faqat mavhum statistikalar bor: kuch, chidamlilik, narx, saqlash xarajati.
- 6 ta harbiy inshoot: qarorgoh, mashg'ulot maydoni, otxona, saroy qo'riqchilari qarorgohi, ombor, mudofaa.
- Sarkarda darajalari: Yangi, Tajribali, Mashhur. Bonuslar cheklangan: kuch +5/10/15%, ruh +4/8/12.
- Raund hisoblash deterministik (seed). 5 xil qaror bor, jang ko'pi bilan 5 raund davom etadi.
  Ikkala tomon muzokara tanlasa, jang tinchlik bilan tugaydi.
- AI qarori va tinchlikni qabul qilishi uning xarakteriga (`AiStrategy`) va jang holatiga bog'liq.

### Server — `WarService`, `WarController`, `WarEntities`
- Jadvallar: `Armies`, `ArmyUnits`, `Commanders`, `MilitaryFacilities`, `RecruitOrders`, `Battles`, `BattleDecisions`,
  `BattleRounds`, `BattleInvitations`, `KingdomConflicts`, `WarStates`.
- Har bir buyruq `RunCommandAsync` orqali bajariladi: `CommandId` takror bajarilmaydi va hammasi bitta tranzaksiyada.
  Tanga shartli UPDATE bilan yechiladi.
- Yollash: tanga bir marta yechiladi, birliklar vaqt o'tgach qo'shinga qo'shiladi. Qo'shin sig'imi qarorgoh darajasiga
  bog'liq. **Jangdagi qo'shinga yangi birliklar jang tugaguncha qo'shilmaydi** (e2e sinovda topilgan xato tuzatildi).
- Saqlash xarajati har o'yin kunida bir marta yechiladi. Xazina yetmasa, qo'shin ruhi pasayadi (qarzga o'tmaydi).
- AI jangi: avval mojaro e'lon qilinadi (munosabat −25, 120 tanga). AI har raundda o'zi qaror qiladi, qo'shinini faqat
  o'z xazinasidan to'ldiradi (bepul birlik olmaydi).
- Raund faqat ikkala qaror kelganda hisoblanadi. Shartli UPDATE (`Round == r && Status == InProgress`) tufayli
  bir raund ikki marta hisoblanmaydi va jang bir marta yakunlanadi.
- Hudud transferi atomik: shaharlar aholisi bilan birga o'tadi, aholi ikki marta sanalmaydi, binolar saqlanadi.
  Saltanatning yagona hududini tortib olib bo'lmaydi.
- Tinchlik: AI o'z xarakteriga qarab qabul qiladi yoki rad etadi. Qabul qilsa, 12 soatlik sulh tuziladi va munosabat +15 bo'ladi.
- PvP faqat ikki tomon rozi bo'lganda boshlanadi:
  - taklif → qabul/rad. Rad etilganda hech narsa yo'qolmaydi;
  - garov hududi ixtiyoriy;
  - kutilayotgan takliflar 3 tagacha cheklangan;
  - yangi o'yinchi 24 soat himoyada. Himoyadan o'yinchi o'zi "PvP ni yoqish" orqali voz kechishi mumkin;
  - raqib 120 soniya ichida javob bermasa, uning o'rniga avtomatik "Muvozanatli" qaror qo'yiladi.
- SignalR hodisalari: `BattleInvitationCreated/Accepted/Declined`, `BattleStarted`, `BattleDecisionSubmitted`,
  `BattleRoundResolved`, `BattleCompleted`, `TerritoryOwnershipUpdated`, `PeaceAgreed`. Ular faqat muvaffaqiyatli commit'dan keyin yuboriladi.
- Donishmand Vazir faqat statistikadan maslahat beradi: harbiy xarajat, yomonlashgan munosabat, yangi hudud, qo'shin kichikligi.

### Klient
- `OnlinePanel` → yangi **"Harbiy"** bo'limi (`UI/WarPanel.cs`). Tablar: Qo'shinlar, Sarkardalar, Birliklar,
  Harbiy binolar, Mojarolar (oqibatlarni ko'rish + tinchlik), PvP, Jang tarixi.
- `UI/BattleScene.cs` — 2.5D jang sahnasi:
  - izometrik maydon, hilpiragan bayroqlar, birlik guruhlari va sarkardalar;
  - animatsiyalar: maydonga kirib kelish, idle (har birining fazasi har xil), harakat, reaksiya, g'alaba, chekinish, chang;
  - raund ko'rsatkichi, kuch va ruh chiziqlari, qaror paneli, natija oynasi (3 tugma + jang hisoboti).
- Dunyo xaritasida (Saltanat → Hududlar) qo'shinlar rangli bayroqcha bilan ko'rsatiladi, faol jang hududi pulslanadi.

## 2b. Xarita: qasrlarga yurish (keyingi so'rov bo'yicha qo'shildi)
- Harbiy → **Xarita** tabi. Vodiy xaritasi, qasrlar (egasi rangidagi bayroq, poytaxt toj belgisi bilan), yo'llar,
  yurayotgan qo'shinlar (yo'l bo'ylab harakatlanadi, qolgan soniyalar bilan) va faol jang pulsatsiyasi ko'rsatiladi.
- Qasr tanlanadi → ma'lumot paneli: egasi, himoya bahosi, g'alaba oqibati → qo'shin tanlanadi → **YURISH BOSHLASH**.
  Ta'minot 80 tanga, yo'l 20–75 soniya.
- Yetib kelish va jang boshlanishini server fon tiki bajaradi (`WorldSimulationService`, har 2 s), o'yinchilar offline bo'lsa ham.
- Himoyachi: eng kuchli tayyor qo'shini, u bo'lmasa — **qasr garnizoni** (aholiga bog'liq militsiya, 8–30 birlik).
- Real o'yinchi raund vaqtida (120 s) javob bermasa, uning o'rniga **sarkardasi (AI)** qaror qiladi.
- **Poytaxt qamali:** poytaxtni tortib olib bo'lmaydi (o'yinchi yo'q bo'lib ketmasligi uchun). G'olib xazinadan 20% o'lja oladi
  (ko'pi bilan 2000 tanga). Boshqa qasrlar g'alabada hududi bilan o'tadi.
- Real o'yinchiga hujum faqat u «PvP ni yoqish» ni tanlagan bo'lsa mumkin. Hujum qilgan o'yinchining himoyasi ham bekor bo'ladi.
- Testlar: AI qasriga yurish → tik → jang → hudud; offline o'yinchi poytaxti → garnizon + sarkarda qarorlari → o'lja;
  himoyadagi o'yinchiga yurish taqiqlangan. Playwright e2e da yurish → fon tikida avtomatik jang → ikkala xaritada ko'rinish tekshirildi.

## 3. Test natijalari
- `dotnet test`: **104/104 o'tdi**. Shulardan 10 tasi urush tizimiga oid: qo'shin, yollash/sig'im/talablar, inshoot va sarkarda,
  AI jangi + hudud transferi + restart, muzokara/determinizm, tinchlik/sulh, PvP (vaqt tugashi bilan),
  himoya/rad/o'chirish, saqlash xarajati, jang paytida kelgan birliklar.
- Playwright e2e (haqiqiy server, alohida test bazasi). Tekshirilganlar:
  - Google kirish (dev rejim);
  - inshoot, yollash, takroriy `CommandId` himoyasi;
  - AI jangi raundma-raund;
  - jang o'rtasida **server qayta ishga tushirildi**, jang yo'qolmadi;
  - noto'g'ri raund va yakunlangan jangga qaror rad etildi;
  - PC ↔ telefon PvP: taklif → himoya rad etdi → telefon PvP ni yoqdi → qabul → 4 raund → **ikkala tomonda bir xil natija**.
- O'yin skrinshotlari (PC 1280×720 va telefon 2400×1080) tekshirildi: Harbiy panel, Birliklar, PvP, jang sahnasi, natija oynasi.

## 4. Build natijalari
- Windows client: muvaffaqiyatli. Server: muvaffaqiyatli. Android APK: muvaffaqiyatli (23 MB).
- **Haqiqiy Android qurilmada sinalmagan.** Telefon ko'rinishi PC'da `--phone` rejimida tekshirilgan.

## 5. Hali bajarilmagan / cheklovlar (Completed deb belgilanmaydi)
- **Harbiy binolar shahar xaritasida qurilmaydi.** Ular serverdagi "inshootlar" sifatida harbiy menyudan quriladi
  va lokal `ConstructionSystem` bilan bog'lanmagan.
- **Texnologiya daraxti bilan integratsiya yo'q.** Birliklarni ochish hozircha faqat inshoot darajasiga bog'liq.
  Lokal `TechnologyCatalog` serverga sinxronlanmaydi.
- AI saltanatlar o'z tashabbusi bilan urush e'lon qilmaydi: ular faqat himoyalanadi va tinchlikni baholaydi.
- Haqiqiy otliq va kamonchi spraytlari yo'q. Mavjud askar spraytlari rang bilan farqlanadi (vaqtinchalik variant).
- Profiling (FPS o'lchovi) o'tkazilmagan.
- Ittifoqchilar bilan birgalikda jang qilish (koalitsiya) yo'q. Jang 1 ga 1.
- Lokal (offline) `ArmyManager`/`StrategicView` eski tizim sifatida qoldi va server qo'shini bilan sinxron emas.

## UI yangilanishi: o'yin uslubi, eski xarita, internet orqali o'yin

- **O'yin uslubidagi UI.** Barcha menyu va panellar bitta palitrada: `ThemeManager.Game` (to'q yog'och va oltin).
  Matn piksel shriftda (`Typography.PixelMode`), ramkalar to'rtburchak va bo'rtma (`UiKit.Frame`).
- **Matn tugmadan chiqmaydi.** `Typography.DrawFitted` matnni avval kichraytiradi, sig'masa ".." bilan qisqartiradi.
  Tugma va ustun enlari matn o'lchamidan hisoblanadi. Bu o'yin menyusi, Google kirish, Saltanat, Bozor, Harbiy,
  Vazir bo'limlari va jang natijasi oynasida qilingan.
- **Eski xarita.** `WarMapView` pergament fonda chiziladi: siyoh tog'lar, daryo, o'rmonlar, kartush, kompas va nuqtali yo'llar.
  Qasrlar siyoh chizig'i bilan chiziladi.
- **Internet orqali o'yin.** Taklif kodi (`Google:DevLoginCode`), `UseForwardedHeaders` (tunnel ortida https havolalar),
  internet domeni uchun standart `https://` va `tools/Internetga_ochish.ps1` (Cloudflare quick tunnel).
- **Testlar.** 110/110 (yangi: `DevLogin_InviteCode_RequiredWhenSet`, `ServerUrl_InternetHostDefaultsToHttps`).
