# Andijon: Saltanatlar

Andijon / Asaka ruhidagi tarixiy-fantasy **2.5D izometrik city-builder** o'yin prototipi.
SimCity + Civilization + Clash of Clans janrlari aralashmasi. Texnologiya: **C# + MonoGame (DesktopGL)**.

Shahar bo'sh xaritadan emas, qal'a devori bilan o'ralgan, ko'cha-mahallalari, bozori, saroyi,
minoralari va navbatchi jangchilari bor **tayyor shahristondan** boshlanadi.

---

## 1. Avval nima o'rnatish kerak (MUHIM)

Bu kompyuterda hozircha **.NET SDK o'rnatilmagan**, shusiz o'yinni qura/ishga tushira olmaysiz.

1. **.NET 8 SDK** ni o'rnating (LTS, tavsiya etiladi):
   - Sayt: https://dotnet.microsoft.com/download/dotnet/8.0  → "SDK x64"
   - yoki terminalda:
     ```powershell
     winget install Microsoft.DotNet.SDK.8
     ```
2. Terminalni qayta oching va tekshiring:
   ```powershell
   dotnet --version
   ```
   `8.x.x` chiqsa — tayyor.

> MonoGame va boshqa kutubxonalar birinchi `dotnet build` paytida internetdan avtomatik yuklanadi.

---

## 2. Ishga tushirish

**Eng oson yo'l** — shu papkadagi skript:
```powershell
./run.ps1
```

**Yoki qo'lda:**
```powershell
dotnet run --project AndijonSaltanatlar/AndijonSaltanatlar.csproj
```

**VS Code'da:** papkani oching, C# kengaytmasini o'rnating, `F5` bosing (`Andijon: Saltanatlar` konfiguratsiyasi).

---

## 3. Boshqaruv

| Amal | Tugma |
|------|-------|
| Kamerani surish | `W A S D` / o'q tugmalar, yoki o'ng/o'rta tugmani bosib tortish |
| Zoom | sichqoncha g'ildiragi yoki `+` / `-` |
| Qurilish rejimi | pastdagi panel yoki `1` `2` `3` `4` |
| Bino qo'yish | chap tugma (devorni bosib turib sudrash mumkin) |
| Bekor qilish | o'ng tugma yoki `Esc` |
| Obyektni tanlash | ustiga chap tugma bilan bosish |
| To'xtatish (pauza) | `Space` |
| Donishmand Vazir paneli | yuqoridagi `VAZIR` tugmasi yoki `V` |
| Blueprint tahriri | panelda `KO'CHIRISH` / `O'CHIRISH` / `YO'L`, `UY+` / `BOZOR+` / `BOG'+`, so'ng xaritada joyni bosing |
| Tahrirni bekor qilish | `Esc` |
| Yordam | `H` |

---

## 4. Loyiha tuzilmasi

```
AndijonSaltanatlar/
├─ Program.cs                 # kirish nuqtasi
├─ Game1.cs                   # asosiy o'yin halqasi (barcha tizimlarni ulaydi)
├─ Core/
│  ├─ GameConfig.cs           # umumiy sozlamalar + LaunchOptions
│  ├─ IsoMath.cs              # izometrik koordinata matematikasi + IWorldObject
│  ├─ CameraManager.cs        # pan + silliq zoom
│  ├─ InputManager.cs         # klaviatura/sichqoncha holati
│  ├─ GameStateManager.cs     # rejim, resurslar, tanlov, xabarlar
│  ├─ AssetManager.cs         # sprite'lar (PNG bo'lsa — o'sha, bo'lmasa placeholder)
│  └─ Graphics/               # kod bilan chiziladigan sprite'lar
│     ├─ Canvas.cs            # CPU rasm chizgich
│     ├─ TileArt / BuildingArt / DecorArt / UnitArt / IsoCanvas
│     └─ PixelFont.cs         # TTF'siz 5x7 piksel shrift
├─ World/
│  ├─ MapManager.cs           # 64x64 izometrik tile-xarita
│  ├─ WorldRenderer.cs        # chuqurlik bo'yicha saralab chizish + piksel-aniq tanlash
│  ├─ CityGenerator.cs        # boshlang'ich shaharni quradi
│  └─ Decoration.cs           # daraxt, rasta, hovuz, tandir va h.k.
├─ Buildings/
│  ├─ Building.cs + BuildingManager.cs + ConstructionSystem.cs
│  └─ House / Market / Tower / Wall / Gate / MainHall / Garden / Barracks / Landmarks
├─ Units/
│  ├─ Unit.cs + UnitManager.cs
│  └─ GuardUnit (+Spearman) / PatrolUnit (+Swordsman)
└─ UI/
   ├─ Texts.cs                # barcha o'zbekcha matnlar
   └─ UIManager.cs            # sarlavha, resurs paneli, qurilish paneli, obyekt paneli
```

