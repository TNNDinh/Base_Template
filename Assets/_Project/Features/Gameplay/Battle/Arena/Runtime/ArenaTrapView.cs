using DG.Tweening;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     View 1 cái bẫy: 1 sprite tròn RƠI TỪ TRÊN xuống ô (bounce), xoay nhẹ tại chỗ. <see cref="Flash" />
    ///     khi dính enemy, <see cref="Kill" /> khi bẫy hết hạn. Tự sinh sprite tròn — không cần art.
    /// </summary>
    public class ArenaTrapView : MonoBehaviour
    {
        private static Sprite _sprite;
        private static readonly Color TrapColor = new Color(0.92f, 0.28f, 0.14f, 0.95f);

        private SpriteRenderer _sr;

        public static ArenaTrapView Create(Transform parent, Vector3 cellWorld, float size)
        {
            var go = new GameObject("Trap");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<ArenaTrapView>();
            v.Init(cellWorld, size);
            return v;
        }

        private void Init(Vector3 cellWorld, float size)
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = GetSprite();
            _sr.color = TrapColor;
            _sr.sortingOrder = 40; // trên nền lưới, dưới nhân vật

            Vector3 baseP = new Vector3(cellWorld.x, cellWorld.y, cellWorld.z + 0.02f);
            transform.position = baseP + Vector3.up * 3f; // rơi từ trên
            transform.localScale = Vector3.one * size * 1.4f;
            transform.DOMove(baseP, 0.35f).SetEase(Ease.OutBounce);
            transform.DOScale(Vector3.one * size, 0.35f).SetEase(Ease.OutBack);
        }

        private void Update()
        {
            transform.Rotate(0f, 0f, 45f * Time.deltaTime); // xoay chậm cho sinh động
        }

        /// <summary>Chớp sáng + giật nhẹ khi dính enemy.</summary>
        public void Flash()
        {
            if (_sr == null) return;
            _sr.DOKill();
            _sr.color = Color.white;
            _sr.DOColor(TrapColor, 0.25f);
            transform.DOPunchScale(transform.localScale * 0.25f, 0.22f, 6, 0.6f);
        }

        /// <summary>Bẫy hết hạn → thu nhỏ rồi biến mất.</summary>
        public void Kill()
        {
            transform.DOKill();
            if (_sr != null) _sr.DOKill();
            transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => { if (this != null) Destroy(gameObject); });
        }

        private void OnDestroy()
        {
            transform.DOKill();
            if (_sr != null) _sr.DOKill();
        }

        private static Sprite GetSprite()
        {
            if (_sprite != null) return _sprite;
            const int s = 48;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[s * s];
            var c = new Vector2(s / 2f, s / 2f);
            float rOuter = s / 2f - 1f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float t = d / rOuter;
                float a = t <= 1f ? 1f : 0f;
                // viền đậm + lõi sáng hơn (kiểu bẫy gai)
                float shade = t > 0.72f ? 0.55f : 1f;
                px[y * s + x] = new Color(shade, shade, shade, a);
            }

            tex.SetPixels(px);
            tex.Apply();
            _sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _sprite;
        }
    }
}
