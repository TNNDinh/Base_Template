using System.Collections.Generic;
using BlackFace.Libraries.Modules.UIModule;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Feature screen (UIManager) CHỌN HERO + VŨ KHÍ + MỞ KHÓA cho arena. Kế thừa <see cref="FeatureBaseController" />
    ///     — mở qua <c>UIManager.Instance.Show(GameEnums.Features.ArenaLoadout)</c>. Nội dung (list hero + slot vũ khí)
    ///     dựng runtime dưới <see cref="FeatureBaseController.MainUI" /> vì phụ thuộc data (số hero/vũ khí động).
    /// </summary>
    public class ArenaLoadoutController : FeatureBaseController
    {
        private static readonly Color BoxColor = new Color(0.12f, 0.13f, 0.18f, 1f);
        private static readonly Color SelectBtn = new Color(0.22f, 0.6f, 0.3f, 1f);
        private static readonly Color UnlockBtn = new Color(0.85f, 0.55f, 0.15f, 1f);
        private static readonly Color LockedBtn = new Color(0.3f, 0.3f, 0.34f, 1f);

        private ArenaSceneController _arena;
        private Font _font;
        private Transform _box;
        private Text _goldText;
        private Text[] _wLabels;
        private readonly List<GameObject> _rows = new List<GameObject>();
        private bool _built;

        protected override void LoadData()
        {
            base.LoadData();
            if (_built) { Refresh(); return; }
            _built = true;

            _arena = FindFirstObjectByType<ArenaSceneController>();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Build();
            Refresh();
        }

        private Transform Content => MainUI != null ? MainUI : transform;

        private void Build()
        {
            _box = MakePanel(Content, "LoadoutBox", new Vector2(900f, 1780f), Vector2.zero, BoxColor);
            MakeText(_box, "Title", "HERO & VŨ KHÍ", 56, new Vector2(0f, 820f), new Vector2(840f, 90f), TextAnchor.MiddleCenter, Color.white);
            _goldText = MakeText(_box, "Gold", "", 38, new Vector2(0f, 748f), new Vector2(840f, 66f), TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.25f, 1f));

            MakeText(_box, "WpLabel", "— VŨ KHÍ (bấm để đổi) —", 34, new Vector2(0f, 672f), new Vector2(840f, 60f), TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.8f, 1f));
            _wLabels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var b = MakeButton(_box, "Wp" + i, "", new Vector2(240f, 96f), new Vector2(-250f + i * 250f, 592f), new Color(0.42f, 0.36f, 0.58f, 1f));
                b.onClick.AddListener(() => { if (_arena != null) _arena.CycleWeaponSlot(idx); RefreshWeapons(); });
                _wLabels[i] = b.GetComponentInChildren<Text>();
            }

            var gacha = MakeButton(_box, "GachaBtn", "GACHA (" + ArenaHeroUnlockService.GachaCost + "g)", new Vector2(380f, 100f), new Vector2(-220f, -820f), UnlockBtn);
            gacha.onClick.AddListener(() => { ArenaHeroUnlockService.GachaPull(); Refresh(); });
            var close = MakeButton(_box, "CloseBtn", "ĐÓNG", new Vector2(380f, 100f), new Vector2(220f, -820f), new Color(0.3f, 0.5f, 0.85f, 1f));
            close.onClick.AddListener(() => CloseMe());
        }

        private void Refresh()
        {
            if (_arena == null || _arena.HeroStats == null) return;
            if (_goldText != null) _goldText.text = "Gold: " + ArenaUpgradeService.Gold;

            for (int i = 0; i < _rows.Count; i++) if (_rows[i] != null) Destroy(_rows[i]);
            _rows.Clear();
            RefreshWeapons();

            var heroes = _arena.HeroStats.dataGroup;
            float y = 452f;
            for (int i = 0; i < heroes.Length; i++)
            {
                BuildRow(heroes[i].id, heroes[i].name, y);
                y -= 118f;
            }
        }

        private void RefreshWeapons()
        {
            if (_wLabels == null || _arena == null) return;
            var col = _arena.Weapons;
            for (int i = 0; i < _wLabels.Length; i++)
            {
                if (_wLabels[i] == null) continue;
                if (i < _arena.SlotCount)
                {
                    string id = _arena.SlotWeaponId(i);
                    _wLabels[i].text = col != null ? col.GetById(id).name : id;
                }
                else _wLabels[i].text = "-";
            }
        }

        private void BuildRow(string heroId, string name, float y)
        {
            var row = new GameObject("Row_" + heroId, typeof(RectTransform));
            row.transform.SetParent(_box, false);
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(840f, 120f);
            rt.anchoredPosition = new Vector2(0f, y);
            _rows.Add(row);

            bool current = heroId == _arena.HeroId;
            bool unlocked = ArenaHeroUnlockService.IsUnlocked(heroId);
            MakeText(row.transform, "Info", (current ? "> " : "") + name, 38, new Vector2(-175f, 0f), new Vector2(360f, 110f),
                TextAnchor.MiddleLeft, unlocked ? Color.white : new Color(0.6f, 0.6f, 0.65f, 1f));

            if (unlocked)
            {
                var b = MakeButton(row.transform, "Sel", current ? "ĐANG DÙNG" : "CHỌN", new Vector2(300f, 100f), new Vector2(255f, 0f), SelectBtn);
                b.interactable = !current;
                b.onClick.AddListener(() => { _arena.SetHero(heroId); Refresh(); });
            }
            else
            {
                bool can = ArenaHeroUnlockService.CanUnlock(heroId);
                string req = ArenaHeroUnlockService.Requirement(heroId);
                var b = MakeButton(row.transform, "Unlock", can ? ("MỞ " + req) : req, new Vector2(300f, 100f), new Vector2(255f, 0f), can ? UnlockBtn : LockedBtn);
                b.interactable = can;
                b.onClick.AddListener(() => { ArenaHeroUnlockService.TryUnlock(heroId); Refresh(); });
            }
        }

        #region UI helpers

        private Transform MakePanel(Transform parent, string name, Vector2 size, Vector2 pos, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = color;
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

        private Button MakeButton(Transform parent, string name, string label, Vector2 size, Vector2 pos, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = color;
            var lbl = MakeText(go.transform, "Label", label, 34, Vector2.zero, size, TextAnchor.MiddleCenter, Color.white);
            lbl.rectTransform.anchoredPosition = Vector2.zero;
            return go.GetComponent<Button>();
        }

        #endregion
    }
}
