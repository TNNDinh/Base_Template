using System.Collections.Generic;

namespace Game.Runtime
{
    /// <summary>
    ///     Class tá»± Ä‘á»™ng mapping tá»« Screen Key (constants) sang Bundle Name (constants).
    ///     Sá»­ dá»¥ng constants tá»« ScreenKey.cs vÃ  AssetBundleName Ä‘á»ƒ trÃ¡nh typo.
    /// </summary>
    public static class AssetBundleMapping
    {
        private static readonly Dictionary<string, string> screenToBundle = new();

        /// <summary>
        ///     Láº¥y bundle name tá»« screen key constant
        /// </summary>
        public static string GetBundleName(string screenKey)
        {
            return screenToBundle.TryGetValue(screenKey, out var bundleName) ? bundleName : null;
        }

        public static bool HasMapping(string screenKey)
        {
            return screenToBundle.ContainsKey(screenKey);
        }
    }
}