# PROMPT 19B — ANDIJON: SALTANATLAR
## Territory Conquest, Population Governance, Advanced Trading & Premium Cross-Platform UI/UX
### C# + MonoGame + ASP.NET Core + PostgreSQL

## 1. SENING ROLING

Sen Senior C# Game Developer, Multiplayer Strategy Architect, Game Economy Engineer, Kingdom Simulation Designer, Professional UI/UX Designer va Cross-Platform Optimization Engineer sifatida ishlaysan.

Bu PROMPT 19B.

**PROMPT 19A DAGI ISHLAYDIGAN MULTIPLAYER, FARMING VA MARKETPLACE TIZIMLARINI BUZMA!**

Avval kod bazasini tekshir.

Mavjud:

- Windows client
- Android client
- ASP.NET Core server
- PostgreSQL database
- FarmingSystem
- Marketplace
- WorldMap
- CityManager
- EconomyManager
- PopulationManager
- AI Kingdoms
- MilitarySystem
- Donishmand Vazir
- SaveSystem

modullarining amaldagi holatini tekshir.

Mavjud ishlaydigan modullardan foydalan va yetishmayotgan qismlarni implementatsiya qil.

---

# 2. ASOSIY MAQSAD

Bu bosqichda quyidagilarni yarat:

1. Yangi hududlarni saltanatga qo‘shish.
2. Boshqa davlatga tegishli hudud ustidan boshqaruvni qo‘lga kiritish.
3. Hududning shahar va aholisini yangi davlatga biriktirish.
4. Bir nechta shaharni boshqarish.
5. AI hokimlar tayinlash.
6. Davlatlararo savdoni kengaytirish.
7. Savdo karvonlarini server bilan bog‘lash.
8. Windows va Androidda professional interfeys.
9. Barcha shrift, matn, ikonka va tugmalarni tartibga keltirish.
10. Donishmand Vazirni yangi tizimlarga integratsiya qilish.

Barcha asosiy o‘zgarishlar online multiplayerda sinxron ishlashi shart.

---

# 3. TERRITORY EXPANSION SYSTEM

O‘yinda ikkita yer kengaytirish tizimi bo‘lsin.

**CITY EXPANSION**

Shahar ichida yangi yer uchastkalarini ochish va bino qurish.

**KINGDOM EXPANSION**

Butun hudud yoki shaharni boshqa saltanatdan o‘z davlatiga qo‘shish.

Bu ikki tizimni aralashtirma.

Hududlarni qo‘shish yo‘llari:

- Diplomatik birlashish.
- Kelishuv orqali topshirish.
- Saltanatlar o‘rtasida siyosiy birlashish.
- O‘yin ichidagi abstrakt strategik raqobat.
- Mavjud mavhum jang tizimi natijasida hudud nazoratining o‘zgarishi.

Real jangovar taktikalarni emas, o‘yin uchun abstrakt siyosiy va strategik ko‘rsatkichlarni ishlat.

---

# 4. TERRITORY OWNERSHIP TRANSFER

Hudud egasi o‘zgarganda server quyidagilarni bajarishi shart:

1. Region OwnerKingdomId ni yangilash.
2. Shaharlarni yangi saltanatga biriktirish.
3. Hududdagi binolarni saqlash.
4. Aholi va oilalarni saqlash.
5. Hududning iqtisodiy holatini saqlash.
6. Soliq boshqaruvini yangi davlatga bog‘lash.
7. Dunyo xaritasidagi rangni o‘zgartirish.
8. Barcha online clientlarga yangilanish yuborish.
9. SaveSystem va PostgreSQLni yangilash.

Hudud transferi atomik va server authoritative bo‘lsin.

Bitta hudud ikki davlatga bir vaqtning o‘zida asosiy mulk sifatida tegishli bo‘lmasin.

---

# 5. POPULATION GOVERNANCE

**Muhim talab: hudud yangi saltanatga qo‘shilsa, u yerdagi aholi ham yangi davlat boshqaruviga o‘tsin.**

Ammo aholi sotib olinadigan yoki ombor resursi sifatida saqlanadigan obyekt bo‘lmasin.

Fuqarolar o‘z:

- Uylarini
- Oilalarini
- Kasblarini
- Ish joylarini
- Shaxsiy statistikalarini

saqlab qolsin.

Yangi boshqaruvga quyidagilar bog‘lansin:

- Soliq
- Jamoat xizmatlari
- Shahar infratuzilmasi
- Siyosiy ishonch
- Aholi mamnunligi
- Mahalliy iqtisodiyot

### Misol

Asaka saltanati Shahrixon hududini o‘yin qoidalari orqali qo‘shib oldi.

Natijada:

- Shahrixon Asaka saltanatining hududiga aylanadi.
- Shahrixon aholisining mavjud ma’lumotlari saqlanadi.
- Shahrixon binolari yo‘qolmaydi.
- O‘yinchi Shahrixon shahriga kirishi mumkin.
- Yangi shaharni rivojlantira oladi.
- Saltanatning umumiy statistikasi avtomatik yangilanadi.

Aholini umumiy statistikaga ikki marta qo‘shma.

Umumiy PopulationCount saltanatga tegishli hududlardan hisoblanishi kerak.

---

# 6. MULTI-CITY MANAGEMENT

O‘yinchi bir nechta shaharni boshqara olsin.

Masalan:

- Asaka
- Shahrixon
- Andijon

Har bir shaharda alohida:

- CityId
- CityState
- Buildings
- LandParcels
- Population
- Economy
- Infrastructure
- FarmingFields
- TradeConnections

bo‘lsin.

Foydalanuvchi dunyo xaritasidan o‘z shahrini tanlaganda uning izometrik qurilish xaritasiga kira olsin.

Faol shahar batafsil ishlasin.

Boshqa shaharlar yengil statistik simulyatsiyada davom etsin.

Bitta shahardagi uy boshqa shahar xaritasida paydo bo‘lib qolmasin.

---

# 7. REGIONAL GOVERNORS

Shaharlarni boshqarish uchun AI hokimlar yarat.

Hokim:

- Shahar iqtisodiyotini kuzatadi.
- Aholi ehtiyojlarini aniqlaydi.
- Qurilish rejasini tuzadi.
- Savdo imkoniyatlarini tahlil qiladi.
- Donishmand Vazirga hisobot yuboradi.

Boshqaruv rejimlari:

**MANUAL**
O‘yinchi hamma narsani o‘zi qiladi.

**ADVISORY**
Hokim maslahat beradi.

**APPROVED AUTOMATION**
Hokim foydalanuvchi tasdiqlagan rejalarni bajaradi.

Hokim o‘yinchi ruxsatisiz katta xarajat qilmasin.

---

# 8. ADVANCED FARMING & INTER-KINGDOM TRADING

PROMPT 19A da yaratilgan guruch tizimini kengaytir.

Yangi ekinlar:

- Bug‘doy
- Paxta
- Mevalar
- Sabzavotlar

Barcha ekinlar umumiy CropSystem orqali ishlasin.

Savdoda:

- Mahsulot turi
- Miqdori
- Narxi
- Sotuvchi saltanat
- Xaridor saltanat
- Savdo holati
- Yetkazib berish holati

saqlansin.

Player-to-player savdo tizimini buzma.

AI saltanatlar ham bozor orqali mahsulot sotib olishi va sotishi mumkin bo‘lsin.

---

# 9. TRADE CARAVAN DELIVERY

Savdo tasdiqlangach karvon orqali tashishni qo‘sh.

Jarayon:

**SOTUVCHI OMBORI → YUK ORTISH → KARVON YO‘LI → XARIDOR SHAHRI → YUK TUSHIRISH → SAVDONI YAKUNLASH**

Server mahsulot va pulni to‘g‘ri rezerv qilsin.

Bir mahsulot ikki marta sotilmasin.

Karvon manzilga yetganda mahsulot xaridor omboriga o‘tsin.

Karvonning xaritadagi harakati mavjud AnimationSystem bilan bog‘lansin.

Offline o‘yinchi bo‘lsa ham server savdo holatini saqlasin.

---

# 10. PREMIUM UI/UX REDESIGN

**BU PROMPTNING ENG MUHIM QISMLARIDAN BIRI.**

O‘yinning grafik dunyosini emas, aynan dastur interfeysini professional darajada yaxshila.

Qayta ishlanadigan ekranlar:

- Main Menu
- Login / Register
- World Selection
- Kingdom Selection
- Game HUD
- Bottom Navigation
- Construction Panel
- Farming Panel
- Marketplace
- World Map Panels
- Territory Management
- Population Management
- Donishmand Vazir
- Settings
- Notifications

Barcha komponentlar yagona dizayn tizimiga ega bo‘lsin.

---

# 11. UI COLOR SYSTEM

Interfeys professional, minimalistik va tarixiy strategiya o‘yiniga mos bo‘lsin.

### Light Theme

- Background: #F7F9FC
- Surface: #FFFFFF
- Primary: #326B96
- Secondary: #6E9FC2
- Accent: #C59A4B
- Primary Text: #182634
- Secondary Text: #627487
- Border: #DFE7EF

### Dark Theme

- Background: #111B29
- Surface: #1B293B
- Primary: #76B3DF
- Accent: #D6AE67
- Primary Text: #F3F7FB
- Secondary Text: #A9BBCB

ThemeManager yarat.

Barcha panellar va tugmalar bir xil dizayn tokenlaridan foydalanishi kerak.

---

# 12. TYPOGRAPHY — SHRIFTLARNI TO‘G‘RILASH

Barcha yozuvlarni yagona typography system asosida yarat.

Tavsiya etilgan shriftlar:

- Inter
- Noto Sans

Desktop uchun:

- Main Title: 36 px
- Screen Title: 28 px
- Panel Title: 20 px
- Body: 16 px
- Button: 16 px
- Secondary: 14 px

Mobile uchun ekran zichligi va UI scaling asosida moslashtir.

### Muhim

- Matnlar kesilib qolmasin.
- Kichik ekranlarda wrapping ishlasin.
- Shriftlar xiralashib ketmasin.
- O‘zbekcha harflar to‘g‘ri ko‘rsatilsin.
- `Oʻ`, `Gʻ`, `Sh`, `Ch` kabi yozuvlarni tekshir.
- Barcha matnlar localization katalogidan olinsin.

Fontlarni yuklash va render qilish mavjud MonoGame tizimiga mos bo‘lsin.

---

# 13. COMPONENT DESIGN SYSTEM

Qayta ishlatiladigan UI komponentlar yarat:

- UIButton
- UIIconButton
- UIPanel
- UICard
- UIModal
- UITabBar
- UIProgressBar
- UIResourceCounter
- UIToast
- UIScrollView
- UITextInput
- UITradeCard
- UIKingdomCard
- UICityCard
- UIDistrictCard

Har bir komponentda:

- Normal state
- Hover state
- Pressed state
- Disabled state
- Focus state

bo‘lsin.

Tugmalar va panellar har xil dizaynda ko‘rinmasin.

UI elementlari grafik dunyoni keraksiz yopib qo‘ymasligi kerak.

---

# 14. RESPONSIVE PC + ANDROID UI

Windows va Android bir xil serverda ishlashi bilan birga, alohida qurilmalarda qulay boshqarilishi shart.

### Windows

- Mouse + Keyboard
- Hover
- Tooltip
- Sidebar
- Katta HUD
- Responsive panels

### Android

- Touch controls
- Katta tugmalar
- Bottom navigation
- Safe area
- Landscape layout
- Touch-friendly menus
- Scalable text

PC interfeysini telefonga shunchaki kichraytirib qo‘yma.

Mobil uchun layoutni qayta joylashtir.

Ekran kengligi va balandligiga qarab panellar dinamik moslashsin.

---

# 15. MARKETPLACE UI REDESIGN

Bozorni zamonaviy o‘yin marketplace ko‘rinishida yarat.

Tablar:

- SOTIB OLISH
- SOTISH
- MENING TAKLIFLARIM
- SAVDO TARIXI

Har bir mahsulot kartasida:

- Mahsulot ikonkasi
- Nomi
- Miqdori
- Narxi
- Sotuvchi saltanat
- Yetkazib berish holati

ko‘rsatilsin.

Savdo tasdiqlash modal oynasi bo‘lsin.

O‘yinchi nechta mahsulot olayotganini va qancha tanga sarflashini aniq ko‘rsin.

---

# 16. KINGDOM MANAGEMENT UI

Yangi panel:

**SALTANAT BOSHQARUVI**

Bo‘limlar:

- UMUMIY
- SHAHARLAR
- HUDUDLAR
- AHOLI
- IQTISODIYOT
- DIPLOMATIYA
- HOKIMLAR

Hudud tanlanganda:

- Hudud nomi
- Egasi
- Aholisi
- Shaharlar
- Iqtisodiyot
- Barqarorlik
- Hudud boshqaruvi

ko‘rsatilishi kerak.

Agar hudud o‘yinchiga tegishli bo‘lsa, SHAHARNI BOSHQARISH tugmasi chiqsin.

Boshqa davlatga tegishli bo‘lsa, ruxsat etilgan diplomatik va strategik variantlar ko‘rsatiladi.

---

# 17. DONISHMAND VAZIR INTEGRATION

Vazir yangi tizimlarni kuzatsin:

- Guruch va boshqa mahsulotlar
- Savdo takliflari
- Karvonlar
- Yangi hududlar
- Hudud aholisi
- Yangi shaharlar
- Shahar hokimlari
- Saltanat iqtisodiyoti

Masalan:

“Hazratim, saltanatimizga Shahrixon hududi qo‘shildi. U yerda uy-joy va savdo infratuzilmasini rivojlantirishni tavsiya qilaman.”

Tugma:

**SHAHRIXONNI BOSHQARISH**

Bosilganda tegishli shahar ochilsin.

Barcha tavsiyalar haqiqiy GameState asosida bo‘lsin.

---

# 18. SERVER SECURITY

Online o‘yinda quyidagilar serverda tekshirilsin:

- Player authentication
- Kingdom ownership
- Territory ownership
- Resource ownership
- Market permissions
- Trade transaction consistency
- Construction validation
- Population transfer integrity
- Command idempotency