Qo'shimcha AI Governor qatlamlari: `AIGovernor/`, `CityPlanning/`, `Blueprints/`,
`SmartConstruction/`, `Forecasting/` va `UI/AIGovernorPanel.cs`. Server tomonda
`AndijonSaltanatlar.Shared/Planning/` va `AndijonSaltanatlar.Server/Services/AiPlanValidationService.cs`.

---

## 5. Hozir nima tayyor

- ✅ 64×64 izometrik tile-xarita (yer, ko'cha, maydon, yashillik, suv/ariq)
- ✅ Kamera: pan + kursorga qarab silliq zoom
- ✅ Devor bilan o'ralgan boshlang'ich shahar: 4 darvoza, burj minoralari, saroy, masjid,
  madrasa, karvonsaroy, bozor, qo'shinxona, bog'lar, ~26 ta uy
- ✅ Shahar muhiti: chinor/terak/o'rik/archa, hovuz, quduq, tandir, so'ri, rasta, bayroq, mash'ala
- ✅ 16+ jangchi: nayzali/qalqonli soqchilar (post), qilichli va patrul askarlari (yurish/aylanish)
- ✅ Qurilish tizimi: build-mode, ghost preview, yashil/qizil joylashuv, narx tekshiruvi
- ✅ O'zbekcha UI: nom, resurslar (tanga/oziq-ovqat/aholi), qurilish paneli, obyekt paneli, yordam
- ✅ **Donishmand Vazir (AI Governor)**: shahar tahlili, tavsiyalar, sakkiz xil mahalla rejasi
  (dehqonchilik va "mavjudni yaxshilash" bilan), blueprint preview va muharriri (ko'chirish,
  o'chirish, bino qo'shish, yo'l tahriri), tanlangan bino ma'lumoti, tasdiqlashdan keyingi
  qurilish navbati, iqtisodiy prognoz, **qurilishdan keyingi haqiqiy natija hisoboti**,
  `YERNI KO'RSAT` bilan kamerani tavsiya etilgan uchastkaga siljitish, reja tarixi va to'liq
  save/load — batafsil: `docs/AI_GOVERNOR_PROGRESS.md`
- ✅ AI reja validatsiyasi serverda ham mavjud: `POST /api/game/validate-plan` (egalik, narx,
  texnologiya, bandlik va budjetni tekshiradi; client narxiga ishonmaydi)
- ✅ Hamma sprite kod bilan chizilgan — **hech qanday asset fayl shart emas**

## 6. Keyin rivojlantirish mumkin

- Haqiqiy sprite'lar: `AndijonSaltanatlar/Content/Sprites/` ga `<kalit>.png` tashlansa,
  kod o'zgarmasdan o'sha rasm ishlatiladi (kalitlar — `Core/AssetManager.cs` dagi `SpriteKeys`).
- Yo'l topish (pathfinding) — `MapManager.IsWalkable` tayyor.
- Iqtisod chuqurlashtirish, texnologiyalar daraxti, dushman hujumi, saqlash/yuklash.

---

🤖 [Claude Code](https://claude.com/claude-code) yordamida yaratildi.
