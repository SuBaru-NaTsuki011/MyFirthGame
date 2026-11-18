using System;
using System.IO;
using System.Text;

namespace AndijonSaltanatlar.Online
{
    /// <summary>
    /// Sessiya tokenlarini qurilmada shifrlab saqlash (ochiq JSON'da emas):
    ///   - Windows: DPAPI (CurrentUser) — faqat shu Windows foydalanuvchisi ocha oladi;
    ///   - Android: Android Keystore'dagi AES-256-GCM kaliti (kalit qurilmadan chiqmaydi).
    /// Boshqa tizimda (Linux/macOS) — xavfsiz saqlash imkoni yo'q: hech narsa saqlanmaydi (har safar qayta kirish).
    /// Tokenlar hech qachon logga yozilmaydi.
    /// </summary>
    public static class SecureTokenStorage
    {
        private static string FilePath => AndijonSaltanatlar.Core.AppPaths.File("session.bin");

        public static bool Available
        {
            get
            {
#if ANDROID
                return true;
#else
                return OperatingSystem.IsWindows();
#endif
            }
        }

        public static void Save(string plain)
        {
            try
            {
                if (!Available || string.IsNullOrEmpty(plain)) { Clear(); return; }
                byte[] data = Protect(Encoding.UTF8.GetBytes(plain));
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllBytes(FilePath, data);
            }
            catch { /* saqlanmasa — keyingi safar qayta kiriladi */ }
        }

        public static string Load()
        {
            try
            {
                if (!Available || !File.Exists(FilePath)) return null;
                return Encoding.UTF8.GetString(Unprotect(File.ReadAllBytes(FilePath)));
            }
            catch { Clear(); return null; }   // buzilgan yoki boshqa foydalanuvchi — o'chiriladi
        }

        public static void Clear()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
        }

#if ANDROID
        private const string KeyAlias = "andijon_saltanatlar_session";

        private static Javax.Crypto.ISecretKey Key()
        {
            var ks = Java.Security.KeyStore.GetInstance("AndroidKeyStore");
            ks.Load(null);
            if (ks.GetKey(KeyAlias, null) is Javax.Crypto.ISecretKey k) return k;
            var gen = Javax.Crypto.KeyGenerator.GetInstance(Android.Security.Keystore.KeyProperties.KeyAlgorithmAes, "AndroidKeyStore");
            var spec = new Android.Security.Keystore.KeyGenParameterSpec.Builder(KeyAlias,
                    Android.Security.Keystore.KeyStorePurpose.Encrypt | Android.Security.Keystore.KeyStorePurpose.Decrypt)
                .SetBlockModes(Android.Security.Keystore.KeyProperties.BlockModeGcm)
                .SetEncryptionPaddings(Android.Security.Keystore.KeyProperties.EncryptionPaddingNone)
                .SetKeySize(256)
                .Build();
            gen.Init(spec);
            return gen.GenerateKey();
        }

        private static byte[] Protect(byte[] plain)
        {
            var c = Javax.Crypto.Cipher.GetInstance("AES/GCM/NoPadding");
            c.Init(Javax.Crypto.CipherMode.EncryptMode, Key());
            byte[] iv = c.GetIV();
            byte[] enc = c.DoFinal(plain);
            byte[] outp = new byte[1 + iv.Length + enc.Length];
            outp[0] = (byte)iv.Length;
            Buffer.BlockCopy(iv, 0, outp, 1, iv.Length);
            Buffer.BlockCopy(enc, 0, outp, 1 + iv.Length, enc.Length);
            return outp;
        }

        private static byte[] Unprotect(byte[] data)
        {
            int n = data[0];
            byte[] iv = new byte[n];
            Buffer.BlockCopy(data, 1, iv, 0, n);
            var c = Javax.Crypto.Cipher.GetInstance("AES/GCM/NoPadding");
            c.Init(Javax.Crypto.CipherMode.DecryptMode, Key(), new Javax.Crypto.Spec.GCMParameterSpec(128, iv));
            return c.DoFinal(data, 1 + n, data.Length - 1 - n);
        }
#else
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Andijon:Saltanatlar/session/v1");

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static byte[] ProtectWin(byte[] plain) =>
            System.Security.Cryptography.ProtectedData.Protect(plain, Entropy, System.Security.Cryptography.DataProtectionScope.CurrentUser);

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static byte[] UnprotectWin(byte[] data) =>
            System.Security.Cryptography.ProtectedData.Unprotect(data, Entropy, System.Security.Cryptography.DataProtectionScope.CurrentUser);

        private static byte[] Protect(byte[] plain) => OperatingSystem.IsWindows() ? ProtectWin(plain) : throw new PlatformNotSupportedException();
        private static byte[] Unprotect(byte[] data) => OperatingSystem.IsWindows() ? UnprotectWin(data) : throw new PlatformNotSupportedException();
#endif
    }
}