Hududni qo‘shish jarayoni database tranzaksiyasi orqali bajarilsin.

Bitta hudud ikki davlatga bir vaqtning o‘zida biriktirilmasin.

Bir fuqaro umumiy statistikada ikki marta hisoblanmasin.

Serverdagi ma’lumotlar clientdan kelgan tasdiqlanmagan raqamlar bilan almashtirilmasin.

---

# 19. FIRST WORKING VERTICAL SLICE

**BIRINCHI ZANJIR:**

WINDOWS PLAYER → DUNYO XARITASI → AI HUDUDINI TANLASH → DIPLOMATIK BIRLASHISH → SERVER VALIDATION → HUDUD EGASI O‘ZGARADI → SHAHAR VA AHOLI YANGI SALTANATGA QO‘SHILADI → ANDROID CLIENT XARITASIDA YANGILANADI.

**IKKINCHI ZANJIR:**

YANGI SHAHAR → HOKIM TAYINLASH → SHAHAR HOLATINI TAHLIL QILISH → YANGI QURILISH REJASI → FOYDALANUVCHI TASDIG‘I → QURILISH → STATISTIKA YANGILANISHI.

**UCHINCHI ZANJIR:**

MARKETPLACE → MAHSULOT SOTISH → KARVON YETKAZIB BERISH → XARIDOR OMBORI → SAVDO YAKUNLANISHI.

**TO‘RTINCHI ZANJIR:**

WINDOWS + ANDROID → MAIN MENU → WORLD SELECTION → GAME HUD → MARKETPLACE → KINGDOM MANAGEMENT.

Oxirgi zanjirda barcha oynalar professional responsive dizaynga ega bo‘lishi kerak.

---

# 20. TESTING

Quyidagilarni tekshir:

### Territory

- [ ] Hudud transferi ishlaydi.
- [ ] Yangi hudud xaritada ko‘rinadi.
- [ ] Shaharlar yangi egaga biriktiriladi.
- [ ] Aholi statistikasi to‘g‘ri.
- [ ] Binolar saqlanadi.
- [ ] Ikki client sinxronlashadi.

### Kingdom Management

- [ ] Bir nechta shahar boshqariladi.
- [ ] Hokim tayinlash mumkin.
- [ ] Shaharlar alohida GameState saqlaydi.
- [ ] Hokim tasdiqlangan rejalarni bajaradi.

### Trade

- [ ] Farming tizimi buzilmagan.
- [ ] Marketplace ishlaydi.
- [ ] Karvon yetkazib berish ishlaydi.
- [ ] Mahsulot ikki marta berilmaydi.
- [ ] Savdo serverda saqlanadi.

### UI

- [ ] Main Menu professional.
- [ ] Login va World Selection responsive.
- [ ] Shriftlar yagona.
- [ ] O‘zbekcha harflar to‘g‘ri.
- [ ] Tugmalar ishlaydi.
- [ ] Windows UI yaxshi ko‘rinadi.
- [ ] Android UI yaxshi ko‘rinadi.
- [ ] Marketplace dizayni yangilangan.
- [ ] Kingdom Management dizayni yangilangan.
- [ ] Matnlar kesilib qolmaydi.

### Build

- [ ] Windows build muvaffaqiyatli.
- [ ] Android build muvaffaqiyatli.
- [ ] Server ishlaydi.
- [ ] PostgreSQL ishlaydi.
- [ ] Ikkala client bir dunyoga ulanadi.

---

# 21. ISH TARTIBI

1. PROMPT 19A natijalarini audit qil.
2. Territory ownership tizimini yarat.
3. Hudud transferini serverga integratsiya qil.
4. Population Governance qo‘sh.
5. Multi-city management yarat.
6. Regional Governor tizimini qo‘sh.
7. Advanced farming va caravan delivery ni integratsiya qil.
8. UI Design System yarat.
9. Typography ni to‘g‘rila.
10. Main Menu va Game HUD ni yangila.
11. Marketplace dizaynini yangila.
12. Kingdom Management panelini yarat.
13. Android responsive layoutni yaxshila.
14. Donishmand Vazirni integratsiya qil.
15. Multiplayer integration tests bajar.
16. Windows va Android release buildlarni tekshir.

---

# 22. AGENT CONTINUITY

Quyidagi faylni yarat:

`docs/PROMPT_19B_PROGRESS.md`

Unda:

- Bajarilgan ishlar
- Ishlayotgan funksiyalar
- O‘zgartirilgan fayllar
- UI holati
- Multiplayer natijalari
- Test natijalari
- Build holati
- Bajarilmagan vazifalar

saqlansin.

Faqat dokumentatsiya yaratib ishni tugatma.

Kodda haqiqiy o‘zgarishlar bajar.

---

# 23. FINAL INSTRUCTION

**MAVJUD ANDIJON: SALTANATLAR O‘YININI KENGAYTIR.**

Bu bosqichdan keyin men:

1. Boshqa hududlarni saltanatimga qo‘sha olishim.
2. Yangi hududdagi aholini boshqarishim.
3. Yangi shaharlarni rivojlantirishim.
4. Hokimlar tayinlashim.
5. Mahsulotlarni boshqa o‘yinchilarga sotishim.
6. Karvonlar orqali savdo qilishim.
7. Bularning barchasini Windows va Android multiplayerda ko‘rishim.
8. Professional menyular, chiroyli shriftlar, aniq yozuvlar va qulay tugmalardan foydalanishim.

kerak.

**ENG MUHIM:**

O‘yin dunyosini boshidan qayta qurma.

Asosiy e’tiborni multiplayer hudud boshqaruvi, aholi integratsiyasi va ayniqsa interfeysning shriftlari, menyulari, panellari va mobil moslashuviga qarat.

Ish yakunida haqiqiy kod o‘zgarishlari, Windows/Android testlari, server holati, UI/UX natijalari va bajarilmagan qismlar haqida aniq hisobot ber.

**ANDIJON: SALTANATLAR — PC VA ANDROIDDA BIRGALIKDA O‘YNALADIGAN, SAVDO QILINADIGAN VA HUDUDLARI RIVOJLANADIGAN ONLINE STRATEGIYA O‘YINI!**
# PROMPT 20 — ANDIJON: SALTANATLAR
## Multiplayer Alliances, World Politics, Shared Markets, Cooperative Projects & Advanced Kingdom Administration
### C# + MonoGame + ASP.NET Core + PostgreSQL
### Windows + Android Cross-Platform Strategy Game

---

# 1. SENING ROLING

Sen Senior C# Game Developer, Multiplayer Systems Architect, Strategy Game AI Engineer, Backend Developer, Economy Simulation Designer va Professional Game UI/UX Engineer sifatida ishlaysan.

Biz **ANDIJON: SALTANATLAR** nomli 2.5D izometrik tarixiy strategiya va shahar qurish o‘yinini yaratmoqdamiz.

Texnologiyalar:

- C#
- MonoGame
- .NET
- Visual Studio Code
- ASP.NET Core
- PostgreSQL
- Entity Framework Core
- SignalR
- JSON Data Definitions

**BU PROMPT 20. MAVJUD LOYIHANI BOSHDAN YARATMA!**

Avval repositoryni to‘liq tekshir.

Ayniqsa PROMPT 19A va 19B dagi cross-platform multiplayer, farming, marketplace, hudud boshqaruvi va UI/UX tizimlarining haqiqiy holatini aniqlagin.

Mavjud kodni buzmasdan, yangi funksiyalarni yagona arxitekturaga integratsiya qil.

Implementatsiya qilinmagan funksiyalarni tayyor deb ko‘rsatma.

---

# 2. ASOSIY MAQSAD

O‘yinchilar bitta dunyoda mustaqil saltanatlarni boshqaradi.

Ular:

1. Bir-birining saltanatlarini ko‘radi.
2. Diplomatik munosabatlar o‘rnatadi.
3. Ittifoq tuzadi.
4. Ittifoqdan chiqadi.
5. O‘zaro savdo qiladi.
6. Birgalikda qurilish loyihalarini moliyalashtiradi.
7. Savdo yo‘llarini rivojlantiradi.
8. Hududlar bo‘yicha kelishuvlar tuzadi.
9. Boshqa o‘yinchilarga rasmiy takliflar yuboradi.
10. Dunyo siyosatidagi o‘zgarishlarni kuzatadi.

**Barcha asosiy multiplayer harakatlari server authoritative bo‘lsin.**

---

# 3. KINGDOM DIPLOMACY SYSTEM

Har bir saltanat boshqa saltanatlar bilan alohida diplomatik munosabatga ega bo‘lsin.

Munosabat holatlari:

- Neutral
- Friendly
- TradingPartner
- Allied
- Rival
- DiplomaticDispute

Har bir munosabat uchun:

- RelationId
- KingdomAId
- KingdomBId
- RelationStatus
- TrustScore
- TradeVolume
- ActiveTreaties
- LastDiplomaticEvent
- UpdatedAt

ma’lumotlarini saqla.

Munosabatlar o‘yin voqealariga qarab o‘zgarsin.

Masalan:

- Muvaffaqiyatli savdo ishonchni oshirishi mumkin.
- Kelishuvga rioya qilmaslik ishonchni kamaytirishi mumkin.
- Qo‘shma loyiha munosabatlarni yaxshilashi mumkin.
- Diplomatik kelishmovchilik hamkorlikni cheklashi mumkin.

Barcha qiymatlar serverdagi haqiqiy voqealar asosida hisoblanishi kerak.

---

# 4. MULTIPLAYER ALLIANCE SYSTEM

O‘yinchilar bir-biri bilan ittifoq tuzishi mumkin bo‘lsin.

Yangi bo‘lim:

**ITTIFOQLAR**

### Ittifoq yaratish

O‘yinchi:

1. Ittifoq nomini kiritadi.
2. Ittifoq ramzini tanlaydi.
3. Ittifoq yaratishni tasdiqlaydi.
4. Server ittifoqni saqlaydi.

### Taklif yuborish

O‘yinchi boshqa saltanatni tanlaydi.

**ITTIFOQQA TAKLIF QILISH**

tugmasini bosadi.

Ikkinchi o‘yinchiga taklif boradi.

U:

- QABUL QILISH
- RAD ETISH

variantlarini tanlaydi.

Ittifoq faqat taklif qabul qilingandan keyin tuzilsin.

Boshqa foydalanuvchini uning tasdig‘isiz ittifoqqa majburan qo‘shma.

---

# 5. ALLIANCE MEMBERS & ROLES

Har bir ittifoq quyidagi ma’lumotlarga ega bo‘lsin:

```text
AllianceId
AllianceName
FounderKingdomId
MemberKingdomIds
AllianceTreasury
AllianceProjects
AllianceRelations
CreatedAt
```

Rollarga asoslangan boshqaruv yarat.

### ASOSCHI

- Ittifoq sozlamalarini boshqaradi.
- Yangi a’zolarni taklif qiladi.
- Ruxsatlarni belgilaydi.

### VAKIL

- Ruxsat etilgan diplomatik takliflarni boshqaradi.
- Ittifoq loyihalarini taklif qiladi.

### A’ZO

- Ittifoq holatini ko‘radi.
- Savdo qiladi.
- Qo‘shma loyihalarda qatnashadi.

Dastlab ikki o‘yinchi bilan ishlaydigan sodda tizim yetarli.

Keyinchalik ko‘proq a’zo qo‘shishga tayyor arxitektura yarat.

---

# 6. ALLIANCE ECONOMY

Ittifoq a’zolari o‘z iqtisodiyotini mustaqil boshqarishda davom etsin.

Ittifoq umumiy resurs havzasiga ega bo‘lishi mumkin, ammo bu resurslar o‘yinchilarning shaxsiy omborlaridan avtomatik olinmasin.

Har bir o‘yinchi ixtiyoriy ravishda:

- Tanga
- Guruch
- Bug‘doy
- Yog‘och
- Tosh

kabi virtual o‘yin resurslarini qo‘shma loyihaga ajrata olsin.

### Muqim qoidalar

- Resurs topshirish serverda validatsiya qilinsin.
- Resurslar ikki marta hisoblanmasin.
- Har bir hissaga tranzaksiya identifikatori berilsin.
- Ittifoq xazinasini sarflash ruxsatlarga bog‘liq bo‘lsin.
- Ittifoqdan chiqishda resurslarga nima bo‘lishi aniq o‘yin qoidasi bilan belgilansin.

---

# 7. COOPERATIVE KINGDOM PROJECTS

Ittifoq a’zolari birgalikda loyiha yaratishi mumkin bo‘lsin.

Dastlab uchta qo‘shma loyiha yarat.

## A. SAVDO YO‘LI

Ikki saltanat o‘rtasida savdo aloqalarini yaxshilaydi.

## B. KARVONSAROY

Ittifoq savdogarlari foydalanadigan savdo inshooti.

## C. MADANIYAT MARKAZI

Ittifoqning madaniy va ilmiy rivojlanishiga bonus beradi.

Har bir loyiha quyidagilarga ega bo‘lsin:

- ProjectId
- ProjectType
- AllianceId
- RequiredResources
- ContributedResources
- ConstructionProgress
- ProjectStatus
- Benefits

Loyiha boshlanishidan oldin uning narxi, maqsadi va foydasi ko‘rsatilsin.

O‘yinchi qancha resurs qo‘shishini o‘zi tanlasin.

---

# 8. SHARED MARKETPLACE 2.0

PROMPT 19A dagi Marketplace tizimini kengaytir.

Yangi bozor bo‘limlari:

- UMUMIY BOZOR
- ITTIFOQ BOZORI
- MENING TAKLIFLARIM
- SAVDO TARIXI
- KARVONLAR

### Umumiy bozor

Barcha ruxsat etilgan saltanatlarning savdo takliflari.

### Ittifoq bozori

Faqat ittifoq a’zolariga mo‘ljallangan takliflar.

### Savdo tarixi

- Sotuvchi
- Xaridor
- Mahsulot
- Miqdor
- Narx
- Sana
- Status

