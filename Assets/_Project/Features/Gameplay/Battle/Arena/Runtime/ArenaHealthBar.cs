using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Thanh máu world-space nổi trên đầu 1 unit. KHÔNG parent vào rig (rig bị lật scale.x theo
    ///     <see cref="CharacterRig.SetFacing" />) — thay vào đó tự bám theo target ở <see cref="LateUpdate" />
    ///     nên thanh máu không bị lật/biến dạng. Fill co theo tỉ lệ máu, đổi màu xanh→đỏ khi máu thấp.
    /// </summary>
    public class ArenaHealthBar : MonoBehaviour
    {
        #region Fields

        private static Sprite _sprite;

        private static readonly Color FullColor = new Color(0.30f, 0.85f, 0.35f, 1f);
        private static readonly Color LowColor = new Color(0.90f, 0.25f, 0.20f, 1f);

        private Transform _target;
        private float _yOffset;

        private Transform _fillPivot;
        private SpriteRenderer _fill;
        private float _width;
        private float _height;

        #endregion

        #region Public

        /// <summary>Tạo 1 thanh máu con của <paramref name="parent" /> (thường là arena root, không bị lật).</summary>
        public static ArenaHealthBar Create(Transform parent, float width, float height, int sortingOrder)
        {
            var go = new GameObject("HealthBar");
            go.transform.SetParent(parent, false);
            var bar = go.AddComponent<ArenaHealthBar>();
            bar.Build(width, height, sortingOrder);
            return bar;
        }

        /// <summary>Bám theo <paramref name="target" />, nổi cao hơn <paramref name="yOffset" /> world unit.</summary>
        public void Follow(Transform target, float yOffset)
        {
            _target = target;
            _yOffset = yOffset;
            SnapToTarget();
        }

        /// <summary>Đặt tỉ lệ máu 0..1 (co fill + đổi màu theo mức máu).</summary>
        public void SetRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            if (_fill == null) return;
            _fill.transform.localScale = new Vector3(_width * ratio, _height * 0.78f, 1f);
            _fill.transform.localPosition = new Vector3(_width * ratio * 0.5f, 0f, 0f);
            _fill.color = Color.Lerp(LowColor, FullColor, ratio);
        }

        public void Kill()
        {
            if (this == null) return;
            Destroy(gameObject);
        }

        #endregion

        #region Private

        private void Build(float width, float height, int sortingOrder)
        {
            _width = width;
            _height = height;
            Sprite s = GetSprite();

            var bg = new GameObject("bg").AddComponent<SpriteRenderer>();
            bg.transform.SetParent(transform, false);
            bg.sprite = s;
            bg.color = new Color(0f, 0f, 0f, 0.6f);
            bg.sortingOrder = sortingOrder;
            bg.transform.localScale = new Vector3(width, height, 1f);

            _fillPivot = new GameObject("fillPivot").transform;
            _fillPivot.SetParent(transform, false);
            _fillPivot.localPosition = new Vector3(-width * 0.5f, 0f, 0f); // mép trái để fill co từ trái

            _fill = new GameObject("fill").AddComponent<SpriteRenderer>();
            _fill.transform.SetParent(_fillPivot, false);
            _fill.sprite = s;
            _fill.sortingOrder = sortingOrder + 1;

            SetRatio(1f);
        }

        private void LateUpdate() => SnapToTarget();

        private void SnapToTarget()
        {
            if (_target == null) return;
            Vector3 p = _target.position;
            transform.position = new Vector3(p.x, p.y + _yOffset, p.z - 0.01f);
        }

        private static Sprite GetSprite()
        {
            if (_sprite == null)
                _sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            return _sprite;
        }

        #endregion
    }
}
