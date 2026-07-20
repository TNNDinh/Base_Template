using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Thanh BÀI TRÊN TAY in-battle (code-built Canvas riêng — hiện được cả khi scene chạy trần, giống
    ///     <see cref="ArenaResultView" />). Mỗi lá trên tay = 1 thẻ bấm được; đủ energy mới bấm (thiếu → mờ).
    ///     Chỉ render + báo tap về controller; logic bốc/trừ energy nằm ở <see cref="CardDeckRuntime" />.
    /// </summary>
    public class ArenaHandView : MonoBehaviour
    {
        private static readonly Color EnergyCol = new Color(0.35f, 0.8f, 1f, 1f);
        private static readonly Color CostCol = new Color(1f, 0.85f, 0.3f, 1f);
        private static readonly Color DimCard = new Color(0.35f, 0.35f, 0.4f, 0.9f);

        private static readonly Color[] RarityCol =
        {
            new Color(0.45f, 0.5f, 0.6f, 1f),  // 0/1 common
            new Color(0.45f, 0.5f, 0.6f, 1f),
            new Color(0.35f, 0.65f, 0.42f, 1f), // 2 uncommon
            new Color(0.55f, 0.4f, 0.75f, 1f)   // 3 rare
        };

        private Font _font;
        private CardCollection _cards;
        private Action<string> _onTap;
        private Transform _row;
        private Text _energyLabel;

        /// <summary>Dựng thanh bài (ẩn sẵn). <paramref name="onTap" /> = controller.TryPlayCard.</summary>
        public static ArenaHandView Create(CardCollection cards, Action<string> onTap)
        {
            var go = new GameObject("ArenaHandView");
            var v = go.AddComponent<ArenaHandView>();
            v._cards = cards;
            v._onTap = onTap;
            v.Build();
            v.SetVisible(false);
            return v;
        }

        private void Build()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4500; // dưới màn kết quả (5000)
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2400);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // Nhãn energy (trên thanh bài).
            _energyLabel = MakeText(transform, "Energy", "", 44, new Vector2(0f, 470f),
                new Vector2(700f, 60f), TextAnchor.MiddleCenter, EnergyCol);

            // Hàng chứa các lá (neo đáy giữa).
            var rowGo = new GameObject("Row", typeof(RectTransform));
            rowGo.transform.SetParent(transform, false);
            var rt = rowGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 40f);
            rt.sizeDelta = new Vector2(1060f, 250f);
            _row = rowGo.transform;
        }

        public void SetVisible(bool on) => gameObject.SetActive(on);

        /// <summary>Dựng lại toàn bộ thẻ theo tay hiện tại + cập nhật energy (mờ lá không đủ energy).</summary>
        public void Render(IReadOnlyList<string> hand, int energy)
        {
            if (_energyLabel != null) _energyLabel.text = "NANG LUONG  " + energy;

            for (int i = _row.childCount - 1; i >= 0; i--) Destroy(_row.GetChild(i).gameObject);
            if (hand == null) return;

            const float w = 150f, h = 210f, gap = 12f;
            int n = hand.Count;
            float total = n * w + Mathf.Max(0, n - 1) * gap;
            float x0 = -total * 0.5f + w * 0.5f;

            for (int i = 0; i < n; i++)
            {
                string id = hand[i];
                var m = _cards != null ? _cards.GetById(id) : default;
                bool afford = energy >= m.cost;
                BuildCard(id, m, new Vector2(x0 + i * (w + gap), 0f), new Vector2(w, h), afford);
            }
        }

        private void BuildCard(string id, CardModel m, Vector2 pos, Vector2 size, bool afford)
        {
            int rar = Mathf.Clamp(m.rarity, 0, RarityCol.Length - 1);
            var baseCol = afford ? RarityCol[rar] : DimCard;

            var go = new GameObject("Card_" + id, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_row, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = baseCol;

            // Badge energy (góc trên trái).
            var badge = MakeText(go.transform, "Cost", m.cost.ToString(), 46, new Vector2(-size.x * 0.5f + 32f, size.y * 0.5f - 30f),
                new Vector2(56f, 56f), TextAnchor.MiddleCenter, afford ? CostCol : new Color(0.8f, 0.8f, 0.85f, 1f));
            badge.fontStyle = FontStyle.Bold;

            // Tên lá.
            MakeText(go.transform, "Name", m.name ?? id, 30, new Vector2(0f, 40f),
                new Vector2(size.x - 12f, 80f), TextAnchor.MiddleCenter, Color.white);

            // Mô tả ngắn.
            MakeText(go.transform, "Desc", m.desc ?? "", 20, new Vector2(0f, -55f),
                new Vector2(size.x - 16f, 100f), TextAnchor.UpperCenter, new Color(0.9f, 0.92f, 0.95f, 0.95f));

            string cardId = id;
            go.GetComponent<Button>().onClick.AddListener(() => _onTap?.Invoke(cardId));
        }

        private Text MakeText(Transform parent, string name, string content, int size, Vector2 pos, Vector2 sd,
            TextAnchor align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sd;
            rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.text = content;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }
    }
}
