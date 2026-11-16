# DONISHMAND VAZIR — AI GOVERNOR PROGRESS (PROMPT 15)

Sana: 2026-11-15 · Build: Debug/Release · .NET 8 · MonoGame DesktopGL/DX
Holat: **VERTICAL SLICE TAYYOR** — tahlil → reja → preview → tahrir → tasdiq → navbat → qurilish →
iqtisod → tarix → save/load. Server tomonda reja validatsiyasi ham ishlaydi.

## 1. Nima ishlaydi

- **Shahar tahlili**: aholi, uy-joy sig'imi/tanqisligi, xazina, oziq-ovqat, baxt, sog'liq, ta'lim,
  xavfsizlik, ishchilar, kunlik oqim, binolar/yo'llar, bo'sh kataklar, yer uchastkalari va umumiy
  salomatlik indeksi (0–100).
- **Tavsiyalar**: Utility AI ustuvorlik ballari bilan 3–5 tavsiya; har biri amal tugmasi bilan
  (turar joy, savdo, hunarmandchilik, madaniyat, dehqonchilik, bog', muvozanatli mahalla, yer
  ochish, soliq).
- **Yangi mahalla rejasi**: sakkiz variant — turar joy, savdo, hunarmandchilik, madaniyat,
  dehqonchilik, bog'/dam, muvozanatli va **mavjud mahallani yaxshilash**. Reja haqiqiy xarita,
  xazina va texnologiyalar asosida tuziladi.
- **Mavjudni yaxshilash**: yangi chekka hudud ochish o'rniga mavjud ko'cha va binolar yaqinidagi
  bo'shliqlar (`FindInfillAnchors`) topilib, kichik va tejamkor reja tuziladi.
- **Blueprint preview**: vaqtinchalik; tasdiqlanmaguncha `GameState`, xazina, yer yoki binolar
  o'zgarmaydi.
- **Blueprint muharriri**: element tanlash, ko'chirish, o'chirish, **yangi bino qo'shish** (uy,
  bozor, bog'), yo'l katagini qo'shish/olib tashlash, qayta tekshirish, bekor qilish. Noto'g'ri
  tahrir (to'qnashuv, yo'l uzilishi, yopiq yer) avtomatik qaytariladi.
- **Tanlangan element ma'lumoti**: xaritadan yoki ro'yxatdan tanlanganda nomi, narxi, maydoni
  (NxN), qurilish kunlari, foydasi, texnologiya/yo'l talabi va blok sababi ko'rsatiladi.
- **Tasdiqlash va navbat**: har bir reja foydalanuvchi tugmasi bilan tasdiqlanadi; qurilish
  navbati avval yo'llarni, keyin binolarni bajaradi. Pauza, davom, bekor qilish, bir qadam,
  budjet yetmasa qayta tasdiqlash mavjud.
- **Iqtisodiy prognoz**: Odatiy / Optimistik / Salbiy stsenariylar, kunlik sof oqim, qoplanish
  muddati va aholi sig'imi o'zgarishi.
- **Rivojlanish hisoboti**: shahar holati, navbat holati, keyingi tavsiya, yer bo'yicha maslahat,
  iqtisodiy prognoz (kafolat emas) va **yakunlangan/bekor qilingan rejalar tarixi**. Har bir
  yakunlangan reja uchun natija **prognoz emas, haqiqiy o'lchov**: tasdiqlash paytidagi uy-joy
  sig'imi va aholi soni qurilishdan keyingi qiymatlar bilan solishtiriladi (`DevelopmentImpact`).
