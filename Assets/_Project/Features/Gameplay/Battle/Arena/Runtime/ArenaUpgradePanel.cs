using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Panel NÂNG CẤP (tự dựng UI bằng code — chỉ cần đặt 1 GameObject có script này lên ArenaCanvas).
    ///     Hiện gold + 1 dòng hero + N dòng vũ khí (theo slot của <see cref="ArenaSceneController" />), mỗi dòng
    ///     có nút nâng cấp trừ gold qua <see cref="ArenaUpgradeService" />. Có nút mở/đóng và nút +Gold (debug).
    /// </summary>
    public class ArenaUpgradePanel : MonoBehaviour
    {
        #region Fields

        [SerializeField] private ArenaSceneController _controller;
        [SerializeField] private int _debugGrantGold = 500;

        private Font _font;
        private CanvasGroup _content;
        private Text _goldText;

        private class Row
        {
            public bool isHero;
            public bool isSkill;
            public string weaponId;
            public Text info;
            public Text cost;
            public Button button;
        }

        private readonly List<Row> _rows = new List<Row>();

        #endregion

        #region Initialize

        private void Awake()
        {
            if (_controller == null) _controller = FindFirstObjectByType<ArenaSceneController>();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Build();
            SetContentVisible(false);
        }

        private void OnEnable() => ArenaUpgradeService.OnChanged += Refresh;
        private void OnDisable() => ArenaUpgradeService.OnChanged -= Refresh;

        #endregion

        #region Build UI

        private void Build()
        {
            // Nút MỞ panel — đặt trên canvas (không nằm trong content nên luôn hiển thị).
            var openBtn = MakeButton(transform.parent, "UpgradeOpenBtn", "⚙ NÂNG CẤP",
                new Vector2(0f, 1f), new Vector2(300f, 90f), new Vector2(170f, -70f));
            openBtn.onClick.AddListener(() => { Refresh(); SetContentVisible(true); });

            // Content (nền mờ toàn màn + hộp giữa).
            var rt = gameObject.GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            Stretch(rt);
            var dim = gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.8f);
            _content = gameObject.AddComponent<CanvasGroup>();

            var box = MakePanel(transform, "Box", new Vector2(0.5f, 0.5f), new Vector2(900f, 1240f), Vector2.zero,
                new Color(0.12f, 0.13f, 0.18f, 1f));

            MakeText(box, "Title", "NÂNG CẤP", 64, new Vector2(0f, 540f), new Vector2(820f, 90f),
                TextAnchor.MiddleCenter, Color.white);
            _goldText = MakeText(box, "Gold", "Gold: 0", 48, new Vector2(0f, 450f), new Vector2(820f, 80f),
                TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.25f, 1f));

            float y = 330f;
            AddRow(box, new Row { isHero = true }, ref y);
            AddRow(box, new Row { isSkill = true }, ref y); // skill ultimate (nâng cấp = đổi id skill)
            int slots = _controller != null ? _controller.SlotCount : 0;
            for (int i = 0; i < slots; i++)
            {
                string id = _controller.SlotWeaponId(i);
                if (string.IsNullOrEmpty(id)) continue;
                AddRow(box, new Row { weaponId = id }, ref y);
            }

            // +Gold (debug) + Đóng.
            var addGold = MakeButton(box, "AddGoldBtn", "+ " + _debugGrantGold + " Gold",
                new Vector2(0.5f, 0.5f), new Vector2(400f, 100f), new Vector2(-225f, -540f));
            addGold.onClick.AddListener(() => ArenaUpgradeService.AddGold(_debugGrantGold));
            var closeBtn = MakeButton(box, "CloseBtn", "ĐÓNG",
                new Vector2(0.5f, 0.5f), new Vector2(400f, 100f), new Vector2(225f, -540f));
            closeBtn.onClick.AddListener(() => SetContentVisible(false));
        }

        private void AddRow(Transform parent, Row row, ref float y)
        {
            row.info = MakeText(parent, "Info", "", 42, new Vector2(-150f, y), new Vector2(380f, 100f),
                TextAnchor.MiddleLeft, Color.white);
            row.cost = MakeText(parent, "Cost", "", 38, new Vector2(120f, y), new Vector2(150f, 100f),
                TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.25f, 1f));
            row.button = MakeButton(parent, "UpBtn", "NÂNG", new Vector2(0.5f, 0.5f), new Vector2(190f, 100f),
                new Vector2(275f, y));

            var captured = row;
            row.button.onClick.AddListener(() => OnUpgradeClicked(captured));
            _rows.Add(row);
            y -= 130f;
        }

        #endregion

        #region Logic

        private void OnUpgradeClicked(Row row)
        {
            string heroId = _controller != null ? _controller.HeroId : null;
            if (row.isHero) ArenaUpgradeService.TryUpgradeHero(heroId);
            else if (row.isSkill) ArenaUpgradeService.TryUpgradeSkill(heroId);
            else ArenaUpgradeService.TryUpgradeWeapon(row.weaponId, _controller != null ? _controller.Weapons : null);
            Refresh();
        }

        private void Refresh()
        {
            if (_goldText != null) _goldText.text = "Gold: " + ArenaUpgradeService.Gold;

            var col = _controller != null ? _controller.Weapons : null;
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                if (r.isHero)
                {
                    string heroId = _controller != null ? _controller.HeroId : null;
                    int lv = ArenaUpgradeService.HeroLevel(heroId);
                    bool max = ArenaUpgradeService.HeroMaxed(heroId);
                    int cost = ArenaUpgradeService.HeroUpgradeCost(heroId);
                    r.info.text = "Hero Lv " + lv;
                    ApplyCost(r, max, cost);
                }
                else if (r.isSkill)
                {
                    string heroId = _controller != null ? _controller.HeroId : null;
                    string name = ArenaUpgradeService.SkillName(heroId);
                    bool max = !ArenaUpgradeService.CanUpgradeSkill(heroId);
                    int cost = ArenaUpgradeService.SkillUpgradeCost(heroId);
                    r.info.text = "Skill: " + name;
                    ApplyCost(r, max, cost);
                }
                else
                {
                    int lv = ArenaUpgradeService.WeaponLevel(r.weaponId);
                    bool max = ArenaUpgradeService.WeaponMaxed(r.weaponId, col);
                    int cost = ArenaUpgradeService.WeaponUpgradeCost(r.weaponId, col);
                    string name = col != null ? col.GetById(r.weaponId).name : r.weaponId;
                    r.info.text = name + "  Lv " + lv;
                    ApplyCost(r, max, cost);
                }
            }
        }

        private static void ApplyCost(Row r, bool maxed, int cost)
        {
            if (maxed)
            {
                r.cost.text = "MAX";
                r.button.interactable = false;
                return;
            }

            r.cost.text = cost + "g";
            r.button.interactable = ArenaUpgradeService.Gold >= cost;
        }

        private void SetContentVisible(bool on)
        {
            if (_content == null) return;
            if (on) transform.SetAsLastSibling(); // mở panel → đưa lên trên cùng (che nút ULT/HUD)
            _content.alpha = on ? 1f : 0f;
            _content.interactable = on;
            _content.blocksRaycasts = on;
        }

        #endregion

        #region UI helpers

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private RectTransform MakePanel(Transform parent, string name, Vector2 anchor, Vector2 size,
            Vector2 anchoredPos, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = color;
            return rt;
        }

        private Text MakeText(Transform parent, string name, string content, int fontSize, Vector2 anchoredPos,
            Vector2 size, TextAnchor align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var txt = go.GetComponent<Text>();
            txt.font = _font;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = align;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }

        private Button MakeButton(Transform parent, string name, string label, Vector2 anchor, Vector2 size,
            Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = new Color(0.22f, 0.5f, 0.85f, 1f);

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var trt = txtGo.GetComponent<RectTransform>();
            Stretch(trt);
            var txt = txtGo.GetComponent<Text>();
            txt.font = _font;
            txt.text = label;
            txt.fontSize = 40;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            return go.GetComponent<Button>();
        }

        #endregion
    }
}