Savdo natijalari ikkala clientda ko‘rinsin.

Real pul, pullik mahsulot savdosi yoki qimor mexanikalarini qo‘shma.

---

# 9. DYNAMIC MARKET PRICES

Mahsulotlar narxi bozor holatiga qarab o‘zgarsin.

Dastlab:

- Guruch
- Bug‘doy
- Paxta
- Yog‘och
- Tosh
- Mato

uchun talab va taklif statistikasi mavjud bo‘lsin.

O‘yinchi yangi savdo taklifini yaratganda bozorning joriy narxini ko‘ra olsin.

### Muhim

O‘yinchi o‘z taklif narxini tanlashi mumkin, lekin server umumiy o‘yin balansida belgilangan chegaralarni tekshirsin.

Narx manfiy bo‘lmasin.

Savdo ma’lumotlari database tranzaksiyalari orqali saqlansin.

Mavjud EconomyManager ni takrorlama.

---

# 10. DIPLOMATIC AGREEMENTS

Saltanatlar quyidagi kelishuvlarni tuza olsin:

- Savdo kelishuvi.
- Do‘stlik kelishuvi.
- Ittifoq shartnomasi.
- Hududni ixtiyoriy topshirish kelishuvi.
- Qo‘shma loyiha shartnomasi.
- Savdo yo‘lidan foydalanish kelishuvi.

Har bir kelishuv quyidagi holatlarga ega bo‘lsin:

```text
Proposed
PendingApproval
Active
Expired
Rejected
Cancelled
```

Kelishuvning amaldagi qoidalari serverda saqlansin.

Kelishuv muddati tugaganda tegishli ruxsatlar avtomatik qayta hisoblanishi kerak.

---

# 11. TERRITORY AGREEMENTS

O‘yinchilar hudud bo‘yicha kelishuv tuza olsin.

Misol:

Asaka saltanati bilan Shahrixon saltanati o‘rtasida hududni topshirish kelishuvi.

Jarayon:

1. O‘yinchi hududni tanlaydi.
2. Ruxsat etilgan taklifni yaratadi.
3. Ikkinchi tomon taklifni ko‘radi.
4. Kelishuv shartlari tekshiriladi.
5. Tegishli tomonlar tasdiqlaydi.
6. Server hudud egaligini yangilaydi.
7. Shahar va aholi boshqaruvi yangi saltanatga o‘tadi.
8. Ikkala client dunyo xaritasini yangilaydi.

Hudud transferida:

- Binolar saqlansin.
- Aholi statistikasi ikki marta hisoblanmasin.
- Save/Load buzilmasin.
- Server authorization ishlasin.

---

# 12. ADVANCED KINGDOM ADMINISTRATION

Yangi katta boshqaruv paneli yarat:

**SALTANAT BOSHQARUVI**

Bo‘limlar:

1. UMUMIY
2. SHAHARLAR
3. HUDUDLAR
4. AHOLI
5. IQTISODIYOT
6. SAVDO
7. DIPLOMATIYA
8. ITTIFOQLAR
9. HOKIMLAR

Har bir bo‘lim haqiqiy GameState dan ma’lumot olsin.

### Umumiy panel

- Saltanat nomi
- Hukmdor
- Poytaxt
- Shaharlar soni
- Aholi
- Xazina
- Hududlar
- Rivojlanish ko‘rsatkichi

### Shaharlar paneli

Har bir shaharning:

- Nomi
- Aholisi
- Darajasi
- Daromadi
- Xarajatlari
- Muammolari
- Hokimi

ko‘rsatilsin.

---

# 13. SMART REGIONAL GOVERNORS

PROMPT 19B dagi hokim tizimini kuchaytir.

Har bir hokim:

- Shahar ehtiyojlarini tahlil qiladi.
- Iqtisodiy hisobot beradi.
- Yangi qurilish taklif qiladi.
- Savdo imkoniyatlarini aniqlaydi.
- Aholi muammolarini umumlashtiradi.

Boshqaruv rejimlari:

**MANUAL**

O‘yinchi barcha qarorlarni o‘zi qabul qiladi.

**ADVISORY**

Hokim faqat maslahat beradi.

**APPROVED AUTOMATION**

Hokim tasdiqlangan rejani bajaradi.

Har bir hokim uchun xarajat limiti va ruxsatlar bo‘lsin.

Foydalanuvchi ruxsatisiz katta resurs sarflanmasin.

---

# 14. WORLD POLITICS SIMULATION

AI saltanatlar ham diplomatiya va savdoda faol qatnashsin.

Masalan:

- Andijon va Shahrixon savdo kelishuvi tuzadi.
- AI saltanatlar o‘zaro ittifoq tuzishi mumkin.
- Savdo aloqalari iqtisodiyotga ta’sir qiladi.
- AI hukmdorlar mavjud kelishuvlarni qayta baholaydi.
- Dunyo siyosiy holati vaqt bilan o‘zgaradi.

AI davlatlar cheksiz resursga ega bo‘lmasin.

Ular ham mavjud iqtisodiy qoidalarga amal qilsin.

Barcha AI qarorlari serverdagi GameState ga asoslangan bo‘lsin.

---

# 15. WORLD NEWS & DIPLOMATIC EVENTS

O‘yinda yangi bo‘lim:

**DUNYO YANGILIKLARI**

Unda quyidagi voqealar ko‘rsatilsin:

- Yangi ittifoq tuzildi.
- Savdo shartnomasi imzolandi.
- Saltanat yangi shahar ochdi.
- Hudud egasi o‘zgardi.
- Yangi savdo yo‘li tashkil etildi.
- Qo‘shma loyiha yakunlandi.

Har bir yangilikda:

- Voqea turi
- Tegishli saltanatlar
- O‘yin sanasi
- Qisqacha izoh
- Kerak bo‘lsa xaritada ko‘rsatish tugmasi

mavjud bo‘lsin.

Yangiliklar haqiqiy server eventlari asosida yaratilishi kerak.

---

# 16. WINDOWS + ANDROID CROSSPLAY

**ASOSIY TALAB:**

Men Windows PC da o‘ynayman.

Sherigim Android telefonda o‘ynaydi.

Ikkalamiz bitta dunyoga ulanamiz.

Quyidagi ssenariy ishlasin:

**WINDOWS PLAYER**

1. O‘z saltanatiga kiradi.
2. Android o‘yinchisini xaritada ko‘radi.
3. Ittifoq taklifi yuboradi.

**ANDROID PLAYER**

1. Online xabarnomani oladi.
2. Ittifoq taklifini ochadi.
3. Qabul qiladi.

**SERVER**

1. Ikkala akkauntni tekshiradi.
2. Ittifoq yaratadi.
3. A’zolarni saqlaydi.
4. Ittifoq statusini yangilaydi.
5. Ikkala clientga SignalR orqali xabar beradi.

Natijada ikki o‘yinchi o‘z ittifoqini ko‘ra olsin.

---

# 17. REAL-TIME SYNCHRONIZATION

SignalR orqali quyidagi eventlarni qo‘llab-quvvatla:

```text
AllianceCreated
AllianceInvitationReceived
AllianceInvitationAccepted
AllianceMemberJoined
AllianceMemberLeft
TradeOfferCreated
TradeCompleted
DiplomaticAgreementCreated
DiplomaticAgreementAccepted
TerritoryTransferred
AllianceProjectUpdated
KingdomStatisticsUpdated
WorldNewsPublished
```

Client aloqasi uzilib qayta ulanganda serverdan dolzarb holatni olsin.

Eventlar takror yuborilganda ma’lumotlar ikki marta qo‘shilmasin.

WorldId va KingdomId ruxsatlari tekshirilsin.

---

# 18. MULTIPLAYER SECURITY

Server quyidagilarni tekshirishi shart:

- Authentication
- Kingdom ownership
- Alliance membership
- Role permissions
- Market transactions
- Resource ownership
- Territory ownership
- Diplomacy permissions
- Project contribution limits
- Idempotency

Barcha muhim operatsiyalar server orqali bajarilsin.

Client resurs yoki ittifoq balansini o‘zi o‘zgartira olmasin.

Kelishuvlar va savdo database tranzaksiyasi orqali saqlansin.

Parollar va maxfiy tokenlarni kod ichiga yozma.

Online o‘yinda dastlab tayyor diplomatik xabarlar ishlat. Erkin chat kelajakda qo‘shilsa, tegishli moderatsiya, bloklash va xabar berish mexanizmlarini yarat.

---

# 19. PREMIUM DIPLOMACY UI/UX

PROMPT 19B dagi UI Design System dan foydalan.

Yangi ekran:

**DIPLOMATIYA MARKAZI**

Bo‘limlar:

- MUNOSABATLAR
- ITTIFOQLAR
- KELISHUVLAR
- TAKLIFLAR
- DUNYO YANGILIKLARI

### Diplomatik karta

Har bir saltanat uchun:

- Saltanat nomi
- Hukmdor
- Gerb yoki ramz
- Munosabat holati
- Ishonch darajasi
- Faol kelishuvlar

ko‘rsatilsin.

### Ittifoq paneli

- Ittifoq nomi
- A’zolar
- Umumiy loyihalar
- Ittifoq xazinasi
- Takliflar
- Boshqaruv

### Dizayn

- Professional typography
- Minimalistik kartalar
- Chiroyli ikonkalar
- Silliq animatsiyalar
- Responsive layout
- O‘zbekcha matnlar

Windows va Android interfeyslarini alohida tekshir.

---

# 20. UI ANIMATIONS

Yangi diplomatiya va ittifoq oynalariga professional mikroanimatsiyalar qo‘sh.

Misollar:

- Taklif kelganda notification.
- Ittifoq qabul qilinganda yengil tantanali animatsiya.
- Karta ochilganda smooth transition.
- Yangi kelishuv tasdiqlanganda status yangilanishi.
- Dunyo xaritasida ittifoqdosh davlatlarni ajratib ko‘rsatish.

Animatsiyalar UI ni sekinlashtirmasin.

Mobil qurilmada matn va tugmalar kesilib qolmasin.

---

# 21. DATABASE MODELS

PostgreSQLga zarur modellarni qo‘sh:

```text
Alliances
AllianceMembers
AllianceInvitations
AllianceProjects
AllianceContributions
DiplomaticRelations
DiplomaticAgreements
TerritoryAgreements
KingdomGovernors
WorldNewsEvents
```

Mavjud jadval va migratsiyalarni tekshir.

Bir xil ma’lumotni saqlaydigan takroriy jadvallar yaratma.

Barcha yangi foreign key va unique constraintlarni loyihala.

Migratsiyalarni test bazasida tekshir.

Production ma’lumotlarini nazoratsiz o‘chirib yuborma.

---

# 22. PROJECT ARCHITECTURE

Mavjud loyihaga mos modullarni qo‘sh.

```text
AndijonSaltanatlar/
│
├── Alliances/
│   ├── AllianceManager.cs
│   ├── AllianceMember.cs
│   ├── AllianceInvitation.cs
│   ├── AlliancePermissions.cs
│   └── AllianceProjectManager.cs
│
├── Diplomacy/
│   ├── DiplomacyManager.cs
│   ├── DiplomaticRelation.cs
│   ├── DiplomaticAgreement.cs
│   ├── DiplomaticOffer.cs
│   └── DiplomaticEventSystem.cs
│
├── KingdomAdministration/
│   ├── KingdomAdministrationManager.cs
│   ├── RegionalGovernorManager.cs
│   ├── GovernorPermissions.cs
│   └── KingdomStatisticsService.cs
│
├── WorldPolitics/
│   ├── WorldPoliticsManager.cs
│   ├── AIWorldDiplomacy.cs
│   ├── WorldNewsService.cs
│   └── PoliticalEvent.cs
│
├── SharedMarket/
│   ├── AllianceMarketService.cs
│   ├── SharedTradeAgreement.cs
│   └── AllianceContributionService.cs
│
└── UI/
    ├── DiplomacyCenterScreen.cs
    ├── AllianceScreen.cs
    ├── DiplomaticOfferPanel.cs
    ├── AllianceProjectPanel.cs
    ├── KingdomAdministrationScreen.cs
    └── WorldNewsPanel.cs
```

Mavjud klasslar bo‘lsa, ularning ikkinchi nusxasini yaratma.

---

# 23. FIRST WORKING VERTICAL SLICE

**BIRINCHI NAVBATDA SHU JARAYONNI ISHLAT:**

WINDOWS PLAYER
↓
ASAKA SALTANATIGA KIRADI
↓
ANDROID O‘YINCHISINI TOPADI
↓
ITTIFOQ TAKLIFI YUBORADI
↓
ANDROID PLAYER TAKLIFNI OLADI
↓
QABUL QILADI
↓
SERVER ITTIFOQNI YARATADI
↓
IKKALA CLIENTDA ITTIFOQ KO‘RINADI
↓
SAVE / RECONNECT

Bu zanjir ishlamaguncha murakkab dunyo siyosati mexanikalarini tayyor deb hisoblama.

---

# 24. SECOND VERTICAL SLICE

**IKKI O‘YINCHI BIRGALIKDA SAVDO LOYIHASINI QURADI:**

1. Windows o‘yinchisi ittifoq loyihasini yaratadi.
2. Android o‘yinchisi loyihani ko‘radi.
3. Windows o‘yinchisi yog‘och ajratadi.
4. Android o‘yinchisi tanga ajratadi.
5. Server resurslarni tekshiradi.
6. Ittifoq loyiha progressi yangilanadi.
7. Loyiha yakunlanadi.
8. Ikkala o‘yinchi natijani ko‘radi.

Barcha operatsiyalar serverda saqlansin.

---

# 25. THIRD VERTICAL SLICE

**HUDUD BO‘YICHA KELISHUV:**

1. Windows o‘yinchisi hudud bo‘yicha taklif yuboradi.
2. Android o‘yinchisi taklifni qabul qiladi.
3. Server shartlarni tekshiradi.
4. Hudud transferi amalga oshiriladi.
5. Shaharlar va aholi boshqaruvi yangilanadi.
6. Dunyo xaritasi ikkala clientda yangilanadi.

