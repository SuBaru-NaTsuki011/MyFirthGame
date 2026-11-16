# PROMPT 21 — Repository Audit

Sana: 2026-11-15

## Modullar holati (haqiqiy kod bo'yicha)

| Modul | Holat |
|---|---|
| Windows MonoGame client | WORKING (build OK) |
| Android MonoGame client | PARTIALLY WORKING (csproj mavjud, APK qayta yig'ilmadi) |
| Shared Game Core | WORKING |
| SpriteBatch Renderer (WorldRenderer) | WORKING (viewport culling mavjud) |
| Texture Manager (AssetManager) | WORKING (cache, placeholder) |
| Animation System | PARTIALLY WORKING (fuqaro kadrlari mavjud, silliq interpolatsiya yo'q) |
| Citizen Simulation | WORKING |
| Farming | WORKING |
| Construction | WORKING |
| Road Network | WORKING |
| Caravan System | WORKING (server bilan bog'langan) |
| World Map | WORKING |
| Economy | WORKING |
| Multiplayer Client (OnlineClient) | WORKING (SignalR + idempotent buyruqlar) |
| ASP.NET Core Server | WORKING (build OK) |
| PostgreSQL | PARTIALLY WORKING (SQLite bilan testlangan) |
| SignalR | WORKING |
| Authentication | WORKING |
| Marketplace | WORKING |
| Alliances | WORKING (PROMPT 20) |
| Diplomacy | WORKING (PROMPT 20) |
| Territory Ownership | WORKING (PROMPT 19B/20) |
| Save/Load | WORKING |
| UI Framework | WORKING (UiKit, OnlinePanel) |
| Performance Overlay | ADDED (PROMPT 21, F3) |
| Reconnect | WORKING (WithAutomaticReconnect) |

## Asosiy topilmalar

1. Performance profiler mavjud emas edi — qo'shildi (`Core/PerformanceMonitor.cs`, F3).
2. Katta xaritada viewport culling allaqachon `WorldRenderer.Visible` orqali ishlaydi.
3. Texture cache `AssetManager._textures` orqali ishlaydi.
4. Camera silliq zoom va pan mavjud.
5. Android APK qayta yig'ish va real qurilmada test qilish kerak (bu muhitda amalga oshirilmadi).
6. High-latency (50/150/300ms) sinovlari amalga oshirilmadi.
7. FPS o'lchovlari real qurilmada olinmadi (profiler endi mavjud).

## Qilingan o'zgarishlar (PROMPT 21)

- `Core/PerformanceMonitor.cs` (yangi): FPS, frame/update/draw vaqti, visible sprites, active NPCs, memory, network latency overlay.
- `Game1.cs`: `_perf` field, F3 toggle, Update/Draw timing, overlay chizish.
- `docs/PROMPT_21_AUDIT.md` (yangi).
- `docs/PROMPT_21_PROGRESS.md` (yangi).
