using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>1 dòng nhiệm vụ hiển thị ở màn kết quả.</summary>
    public struct ResultMission
    {
        public string desc;
        public bool met;
    }

    /// <summary>
    ///     Overlay KẾT QUẢ trận arena (THẮNG/THUA) — tự dựng Canvas riêng (ScreenSpaceOverlay, order cao)
    ///     nên hiện được cả khi scene chạy trần. THẮNG: sao đạt + list nhiệm vụ + thưởng. THUA: báo thua.
    ///     2 nút: CHƠI LẠI / VỀ. Skin bằng art ST09 (star, crown label, frame, button) — thiếu art thì fallback.
    /// </summary>
    public class ArenaResultView : MonoBehaviour
    {
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color BoxColor = new Color(0.12f, 0.13f, 0.18f, 1f);
        private static readonly Color RetryCol = new Color(0.22f, 0.6f, 0.3f, 1f);
        private static readonly Color BackCol = new Color(0.3f, 0.5f, 0.85f, 1f);
        private static readonly Color StarOn = new Color(1f, 0.82f, 0.2f, 1f);
        private static readonly Color StarOff = new Color(0.5f, 0.5f, 0.55f, 1f);
        private static readonly Color GoldCol = new Color(1f, 0.85f, 0.25f, 1f);
        private static readonly Color MetCol = new Color(0.85f, 0.95f, 0.85f, 1f);
        private static readonly Color UnmetCol = new Color(0.62f, 0.62f, 0.68f, 1f);

        private Font _font;
        private Sprite _sprPanel, _sprBtnGreen, _sprBtnBlue, _sprStar, _sprTick, _sprWin, _sprLose;

        /// <summary>Dựng &amp; hiện overlay kết quả. Trả về GameObject gốc (huỷ khi đổi scene).</summary>
        public static GameObject Show(bool win, int stars, IList<ResultMission> missions, int gold,
            Action onRetry, Action onBack)
        {
            var go = new GameObject("ArenaResultView");
            var view = go.AddComponent<ArenaResultView>();
            view.Build(win, stars, missions, gold, onRetry, onBack);
            return go;
        }

        private void Build(bool win, int stars, IList<ResultMission> missions, int gold, Action onRetry, Action onBack)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _sprPanel = Resources.Load<Sprite>("art/frame_panel");
            _sprBtnGreen = Resources.Load<Sprite>("art/btn_green");
            _sprBtnBlue = Resources.Load<Sprite>("art/btn_blue");
            _sprStar = Resources.Load<Sprite>("art/star");
            _sprTick = Resources.Load<Sprite>("art/icon_tick");
            _sprWin = Resources.Load<Sprite>("art/win_label");
            _sprLose = Resources.Load<Sprite>("art/lose_label");

            // Canvas overlay riêng (luôn trên cùng).
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2400);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // Nền mờ chặn input.
            var dim = MakeImage(transform, "Dim", Vector2.zero, Vector2.zero, null, Dim);
            var dimRt = dim.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero; dimRt.anchorMax = Vector2.one; dimRt.offsetMin = Vector2.zero; dimRt.offsetMax = Vector2.zero;

            float boxH = win ? 1360f : 700f;
            var box = MakePanel(transform, "Box", new Vector2(840f, boxH), Vector2.zero,
                _sprPanel != null ? Color.white : BoxColor, _sprPanel);

            // Tiêu đề (crown label art) — THẮNG/THUA.
            var titleSpr = win ? _sprWin : _sprLose;
            float boxTop = boxH * 0.5f;
            if (titleSpr != null)
                MakeImage(box, "Title", new Vector2(660f, 164f), new Vector2(0f, boxTop - 40f), titleSpr, Color.white);
            else
                MakeText(box, "Title", win ? "CHIẾN THẮNG!" : "THẤT BẠI", 64, new Vector2(0f, boxTop - 90f),
                    new Vector2(760f, 110f), TextAnchor.MiddleCenter, win ? GoldCol : new Color(0.9f, 0.5f, 0.5f, 1f));

            float y = boxTop - 190f;

            if (win)
            {
                // Hàng sao.
                BuildStars(box, stars, new Vector2(0f, y));
                y -= 180f;

                MakeText(box, "Gold", "+ " + gold + " Gold", 40, new Vector2(0f, y), new Vector2(760f, 60f),
                    TextAnchor.MiddleCenter, GoldCol);
                y -= 90f;

                MakeText(box, "MissTitle", "— NHIỆM VỤ —", 34, new Vector2(0f, y), new Vector2(760f, 56f),
                    TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.8f, 1f));
                y -= 80f;

                if (missions != null)
                    for (int i = 0; i < missions.Count; i++)
                    {
                        BuildMissionRow(box, missions[i], new Vector2(0f, y));
                        y -= 92f;
                    }
            }
            else
            {
                MakeText(box, "Sub", "Hero đã gục! Thử lại nhé.", 38, new Vector2(0f, y),
                    new Vector2(760f, 70f), TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.9f, 1f));
            }

            // 2 nút dưới cùng.
            float btnY = -boxTop + 110f;
            var retry = MakeButton(box, "Retry", "CHƠI LẠI", new Vector2(360f, 108f), new Vector2(-195f, btnY), RetryCol, _sprBtnGreen);
            retry.onClick.AddListener(() => { onRetry?.Invoke(); });
            var back = MakeButton(box, "Back", "VỀ", new Vector2(360f, 108f), new Vector2(195f, btnY), BackCol, _sprBtnBlue);
            back.onClick.AddListener(() => { onBack?.Invoke(); });
        }

        private void BuildStars(Transform parent, int stars, Vector2 center)
        {
            const float size = 130f, gap = 150f;
            for (int i = 0; i < 3; i++)
            {
                var pos = new Vector2(center.x + (i - 1) * gap, center.y);
                bool on = i < stars;
                if (_sprStar != null)
                {
                    var img = MakeImage(parent, "Star" + i, new Vector2(size, size), pos, _sprStar, on ? StarOn : StarOff);
                    if (!on) img.GetComponent<Image>().color = StarOff;
                }
                else
                {
                    MakeText(parent, "Star" + i, "★", 96, pos, new Vector2(size, size), TextAnchor.MiddleCenter, on ? GoldCol : StarOff);
                }
            }
        }

        private void BuildMissionRow(Transform parent, ResultMission m, Vector2 pos)
        {
            var row = new GameObject("Miss", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(720f, 80f); rt.anchoredPosition = pos;

            if (m.met && _sprTick != null)
                MakeImage(row.transform, "Tick", new Vector2(52f, 46f), new Vector2(-320f, 0f), _sprTick, Color.white);
            else
                MakeText(row.transform, "Mark", m.met ? "OK" : "X", 38, new Vector2(-320f, 0f), new Vector2(70f, 60f),
                    TextAnchor.MiddleCenter, m.met ? MetCol : UnmetCol);

            MakeText(row.transform, "Desc", m.desc, 32, new Vector2(20f, 0f), new Vector2(600f, 70f),
                TextAnchor.MiddleLeft, m.met ? MetCol : UnmetCol);
        }

        #region UI helpers

        private Transform MakePanel(Transform parent, string name, Vector2 size, Vector2 pos, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = color;
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
            return go.transform;
        }

        private Transform MakeImage(Transform parent, string name, Vector2 size, Vector2 pos, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = color;
            if (sprite != null) img.sprite = sprite;
            return go.transform;
        }

        private Text MakeText(Transform parent, string name, string content, int size, Vector2 pos, Vector2 sd, TextAnchor align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sd; rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = _font; t.text = content; t.fontSize = size; t.alignment = align; t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private Button MakeButton(Transform parent, string name, string label, Vector2 size, Vector2 pos, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; img.color = Color.white; }
            else img.color = color;
            var lbl = MakeText(go.transform, "Label", label, 36, Vector2.zero, size, TextAnchor.MiddleCenter, Color.white);
            lbl.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            return go.GetComponent<Button>();
        }

        #endregion
    }
}