Avval test dunyosida oddiy hudud bilan sinovdan o‘tkaz.

Transfer noto‘g‘ri bajarilganda eski ma’lumotlar buzilmasin.

---

# 26. TESTING

Quyidagilarni tekshir:

### Alliances

- [ ] Ittifoq yaratish mumkin.
- [ ] Taklif yuborish ishlaydi.
- [ ] Taklifni qabul qilish ishlaydi.
- [ ] Taklifni rad qilish ishlaydi.
- [ ] Ittifoq a’zolari to‘g‘ri saqlanadi.
- [ ] Ittifoqdan chiqish mumkin.
- [ ] Rollar va ruxsatlar ishlaydi.

### Multiplayer

- [ ] Windows client serverga ulanadi.
- [ ] Android client serverga ulanadi.
- [ ] Ikkala o‘yinchi bir WorldId ga ulanadi.
- [ ] Ittifoq holati sinxronlashadi.
- [ ] Reconnect ishlaydi.
- [ ] Offline o‘yinchi qayta kirganda takliflarni ko‘radi.

### Economy

- [ ] Qo‘shma loyiha yaratish mumkin.
- [ ] Resurs ajratish ishlaydi.
- [ ] Resurs ikki marta yechilmaydi.
- [ ] Ittifoq bozori ishlaydi.
- [ ] Mavjud Marketplace buzilmagan.

### Territory

- [ ] Hudud transferi server orqali bajariladi.
- [ ] Hudud egasi bir marta o‘zgaradi.
- [ ] Aholi statistikasi to‘g‘ri.
- [ ] Shahar va binolar saqlanadi.

### UI/UX

- [ ] Diplomatiya paneli professional.
- [ ] Ittifoqlar paneli professional.
- [ ] Shriftlar yagona.
- [ ] O‘zbekcha belgilar to‘g‘ri.
- [ ] Windows layout ishlaydi.
- [ ] Android touch layout ishlaydi.
- [ ] Tugmalar va xabarnomalar to‘g‘ri ishlaydi.

### Build

- [ ] `dotnet build` muvaffaqiyatli.
- [ ] Server testlari bajarilgan.
- [ ] Windows release ishlaydi.
- [ ] Android APK build qilinadi.

---

# 27. IMPLEMENTATION ORDER

Ishni quyidagi tartibda bajar:

1. Repository audit.
2. Existing multiplayer audit.
3. Alliance models.
4. Alliance API.
5. Alliance invitations.
6. SignalR notifications.
7. Windows and Android alliance UI.
8. Alliance roles and permissions.
9. Shared economy.
10. Cooperative projects.
11. Marketplace integration.
12. Diplomatic agreements.
13. Territory agreements.
14. Kingdom administration.
15. AI world politics.
16. World News panel.
17. Save/Load.
18. Integration tests.
19. Windows and Android runtime tests.
20. Release builds.

**Faqat reja yozib to‘xtama.**

Har bir bosqichda haqiqiy kod yoz va ishlaydigan funksiyani tekshir.

---

# 28. AGENT CONTINUITY

Quyidagi faylni yarat yoki yangila:

`docs/PROMPT_20_PROGRESS.md`

Unda:

- Mavjud tizimlar auditi.
- O‘zgartirilgan fayllar.
- Ishlayotgan ittifoq tizimi.
- Multiplayer sinovlar.
- Savdo va resurs natijalari.
- UI/UX holati.
- Build natijalari.
- Hali bajarilmagan ishlar.
- Keyingi aniq vazifa.

bo‘lsin.

---

# 29. FINAL ACCEPTANCE CRITERIA

- [ ] Windows va Android crossplay ishlaydi.
- [ ] O‘yinchilar bir-biriga ittifoq taklifi yubora oladi.
- [ ] Ittifoqni qabul qilish va rad etish mumkin.
- [ ] Ittifoq a’zolari va rollari ishlaydi.
- [ ] Ittifoq bozori mavjud.
- [ ] Qo‘shma loyihalar ishlaydi.
- [ ] Resurslar xavfsiz hisoblanadi.
- [ ] Diplomatik kelishuvlar ishlaydi.
- [ ] Hudud bo‘yicha kelishuv mavjud.
- [ ] Hudud transferi shaharlarga va aholiga to‘g‘ri ta’sir qiladi.
- [ ] Saltanat boshqaruvi paneli ishlaydi.
- [ ] Hokimlar tizimi bilan integratsiya mavjud.
- [ ] AI davlatlar diplomatik faoliyat yuritadi.
- [ ] Dunyo yangiliklari mavjud.
- [ ] Professional responsive UI mavjud.
- [ ] O‘zbekcha shriftlar va matnlar to‘g‘ri.
- [ ] Server authoritative.
- [ ] PostgreSQL saqlash ishlaydi.
- [ ] Windows build tekshirilgan.
- [ ] Android build tekshirilgan.

---

# 30. FINAL INSTRUCTION

**MAVJUD ANDIJON: SALTANATLAR LOYIHASINI HAQIQIY KO‘P O‘YINCHILI STRATEGIYA O‘YINIGA AYLANTIR.**

Maqsad:

Men Windows PC da o‘ynayman.

Sherigim Android telefonda o‘ynaydi.

Biz bir xil dunyoga ulanib:

- O‘z shaharlarimizni quramiz.
- Guruch va boshqa mahsulotlar bilan savdo qilamiz.
- Bir-birimizga ittifoq taklifi yuboramiz.
- Ittifoq tuzamiz.
- Umumiy loyihalarga resurs ajratamiz.
- Hudud bo‘yicha diplomatik kelishuvlar tuzamiz.
- O‘z saltanatlarimizni rivojlantiramiz.
- Dunyo siyosatidagi o‘zgarishlarni ko‘ramiz.

Barcha natijalar serverda saqlansin va qurilmalar orasida sinxronlashsin.

**ENG MUHIM:**

Avval PC → Android ittifoq taklifi → qabul qilish → serverda saqlash → ikkala clientda ko‘rinish zanjirini haqiqatan ishlat.

Keyin qo‘shma iqtisodiyot va hudud kelishuvlarini qo‘sh.

UI dizaynini mavjud PROMPT 19B Design System bilan bir xil uslubda saqla.

Ish yakunida:

1. O‘zgartirilgan C# fayllar.
2. Backend API va SignalR eventlari.
3. Ittifoq tizimi.
4. Umumiy iqtisodiyot.
5. Hudud bo‘yicha kelishuvlar.
6. Saltanat boshqaruvi.
7. UI/UX o‘zgarishlari.
8. Multiplayer test natijalari.
9. Windows build holati.
10. Android build holati.
11. Hali bajarilmagan funksiyalar.

haqida aniq hisobot ber.

**ANDIJON: SALTANATLAR — O‘YINCHILAR BIR-BIRI BILAN SAVDO QILADIGAN, ITTIFOQ TUZADIGAN VA BIRGALIKDA TARIXIY DUNYONI RIVOJLANTIRADIGAN ONLINE STRATEGIYA O‘YINI BO‘LSIN!**
# PROMPT 21 — ANDIJON: SALTANATLAR
## Cross-Platform Performance Optimization, Smooth Animations, Premium UI/UX, Multiplayer Stability & Release Engineering
### C# + MonoGame + ASP.NET Core + PostgreSQL
### Windows PC + Android | Shared Online Strategy World

---

# 1. SENING ROLING

Sen quyidagi mutaxassisliklarga ega professional AI coding agent sifatida ishlaysan:

- Senior C# Game Developer
- MonoGame Rendering Engineer
- Game Performance Optimization Specialist
- Technical Animation Engineer
- Android Game Development Engineer
- Multiplayer Networking Architect
- UI/UX & Typography Specialist
- QA Automation Engineer
- Release Engineer

Biz **ANDIJON: SALTANATLAR** nomli 2.5D izometrik tarixiy city-builder va multiplayer strategiya o‘yinini ishlab chiqmoqdamiz.

Texnologiyalar:

- C#
- MonoGame
- .NET
- Visual Studio Code
- MonoGame DesktopGL
- MonoGame Android
- ASP.NET Core Web API
- SignalR
- PostgreSQL
- Entity Framework Core

**BU PROMPT 21. MAVJUD LOYIHANI BOSHDAN YARATMA!**

Oldingi 20 bosqichdagi tizimlar saqlansin.

Mavjud kodni o‘rganmasdan yangi GameEngine, Renderer, UI Framework yoki Multiplayer Server yaratma.

Bu bosqichning asosiy maqsadi:

**O‘YINNI WINDOWS VA ANDROIDDA SILLIQ, BARQAROR, PROFESSIONAL VA HAQIQATAN BIRGALIKDA O‘YNALADIGAN HOLATGA KELTIRISH.**

---

# 2. ENG MUHIM QOIDALAR

Quyidagi qoidalarga qat’iy amal qil:

1. Ishlayotgan kodni sababsiz qayta yozma.
2. Oldingi gameplay mexanikalarini olib tashlama.
3. Muammolarni profiling va real test orqali aniqlagin.
4. FPSni o‘lchamasdan optimallashtirildi deb aytma.
5. Android APKni real telefonda sinamasdan to‘liq tekshirildi deb belgilama.
6. Server ishlamasa, multiplayerni tayyor deb ko‘rsatma.
7. Animatsiya asseti mavjud bo‘lmasa, tayyor deb da’vo qilma.
8. Yangi UI mavjud funksiyalarni yashirib qo‘ymasligi kerak.
9. Performance uchun iqtisodiyot va multiplayer qoidalarini buzma.
10. O‘yinchi ma’lumotlarini saqlashning xavfsizligini tekshir.

**Natija faqat hujjat emas, haqiqiy kod o‘zgarishlari va ishlaydigan build bo‘lishi kerak.**

---

# 3. COMPLETE REPOSITORY AUDIT

Avval repositoryni tekshir.

Quyidagilarning haqiqiy holatini aniqlagin:

- Windows MonoGame client
- Android MonoGame client
- Shared Game Core
- SpriteBatch Renderer
- Texture Manager
- Animation System
- Citizen Simulation
- Farming
- Construction
- Road Network
- Caravan System
- World Map
- Economy
- Multiplayer Client
- ASP.NET Core Server
- PostgreSQL
- SignalR
- Authentication
- Marketplace
- Alliances
- Diplomacy
- Territory Ownership
- Save/Load
- UI Framework

Har bir tizimga status ber:

- WORKING
- PARTIALLY WORKING
- BROKEN
- MISSING
- NOT TESTED

Auditni `docs/PROMPT_21_AUDIT.md` fayliga yoz.

Auditdan keyin aniqlangan muammolarni muhimligiga qarab tuzatishga kirish.

---

# 4. PERFORMANCE TARGETS

Maqsadli Windows kompyuteri:

- Intel Core i5-12400
- 8 GB RAM
- Intel UHD Graphics 730
- Windows 11

Android uchun bir nechta grafik quvvat darajasini qo‘llab-quvvatla.

### Maqsadli natijalar

| Qurilma | Maqsadli FPS |
|---|---|
| Windows, Low | 60 FPS |
| Windows, Medium | 60 FPSga intilish |
| Android, Low | Barqaror 30 FPS |
| Android, Medium | Qurilma imkoniyatiga mos 30–60 FPS |

Bu qiymatlar kafolatlangan natijalar emas, optimallashtirish maqsadlari hisoblanadi.

O‘yin grafikasi va aholi soniga qarab natija o‘zgarishi mumkin.

Har bir testda:

- Average FPS
- 1% Low FPS
- Frame Time
- Memory Usage
- CPU Usage
- GC Allocations

imkon qadar o‘lchansin.

---

# 5. PERFORMANCE PROFILER

O‘yinga development-only performance overlay qo‘sh.

Unda quyidagilar bo‘lsin:

- FPS
- Frame time
- Update time
- Draw time
- Visible sprites
- Active animations
- Active NPCs
- Memory usage
- Network latency
- Simulation tick duration

PC'da maxsus debug tugmasi orqali ochilsin.

Androidda development sozlamasidan yoqilsin.

Production versiyada sukut bo‘yicha o‘chirilgan bo‘lsin.

Profilerning o‘zi o‘yinni sezilarli sekinlashtirmasin.

---

# 6. SPRITEBATCH OPTIMIZATION

Mavjud MonoGame Renderer tizimini optimallashtir.

Quyidagilarni tekshir:

- SpriteBatch Begin/End chaqiruvlari.
- Texture switching.
- Draw call batching.
- Transparency.
- Render sorting.
- Camera transforms.
- Texture cache.
- Sprite atlas.

Maqsad:

Bir xil texture atlasdagi obyektlarni imkon qadar samarali chizish.

Biroq optimallashtirish natijasida izometrik depth sorting buzilmasin.

Binolar, fuqarolar, daraxtlar va transportlar to‘g‘ri qatlamda chizilsin.

---

# 7. MAP CHUNKING & VIEWPORT CULLING

Katta Asaka va Andijon xaritalari uchun chunk-based rendering yarat yoki mavjudini yaxshila.

Xaritani mantiqiy chunklarga bo‘l.

Masalan:

- 16×16 tile
- 32×32 tile

Haqiqiy o‘lchamni profiling asosida tanla.

Faqat kamera ko‘rayotgan hududdagi obyektlarni render qil.

Quyidagilar uchun culling ishlasin:

- Terrain
- Buildings
- Roads
- Trees
- Citizens
- Caravans
- Decorations
- Effects

Ko‘rinmaydigan obyektlarni chizish shart emas.

Ammo ularning iqtisodiy va simulyatsiya holati yo‘qolmasin.

---

# 8. TEXTURE & MEMORY MANAGEMENT

Windows va Androidda texture xotirasini optimallashtir.

Talablar:

- Texture atlas.
- Asset caching.
- Texture reuse.
- Resource disposal.
- On-demand loading.
- Unused asset cleanup.
- Device reset handling.

Har bir NPC uchun alohida yangi Texture2D yaratma.

Bir xil texturelar umumiy resurs sifatida ishlatilsin.

Texturelarni har frame da qayta yuklama.

