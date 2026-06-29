using System;
using System.Reflection;
using Ezg.Package.Localize.Localization;
using UnityEngine;

namespace Ezg.Feature.IAP
{
    /// <summary>
    ///     Điểm wiring giữa game và module IAP. Build secrets/fallback rồi inject impl game vào
    ///     <see cref="InAppManager" />. PHẢI gọi Configure() trước InAppManager.Init()/Buy().
    /// </summary>
    public static class IapBootstrap
    {
        // Public key dùng cho AppsFlyer validateAndSendInAppPurchase (Android) — secret giữ ở game.
        private const string APPSFLYER_PUBLIC_KEY =
            "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAi1xb/WSx4vzpsm0D9chuiSb2bhXo0P2EwFBzlYPOqmRRl+ctxA1weKZ5EfXdCg3vIEF01JAGHpqikqfCHcHK/wdByunu4gDf7ZgvCJsVg2XIGlzQrzIsDzgVQF0OHd894ELrmhM8DmDWctJYapkBfbhuGdUHLQXZxymfcqHVkLaasTtN/bDz4NBpkYd7jWX5xcsCniAPXyYvrnsnEy7QSCAHaklMi8dVZliaHQ937N4ahIcdqFdnUztc7ZgqFUfsbtOUPuWku01fK0QnBJYWWX0yx3d6QhYw1UdYVnQwXgW8HpE16ID2wUfbO+QB1J8OobG7+MUW8tkgPfESOQ841QIDAQAB";

        private static GameIapHost _host;
        private static bool _configured;

        public static void Configure()
        {
            if (_configured) return;

            _host ??= new GameIapHost();

            var config = new IapSecurityConfig
            {
                // Tangle.Data() dùng reflection để tránh dependency trực tiếp vào Assembly-CSharp
                // (GooglePlayTangle/AppleTangle được Unity Purchasing generate trong Assembly-CSharp).
                GooglePlayTangle = CallTangleData("GooglePlayTangle"),
                AppleTangle = CallTangleData("AppleTangle"),
                AppsFlyerPublicKey = APPSFLYER_PUBLIC_KEY,
                DefaultPriceTextProvider = () => Localization.Current.Get("common", "coming_soon")
            };

            InAppManager.Instance.Configure(new InAppPurchase(), _host, _host, config);
            _configured = true;
        }

        /// <summary>
        ///     Gọi static method Data() trên lớp Tangle do Unity Purchasing generate trong Assembly-CSharp.
        ///     Dùng reflection để tránh compile-time dependency (asmdef không reference được Assembly-CSharp).
        /// </summary>
        private static byte[] CallTangleData(string className)
        {
            // Tangle/Obfuscator chỉ được implement thật trên build device (Android/iOS). Trong Editor
            // và các platform khác, Unity compile UnityEngine.Purchasing.SecurityStub.Obfuscator —
            // luôn ném NotImplementedException. Reflection.Invoke sẽ bọc nó thành
            // TargetInvocationException, nên ta nuốt lỗi và trả null (game đã có fallback cho tangle null).
            try
            {
                var type = Type.GetType($"UnityEngine.Purchasing.Security.{className}, Assembly-CSharp");
                if (type != null)
                {
                    var method = type.GetMethod("Data", BindingFlags.Public | BindingFlags.Static);
                    if (method != null)
                        return (byte[])method.Invoke(null, null);
                    // Data could be a static property — try that
                    var prop = type.GetProperty("Data", BindingFlags.Public | BindingFlags.Static);
                    if (prop != null)
                        return (byte[])prop.GetValue(null);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"[IapBootstrap] Tangle '{className}' không khả dụng (Editor/unsupported platform), bỏ qua receipt validation: {e.InnerException?.Message ?? e.Message}");
            }

            return null;
        }
    }
}