using System.Collections.Generic;
using BlackFace.Libraries.Modules.UIModule;
using Ezg.Feature.Shared.Config;
using Ezg.Feature.Shared.Systems;
using Ezg.Package.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Feature screen (UIManager) CHỌN STAGE/MAP cho arena. Kế thừa <see cref="FeatureBaseController" />
    ///     — mở qua <c>UIManager.Instance.Show(GameEnums.Features.ArenaStageSelect)</c>. Danh sách stage dựng
    ///     runtime dưới <see cref="FeatureBaseController.MainUI" /> (đọc <see cref="ArenaStageCollection" /> +
    ///     thời tiết từ <see cref="WeatherCollection" />). Bấm CHƠI → set <see cref="ArenaLaunch.PendingStageId" />
    ///     rồi đổi sang BattleScene. UI skin bằng art ST09 (Resources/art) — thiếu art thì fallback màu phẳng.
    /// </summary>
    public class ArenaStageSelectController : FeatureBaseController
    {
        private static readonly Color BoxColor = new Color(0.12f, 0.13f, 0.18f, 1f);
        private static readonly Color PlayBtn = new Color(0.22f, 0.6f, 0.3f, 1f);
        private static readonly Color RowColor = new Color(0.18f, 0.19f, 0.26f, 1f);
        private static readonly Color ClearColor = new Color(0.82f, 0.5f, 0.08f, 1f);   // tên stage đã clear (chữ vàng đậm — đọc trên khung sáng)
        private static readonly Color NameColor = new Color(0.16f, 0.17f, 0.22f, 1f);   // tên stage thường (chữ navy đậm — khung art sáng màu)

        private ArenaStageCollection _stages;
        private WeatherCollection _weathers;
        private Font _font;
        private Transform _box;
        private readonly List<GameObject> _rows = new List<GameObject>();
        private bool _built;

        // Art ST09 (9-slice frame + button + icon). Null = fallback màu phẳng.
        private Sprite _sprPanel, _sprTitle, _sprBtnGreen, _sprBtnBlue, _sprTick, _sprStar;
        private static readonly Color StarOn = new Color(1f, 0.82f, 0.2f, 1f);
        private static readonly Color StarOff = new Color(0.4f, 0.4f, 0.45f, 1f);

        protected override void LoadData()
        {
            base.LoadData();
            if (_built) { Refresh(); return; }
            _built = true;

            _stages = Resources.Load<ArenaStageCollection>("ArenaCsv/ArenaStageCollection");
            _weathers = Resources.Load<WeatherCollection>("ArenaCsv/WeatherCollection");
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            _sprPanel = Resources.Load<Sprite>("art/frame_panel");
            _sprTitle = Resources.Load<Sprite>("art/frame_title");
            _sprBtnGreen = Resources.Load<Sprite>("art/btn_green");
            _sprBtnBlue = Resources.Load<Sprite>("art/btn_blue");
            _sprTick = Resources.Load<Sprite>("art/icon_tick");
            _sprStar = Resources.Load<Sprite>("art/star");

            Build();
            Refresh();
        }

        private Transform Content => MainUI != null ? MainUI : transform;

        private void Build()
        {
            _box = MakePanel(Content, "StageBox", new Vector2(900f, 1780f), Vector2.zero, BoxColor, _sprPanel);

            var titleBar = MakeImage(_box, "TitleBar", new Vector2(600f, 150f), new Vector2(0f, 800f), _sprTitle,
                _sprTitle != null ? Color.white : new Color(0.42f, 0.36f, 0.58f, 1f));
            MakeText(titleBar, "Title", "CHỌN MÀN", 54, Vector2.zero, new Vector2(560f, 100f), TextAnchor.MiddleCenter, Color.white);
            MakeText(_box, "Hint", "— chọn map để vào trận —", 32, new Vector2(0f, 700f), new Vector2(840f, 56f), TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.8f, 1f));

            var close = MakeButton(_box, "CloseBtn", "ĐÓNG", new Vector2(400f, 110f), new Vector2(0f, -820f), new Color(0.3f, 0.5f, 0.85f, 1f), _sprBtnBlue);
            close.onClick.AddListener(() => CloseMe());
        }

        private void Refresh()
        {
            for (int i = 0; i < _rows.Count; i++) if (_rows[i] != null) Destroy(_rows[i]);
            _rows.Clear();
            if (_stages == null) return;

            var all = _stages.All;
            float y = 560f;
            for (int i = 0; i < all.Count; i++)
            {
                BuildRow(all[i], y);
                y -= 156f;
            }
        }

        private void BuildRow(ArenaStageModel stage, float y)
        {
            var row = MakePanel(_box, "Row_" + stage.id, new Vector2(840f, 138f), new Vector2(0f, y),
                _sprPanel != null ? Color.white : RowColor, _sprPanel);
            _rows.Add(row.gameObject);

            bool cleared = ArenaHeroUnlockService.IsStageCleared(stage.id);
            int stars = ArenaHeroUnlockService.GetStars(stage.id);

            string title = (string.IsNullOrEmpty(stage.name) ? stage.id : stage.name);
            Color nameCol = cleared ? ClearColor : (_sprPanel != null ? NameColor : Color.white);
            MakeText(row, "Name", title, 38, new Vector2(-186f, 34f), new Vector2(430f, 56f), TextAnchor.MiddleLeft, nameCol);
            BuildRowStars(row, stars, new Vector2(-236f, -30f)); // 3 sao nhỏ dưới tên
            MakeText(row, "Sub", $"Round: {stage.maxRounds}    Thời tiết: {WeatherLabel(stage.weather)}", 25,
                new Vector2(-40f, -34f), new Vector2(340f, 48f), TextAnchor.MiddleLeft, new Color(0.32f, 0.3f, 0.26f, 1f));

            string id = stage.id;
            var play = MakeButton(row, "Play", "CHƠI", new Vector2(240f, 100f), new Vector2(268f, 0f), PlayBtn, _sprBtnGreen);
            play.onClick.AddListener(() => EnterStage(id));
        }

        /// <summary>3 sao nhỏ hiển thị tiến trình stage (đã đạt = vàng, chưa = xám).</summary>
        private void BuildRowStars(Transform row, int stars, Vector2 start)
        {
            const float size = 34f, step = 40f;
            for (int i = 0; i < 3; i++)
            {
                var pos = new Vector2(start.x + i * step, start.y);
                bool on = i < stars;
                if (_sprStar != null)
                    MakeImage(row, "St" + i, new Vector2(size, size), pos, _sprStar, on ? StarOn : StarOff);
                else
                    MakeText(row, "St" + i, "★", 28, pos, new Vector2(size, size), TextAnchor.MiddleCenter, on ? StarOn : StarOff);
            }
        }

        /// <summary>Vào stage: nhớ id cho ArenaSceneController, dừng nhạc home rồi đổi sang BattleScene.</summary>
        private void EnterStage(string stageId)
        {
            ArenaLaunch.PendingStageId = stageId;
            CloseMe();
            try { AudioService.Default.StopMusic(); } catch { /* audio chưa init */ }
            GameSystems.ChangeScene(GameEnums.Scenes.BattleScene);
        }

        /// <summary>Nhãn thời tiết map: rỗng = Trời quang, "random" = Ngẫu nhiên, còn lại lấy tên biome.</summary>
        private string WeatherLabel(string weatherId)
        {
            if (string.IsNullOrEmpty(weatherId)) return "Trời quang";
            if (weatherId.Trim().ToLowerInvariant() == "random") return "Ngẫu nhiên";
            var m = _weathers != null ? _weathers.GetById(weatherId) : default;
            return !string.IsNullOrEmpty(m.name) ? m.name : weatherId;
        }

        #region UI helpers

        private Transform MakePanel(Transform parent, string name, Vector2 size, Vector2 pos, Color color, Sprite sprite = null)
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
            if (sprite != null) img.sprite = sprite; else img.color = new Color(color.r, color.g, color.b, 0f);
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

        private Button MakeButton(Transform parent, string name, string label, Vector2 size, Vector2 pos, Color color, Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; img.color = Color.white; }
            else img.color = color;
            var lbl = MakeText(go.transform, "Label", label, 34, Vector2.zero, size, TextAnchor.MiddleCenter, Color.white);
            lbl.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            return go.GetComponent<Button>();
        }

        #endregion
    }
}