Android qurilmada grafik xotira cheklangan bo‘lishi mumkinligini hisobga ol.

Katta atlaslarni avtomatik ishlatishdan oldin maksimal texture o‘lchamlarini tekshir.

---

# 9. ANIMATION SYSTEM OVERHAUL

**BU PROMPTNING ENG MUHIM QISMLARIDAN BIRI.**

Mavjud animatsiyalarni professional darajaga yaqinlashtir.

Muammolar:

- NPC yurayotganda sirpanishi.
- Ot animatsiyasi tezlikka mos kelmasligi.
- Karvonlarning keskin burilishi.
- Animatsiya kadrlarining sakrashi.
- Bino qurilishining birdan paydo bo‘lishi.
- UI panellarining keskin ochilishi.
- Kamera harakatining qotishi.
- Bir xil NPC animatsiyalarining bir vaqtda takrorlanishi.

Har bir muammoni alohida tekshir.

Animatsiya holati gameplay holatiga bog‘langan bo‘lsin.

---

# 10. SMOOTH CHARACTER MOVEMENT

Fuqarolar va boshqa personajlar uchun silliq harakatni yaxshila.

Talablar:

- Fixed timestep gameplay simulation.
- Render interpolation.
- Directional animation.
- Smooth visual transitions.
- Stable sprite anchors.
- Correct animation playback speed.
- No unnecessary animation resets.

NPC yo‘l bo‘ylab yurganda animatsiyasi uning harakat tezligiga moslashsin.

Burilishda noto‘g‘ri sprite ko‘rinib qolmasin.

Agar mavjud assetlar 4 yo‘nalishli bo‘lsa, tizimni avval shu assetlarga mos ishlat.

Keyinchalik 8 yo‘nalishli animatsiyalarga kengaytirish mumkin bo‘lsin.

---

# 11. HORSE, CARAVAN & VEHICLE ANIMATIONS

Ot va aravalar harakatini takomillashtir.

### Ot

- Idle
- Walking
- Trotting
- Turning
- Stopping

### Arava

- Smooth movement
- Wheel rotation
- Loading
- Unloading
- Turning
- Stopping

### Karvon

- Group movement
- Formation spacing
- Route following
- Arrival animation

Transportlar yangi yo‘lga o‘tganda tabiiy harakatlansin.

Serverdagi haqiqiy koordinatalar bilan clientdagi vizual holat ajratilsin.

Client interpolation orqali silliq ko‘rsatish mumkin.

---

# 12. CONSTRUCTION & FARMING ANIMATIONS

Qurilish va dehqonchilik animatsiyalarini yaxshila.

### Bino qurilishi

- Foundation
- Walls
- Roof
- Completed

### Guruch yetishtirish

- Prepared
- Planted
- Sprouting
- Growing
- Mature
- Harvested

Animatsiya haqiqiy ConstructionProgress yoki CropGrowth ma’lumotlaridan kelib chiqsin.

Clientda hosil yetildi deb ko‘ringani uchun server avtomatik resurs bermasin.

Hosilni yig‘ish serverdagi haqiqiy qoida orqali bajarilsin.

Bino va ekin assetlari yetishmasa, aniq belgilangan vaqtinchalik animatsiyalar ishlat.

---

# 13. CAMERA SMOOTHNESS

PC va Android uchun kamera tizimini yaxshila.

### Windows

- Mouse drag.
- Keyboard movement.
- Scroll zoom.
- Smooth camera focus.
- Camera bounds.

### Android

- One-finger pan.
- Two-finger pinch-to-zoom.
- Tap to select.
- Touch-friendly construction.
- Smooth camera inertia.

### Muhim

- Kamera juda tez harakatlanmasin.
- Zoom vaqtida xarita sakramasin.
- Sensorli boshqaruv UI tugmalariga xalaqit bermasin.
- Katta xaritada kamera chegaradan chiqib ketmasin.

---

# 14. UI/UX FINAL POLISH

PROMPT 19B da yaratilgan Design Systemni saqla.

Asosiy e’tibor:

- Typography
- Layout
- Spacing
- Buttons
- Panels
- Icons
- Navigation
- Modal windows
- Animations
- Responsive behavior

Barcha yozuvlar o‘zbekcha bo‘lsin.

### Shriftlar

Inter yoki Noto Sans kabi o‘qilishi oson shriftlardan foydalanish mumkin.

Tekshir:

- Oʻ
- Gʻ
- O‘
- G‘
- Qo‘shma so‘zlar
- Uzun tugma nomlari

Unicode matnlar MonoGame rendering tizimida buzilmasin.

### UI muammolari

- Matn kesilib qolmasin.
- Tugmalar bir-biriga yopishib ketmasin.
- Bo‘limlar bir xil spacing ishlatsin.
- Resurslar paneli ekran tashqarisiga chiqmasin.
- Mobil oynalarda scroll ishlasin.

---

# 15. MOBILE UI OPTIMIZATION

Android interfeysi PC interfeysining kichraytirilgan nusxasi bo‘lmasin.

### Android talablar

- Landscape layout.
- Safe area support.
- Touch-friendly navigation.
- Large tap targets.
- Responsive modals.
- Scalable typography.
- Scrollable panels.
- Device DPI awareness.

### Asosiy mobil menyular

- SHAHAR
- QURILISH
- DEHQONCHILIK
- BOZOR
- SALTANAT
- DUNYO
- VAZIR

Navigatsiya qulay va ixcham bo‘lsin.

Kichik telefonda foydalanuvchi barcha muhim funksiyalarga ortiqcha murakkabliksiz kira olsin.

---

# 16. GRAPHICS QUALITY PRESETS

Grafika sozlamalarini yarat:

## LOW

- Reduced decorative animations.
- Limited particles.
- Reduced visible NPC count.
- Simplified effects.
- Lower render resolution where appropriate.

## MEDIUM

- Standard animations.
- Moderate decorations.
- Balanced NPC rendering.

## HIGH

- More environmental animations.
- Higher visual detail.
- More particles.
- Improved optional effects.

Grafika sozlamalari gameplay iqtisodiyotini o‘zgartirmasin.

LOW grafikada ham barcha muhim o‘yin obyektlari ko‘rinishi kerak.

---

# 17. MULTIPLAYER NETWORK OPTIMIZATION

Windows va Android bir serverga ulanadi.

Tarmoq uzatmalarini optimallashtir.

Talablar:

- Delta updates.
- Event batching.
- Interest management.
- Snapshot recovery.
- Network message versioning.
- Compression where beneficial.
- Server-side validation.

Har frame da butun GameState ni yuborma.

Faqat tegishli o‘zgarishlar yuborilsin.

Masalan:

O‘yinchi guruch sotdi.

Server barcha shahar binolarini qayta yuborish o‘rniga:

- TradeCompleted
- InventoryUpdated
- TreasuryUpdated

kabi zarur hodisalarni yuborsin.

Hodisalar tartibi va state versiyasi saqlansin.

---

# 18. REAL INTERNET MULTIPLAYER

**MUHIM: FAQAT BITTA WI-FI DA ISHLASH YETARLI EMAS.**

Maqsad:

Men Windows PC da uy internetidan ulanaman.

Sherigim Android telefonda boshqa Wi-Fi yoki mobil internetdan ulanadi.

Ikkalamiz bitta dunyoda o‘ynaymiz.

Buning uchun:

1. Internet orqali mavjud ASP.NET Core server.
2. PostgreSQL database.
3. HTTPS.
4. To‘g‘ri API endpoint.
5. SignalR ulanishi.
6. Authentication.
7. Server-side save.
8. Reconnect.

kerak.

### Deployment

Mavjud loyihaga mos server hosting variantini tanla.

Backend va ma’lumotlar bazasini ommaga to‘g‘ridan-to‘g‘ri bir xil usulda ochma: database faqat backend uchun kerakli darajada mavjud bo‘lsin.

Development uchun lokal serverdan foydalanish mumkin.

Production uchun xavfsiz internet server talab qilinadi.

Server endpointini environment yoki konfiguratsiya orqali boshqar.

Android clientda PCning `localhost` manzilini ishlatma.

---

# 19. AUTOMATIC RECONNECT

Internet vaqtincha uzilganda o‘yin butunlay yopilib ketmasin.

Client holatlari:

```text
Connected
Connecting
Reconnecting
Disconnected
AuthenticationExpired
VersionMismatch
```

Aloqa uzilganda:

- Foydalanuvchiga xabar ko‘rsat.
- Muhim online buyruqlarni tasdiqlanmaguncha bajarilgan deb hisoblama.
- Qayta ulanishga harakat qil.
- Serverdan dolzarb holatni ol.
- Takroriy buyruqlarni idempotency orqali boshqar.

Internet tiklanganda butun dunyoni noto‘g‘ri qayta yaratma.

Save va ownership ma’lumotlari buzilmasin.

---

# 20. HIGH LATENCY HANDLING

Mobil internetda kechikish bo‘lishi mumkin.

Tarmoq testlari:

- 50 ms
- 150 ms
- 300 ms

sharoitlarini simulyatsiya qilib tekshir.

Bu qiymatlar test ssenariylari, kafolatlangan internet tezliklari emas.

### Talablar

- UI input darhol feedback ko‘rsatsin.
- Server tasdiqlanmaguncha muhim iqtisodiy natija yakunlangan deb ko‘rsatilmasin.
- Savdo tugmasi takror bosilsa, ikki savdo bajarilmasin.
- Hudud egasi server tasdiqlagandan keyin yangilansin.
- NPC va karvon animatsiyalarida interpolation ishlasin.

---

# 21. ONLINE ECONOMY CONSISTENCY

Oldingi farming va marketplace tizimlarini stress test qil.

Muhim holatlar:

- Bir vaqtning o‘zida ikkita xaridor bitta taklifni olmoqchi.
- Sotuvchi taklifni bekor qiladi.
- Xaridor savdo paytida internetdan uziladi.
- Server savdo davomida qayta ishga tushadi.
- Ekin hosili yig‘ilganda client qayta ulanadi.
- Savdo yetkazib berish holatida qoladi.

Server tranzaksiyalarni atomik bajarsin.

Resurslar:

- Yo‘qolib qolmasin.
- Ikki marta qo‘shilmasin.
- Noto‘g‘ri o‘yinchiga o‘tmasin.

Bu multiplayer iqtisodiyotining majburiy talabi.

---

# 22. TERRITORY OWNERSHIP CONSISTENCY

Hudud egaligi o‘zgarishini sinovdan o‘tkaz.

Misol:

Asaka saltanati Shahrixon hududini diplomatik kelishuv orqali qo‘shadi.

Server:

1. Ruxsatni tekshiradi.
2. Hudud egasini tekshiradi.
3. Transfer shartlarini tekshiradi.
4. Region ownershipni yangilaydi.
5. City ownershipni yangilaydi.
6. Aholi boshqaruvini yangilaydi.
7. Databasega yozadi.
8. Ikkala clientga xabar beradi.

Quyidagi xatolar bo‘lmasin:

- Ikki marta aholi qo‘shish.
- Hudud egasining ikki xil ko‘rinishi.
- Shaharning yo‘qolishi.
- Ruxsatsiz transfer.
- Reconnectdan keyin eski ownership qaytishi.

---

# 23. SERVER PERFORMANCE

Backendda quyidagilarni optimallashtir:

- Database queries.
- EF Core tracking.
- Network serialization.
- World simulation ticks.
- AI decisions.
- Market transactions.
- SignalR broadcasts.
- Background jobs.

Database indekslarini haqiqiy query patternlarga qarab yarat.

Har bir o‘yinchi uchun butun dunyo simulyatsiyasini alohida qayta ishlatma.

Bitta WorldId uchun izchil authoritative state bo‘lsin.

Server CPU va RAM sarfini o‘lcha.

---

# 24. MOBILE BATTERY & MEMORY

Android qurilmada:

- Keraksiz CPU faoliyatini kamaytir.
- Fon holatida renderingni chekla.
- App pause/resume jarayonini to‘g‘ri boshqar.
- Texture va asset resurslarini ehtiyotkorlik bilan ishlat.
- Internet ulanishini to‘g‘ri tikla.
- UI va GameState yo‘qolishining oldini ol.

Ilova backgroundga o‘tganda server simulyatsiyasi clientga bog‘liq bo‘lmasin.

Client qayta ochilganda serverdan yangi state olsin.

---

# 25. ERROR HANDLING & DEBUGGING

Quyidagi holatlar uchun tushunarli xabarlar yarat:

- Serverga ulanib bo‘lmadi.
- Internet mavjud emas.
- Akkaunt sessiyasi tugagan.
- O‘yin versiyasi mos emas.
- Savdo taklifi allaqachon sotilgan.
- Mahsulot yetarli emas.
- Resurs yetarli emas.
- Hududga ruxsat yo‘q.
- Save xatosi.
- Asset yuklash xatosi.

Foydalanuvchiga texnik stack trace ko‘rsatma.

Development loglarda kerakli diagnostik ma’lumotlarni saqla.

Parol yoki autentifikatsiya tokenlarini loglarga yozma.

---

# 26. FIRST WORKING VERTICAL SLICE

**ASOSIY MAJBURIY TEST:**

WINDOWS PC
↓
O‘YINNI OCHISH
↓
ASAKA SALTANATIGA KIRISH
↓
GURUCH EKISH
↓
HOSIL YIG‘ISH
↓
BOZORGA SOTUVGA QO‘YISH
↓
ANDROID O‘YINCHI BOSHQA INTERNETDAN ULANADI
↓
GURUCHNI SOTIB OLADI
↓
SERVER SAVDONI BAJARADI
↓
IKKALA CLIENT YANGILANADI
↓
O‘YINCHILAR QAYTA KIRADI
↓
SAVDO NATIJALARI SAQLANGAN BO‘LADI

Bu jarayon ishlamaguncha internet multiplayerni yakunlangan deb hisoblama.

---

# 27. SECOND VERTICAL SLICE — ANIMATION QUALITY

