using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Sinh runtime 1 sprite hình chữ nhật BO GÓC (trắng, có alpha mềm ở viền) để làm ô lưới khi
    ///     chưa có art. Sprite rộng đúng 1 world unit (pixelsPerUnit = size) để tiện scale theo ô.
    /// </summary>
    public static class RoundedRectSpriteFactory
    {
        private const int DefaultSize = 128;
        private const float DefaultCornerRatio = 0.30f; // bán kính bo góc / nửa cạnh
        private const float DefaultEdgeSoftnessPx = 2.5f;

        private static Sprite _cached;

        public static Sprite GetDefault()
        {
            if (_cached == null) _cached = Create(DefaultSize, DefaultCornerRatio, DefaultEdgeSoftnessPx);
            return _cached;
        }

        /// <summary>Texture mask bo góc (trắng, alpha mềm) — dùng làm _MainTex cho mesh ô cong.</summary>
        public static Texture2D GetDefaultTexture()
        {
            return GetDefault().texture;
        }

        public static Sprite Create(int size, float cornerRatio, float edgeSoftnessPx)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RoundedRect",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var px = new Color32[size * size];
            float half = size * 0.5f;
            float radius = Mathf.Clamp01(cornerRatio) * half;
            float innerHalf = half - radius; // nửa cạnh phần "thẳng" (chưa tính bo góc)

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - half);
                float dy = Mathf.Abs(y + 0.5f - half);

                // Signed distance tới biên rounded-rect (âm = bên trong).
                float qx = dx - innerHalf;
                float qy = dy - innerHalf;
                float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                float dist = outside + inside - radius;

                float alpha = Mathf.Clamp01(0.5f - dist / edgeSoftnessPx);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