- **Yerni ko'rsatish**: tahlil va tavsiyalardagi `YERNI KO'RSAT` tugmasi kamerani tavsiya etilgan
  uchastkaga siljitadi, uchastka tanlanadi va narxi/maydoni/aholi talabi tushuntiriladi. Bu tugma
  yerni **sotib olmaydi** — ochish uchun foydalanuvchi `YERNI OCHISH`ni o'zi bosadi.
- **O'zbekcha buyruq parseri**: "tahlil qil", "uy-joy mahallasi qur", "bozor qur", "dehqonchilik
  rejasi", "mavjudni yaxshila", "aholini ko'paytir", "xazina holati", "navbatni to'xtat",
  "davom et", "bekor qil" kabi qisqa buyruqlar.

## 2. Arxitektura va fayllar

| Qatlam | Fayl | Vazifa |
|---|---|---|
| Grid adapter | `CityPlanning/PlanningGrid.cs` | Read-only `ICityPlanningGrid` + `MapPlanningGrid`; testlarda fake grid |
| Joylashuv | `SmartBuildingPlacement.cs`, `PlacementCandidate.cs` | Anchor atrofida mos origin, ballash, to'qnashuv/muhofaza tekshiruvi, infill anchorlar |
| Yo'llar | `SmartRoadPlanner.cs` | Anchor → mavjud ko'cha trunk yo'li; har bir bino uchun ulanish; A*/uniform-cost |
| Validatsiya | `PlanValidator.cs` | Yer, texnologiya, geometriya, muhofaza, rejalashtirilgan yo'l bilan to'qnashuv, yo'l ulanishi, budjet/resurs holati, Draft/ReadyForApproval |
| Smeta | `PlanCostEstimator.cs`, `CityPlan.cs` | Katalog narxlari, yo'l narxi, jami/missing, kunlar, bino foydasi |
| Blueprint | `BlueprintManager.cs`, `BlueprintEditor.cs`, `BlueprintRenderer.cs` | Preview, tanlash, ko'chirish/o'chirish/qo'shish/yo'l tahriri, rangli render, save restore |
| Tahlil | `AIGovernor/CityAnalysisEngine.cs`, `CityAnalysisReport.cs` | GameState'dan o'qish-only hisobot |
| Tavsiya | `AIGovernor/RecommendationEngine.cs` | Utility scoring va amal takliflari |
| Ruxsatlar | `AIGovernor/AdvisorPermissions.cs` | Manual / Planning / ApprovedExecution |
| Prognoz | `Forecasting/EconomicForecastService.cs` | 3 stsenariy, payback |
| Natija | `Forecasting/DevelopmentImpactEvaluator.cs` | Tasdiqlash paytidagi baza vs qurilishdan keyingi haqiqiy sig'im/aholi, bino/yo'l soni |
| Navbat | `SmartConstruction/ConstructionQueueManager.cs` | Idempotent qadamlar, budjet, pauza/davom/bekor, reapproval, tasdiqlash snapshoti |
| Facade | `AIGovernor/AIGovernorManager.cs` | Tahlil, reja, preview cache, tasdiq, tick, blueprint/history save DTO, parser |
| UI | `UI/AIGovernorPanel.cs` | 5 bo'limli modal panel, xarita tahrir rejimlari, bino qo'shish, tanlangan element paneli |
| Integratsiya | `Game1.cs`, `UI/UIManager.cs`, `SaveSystem/SaveGameData.cs` | VAZIR tugmasi, `V`, kunlik tick, blueprint pick, render, save/load, yo'l saqlash |
| Server qoidalari | `AndijonSaltanatlar.Shared/Planning/AiPlanRules.cs`, `AiPlanDtos.cs` | Server-authoritative narx/o'lcham/texnologiya va reja tekshiruvi |
| Server endpoint | `AndijonSaltanatlar.Server/Services/AiPlanValidationService.cs`, `Controllers/GameController.cs` | `POST /api/game/validate-plan` — egalik, resurs, texnologiya, bandlik |

## 3. Rejimlar va xavfsizlik qoidalari

1. **Faqat maslahat (Manual)** — tahlil va tavsiyalar ishlaydi, reja tuzilmaydi, navbat bajarilmaydi.
2. **Shahar me'mori (Planning)** — reja/blueprint tuziladi va tahrirlanadi, lekin tasdiqsiz hech
   narsa qurilmaydi.
3. **Tasdiqlangan reja ijrochisi (ApprovedExecution)** — faqat foydalanuvchi tasdiqlagan reja
   navbatga tushadi va bajariladi.

Qat'iy qoidalar:

- AI **hech qachon** o'zicha tanga sarflamaydi, yer sotib olmaydi yoki bino qurmaydi.
- Har bir reja `TASDIQLASH` tugmasi bilan tasdiqlanadi; tasdiqdan oldin xazina o'zgarmaydi.
- Yer faqat foydalanuvchi "YERNI OCHISH" tugmasini bosganda ochiladi; AI faqat uchastkani
  `YERNI KO'RSAT` bilan ko'rsatadi va kamerani siljitadi, hech qachon sotib olmaydi.