ASAKA SHAHRI
↓
FUQARO UYDAN CHIQADI
↓
SILLIQ YURADI
↓
BOZORGA BORADI
↓
ISH ANIMATSIYASI
↓
KARVON KELADI
↓
MAHSULOT YETKAZADI
↓
YANGI BINO QURILADI
↓
QURILISH ANIMATSIYASI
↓
KAMERA YANGI BINOGA FOKUS QILADI

Ushbu jarayon PC va Androidda sinab ko‘rilsin.

Animatsiya sifati, sprite sorting va FPS tekshirilsin.

---

# 28. THIRD VERTICAL SLICE — UI/UX QUALITY

Quyidagi ekranlarni to‘liq tekshir:

- Main Menu
- Login
- World Selection
- Game HUD
- Construction Menu
- Farming Panel
- Marketplace
- Alliance Panel
- Kingdom Management
- Donishmand Vazir
- Settings

Har bir ekran:

- O‘qilishi oson.
- Professional.
- Responsive.
- Uzbek localization bilan.
- Mouse va touch inputga mos.
- Tegishli animatsiyalarga ega.

bo‘lsin.

Barcha tugmalar real funksiyalarga bog‘langan bo‘lsin.

---

# 29. TEST MATRIX

Quyidagi kombinatsiyalarda testlar yarat:

### Windows

- 1280×720
- 1600×900
- 1920×1080

### Android

- Kichik landscape ekran.
- O‘rtacha ekran.
- Yuqori DPI ekran.

### Network

- Local Wi-Fi.
- Different Wi-Fi networks.
- Mobile internet.
- Temporary disconnect.
- Server restart.
- High latency.

### Gameplay

- Building construction.
- Farming.
- Harvesting.
- Trading.
- Alliances.
- Diplomacy.
- Territory ownership.
- Save/Load.

### Animation

- Citizen walking.
- Caravan movement.
- Crop growth.
- Building construction.
- UI transitions.
- Camera controls.

---

# 30. WINDOWS RELEASE

Windows uchun haqiqiy release build tayyorla.

Target:

`AndijonSaltanatlar.exe`

Kerakli:

- Content assets.
- Configuration.
- Native dependencies.
- Release metadata.
- README.

Build buyruqlarini mavjud `.csproj` va target frameworkga moslashtir.

`dotnet build`, `dotnet test` va `dotnet publish` natijalarini hujjatlashtir.

Release papkasidan o‘yinni ishga tushirib tekshir.

---

# 31. ANDROID RELEASE

Android uchun test APK tayyorla.

Talablar:

- To‘g‘ri package identifier.
- Application icon.
- Landscape orientation.
- Touch controls.
- Server configuration.
- Required permissions.
- Release build.
- App signing.

Signing kalitlari va maxfiy ma’lumotlarni repositoryga qo‘shma.

Dastlab APK orqali test qil.

Keyinchalik Play Store tarqatilishi uchun mos release jarayonini alohida tayyorlash mumkin.

APKni haqiqiy Android qurilmada ishga tushirish testi muhim.

---

# 32. PROJECT ARCHITECTURE

Mavjud arxitekturani saqlab, zarur modullarni qo‘sh.

```text
AndijonSaltanatlar/
│
├── Performance/
│   ├── PerformanceMonitor.cs
│   ├── FrameTimeTracker.cs
│   ├── MemoryMonitor.cs
│   ├── RenderingProfiler.cs
│   └── PerformanceSettings.cs
│
├── Rendering/
│   ├── ChunkRenderer.cs
│   ├── SpriteBatchOptimizer.cs
│   ├── TextureCache.cs
│   ├── ViewportCulling.cs
│   └── RenderQualityManager.cs
│
├── Animation/
│   ├── AnimationOptimizer.cs
│   ├── AnimationLODManager.cs
│   ├── SmoothMovementController.cs
│   └── RenderInterpolation.cs
│
├── Networking/
│   ├── ConnectionManager.cs
│   ├── ReconnectionManager.cs
│   ├── StateSynchronization.cs
│   ├── NetworkDiagnostics.cs
│   └── NetworkMessageVersioning.cs
│
├── Platform/
│   ├── Windows/
│   └── Android/
│
├── UI/
│   ├── ResponsiveLayoutManager.cs
│   ├── TypographyManager.cs
│   ├── UIAnimationManager.cs
│   └── MobileInputAdapter.cs
│
├── Tests/
│   ├── Performance/
│   ├── Multiplayer/
│   ├── Animation/
│   ├── Economy/
│   └── Integration/
│
└── docs/
    ├── PROMPT_21_AUDIT.md
    ├── PERFORMANCE_RESULTS.md
    ├── MULTIPLAYER_TEST_RESULTS.md
    ├── ANDROID_TEST_REPORT.md
    └── RELEASE_CHECKLIST.md
```

Agar ushbu vazifani bajaruvchi modullar allaqachon mavjud bo‘lsa, ularni kengaytir.

---

# 33. IMPLEMENTATION ORDER

Ishni quyidagi tartibda bajar:

### PHASE 01 — PROJECT AUDIT
Mavjud tizimlarning holatini tekshir.

### PHASE 02 — BUILD STABILITY
Windows, Android va serverdagi kompilyatsiya xatolarini tuzat.

### PHASE 03 — PERFORMANCE PROFILING
Asosiy sekinlashish sabablarini o‘lcha.

### PHASE 04 — RENDER OPTIMIZATION
SpriteBatch, texture va chunk renderingni yaxshila.

### PHASE 05 — ANIMATION OPTIMIZATION
Fuqarolar, otlar, aravalar va qurilish animatsiyalarini yaxshila.

### PHASE 06 — CAMERA & INPUT
PC va Android boshqaruvini silliqlashtir.

### PHASE 07 — UI/UX
Shriftlar, panellar va responsive layoutni tuzat.

### PHASE 08 — NETWORK OPTIMIZATION
SignalR va state synchronizationni yaxshila.

### PHASE 09 — INTERNET MULTIPLAYER
Ikki xil internet tarmog‘idan ulanishni test qil.

### PHASE 10 — ECONOMY CONSISTENCY
Savdo va resurs tranzaksiyalarini tekshir.

### PHASE 11 — TERRITORY CONSISTENCY
Hudud ownership va aholi hisobini tekshir.

### PHASE 12 — ANDROID OPTIMIZATION
RAM, grafik va battery performance ustida ishlagin.

### PHASE 13 — AUTOMATED TESTING
Testlarni bajar.

### PHASE 14 — RELEASE BUILD
Windows va Android build tayyorla.

### PHASE 15 — FINAL VERIFICATION
Real qurilmalarda foydalanish mumkinligini tekshir.

---

# 34. ACCEPTANCE CRITERIA

- [ ] Windows o‘yini ochiladi.
- [ ] Android o‘yini ochiladi.
- [ ] PC va Android bitta dunyoga ulanadi.
- [ ] Turli internet tarmoqlarida online o‘ynash mumkin.
- [ ] Multiplayer reconnect ishlaydi.
- [ ] FPS profiler mavjud.
- [ ] Asosiy rendering optimallashtirilgan.
- [ ] Katta xaritada culling ishlaydi.
- [ ] Texture caching ishlaydi.
- [ ] Fuqarolar silliq yuradi.
- [ ] Ot va karvon animatsiyalari yaxshilangan.
- [ ] Qurilish animatsiyalari ishlaydi.
- [ ] Guruch o‘sish animatsiyasi ishlaydi.
- [ ] Kamera silliq.
- [ ] PC boshqaruvi qulay.
- [ ] Android touch boshqaruvi qulay.
- [ ] UI shriftlari bir xil.
- [ ] O‘zbekcha matnlar to‘g‘ri.
- [ ] Mobil panellar kesilib qolmaydi.
- [ ] Savdo tranzaksiyalari xavfsiz.
- [ ] Hudud ownership sinxronlashadi.
- [ ] Save/Load ishlaydi.
- [ ] Server performance tekshirilgan.
- [ ] Windows release build mavjud.
- [ ] Android APK build mavjud.
- [ ] Test natijalari hujjatlashtirilgan.

---

# 35. FINAL INSTRUCTION

**MAVJUD ANDIJON: SALTANATLAR O‘YININI PROFESSIONAL DARAJADA OPTIMALLASHTIR.**

Yangi loyiha yaratma.

Asosiy gameplay mexanikalarini buzma.

Mening maqsadim:

**MEN WINDOWS PC DA, SHERIGIM ANDROID TELEFONDA, TURLI INTERNET TARMOQLARIDAN BITTA ONLINE DUNYOGA ULANA OLISHIMIZ KERAK.**

Biz:

- O‘z shaharlarimizni quramiz.
- Guruch ekamiz.
- Hosil yig‘amiz.
- Savdo qilamiz.
- Ittifoq tuzamiz.
- Hududlarni boshqaramiz.
- Aholini rivojlantiramiz.

Shu bilan birga:

- O‘yin qotmasin.
- Animatsiyalar silliq bo‘lsin.
- Fuqarolar tabiiy yursin.
- Karvonlar harakatlansin.
- Binolar bosqichma-bosqich qurilsin.
- O‘zbekcha yozuvlar aniq ko‘rinsin.
- Tugmalar va menyular professional bo‘lsin.
- Androidda touch boshqaruv qulay bo‘lsin.
- Server ma’lumotlarni to‘g‘ri saqlasin.

**AVVAL ISHLAYDIGAN WINDOWS + ANDROID ONLINE SAVDO ZANJIRINI, KEYIN FPS VA ANIMATSIYA SIFATINI, SO‘NG YAKUNIY UI/UX VA RELEASE BUILDNI TEKSHIR.**

Ish yakunida quyidagilarni hisobot qil:

1. O‘zgartirilgan C# fayllar.
2. Tuzatilgan xatolar.
3. Oldingi va keyingi FPS natijalari (agar o‘lchangan bo‘lsa).
4. Animatsiya yaxshilanishlari.
5. UI/UX o‘zgarishlari.
6. Multiplayer test natijalari.
7. Server va database holati.
8. Windows build holati.
9. Android APK holati.
10. Real qurilmada test natijalari.
11. Hali bajarilmagan ishlar.

**ENG MUHIM: MENGA FAQAT REJA EMAS, HAQIQATAN O‘YNASH MUMKIN BO‘LGAN VA WINDOWS HAMDA ANDROIDDA SILLIQ ISHLAYDIGAN ANDIJON: SALTANATLAR KERAK!**
# PROMPT 22 — ANDIJON: SALTANATLAR
## Advanced Population Growth, Living Families, Generations, Education, Jobs, Migration & Dynamic Kingdom Society
### C# + MonoGame + ASP.NET Core + PostgreSQL
### Windows PC + Android Cross-Platform Multiplayer

---

# 1. SENING ROLING

Sen Senior C# Game Developer, Population Simulation Engineer, Game AI Architect, City Builder Systems Designer, Multiplayer Backend Engineer va Game UI/UX Specialist sifatida ishlaysan.

Biz **ANDIJON: SALTANATLAR** nomli tarixiy muhitdan ilhomlangan 2.5D izometrik strategiya va sivilizatsiya o‘yinini ishlab chiqmoqdamiz.

Asosiy texnologiyalar:

- C#
- MonoGame
- .NET
- Visual Studio Code
- ASP.NET Core
- PostgreSQL
- Entity Framework Core
- SignalR
- JSON Data Definitions

**BU PROMPT 22. MAVJUD LOYIHANI BOSHDAN YARATMA!**

Oldingi 21 bosqichda yaratilgan tizimlarni tekshir.

Ayniqsa:

- PopulationManager
- CitizenAI
- HouseholdManager
- EconomyManager
- JobSystem
- BuildingSystem
- EducationSystem
- CityManager
- KingdomManager
- MigrationSystem
- NPC Dialogue
- Donishmand Vazir
- SaveSystem
- Online Multiplayer

modullarining haqiqiy holatini aniqlagin.

Mavjud modullarni takrorlamasdan kengaytir.

---

# 2. ASOSIY MAQSAD

O‘yin aholisi oddiy raqam sifatida qolmasin.

Har bir shahar rivojlanadigan jamiyatga ega bo‘lsin.

Tizim quyidagilarni qo‘llab-quvvatlasin:

1. Aholi ko‘payishi.
2. Oilalar va xonadonlar.
3. Avlodlar almashinuvi.
4. Yosh guruhlari.
5. Ta’lim.
6. Kasblar.
7. Ish o‘rinlari.
8. Daromad va xarajatlar.
9. Yangi shaharlarga ko‘chish.
10. Mahallalar rivojlanishi.
11. Fuqarolar mamnunligi.
12. Davlat boshqaruviga ishonch.
13. Fuqarolarning kunlik faoliyati.
14. Hududlararo aholi harakati.
15. Multiplayer aholi sinxronizatsiyasi.

**Har bir o‘zgarish haqiqiy o‘yin simulyatsiyasi bilan bog‘liq bo‘lsin.**

---

# 3. POPULATION GROWTH SYSTEM

Shahar aholisi vaqt o‘tishi bilan o‘zgarib tursin.

Aholi soniga quyidagilar ta’sir qilsin:

- Uy-joy sig‘imi.
- Oziq-ovqat mavjudligi.
- Ish o‘rinlari.
- Shahar xavfsizligi.
- Sog‘liq xizmatlari.
- Ta’lim.
- Mahalliy iqtisodiyot.
- Fuqarolar mamnunligi.
- Migratsiya.
- O‘yin ichidagi tabiiy demografik o‘sish.

Masalan:

**ASAKA**

Boshlang‘ich aholi: 150.

O‘yinchi yangi uylar quradi.

Uy-joy sig‘imi oshadi.

Yangi ish joylari paydo bo‘ladi.

Shahar sharoitlari yaxshilanganda aholining o‘sishi mumkin bo‘lsin.

Bu raqamlar namunaviy bo‘lib, haqiqiy balansni mavjud o‘yin qoidalaridan ol.

Aholi cheksiz tezlikda ko‘paymasin.

---

# 4. AGE GROUP SYSTEM

Fuqarolar quyidagi yosh guruhlariga ajratilsin:

- Children
- Youth
- Adults
- Seniors

