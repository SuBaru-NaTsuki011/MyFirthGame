# RELEASE CHECKLIST — QUALITY GATES (PROMPT 14)

| Gate | Nomi | Holat | Dalil |
|------|------|-------|-------|
| 1 | BUILD — C# kompilyatsiyasi | **PASSED** | `dotnet build -c Release`: 0 xato, 0 ogohlantirish (4 loyiha) |
| 2 | CONTENT — asosiy assetlar yuklanadi | **PASSED** | sprite'lar procedural (Canvas), Content/story.json nusxalangan |
| 3 | STARTUP — crashsiz ochiladi | **PASSED** | smoke: startup PASS; screenshot render qilinadi |
| 4 | GAMEPLAY — qurilish va yer ochish | **PASSED** | smoke: uy qurish, yer ochish, yangi yerда qurish PASS |
| 5 | SIMULATION — aholi va iqtisodiyot | **PASSED** | smoke: 40 kun tick, resurslar manfiy emas |
| 6 | ADVISOR — real maslahat | **PASSED** | smoke: Advisor.Messages > 0 |
| 7 | SAVE — holat tiklanadi | **PASSED** | smoke: tanga/kun/yer/binolar tiklandi |
| 8 | PERFORMANCE — qotib qolmaydi | **PARTIAL** | 40 kun tick darhol, hang yo'q; interaktiv FPS o'lchanmagan |
| 9 | PACKAGE — Windows release fayllari | **PASSED** | `release/windows/`: exe + Content + native dll + README + LICENSES |
| 10 | CLEAN START — alohida papkadan ishga tushish | **PASSED** | published `.exe` release papkadan smoke 18/18 PASS |

## Publish buyrug'i
```
dotnet publish AndijonSaltanatlar/AndijonSaltanatlar.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o ./release/windows
```

## Release papka tarkibi
- `AndijonSaltanatlar.exe` (launcher)
- `.NET 8 runtime` (self-contained, ~190 fayl)
- `SDL2.dll`, `openal.dll` (DesktopGL native)
- `Content/story.json` (data-driven quest kontenti)
- `README.txt`, `LICENSES.txt`
- Umumiy hajm: ~77 MB
