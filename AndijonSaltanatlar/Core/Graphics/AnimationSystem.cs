using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.Core.Graphics
{
    // ------------------------------------------------------------------
    // PROMPT 16 — Animatsiya tizimi (Transaction 11, 13, 15)
    //
    // Maqsad: barcha harakatlanuvchi personajlar uchun frame-based animatsiya
    // tizimini joriy qilish. Hozirgi kunda Unit.Update ichida oddiy 2 kadrli
    // (Idle / Walk) animatsiya mavjud — u to'g'ridan-to'g'ri Frames massividan
    // foydalanadi. Yangi tizim buni umumlashtiradi, lekin boshlang'ich bosqichda
    // uni faqat "silliqroq" variantiga o'zgartirishga harakat qilamiz.
    //
    // QISQA KO'RSATMA:
    // • AnimationClip — bitta animatsiya (masalan, "yurish"). Frame'lar massivi,
    //   cheklovlar va boshqa elementlar bilan birgalikda.
    // • AnimationController — har bir entity uchun animatsiyani boshqaruvchi
    //   komponent. State machine asosida ishlaydi: animatsiya joylashtiriladi,
    //   ozod qilinadi, lekin hech qachon assetlarni "yangi Texture2D" sifatida
    //   yaratmaydi — ular Content/ dagi PNG'lar yoki placeholder'lar.
    //
    // Boshlash uchun quyidagi animatsiyalarni qo'shamiz:
    //   • CitizenWalkAnimation — fuqarolar uchun yurish (3 kadrli placeholder bilan)
    //   • CitizenIdleAnimation — dam olish (1 kadr — placeholder)
    //   • GuardIdleAnimation — jangchi dam olish (1 kadr)
    //   • GuardWalkAnimation — jangchi yurish (3 kadrli placeholder)
    //
    // KEYINCHalik: haqiqiy 8 yo'nalishli sprite sheet'lar tayyorlanganda,
    // DirectionalAnimationSet yordamida har bir yo'nalish uchun alohida clip
    // yuklanadi.
    //
    // Haqiqiy Texture2D yaratish bu yerda amalga oshirilmaydi: barcha sprite'lar
    // Content/Sprites/ dagi PNG'lar bilan almashtirilishi mumkin yoki placeholder
    // Canvas orqali yaratiladi (AssetManager yordamida).
    // ------------------------------------------------------------------

    /// <summary>
    /// Animatsiya chegarasi — bitta "kadr" (Texture2D yoki "placeholder Canvas").
    /// Hozircha Texture2D referenciasidan foydalanamiz; kelajakda atlas bilan
    /// ishlash imkoniyati qo'shilishi mumkin.
    /// </summary>
    public sealed class AnimationClip
    {
        /// <summary>Animatsiya identifikatori (masalan, "citizen_walk").</summary>
        public string ClipId { get; }

        /// <summary>Kadrlar ro'yxati (Texture2D). Agar bo'sh bo'lsa, cheksiz takrorlanadi.</summary>
        public Texture2D[] Frames { get; }

        /// <summary>Mikro sekundlarda — har bir kadr qancha vaqt ko'rsatiladi.</summary>
        public int FrameDurationMicroseconds { get; }

        /// <summary>Chegaralar: dastlabki kadr indeksi (0-based).</summary>
        public int StartFrame { get; }

        /// <summary>Chegaralar: oxirgi kadr indeksi (Frames.length − 1).</summary>
        public int EndFrame { get; }

        /// <summary>Cheksiz siklga o'rash mumkinmi.</summary>
        public bool Loop { get; }

        /// <summary>Yuqori cheklov: frames o'zgarganda cheklovlar (flip va h.k.) qo'llaniladi.</summary>
        public bool SupportsDirectionalFlip { get; }

        public AnimationClip(string clipId, Texture2D[] frames, int frameDurationMs,
                             int startFrame = 0, bool loop = true, bool supportsFlip = true)
        {
            ClipId = clipId;
            Frames = frames;
            FrameDurationMicroseconds = frameDurationMs * 1000;
            StartFrame = startFrame;
            EndFrame = frames.Length - 1;
            Loop = loop;
            SupportsDirectionalFlip = supportsFlip;
        }

        /// <summary>
        /// Berilgan vaqtda qaysi kadr ko'rsatilishi kerakligini hisoblaydi.
        /// Agar Frames bo'sh bo'lsa, null qaytaradi.
        /// </summary>
        public (int frameIndex, bool flip) Sample(TimeSpan elapsed, bool flip = false)
        {
            if (Frames.Length == 0) return (0, flip);

            long us = elapsed.Ticks / (TimeSpan.TicksPerMillisecond / 1000);
            if (FrameDurationMicroseconds <= 0)
                return (0, flip);

            long seq = us / FrameDurationMicroseconds;
            int idx;
            if (Loop)
            {
                int len = EndFrame - StartFrame + 1;
                if (len > 0)
                    idx = StartFrame + (int)(seq % len);
                else
                    idx = StartFrame;
            }
            else
            {
                idx = Math.Min(StartFrame + (int)seq, EndFrame);
            }
            return (idx, flip);
        }
    }

    // ------------------------------------------------------------------
    // Funksional animatsiya controller — har bir unit/ob'ekt uchun
    // bir nechta animatsiyalarni muvozanatlashtiradi. Hozircha turli
    // ob'ektlarda bir xil animatsiya ishlatiladi (keyinchalik LOD bilan).
    // ------------------------------------------------------------------
    public sealed class AnimationController
    {
        private readonly Dictionary<string, AnimationClip> _clips = new();
        private AnimationClip _currentClip;
        private string _currentClipId;
        private TimeSpan _elapsed;
        private bool _flip;
        private bool _loop;

        public string FacingMode { get; private set; } = "none";
        public bool IsPlaying { get; private set; }
        public bool IsLooping => _loop;
        public int CurrentFrameIndex { get; private set; }
        public bool Flip => _flip;

        public AnimationController()
        {
        }

        /// <summary>
        /// Clip qo'shish (agar mavjud bo'lsa). ClipId — "citizen_walk", "guard_idle" k.m.
        /// </summary>
        public void RegisterClip(AnimationClip clip)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            _clips[clip.ClipId] = clip;
        }

        /// <summary>
        /// Animatsiyani boshlash — clipId orqali.
        /// Agar clip topilmasa, hozirgi clip saqlanib qoladi (xato bermaydi).
        /// </summary>
        public void Play(string clipId, bool flip = false, bool loop = true)
        {
            if (_clips.TryGetValue(clipId, out var clip))
            {
                _currentClip = clip;
                _currentClipId = clipId;
                _elapsed = TimeSpan.Zero;
                _flip = flip;
                _loop = loop;
                IsPlaying = true;
                CurrentFrameIndex = clip.StartFrame;
            }
            else
            {
                // Clip yo'q — xato bermaydi, hozirgi holatga qolish
                IsPlaying = false;
            }
        }

        /// <summary>
        /// Animatsiyani to'xtatish (dolayli). Keyincha play qilinmasa,
        /// oxirgi kadr qoladi.
        /// </summary>
        public void Stop()
        {
            IsPlaying = false;
            _elapsed = TimeSpan.Zero;
        }

        /// <summary>
        /// Animatsiya vaqtini yangilash. Delta — mikro sekund.
        /// Agar clip yo'q yoki o'ynapmasa, hech narsa o'zgarmaydi.
        /// </summary>
        public void Update(long deltaMicroseconds)
        {
            if (!IsPlaying || _currentClip == null || _currentClip.Frames.Length == 0) return;

            _elapsed += TimeSpan.FromTicks(deltaMicroseconds * TimeSpan.TicksPerMillisecond / 1000);

            var (idx, _) = _currentClip.Sample(_elapsed, _flip);
            CurrentFrameIndex = idx;

            // Agar oxirgi kadrga yetkansa va loop bo'lmasa — to'xtat
            if (!_loop && idx >= _currentClip.EndFrame)
            {
                IsPlaying = false;
            }
        }

        /// <summary>
        /// Hozirgi kadrni qaytaradi. Agar animatsiya o'ynapmasa yoki clip yo'q bo'lsa —
        /// null qaytaradi.
        /// </summary>
        public Texture2D GetCurrentFrame()
        {
            if (!IsPlaying || _currentClip == null || CurrentFrameIndex < 0 || CurrentFrameIndex >= _currentClip.Frames.Length)
                return null;

            return _currentClip.Frames[CurrentFrameIndex];
        }

        /// <summary>
        /// Film radd ni qaytaradi. Agar clip yo'q vai frames bo'sh —
        /// default flip qiymati qaytariladi (false).
        /// </summary>
        public bool GetFlip() => _flip;
    }

    // ------------------------------------------------------------------
    // Placeholder sprite'lar (3 kadr): CitizenWalk, CitizenIdle, GuardWalk, GuardIdle.
    // Bular AssetManager'ning LoadAll() dan oldin ishlatiladi; keyingi yuklamalar
    // undan oldin placeholder'lar yaratiladi.
    //
    // NOTE: Ushbu placeholder'lar aslida haqiqiy assetlarga o'rnatiladi;
    // Ammo dastlabki ishlov berish uchun placeholder'lar bilan ishlaydi.
    // ------------------------------------------------------------------

    /// <summary>
    /// Fuqaro yurish animatsiyasi: 3 ta placeholder kadr (Canvas → Texture2D).
    /// </summary>
    public static class CitizenWalkAnimation
    {
        public static AnimationClip Create(AssetManager assets, int citizenKind = 0)
        {
            var frames = new Texture2D[3];
            for (int f = 0; f < 3; f++)
            {
                var canvas = CitizenArt.Frame(citizenKind, f + 1);
                frames[f] = canvas.ToTexture(assets.GraphicsDevice);
            }
            return new AnimationClip("citizen_walk", frames, 200, 0, true, true);
        }
    }

    /// <summary>
    /// Fuqaro turish animatsiyasi (Idle).
    /// </summary>
    public static class CitizenIdleAnimation
    {
        public static AnimationClip Create(AssetManager assets, int citizenKind = 0)
        {
            var canvas = CitizenArt.Frame(citizenKind, 0);
            var tex = canvas.ToTexture(assets.GraphicsDevice);
            return new AnimationClip("citizen_idle", new[] { tex }, 300, 0, true, true);
        }
    }

    /// <summary>
    /// Jangchi yurish animatsiyasi (placeholder). Animatsiya uchun 3 ta
    /// kadr — placeholder Canvas bilan yaratiladi. Keyinchalik haqiqiy
    /// assetlar bilan almashtiriladi.
    /// </summary>
    public static class GuardWalkAnimation
    {
        public static AnimationClip Create(AssetManager assets, int guardKind = 0)
        {
            var frames = new Texture2D[3];
            for (int f = 0; f < 3; f++)
            {
                var canvas = UnitArt.Frame(guardKind, f + 1);
                frames[f] = canvas.ToTexture(assets.GraphicsDevice);
            }
            return new AnimationClip("guard_walk", frames, 200, 0, true, true);
        }
    }

    /// <summary>
    /// Jangchi turish animatsiyasi (Idle) — placeholder.
    /// </summary>
    public static class GuardIdleAnimation
    {
        public static AnimationClip Create(AssetManager assets, int guardKind = 0)
        {
            var canvas = UnitArt.Frame(guardKind, 0);
            var tex = canvas.ToTexture(assets.GraphicsDevice);
            return new AnimationClip("guard_idle", new[] { tex }, 300, 0, true, true);
        }
    }

    // ------------------------------------------------------------------
    // Placeholder animator yordamchisi — animatsiya holatlarini boshqarish
    // uchun ishlatilishi mumkin. Netijada, har bir unit uchun o'z animatsiya
    // holati bo'ladi.
    // ------------------------------------------------------------------

    /// <summary>
    /// Placeholder animatsiyasiga asoslangan holat mashinasi.
    /// Har bir unit 1 animatsiya state machine bilan ishlaydi.
    /// </summary>
    public enum AnimationPlaybackState
    {
        Idle,
        Walk,
        Work,
        Rest,
        None
    }

    /// <summary>
    /// Placeholder animatsiyasida holat o'tkazish uchun muqaddas kirish nuqtasi
    /// (placeholder yordamida).
    /// </summary>
    public sealed class PlaceholderAnimationStateMachine
    {
        public AnimationPlaybackState State { get; private set; } = AnimationPlaybackState.None;

        // Placeholder
        private float _elapsed;

        public void Update(float dt)
        {
            if (State == AnimationPlaybackState.Idle) _elapsed += dt;
        }

        // Holat o'tkazish — placeholder (real animatsiyada clip almashtiriladi)
        public void SetState(AnimationPlaybackState newState)
        {
            State = newState;
            _elapsed = 0f;
        }
    }
}