Yosh guruhlari uchun o‘yin ichidagi vaqtga asoslangan rivojlanish tizimi yarat.

### Bolalar

- Oila tarkibida yashaydi.
- Maktabga qatnashi mumkin.
- Ta’lim tizimi bilan bog‘lanadi.

### Yoshlar

- Ta’lim va kasbiy tayyorgarlik oladi.
- Voyaga yetgach kasb tanlash tizimiga o‘tadi.

### Kattalar

- Ishlaydi.
- Daromad oladi.
- Oilaviy iqtisodiyotda qatnashadi.
- Shahar faoliyatiga hissa qo‘shadi.

### Keksalar

- Oila va jamiyat tarkibida qoladi.
- Shahar xizmatlaridan foydalanadi.
- Ayrim ixtiyoriy ijtimoiy rollarga ega bo‘lishi mumkin.

Ishchilar uchun yosh va kasbga mos talablarni belgilagin.

Bolalarni kattalarga mo‘ljallangan mehnat tizimiga avtomatik qo‘shma.

---

# 5. HOUSEHOLD & FAMILY SYSTEM

Har bir xonadon alohida iqtisodiy birlik bo‘lsin.

`Household` modeli:

- HouseholdId
- HomeBuildingId
- CityId
- DistrictId
- MemberIds
- HouseholdIncome
- HouseholdExpenses
- Savings
- FoodNeeds
- Happiness
- HousingQuality

Fuqarolar xonadonga biriktirilsin.

### Oilaviy hayot

O‘yin vaqtida:

- Yangi xonadonlar shakllanishi.
- Oilaga yangi a’zolar qo‘shilishi.
- Avlodlar ulg‘ayishi.
- Katta bo‘lgan fuqarolarning mustaqil xonadon tashkil qilishi.
- Oilalarning yangi uyga ko‘chishi.

kabi hodisalarni soddalashtirilgan demografik qoidalar bilan modellashtir.

Barcha fuqarolar majburiy ravishda bir xil oilaviy tuzilmaga ega bo‘lmasin.

Yolg‘iz yashaydigan fuqarolar ham mavjud bo‘lishi mumkin.

---

# 6. GENERATION & AGEING SIMULATION

Avlodlar almashinuvi o‘yin vaqtiga bog‘langan bo‘lsin.

`AgeingSystem` yarat.

Asosiy talablar:

- O‘yin vaqti orqali yosh rivojlanadi.
- Yosh guruhi belgilangan davrda o‘zgaradi.
- Ta’lim bosqichlari o‘zgaradi.
- Voyaga yetgan fuqaro yangi kasbga ega bo‘lishi mumkin.
- Katta bo‘lgan fuqaro mustaqil xonadon yaratishi mumkin.

Har bir fuqaroning real vaqt bo‘yicha har soniyada yoshi o‘zgarmasin.

Demografik o‘zgarishlar GameSimulation ticklari orqali ishlasin.

Shahar aholisi katta bo‘lsa, barcha odamlarni alohida batafsil hisoblash majburiy bo‘lmasin.

Statistik aholi guruhlari va ko‘rinadigan NPC tizimlaridan foydalan.

---

# 7. EDUCATION SYSTEM

Ta’lim haqiqiy gameplay mexanikasiga ega bo‘lsin.

Ta’lim inshootlari:

- Boshlang‘ich maktab
- Madrasa
- Kutubxona
- Hunarmandchilik ustaxonasi

Har bir bino:

- EducationCapacity
- TeacherCount
- EducationEfficiency
- ServiceCoverage
- StudentCount

ko‘rsatkichlariga ega bo‘lsin.

Ta’lim olgan fuqarolar kelajakda murakkabroq kasblarni egallash imkoniyatiga ega bo‘lishi mumkin.

### Misol

Asaka shahrida maktab mavjud.

Maktabga yaqin oilalarning bolalari undan foydalanadi.

Ta’lim darajasi asta-sekin oshadi.

Voyaga yetganda ayrim fuqarolar malakali kasblarni tanlash imkoniyatiga ega bo‘ladi.

Ta’lim barcha fuqarolar uchun darhol maksimal natija bermasin.

---

# 8. ADVANCED JOB SYSTEM

Fuqarolar shahardagi real ish joylariga bog‘lansin.

Dastlab kasblar:

- Dehqon
- Savdogar
- Quruvchi
- Hunarmand
- Muallim
- Tabib
- Ombor ishchisi
- Karvon xizmatchisi
- Ma’muriy xodim

Har bir ish joyi:

- BuildingId
- JobType
- RequiredSkills
- EmployeeCapacity
- CurrentEmployees
- Productivity
- Wage

xususiyatlariga ega bo‘lsin.

### Ishga joylashtirish

Fuqaro:

1. Ishlashga mos yoshda bo‘ladi.
2. O‘z malakasiga mos ish qidiradi.
3. Bo‘sh ish o‘rni tekshiriladi.
4. Ish joyiga bog‘lanadi.
5. Ishlab chiqarishga hissa qo‘shadi.
6. Daromad oladi.

Bir fuqaro bir vaqtning o‘zida bir nechta to‘liq stavkali ish o‘rnida hisoblanmasin.

---

# 9. CAREER DEVELOPMENT

Fuqarolar malakasini oshira olsin.

Masalan:

**Oddiy hunarmand → Tajribali hunarmand → Usta**

Kasbiy rivojlanishga quyidagilar ta’sir qilsin:

- Ta’lim.
- Ish tajribasi.
- Ish joyi mavjudligi.
- Tegishli texnologiyalar.
- Shahar iqtisodiyoti.

Malaka oshishi ishlab chiqarish samaradorligiga ta’sir qilishi mumkin.

Malaka darajasi oshganda fuqaroning kasbi va statistikasi yangilansin.

---

# 10. HOUSEHOLD ECONOMY

Oilalarning daromadi va xarajatlari bo‘lsin.

### Daromad

- Ish haqi.
- Ruxsat etilgan iqtisodiy faoliyatdan keladigan daromad.
- O‘yin tizimida mavjud boshqa manbalar.

### Xarajat

- Oziq-ovqat.
- Uy bilan bog‘liq xarajat.
- Bozor mahsulotlari.
- Tegishli soliqlar.

Oilalarning xarid imkoniyati bozor faoliyatiga ta’sir qilsin.

Barcha xarajatlar mavjud EconomyManager bilan bog‘langan bo‘lsin.

Resurslar ikki marta hisoblanmasin.

---

# 11. MIGRATION SYSTEM

**BU BOSQICHNING ENG MUHIM FUNKSIYALARIDAN BIRI.**

Fuqarolar shahardan shaharga ko‘chishi mumkin bo‘lsin.

Misol:

Asakada uy-joy yetishmayapti.

Shahrixonda esa:

- Bo‘sh uylar mavjud.
- Ish joylari mavjud.
- Oziq-ovqat ta’minoti yaxshi.

Sharoit va o‘yin qoidalariga ko‘ra ayrim oilalar Shahrixonga ko‘chishni tanlashi mumkin.

### Migratsiyaga ta’sir qiluvchi omillar

- Uy-joy.
- Ish imkoniyatlari.
- Daromad.
- Oziq-ovqat.
- Xizmatlar.
- Shahar barqarorligi.
- Yo‘lga ulanish.
- Hududlararo munosabatlar.

Migratsiya belgilangan davriy simulyatsiya orqali amalga oshsin.

Odamlar bir shahardan boshqasiga har soniyada tasodifiy ko‘chib yurmasin.

---

# 12. MIGRATION TYPES

Quyidagi migratsiya turlarini qo‘llab-quvvatla:

### INTERNAL MIGRATION

Bitta saltanat ichidagi shaharlar o‘rtasida ko‘chish.

Masalan:

Asaka → Shahrixon.

### INTER-KINGDOM MIGRATION

Ikki alohida saltanat o‘rtasida ko‘chish.

Bu jarayon o‘yin ichidagi diplomatik va shahar sig‘imi qoidalariga bog‘liq bo‘lsin.

### NEW DISTRICT SETTLEMENT

Yangi mahalla ochilganda unga ayrim xonadonlar joylashishi mumkin bo‘lsin.

Migratsiya avtomatik aholini cheksiz yaratib bermasin.

Ko‘chayotgan fuqaro manba shahar statistikasidan chiqib, yangi shaharga faqat bir marta qo‘shilsin.

---

# 13. MIGRATION VISUALIZATION

Ko‘chish xaritada vizual aks etsin.

Masalan:

Asakadan Shahrixonga bir nechta oila ko‘chmoqda.

Dunyo xaritasida:

- Asaka
- Shahrixon
- Ko‘chish yo‘nalishi
- Ko‘chayotgan xonadonlar soni

ko‘rsatilishi mumkin.

Yaqin ko‘rinishda arava va yo‘lovchilar animatsiyasidan foydalan.

Katta masofali ko‘chishlarni soddalashtirilgan xarita animatsiyasi orqali ko‘rsat.

Ko‘chish davomida xonadonlar yo‘qolib qolmasin yoki ikki marta hisoblanmasin.

---

# 14. TERRITORY EXPANSION & POPULATION

PROMPT 19B dagi hududlarni saltanatga qo‘shish tizimi bilan integratsiya qil.

Misol:

O‘yinchi Shahrixon hududini o‘z saltanatiga qo‘shdi.

Natijada:

- Shahrixon yangi saltanat tarkibiga kiradi.
- Mavjud oilalar saqlanadi.
- Fuqarolar uylarida qoladi.
- Ish joylari saqlanadi.
- Mahalla xizmatlari saqlanadi.
- Saltanatning umumiy aholi statistikasi yangilanadi.
- Fuqarolarning yangi boshqaruvga ishonchi vaqt bilan o‘zgarishi mumkin.

Aholi o‘yinchi mulki sifatida sotilmasin.

Hudud egaligi o‘zgargani fuqarolarning kasbi va oilasini avtomatik yo‘q qilmasin.

---

# 15. CITIZEN HAPPINESS & SOCIAL STABILITY

Fuqarolarning mamnunligi quyidagi omillarga bog‘liq bo‘lsin:

- Uy-joy.
- Ish.
- Oziq-ovqat.
- Sog‘liq xizmatlari.
- Ta’lim.
- Bozor imkoniyatlari.
- Soliq siyosati.
- Shahar infratuzilmasi.
- Boshqaruv sifati.

Har bir mahalla alohida mamnunlik ko‘rsatkichiga ega bo‘lsin.

### Misol

Mahallada:

- Uy yetarli.
- Ish joylari kam.
- Bozor mavjud.
- Suv ta’minoti yaxshi.

Bo‘lsa, Donishmand Vazir asosiy muammo ish o‘rinlari ekanini tushuntirsin.

Mamnunlik bitta bino qurilishi bilan darhol maksimal darajaga chiqmasin.

---

# 16. CITIZEN DAILY ROUTINE

Fuqarolarning kunlik hayoti mavjud AnimationSystem bilan integratsiya qilinsin.

Namunaviy jarayon:

**UY → YO‘L → ISH → BOZOR → UY**

O‘quvchilar uchun:

**UY → MAKTAB → UY**

Savdogarlar uchun:

**UY → BOZOR → SAVDO FAOLIYATI → UY**

Fuqarolar haqiqiy yo‘l tarmog‘idan foydalansin.

Animatsiyalar:

- Idle
- Walk
- Work
- Study
- Carry
- Rest

holatlarini qo‘llab-quvvatlasin.

Barcha NPC bir xil vaqtda bir xil animatsiyani boshlamasin.

---

# 17. NPC INTERACTION INTEGRATION

PROMPT 17 dagi dialog tizimi bilan bog‘la.

O‘yinchi fuqaro ustiga bosganda:

- Ismi.
- Yoshi yoki yosh guruhi.
- Kasbi.
- Oilasi.
- Uyi.
- Mahallasi.
- Ish joyi.
- Mamnunligi.
- Asosiy ehtiyoji.

ko‘rsatilsin.

Misol:

**FUQARO: HASAN**

Kasb: Hunarmand.

Mahalla: Asaka sharqiy mahallasi.

Murojaat:

“Hazratim, shaharda yangi ustaxonalar ochilsa, ko‘proq fuqarolar ishlash imkoniyatiga ega bo‘lardi.”

Bu murojaat shaharning haqiqiy ish o‘rinlari statistikasi bilan mos kelishi kerak.

---

# 18. DONISHMAND VAZIR — POPULATION ADVISOR

Donishmand Vazir yangi demografik tizimlarni tahlil qilsin.

### HOLAT 1 — UY-JOY YETISHMAYAPTI

“Hazratim, shahrimiz aholisi ko‘paymoqda. Yangi turar joylar qurishni tavsiya qilaman.”

### HOLAT 2 — ISHSIZLIK

“Hazratim, ayrim fuqarolar ish topa olmayapti. Yangi ustaxona yoki bozor qurish foydali bo‘lishi mumkin.”

### HOLAT 3 — MAKTAB YETISHMAYAPTI

“Hazratim, yosh aholimiz uchun ta’lim imkoniyatlari yetarli emas. Yangi maktab qurishni tavsiya qilaman.”

### HOLAT 4 — MIGRATSIYA

“Hazratim, Shahrixon shahrida yangi uylar va ish o‘rinlari mavjud. Ayrim xonadonlarimiz u yerga ko‘chishi mumkin.”

### HOLAT 5 — YANGI HUDUD AHOLISI

“Hazratim, saltanatimizga yangi shahar qo‘shildi. U yerdagi aholining ehtiyojlarini o‘rganishni tavsiya qilaman.”

Har bir maslahat haqiqiy GameState asosida chiqsin.

---

# 19. POPULATION DASHBOARD

Yangi professional panel yarat:

**AHOLI VA JAMIYAT**

Bo‘limlar:

1. UMUMIY
2. OILALAR
3. YOSH GURUHLARI
4. TA’LIM
5. KASBLAR
6. ISH O‘RINLARI
7. MIGRATSIYA
8. MAHALLALAR

### Umumiy panel

- Jami aholi.
- Aholi o‘sishi.
- Xonadonlar.
- Ishlaydigan fuqarolar.
- Ishsizlar.
- Ta’lim qamrovi.
- Mamnunlik.