- Blueprint — vaqtinchalik preview; `GameState` faqat navbat ijrosida o'zgaradi. Blueprint
  tahriri `GameState`ga emas, faqat preview nusxasiga yoziladi.
- Muhofaza zonasi: markaziy `Plaza` va MainHall/Mosque/Madrasa/Gate/Wall/Tower atrofidagi 1 katak
  chetlab o'tiladi.
- Reja ichida bino-bino to'qnashuvi yo'q; bino rejalashtirilgan yo'l katagiga tusha olmaydi;
  yo'llar mavjud ko'cha tarmog'iga ulanishi shart.
- Har bir qadam idempotent: bajarilgan qadam qayta pul yechmaydi yoki binoni ikki marta qo'ymaydi.
- Qulflangan va boshqa saltanat yerlari rejalashtirishda ochiq hisoblanmaydi.

## 4. Ranglar va ko'rinish

- **Yashil** — qurish mumkin.
- **Sariq** — resurs/budjet hozircha yetmaydi (`NeedsResource`), reja saqlanadi.
- **Qizil** — bloklangan (yer, texnologiya, to'qnashuv, yo'l yo'q).
- **Ko'k** — rejalashtirilgan yo'l.
- **Oltin** — tanlangan reja elementi / tasdiqlash harakati.

Panelning beshta bo'limi: `TAHLIL`, `TAVSIYALAR`, `YANGI REJA`, `NAVBAT`, `HISOBOT`.
Panel `VAZIR` tugmasi yoki `V` bilan ochiladi/yopiladi. Xarita tahrir rejimida `Esc` bekor qiladi,
`V` panelga qaytaradi. Reja bo'limida tanlangan bino detail paneli, `UY +` / `BOZOR +` / `BOG' +`
qo'shish tugmalari va `KO'CHIRISH` / `O'CHIRISH` / `YO'L` / `QAYTA HISOBLASH` / `BEKOR QILISH`
harakatlari mavjud.

## 5. Server tomonda reja tekshiruvi

- `POST /api/game/validate-plan` (`[Authorize]`) — JWT egasi saltanat egasi ekani tekshiriladi.
- So'rov: `CommandId`, `KingdomId`, `ApprovedBudget`, `Steps` (bino turi va X/Y; yo'l uchun
  `Kind="Road"`).
- Server **client yuborgan narx va o'lchamga ishonmaydi**: narx, bino o'lchami va texnologiya
  talabi `AiPlanRules` jadvalidan olinadi.
- Tekshiriladi: egalik, xarita chegarasi, texnologiya, mavjud binolar bilan to'qnashuv, reja
  ichidagi takror/ustma-ust joylashuv, yo'l kataklari, budjet va xazina.
- Natija: `Accepted`, `Affordable`, `ComputedCost`, `ValidBuildingSteps`, `ValidRoadSteps`,
  `ErrorsUz`, `WarningsUz`; xazina yetmasa reja qabul qilinadi, lekin ogohlantirish bilan.
- Endpoint hech narsa qurmaydi va tanga yechmaydi — faqat validatsiya. `BuildingService` ham
  shu shared narx/o'lcham/texnologiya qoidalaridan foydalanadi.
- Cheklov: DB sxemasida faqat shahar darajasidagi `LandParcelEntity` bor, shuning uchun tile
  darajasidagi yer egaligi hozircha controller egalik tekshiruvi bilan almashtiriladi.

## 6. Save / Load

`SaveGameData.Governor` (`AIGovernorSaveState`) saqlaydi:

- rejim darajasi (`AutonomyLevel`) va tasdiqlangan umumiy budjet,
- tasdiqlangan navbat: nom, holat, budjet, sarflangan, `NeedsReapproval`, `PausedForResources`,
  tasdiqlash paytidagi uy-joy sig'imi (`HousingCapacityAtApproval`) va aholi soni
  (`PopulationAtApproval`),