### Migratsiya paneli

- Qaysi shahardan ko‘chish.
- Qaysi shaharga ko‘chish.
- Ko‘chayotgan xonadonlar.
- Ko‘chish sabablari.
- Shaharlar bo‘yicha aholi o‘zgarishi.

Barcha ma’lumotlar real simulyatsiyadan olinsin.

---

# 20. PROFESSIONAL UI/UX

PROMPT 19B va PROMPT 21 dagi Design Systemni saqla.

Yangi Population Dashboard:

- Minimalistik kartalar.
- Aniq sarlavhalar.
- Chiroyli statistika grafiklari.
- Tushunarli ikonkalar.
- Bir xil spacing.
- Professional typography.
- O‘zbekcha yozuvlar.
- Responsive layout.

### Windows

- Mouse.
- Keyboard.
- Hover.
- Tooltips.
- Katta boshqaruv paneli.

### Android

- Touch.
- Scrollable panels.
- Katta tugmalar.
- Landscape layout.
- Moslashuvchan matn o‘lchami.

O‘zbekcha `Oʻ`, `Gʻ`, `O‘`, `G‘` belgilarining to‘g‘ri chiqishini tekshir.

---

# 21. POPULATION ANIMATIONS

Aholining rivojlanishi faqat grafik statistika bo‘lmasin.

Shaharda:

- Fuqarolar yuradi.
- Ishchilar ishga boradi.
- O‘quvchilar maktabga boradi.
- Savdogarlar bozorda faoliyat yuritadi.
- Xonadonlar yangi uyga ko‘chishi ko‘rinishi mumkin.
- Karvonlar va aravalar yo‘llardan yuradi.

Animatsiyalar mavjud sprite sheetlar asosida ishlasin.

Turli yosh guruhlari uchun mos vizual ko‘rinishlarni qo‘llab-quvvatla, ammo mavjud bo‘lmagan assetlarni tayyor deb ko‘rsatma.

Animatsiya holati fuqarolarning haqiqiy simulyatsiya holatiga mos bo‘lsin.

---

# 22. SERVER AUTHORITATIVE POPULATION

**MUHIM: WINDOWS VA ANDROID BIR XIL AHOLI MA’LUMOTLARINI KO‘RISHI KERAK.**

Server boshqarsin:

- Population totals.
- Households.
- Age groups.
- Jobs.
- Education progress.
- Migration.
- City ownership.
- Kingdom ownership.
- Social statistics.

Client boshqarsin:

- Fuqaro animatsiyalari.
- UI.
- NPC selection.
- Kamera.
- Vizual effektlar.

Server har bir fuqaroning har bir animatsiya kadrini yubormasin.

Buning o‘rniga kerakli simulation state va entity movement ma’lumotlarini yuborsin.

Client vizual harakatni o‘zida silliqlashtirsin.

---

# 23. MULTIPLAYER MIGRATION

Ikki o‘yinchi turli shaharlarda yashaydigan aholiga ega bo‘lishi mumkin.

Misol:

**WINDOWS PLAYER**

Asaka saltanatini boshqaradi.

**ANDROID PLAYER**

Boshqa saltanatni boshqaradi.

Agar ikki saltanat o‘rtasida ruxsat etilgan migratsiya mavjud bo‘lsa:

1. Server migratsiya talablarini tekshiradi.
2. Tegishli xonadon manba shahardan ajratiladi.
3. Yangi shaharda joylashish imkoniyati tekshiriladi.
4. Xonadon ko‘chish holatiga o‘tadi.
5. Migratsiya yakunlanadi.
6. Ikkala saltanat statistikalari yangilanadi.
7. Ikkala client yangi ma’lumotni oladi.

Bir fuqaro ikki shaharda bir vaqtda hisoblanmasin.

MigrationId va atomik server tranzaksiyalaridan foydalan.

---

# 24. POPULATION SAVE / LOAD

Quyidagi ma’lumotlarni saqla:

- CitizenIds.
- HouseholdIds.
- CityId.
- DistrictId.
- HomeBuildingId.
- AgeGroup.
- Occupation.
- Skills.
- EducationProgress.
- EmploymentStatus.
- Happiness.
- HouseholdFinances.
- MigrationState.
- KingdomOwnershipReference.

Save qayta ochilganda:

- Fuqarolar yo‘qolmasin.
- Xonadonlar buzilmasin.
- Ish joylari saqlansin.
- Aholi ikki marta hisoblanmasin.
- Migratsiya takroran bajarilmasin.
- Shaharlardagi aholi soni to‘g‘ri tiklansin.

Online versiyada PostgreSQLdagi authoritative state asosiy manba bo‘lsin.

---

# 25. PERFORMANCE REQUIREMENTS

Maqsadli Windows PC:

- Intel Core i5-12400.
- 8 GB RAM.
- Intel UHD 730.

Android uchun ham optimallashtir.

Katta aholiga ega shaharda barcha fuqarolarni batafsil NPC sifatida hisoblash shart emas.

### Hybrid Population Simulation

**VISIBLE CITIZENS**

- Boshqariladigan animatsiyalar.
- Yo‘l bo‘ylab yurish.
- Ish va bozor faoliyati.
- NPC dialog.

**STATISTICAL POPULATION**

- Xonadonlar soni.
- Ish o‘rinlari.
- Daromadlar.
- Ta’lim.
- Demografik o‘sish.
- Migratsiya.

### Talablar

- Population ticklar rendererga bog‘liq bo‘lmasin.
- 10 000+ aholi uchun statistik simulyatsiya imkoniyati bo‘lsin.
- Ko‘rinadigan NPClar LOD bilan boshqarilsin.
- Save fayli keraksiz ravishda kattalashmasin.
- Database so‘rovlari optimallashtirilsin.
- Client RAM sarfi nazorat qilinsin.

Dastlab kichik shahar testlarini to‘liq ishlat, keyin katta aholi bilan load test bajar.

---

# 26. C# ARCHITECTURE

Mavjud kodga mos modullarni qo‘sh.

```text
AndijonSaltanatlar/
│
├── Population/
│   ├── PopulationManager.cs
│   ├── PopulationGrowthSystem.cs
│   ├── PopulationStatistics.cs
│   ├── AgeingSystem.cs
│   └── PopulationSimulationScheduler.cs
│
├── Households/
│   ├── Household.cs
│   ├── HouseholdManager.cs
│   ├── HouseholdEconomy.cs
│   └── HouseholdFormationSystem.cs
│
├── Education/
│   ├── EducationManager.cs
│   ├── EducationServiceCoverage.cs
│   ├── StudentProgress.cs
│   └── SkillDevelopmentSystem.cs
│
├── Employment/
│   ├── JobManager.cs
│   ├── OccupationDefinition.cs
│   ├── EmploymentAllocator.cs
│   └── CareerProgression.cs
│
├── Migration/
│   ├── MigrationManager.cs
│   ├── MigrationRequest.cs
│   ├── MigrationValidator.cs
│   ├── MigrationState.cs
│   └── MigrationSimulation.cs
│
├── Society/
│   ├── CitizenHappinessSystem.cs
│   ├── SocialStabilityManager.cs
│   └── DistrictSocietyStatistics.cs
│
├── Advisor/
│   ├── PopulationAdvisor.cs
│   ├── MigrationAdvisor.cs
│   └── EmploymentAdvisor.cs
│
└── UI/
    ├── PopulationDashboard.cs
    ├── HouseholdPanel.cs
    ├── EducationPanel.cs
    ├── EmploymentPanel.cs
    └── MigrationPanel.cs
```

**Mavjud klasslarni takrorlama.**

Birinchi navbatda yangi modullarni ishlayotgan PopulationManager va EconomyManager bilan integratsiya qil.

---

# 27. FIRST WORKING VERTICAL SLICE

Avval ushbu zanjirni haqiqatan ishlaydigan qil:

**ASAKA SHAHRI → YANGI UY QURISH → UY-JOY SIG‘IMI OSHISHI → YANGI XONADON JOYLASHISHI → AHOLI STATISTIKASI YANGILANISHI → ISH JOYIGA BIRIKTIRISH → DAROMAD HISOBLASH → SAVE → CONTINUE**

Ikkinchi zanjir:

**ASAKA → SHAHRIXON → MIGRATSIYA IMKONIYATI → XONADON KO‘CHISHI → IKKALA SHAHARNING AHOLI SONI YANGILANISHI → SERVERGA SAQLASH**

Uchinchi zanjir:

**WINDOWS PLAYER → AHOLI PANELI → SHAHAR AHOLISI O‘ZGARISHI → SERVER EVENT → ANDROID PLAYER DUNYO XARITASIDA YANGILANISHNI KO‘RISHI**

Ushbu zanjirlar ishlamaguncha Population System tayyor deb hisoblama.

---

# 28. TESTING

Quyidagilarni tekshir:

### Population

- [ ] Aholi uy-joy sig‘imiga mos rivojlanadi.
- [ ] Xonadonlar yaratiladi.
- [ ] Yosh guruhlari mavjud.
- [ ] Ta’lim progressi ishlaydi.
- [ ] Voyaga yetgan fuqaro kasbga biriktiriladi.
- [ ] Ish o‘rinlari cheklovi ishlaydi.
- [ ] Xonadon daromadi hisoblanadi.
- [ ] Fuqarolar mamnunligi yangilanadi.

### Migration

- [ ] Shahardan shaharga ko‘chish mumkin.
- [ ] Manba shahar aholisi to‘g‘ri kamayadi.
- [ ] Manzil shahar aholisi to‘g‘ri oshadi.
- [ ] Ko‘chish talablarini server tekshiradi.
- [ ] Migration ikki marta bajarilmaydi.
- [ ] Xonadonlar yangi shaharga to‘g‘ri biriktiriladi.

### Multiplayer

- [ ] Windows va Android bitta dunyoga ulanadi.
- [ ] Population state sinxronlashadi.
- [ ] Hudud egasi o‘zgarsa aholi boshqaruvi yangilanadi.
- [ ] Reconnectdan keyin aholi tiklanadi.
- [ ] Bitta fuqaro ikki saltanatda hisoblanmaydi.

### UI & Animation

- [ ] Population Dashboard ishlaydi.
- [ ] Mahalla statistikasi ko‘rsatiladi.
- [ ] Migratsiya statistikasi ko‘rsatiladi.
- [ ] Donishmand Vazir maslahat beradi.
- [ ] NPC animatsiyalari ishlaydi.
- [ ] Windows UI responsive.
- [ ] Android UI responsive.
- [ ] O‘zbekcha matnlar to‘g‘ri.

### Build

- [ ] `dotnet build` muvaffaqiyatli.
- [ ] Unit testlar bajarilgan.
- [ ] Integration testlar bajarilgan.
- [ ] Windows release tekshirilgan.
- [ ] Android build tekshirilgan.

---

# 29. IMPLEMENTATION ORDER

1. Repository audit.
2. Population model integration.
3. Household system.
4. Age group system.
5. Education.
6. Employment.
7. Household economy.
8. Population growth.
9. Migration.
10. City-to-city population transfer.
11. Territory ownership integration.
12. NPC dialogues.
13. Donishmand Vazir integration.
14. Population Dashboard.
15. Animation improvements.
16. Multiplayer synchronization.
17. Save/Load.
18. Unit tests.
19. Integration tests.
20. Performance profiling.
21. Windows and Android build verification.

Har bir bosqichda real C# kod yoz.

Bajarilmagan funksiyani tayyor deb ko‘rsatma.

---

# 30. AGENT CONTINUITY

Quyidagi faylni yarat yoki yangila:

`docs/PROMPT_22_PROGRESS.md`

Unda:

- Bajarilgan modullar.
- O‘zgartirilgan fayllar.
- Aholi simulyatsiyasi holati.
- Oilalar va ish joylari.
- Migratsiya testlari.
- Multiplayer natijalari.
- UI va animatsiya holati.
- Build natijasi.
- Hali bajarilmagan vazifalar.

saqlansin.

**Faqat hujjat yozib to‘xtama. Haqiqiy ishlaydigan kod yarat.**

---

# 31. FINAL INSTRUCTION

**MAVJUD ANDIJON: SALTANATLAR LOYIHASIGA HAQIQIY YASHAYDIGAN JAMIYAT SIMULYATSIYASINI QO‘SH.**

Mening maqsadim:

Men Asaka shahrida yangi uylar quraman.

Aholi o‘sadi.

Oilalar yangi uyga joylashadi.

Bolalar ta’lim oladi.

Fuqarolar kasb egallaydi.

Ishlaydigan fuqarolar daromad oladi.

Yangi bozor va ustaxonalar iqtisodiyotni rivojlantiradi.

Men yangi shaharlarni ochaman.

Ayrim xonadonlar boshqa shaharlarga ko‘chadi.

Agar yangi hudud saltanatimga qo‘shilsa, uning aholisi ham o‘z uyi, oilasi va kasbi bilan yangi boshqaruv tizimiga biriktiriladi.

Donishmand Vazir esa aholi ehtiyojlari bo‘yicha maslahat beradi.

**HAMMASI WINDOWS VA ANDROIDDA BIR XIL ONLINE DUNYODA ISHLASHI KERAK.**

Avval uy-joy → xonadon → ish → iqtisodiyot zanjirini ishlat.

Keyin migratsiyani qo‘sh.

So‘ng multiplayer, UI va animatsiyalarni yakuniy tekshir.

Ish yakunida:

1. O‘zgartirilgan C# fayllar.
2. Population System holati.
3. Household System.
4. Ta’lim va kasb tizimi.
5. Migration System.
6. Multiplayer synchronization.
7. UI/UX natijalari.
8. Animatsiyalar.
9. Save/Load testlari.
10. Windows va Android build natijalari.
11. Hali bajarilmagan vazifalar.

haqida aniq hisobot ber.

**ANDIJON: SALTANATLAR — HAR BIR SHAHRI, HAR BIR MAHALLASI VA HAR BIR FUQAROSI RIVOJLANADIGAN TIRIK SALTANAT!**