- qadamlar: tartib, tur (road/building), bino turi, `X`/`Y`, o'lcham, narx, holat, izoh,
- **blueprint**: nom, tur, sabab, anchor, smeta, holat (`Draft`/`ReadyForApproval`/`Approved`),
  tanlangan element, barcha binolar (joy, o'lcham, narx, foyda, talab, blok sababi) va
  rejalashtirilgan yo'llar,
- **reja tarixi**: yakunlangan/bekor qilingan rejalar, qadamlar, sarf, budjet, kun va haqiqiy
  natija (`HousingBefore/After`, `PopulationBefore/After`, qurilgan bino/yo'l soni).

`SaveGameData.Roads` o'yinchi qurgan/rejalashtirilgan yo'l kataklarini saqlaydi; yuklashda ular
qayta tiklanadi, shuning uchun qurilgan yo'llar save/load'dan keyin yo'qolmaydi. `Point` o'rniga
aniq `X`/`Y` DTO maydonlari ishlatiladi (`GovernorStepSave`, `GovernorQueueSave`,
`GovernorBlueprintSave`, `GovernorBlueprintItemSave`, `GovernorBlueprintRoadSave`,
`GovernorHistorySave`, `SavedRoad`).

## 7. Performance

- Tahlil har kadrda emas: panel ochilganda, har 3 o'yin kunida (`OnDayTick`) yoki reja
  **yakunlanganda bir marta** (`StepQueueOnce` → `Analyze()`).
- Reja faqat foydalanuvchi so'raganda hisoblanadi; blueprint tahriri faqat amal bajarilganda
  qayta validatsiya qilinadi.
- Joylashuv qidiruvi radiusi va nomzodlar soni cheklangan (18 radius, 220 nomzod).
- Yo'l qidiruvi `maxExpansions` bilan cheklangan.
- 64×64 xarita, 10–30 reja binosi uchun mo'ljallangan; UI ro'yxati 8 bino / 12 qadam ko'rsatadi.
- Infill qidiruvi butun xaritani bir marta skanerlaydi va faqat reja so'ralganda ishlaydi.

## 8. Tekshiruv natijalari

- `dotnet build AndijonSaltanatlar.sln -v q --nologo` → **0 warning, 0 error**.
- `dotnet test AndijonSaltanatlar.Tests -c Release --nologo` → **57 passed, 0 failed**.
- `dotnet run --project AndijonSaltanatlar\AndijonSaltanatlar.csproj -c Debug -- --smoke <fayl>` →
  **48 PASS / 0 FAIL**.

Smoke zanjiri: tahlil → tavsiya → 3 uy + bozor + bog' + 13 yo'l rejasi → preview o'zgarmasligi →
smeta/prognoz → tanlangan bino ma'lumoti → blueprint save/load (5 bino, 13 yo'l) → muharrir →
Manual rejim bloki → parser → tasdiq (18 qadam, 570 tanga) → tasdiqdan oldin xazina sarflanmasligi
→ save/load (18 pending) → kunlik tick → yakun → tarixga yozilish → **yakunlangach darhol qayta
tahlil** → **hisobot haqiqiy sig'im/aholi natijasi** → tarix va natija save/load → 3 uy,
bozor, bog' qurildi → 13 yo'l qo'shildi → sig'im 178→193 → budjet 570 ichida → yer sotib
olinmadi → yo'llar reload'dan keyin saqlanadi → iqtisod barqaror.

Testlar: joylashuv, yo'l ulanishi, validatsiya, muharrir (ko'chirish/o'chirish/**qo'shish**),
smeta, prognoz, ruxsatlar, blueprint save roundtrip, **rivojlanish natijasi (delta, bino/yo'l
soni, bazasiz holat)**, **natija va snapshotlarning save roundtrip'i**, dehqonchilik maqsadi,
infill anchorlar, reja tarixi va server reja qoidalari (egalik, texnologiya, to'qnashuv, budjet,
xazina).

## 9. Bu bosqichda tuzatilgan xatolar

- **Anchor mavjud yo'lda bo'lsa**, bino ulanish qidiruvi faqat yangi rejalashtirilgan kataklarni maqsad
  qilardi va mavjud ko'chani ko'rmasdi. Endi maqsad: mavjud yo'l **yoki** rejalashtirilgan katak.
- **BluePrint bino bilan rejalashtirilgan yo'l ustma-ust tushishi** mumkin edi; validator endi
  rejalashtirilgan yo'l kataklarini ham band deb hisoblaydi.
- **Qurilgan yo'llar save/load'dan keyin yo'qolardi**; `SavedRoad` qo'shildi va yuklashda tiklanadi.
- **Blueprint va reja tarixi save'da yo'q edi**; DTO'lar qo'shildi, `Restore` va `ImportBlueprint`
  bilan qaytariladi.
- **Reja holati `Approve`dan keyin o'zgarmasdi**; `CityPlan.State` endi `Draft → ReadyForApproval
  → Approved` ketma-ketligini saqlaydi.
- UI'dagi matnlar va tugmalar bir-biriga tegib ketardi: sarlavha/IJRO holati, reja tavsifi, ro'yxat
  sarlavhasi, pastki tugmalar va footer. Joylashuv qayta hisoblandi.
- `PixelFont` qo'llab-quvvatlamaydigan `•`, `✕`, `✓`, `→`, `·` belgilari bo'sh joy bo'lib chizilardi;
  ular ASCII (`-`, `X`, `+`, ` / `) bilan almashtirildi.
- Reja tavsifi ikki marta ma'lumot berardi va 4 qatordan oshib kesilardi; tavsif qisqartirildi va
  tabiiy tugashi ta'minlandi.
- **Navbat yakunlanganda tahlil eski holatda qolardi** va hisobot faqat prognozni ko'rsatardi.
  Endi yakunlanish bir marta `Analyze()` chaqiradi; tarix va hisobot `DevelopmentImpactEvaluator`
  orqali haqiqiy sig'im/aholi o'zgarishini ko'rsatadi.
- **`YERNI KO'RSAT` faqat ro'yxatdan tanlardi**, kamera siljimasdi. Endi `FocusLandRequested`
  callback orqali kamera uchastka markaziga `Jump` qiladi; narx, maydon va aholi talabi xabar
  qilinadi, lekin yer avtomatik ochilmaydi.

## 10. Ma'lum cheklovlar

- Zich markaziy Asakada ayrim nomzodlar yo'l ulanishi topilmay **qizil** bo'lishi mumkin. Bunday
  elementni ko'chirish/o'chirish yoki boshqa mahalla turini tanlash kerak; reja baribir kamida bitta
  quriladigan bino bilan yaroqli hisoblanadi.
- Server validatsiyasi endpointi tayyor va testlangan, lekin MonoGame klientda tarmoq qatlami hali
  yo'q — hozircha reja lokal `PlanValidator` bilan tekshiriladi.
- Server tile darajasidagi yer egaligini tekshira olmaydi (DB'da faqat shahar darajasidagi uchastka).
- Prognoz — taxmin, kafolat emas; real iqtisod kunlik tick bilan farq qilishi mumkin.
- Yangi uy qurilganda uy-joy **sig'imi** darhol oshadi, aholi soni esa fuqarolar ko'chib kelishi
  bilan vaqt o'tib o'sadi; shuning uchun hisobot ikkala ko'rsatkichni alohida beradi.
- UI bir vaqtda 8 bino va 12 navbat qadamini ko'rsatadi; qolganlari hisobotda umumlashtiriladi.
- Interaktiv FPS va katta xarita (64×64 dan yuqori) yuklamasi alohida profillanmagan.

## 11. Qayta tekshirish buyruqlari

```powershell
dotnet build AndijonSaltanatlar.sln -v q --nologo
dotnet test AndijonSaltanatlar.Tests -c Release --nologo
dotnet run --project AndijonSaltanatlar\AndijonSaltanatlar.csproj -c Debug -- --smoke C:\Users\user\Desktop\Andijon\smoke_ai.txt
dotnet run --project AndijonSaltanatlar\AndijonSaltanatlar.csproj -c Debug -- --governor --screenshot governor_analysis.png
dotnet run --project AndijonSaltanatlar\AndijonSaltanatlar.csproj -c Debug -- --plan Mixed --screenshot governor_plan.png
```
