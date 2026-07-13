#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Ezg.Feature.Gameplay.Battle;
using UnityEditor;
using UnityEngine;

namespace TurnBase.BattleEditor
{
    /// <summary>
    ///     Cửa sổ thiết kế hero cho module Battle: xem toàn bộ hero (Heroes.csv), sửa chỉ số/skill trực tiếp,
    ///     xem preview stat + lực chiến theo level/sao, thêm/nhân bản/xóa hero, và cấu hình đội hình mặc định
    ///     (DefaultTeamConfig SO). Ghi thẳng ra CSV (có backup .bak) rồi reload BattleDatabase.
    ///     Editor-only tool — không ảnh hưởng build.
    /// </summary>
    public class BattleHeroDesignerWindow : EditorWindow
    {
        #region Constants / Paths

        private const string CsvRel = "_Project/Features/Gameplay/Battle/Resources/BattleCsv";
        private const string TeamAssetPath = "Assets/_Project/Features/Gameplay/Battle/Resources/BattleDefaultTeam.asset";

        private static string CsvDir => Path.Combine(Application.dataPath, CsvRel);
        private static string HeroesCsv => Path.Combine(CsvDir, "Heroes.csv");
        private static string SkillsCsv => Path.Combine(CsvDir, "Skills.csv");

        #endregion

        #region State

        private CsvDoc _hero;
        private List<string> _skillIds = new List<string>();
        private DefaultTeamConfig _team;

        private int _tab;                 // 0 = Heroes, 1 = Default Team
        private int _sel = -1;
        private string _search = "";
        private ElementType _filterElement = ElementType.None; // None = tất cả
        private bool _onlyPlayable;
        private Vector2 _listScroll, _detailScroll, _teamScroll;
        private int _previewLevel = 60;
        private int _previewStar = 3;
        private bool _dirty;

        private static readonly string[] _elementNames = Enum.GetNames(typeof(ElementType));
        private static readonly string[] _classNames = Enum.GetNames(typeof(HeroClass));
        private static readonly string[] _evolveNames = Enum.GetNames(typeof(EvolutionPath));

        #endregion

        #region Open

        [MenuItem("TurnBase/Battle/Hero Designer %#h")]
        private static void Open()
        {
            var w = GetWindow<BattleHeroDesignerWindow>();
            w.titleContent = new GUIContent("Hero Designer");
            w.minSize = new Vector2(880, 520);
            w.ReloadAll();
        }

        private void OnEnable() => ReloadAll();

        private void ReloadAll()
        {
            try
            {
                _hero = CsvDoc.Load(HeroesCsv);
                _skillIds = LoadIdColumn(SkillsCsv);
                _team = AssetDatabase.LoadAssetAtPath<DefaultTeamConfig>(TeamAssetPath);
                if (!BattleDatabase.IsLoaded) BattleDatabase.Reload();
                _sel = Mathf.Clamp(_sel, -1, (_hero?.rows.Count ?? 0) - 1);
                _dirty = false;
            }
            catch (Exception e)
            {
                Debug.LogError("[HeroDesigner] Load lỗi: " + e);
            }
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            if (_hero == null)
            {
                EditorGUILayout.HelpBox("Không đọc được Heroes.csv.\n" + HeroesCsv, MessageType.Error);
                if (GUILayout.Button("Thử lại")) ReloadAll();
                return;
            }

            DrawToolbar();
            EditorGUILayout.Space(2);
            if (_tab == 0) DrawHeroesTab();
            else DrawTeamTab();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _tab = GUILayout.Toolbar(_tab, new[] { "Heroes (" + _hero.rows.Count + ")", "Default Team" },
                    EditorStyles.toolbarButton, GUILayout.Width(260));

                GUILayout.FlexibleSpace();

                if (_dirty) GUILayout.Label("● chưa lưu", Warn(), GUILayout.Width(80));

                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60))) ReloadAll();
                if (GUILayout.Button("Reload BattleDB", EditorStyles.toolbarButton, GUILayout.Width(110)))
                    BattleDatabase.Reload();

                using (new EditorGUI.DisabledScope(!_dirty))
                {
                    if (GUILayout.Button("Save CSV", EditorStyles.toolbarButton, GUILayout.Width(80))) SaveHeroes();
                }
            }
        }

        // ---------- Heroes tab ----------

        private void DrawHeroesTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawHeroList();
                DrawHeroDetail();
            }
        }

        private void DrawHeroList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(280)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                    if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22))) _search = "";
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Hệ", GUILayout.Width(24));
                    _filterElement = (ElementType)EditorGUILayout.EnumPopup(_filterElement);
                    _onlyPlayable = GUILayout.Toggle(_onlyPlayable, "hero_ only", "Button", GUILayout.Width(80));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("＋ New")) AddHero();
                    using (new EditorGUI.DisabledScope(_sel < 0))
                    {
                        if (GUILayout.Button("⧉ Dup")) DuplicateHero();
                        if (GUILayout.Button("🗑 Del")) DeleteHero();
                    }
                }

                using (var sv = new EditorGUILayout.ScrollViewScope(_listScroll,
                           "box", GUILayout.ExpandHeight(true)))
                {
                    _listScroll = sv.scrollPosition;
                    for (int i = 0; i < _hero.rows.Count; i++)
                    {
                        if (!PassFilter(i)) continue;
                        DrawListItem(i);
                    }
                }
            }
        }

        private void DrawListItem(int i)
        {
            var row = _hero.rows[i];
            var id = _hero.Get(row, "id");
            var name = _hero.Get(row, "name");
            var rarity = ParseInt(_hero.Get(row, "rarity"), 1);
            var el = ParseEnum(_hero.Get(row, "element"), ElementType.None);

            var selected = i == _sel;
            var style = selected ? Selected() : EditorStyles.label;
            using (new EditorGUILayout.HorizontalScope(selected ? "SelectionRect" : GUIStyle.none))
            {
                var rect = GUILayoutUtility.GetRect(10, 16, GUILayout.Width(10));
                EditorGUI.DrawRect(rect, ElementColor(el));

                if (GUILayout.Button(new string('★', Mathf.Clamp(rarity, 1, 6)) + " " + name,
                        style, GUILayout.ExpandWidth(true)))
                {
                    _sel = i;
                    GUI.FocusControl(null);
                }

                GUILayout.Label(id, EditorStyles.miniLabel, GUILayout.Width(90));
            }
        }

        private void DrawHeroDetail()
        {
            using (var sv = new EditorGUILayout.ScrollViewScope(_detailScroll, GUILayout.ExpandWidth(true)))
            {
                _detailScroll = sv.scrollPosition;

                if (_sel < 0 || _sel >= _hero.rows.Count)
                {
                    EditorGUILayout.HelpBox("Chọn 1 hero bên trái, hoặc bấm ＋ New.", MessageType.Info);
                    return;
                }

                var row = _hero.rows[_sel];

                Section("Định danh");
                Text(row, "id", "ID");
                Text(row, "name", "Tên");
                EnumField(row, "element", "Hệ", _elementNames);
                EnumField(row, "heroClass", "Class", _classNames);
                IntField(row, "rarity", "Rarity (sao gốc)");

                Section("Chỉ số gốc (lv0)");
                IntField(row, "baseHp", "Base HP");
                IntField(row, "baseAtk", "Base ATK");
                IntField(row, "baseDef", "Base DEF");
                IntField(row, "baseSpd", "Base SPD");

                Section("Tăng trưởng / cấp");
                FloatField(row, "growthHp", "Growth HP");
                FloatField(row, "growthAtk", "Growth ATK");
                FloatField(row, "growthDef", "Growth DEF");

                Section("Combat");
                FloatField(row, "critRate", "Crit Rate (0..1)");
                FloatField(row, "critDmg", "Crit Dmg (x)");
                FloatField(row, "damageReduction", "Dmg Reduction (0..0.9)");
                FloatField(row, "attackRange", "Attack Range (2.5=cận, 999=xa)");

                Section("Mana");
                FloatField(row, "maxMana", "Max Mana");
                FloatField(row, "startMana", "Start Mana");
                FloatField(row, "manaPerAttack", "Mana / hành động");
                FloatField(row, "manaOnHit", "Mana khi trúng đòn");

                Section("Skill");
                SkillField(row, "basicSkillId", "Basic");
                SkillField(row, "skill2Id", "Active");
                SkillField(row, "ultimateId", "Ultimate");

                Section("Model / Tiến hóa");
                Text(row, "modelKey", "Model (Resources/Heroes)");
                Text(row, "baseHeroId", "Base Hero Id (tiến hóa)");
                EnumField(row, "evolvePath", "Evolve Path", _evolveNames);

                EditorGUILayout.Space(6);
                DrawPreview(row);
                DrawValidation(row);
            }
        }

        private void DrawPreview(Dictionary<string, string> row)
        {
            Section("Preview stat (công thức thật)");
            using (new EditorGUILayout.HorizontalScope())
            {
                _previewLevel = EditorGUILayout.IntSlider("Level", _previewLevel, 0, 120);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _previewStar = EditorGUILayout.IntSlider("Star", _previewStar, 1, 6);
            }

            var h = RowToModel(row);
            if (!BattleDatabase.IsLoaded) BattleDatabase.Reload();

            float hp = HeroStatService.ComputeStat(h, _previewLevel, _previewStar, StatType.Hp);
            float atk = HeroStatService.ComputeStat(h, _previewLevel, _previewStar, StatType.Atk);
            float def = HeroStatService.ComputeStat(h, _previewLevel, _previewStar, StatType.Def);
            float spd = HeroStatService.ComputeStat(h, _previewLevel, _previewStar, StatType.Spd);
            float critR = HeroStatService.ComputeStat(h, _previewLevel, _previewStar, StatType.CritRate);
            float critD = HeroStatService.ComputeStat(h, _previewLevel, _previewStar, StatType.CritDmg);
            float dr = h.damageReduction;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                StatRow("HP", Mathf.RoundToInt(hp).ToString("N0"), "ATK", Mathf.RoundToInt(atk).ToString("N0"));
                StatRow("DEF", Mathf.RoundToInt(def).ToString("N0"), "SPD", Mathf.RoundToInt(spd).ToString("N0"));
                StatRow("Crit", (critR * 100f).ToString("0.#") + "% x" + critD.ToString("0.0"),
                    "DmgRed", (dr * 100f).ToString("0.#") + "%");

                var power = CombatPower(hp, atk, def, spd, critR, critD, dr);
                EditorGUILayout.Space(2);
                var big = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
                EditorGUILayout.LabelField("⚔ Lực chiến: " + power.ToString("N0"), big);
                EditorGUILayout.LabelField(
                    "(HP hiển thị chưa nhân BattleTuning.HpMultiplier x" + BattleTuning.HpMultiplier + " lúc chạy)",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawValidation(Dictionary<string, string> row)
        {
            var msgs = new List<string>();
            var id = _hero.Get(row, "id");
            if (string.IsNullOrEmpty(id)) msgs.Add("ID trống.");
            else if (_hero.rows.Count(r => _hero.Get(r, "id") == id) > 1) msgs.Add("ID trùng: " + id);

            foreach (var col in new[] { "basicSkillId", "skill2Id", "ultimateId" })
            {
                var sid = _hero.Get(row, col);
                if (!string.IsNullOrEmpty(sid) && !_skillIds.Contains(sid))
                    msgs.Add(col + " trỏ skill không tồn tại: " + sid);
            }

            if (string.IsNullOrEmpty(_hero.Get(row, "modelKey")))
                msgs.Add("Chưa set modelKey (sẽ dùng placeholder).");

            if (msgs.Count > 0)
                EditorGUILayout.HelpBox(string.Join("\n", msgs), MessageType.Warning);
            else
                EditorGUILayout.HelpBox("Hợp lệ ✓", MessageType.Info);
        }

        // ---------- Team tab ----------

        private void DrawTeamTab()
        {
            if (_team == null)
            {
                EditorGUILayout.HelpBox("Không tìm thấy DefaultTeamConfig tại\n" + TeamAssetPath, MessageType.Error);
                if (GUILayout.Button("Reload")) ReloadAll();
                return;
            }

            var heroIds = _hero.rows.Select(r => _hero.Get(r, "id"))
                .Where(id => id.StartsWith("hero_")).ToList();
            var options = new List<string> { "(none)" };
            options.AddRange(heroIds);
            var arr = options.ToArray();

            using (var sv = new EditorGUILayout.ScrollViewScope(_teamScroll))
            {
                _teamScroll = sv.scrollPosition;

                Section("Đội hình mặc định (DefaultTeamConfig)");

                EditorGUI.BeginChangeCheck();

                _team.mainHeroId = PopupString("Main Hero", _team.mainHeroId, arr);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Slots", EditorStyles.boldLabel);

                if (_team.team == null) _team.team = new List<DefaultTeamConfig.Entry>();

                int removeAt = -1;
                for (int i = 0; i < _team.team.Count; i++)
                {
                    var e = _team.team[i];
                    using (new EditorGUILayout.HorizontalScope("box"))
                    {
                        GUILayout.Label((i == 0 ? "★MAIN" : "slot " + i), GUILayout.Width(52));
                        e.heroId = PopupString(null, e.heroId, arr, 150);
                        GUILayout.Label("★", GUILayout.Width(12));
                        e.star = EditorGUILayout.IntSlider(e.star <= 0 ? 1 : e.star, 1, 6, GUILayout.Width(160));
                        GUILayout.Label("Lv", GUILayout.Width(20));
                        e.level = Mathf.Max(1, EditorGUILayout.IntField(e.level <= 0 ? 1 : e.level, GUILayout.Width(46)));
                        if (GUILayout.Button("✕", GUILayout.Width(24))) removeAt = i;
                    }

                    _team.team[i] = e;
                }

                if (removeAt >= 0) _team.team.RemoveAt(removeAt);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(_team.team.Count >= 6))
                    {
                        if (GUILayout.Button("＋ Thêm slot", GUILayout.Width(120)))
                            _team.team.Add(new DefaultTeamConfig.Entry { heroId = heroIds.FirstOrDefault() ?? "", star = 3, level = 60 });
                    }
                }

                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(_team);

                EditorGUILayout.Space(6);
                if (GUILayout.Button("💾 Save Team Config", GUILayout.Height(26)))
                {
                    EditorUtility.SetDirty(_team);
                    AssetDatabase.SaveAssets();
                    ShowNotification(new GUIContent("Đã lưu đội hình mặc định"));
                }
            }
        }

        #endregion

        #region Actions

        private void AddHero()
        {
            var row = _hero.NewRow();
            foreach (var kv in DefaultHero()) row[kv.Key] = kv.Value;
            row["id"] = NextHeroId();
            _hero.rows.Add(row);
            _sel = _hero.rows.Count - 1;
            _dirty = true;
        }

        private void DuplicateHero()
        {
            if (_sel < 0) return;
            var src = _hero.rows[_sel];
            var row = _hero.NewRow();
            foreach (var col in _hero.header) row[col] = _hero.Get(src, col);
            row["id"] = NextHeroId();
            row["name"] = _hero.Get(src, "name") + "_copy";
            _hero.rows.Insert(_sel + 1, row);
            _sel += 1;
            _dirty = true;
        }

        private void DeleteHero()
        {
            if (_sel < 0) return;
            var id = _hero.Get(_hero.rows[_sel], "id");
            if (!EditorUtility.DisplayDialog("Xóa hero", "Xóa '" + id + "' khỏi Heroes.csv?", "Xóa", "Hủy")) return;
            _hero.rows.RemoveAt(_sel);
            _sel = Mathf.Clamp(_sel, -1, _hero.rows.Count - 1);
            _dirty = true;
        }

        private void SaveHeroes()
        {
            try
            {
                _hero.Save(HeroesCsv);
                AssetDatabase.Refresh();
                BattleDatabase.Reload();
                _dirty = false;
                ShowNotification(new GUIContent("Đã lưu Heroes.csv + reload DB"));
            }
            catch (Exception e)
            {
                Debug.LogError("[HeroDesigner] Save lỗi: " + e);
                EditorUtility.DisplayDialog("Lỗi", "Save thất bại:\n" + e.Message, "OK");
            }
        }

        private string NextHeroId()
        {
            int max = 1000;
            foreach (var r in _hero.rows)
            {
                var id = _hero.Get(r, "id");
                if (id.StartsWith("hero_") && int.TryParse(id.Substring(5), out var n) && n > max) max = n;
            }

            return "hero_" + (max + 1);
        }

        private static Dictionary<string, string> DefaultHero() => new Dictionary<string, string>
        {
            { "name", "NewHero" }, { "element", "Fire" }, { "heroClass", "Warrior" }, { "rarity", "3" },
            { "baseHp", "1000" }, { "baseAtk", "120" }, { "baseDef", "60" }, { "baseSpd", "95" },
            { "critRate", "0.1" }, { "critDmg", "1.5" }, { "damageReduction", "0" },
            { "growthHp", "70" }, { "growthAtk", "9" }, { "growthDef", "4" },
            { "basicSkillId", "skill_2001" }, { "skill2Id", "" }, { "ultimateId", "" },
            { "modelKey", "Seer1" }, { "baseHeroId", "" }, { "evolvePath", "None" },
            { "maxMana", "100" }, { "startMana", "30" }, { "manaPerAttack", "25" }, { "manaOnHit", "12" },
            { "attackRange", "2.5" }
        };

        #endregion

        #region Field widgets

        private void Text(Dictionary<string, string> row, string col, string label)
        {
            EditorGUI.BeginChangeCheck();
            var v = EditorGUILayout.TextField(label, _hero.Get(row, col));
            if (EditorGUI.EndChangeCheck()) { row[col] = v; _dirty = true; }
        }

        private void IntField(Dictionary<string, string> row, string col, string label)
        {
            EditorGUI.BeginChangeCheck();
            var v = EditorGUILayout.IntField(label, ParseInt(_hero.Get(row, col), 0));
            if (EditorGUI.EndChangeCheck()) { row[col] = v.ToString(CultureInfo.InvariantCulture); _dirty = true; }
        }

        private void FloatField(Dictionary<string, string> row, string col, string label)
        {
            EditorGUI.BeginChangeCheck();
            var v = EditorGUILayout.FloatField(label, ParseFloat(_hero.Get(row, col), 0f));
            if (EditorGUI.EndChangeCheck()) { row[col] = v.ToString(CultureInfo.InvariantCulture); _dirty = true; }
        }

        private void EnumField(Dictionary<string, string> row, string col, string label, string[] names)
        {
            var cur = Mathf.Max(0, Array.IndexOf(names, _hero.Get(row, col)));
            EditorGUI.BeginChangeCheck();
            var idx = EditorGUILayout.Popup(label, cur, names);
            if (EditorGUI.EndChangeCheck()) { row[col] = names[idx]; _dirty = true; }
        }

        private void SkillField(Dictionary<string, string> row, string col, string label)
        {
            var opts = new List<string> { "(none)" };
            opts.AddRange(_skillIds);
            var cur = _hero.Get(row, col);
            var idx = string.IsNullOrEmpty(cur) ? 0 : Mathf.Max(0, opts.IndexOf(cur));
            EditorGUI.BeginChangeCheck();
            var sel = EditorGUILayout.Popup(label, idx, opts.ToArray());
            if (EditorGUI.EndChangeCheck()) { row[col] = sel == 0 ? "" : opts[sel]; _dirty = true; }
        }

        private static string PopupString(string label, string value, string[] options, float width = 0)
        {
            var idx = string.IsNullOrEmpty(value) ? 0 : Mathf.Max(0, Array.IndexOf(options, value));
            int sel;
            if (label != null) sel = EditorGUILayout.Popup(label, idx, options);
            else if (width > 0) sel = EditorGUILayout.Popup(idx, options, GUILayout.Width(width));
            else sel = EditorGUILayout.Popup(idx, options);
            return sel == 0 ? "" : options[sel];
        }

        private static void StatRow(string a, string av, string b, string bv)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(a, av, GUILayout.MinWidth(120));
                EditorGUILayout.LabelField(b, bv, GUILayout.MinWidth(120));
            }
        }

        private static void Section(string title)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            var r = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(1, 1, 1, 0.1f));
        }

        #endregion

        #region Helpers

        private bool PassFilter(int i)
        {
            var row = _hero.rows[i];
            var id = _hero.Get(row, "id");
            if (_onlyPlayable && !id.StartsWith("hero_")) return false;
            if (_filterElement != ElementType.None &&
                ParseEnum(_hero.Get(row, "element"), ElementType.None) != _filterElement) return false;
            if (!string.IsNullOrEmpty(_search))
            {
                var q = _search.ToLowerInvariant();
                if (!id.ToLowerInvariant().Contains(q) &&
                    !_hero.Get(row, "name").ToLowerInvariant().Contains(q)) return false;
            }

            return true;
        }

        private HeroModel RowToModel(Dictionary<string, string> row) => new HeroModel
        {
            id = _hero.Get(row, "id"),
            name = _hero.Get(row, "name"),
            element = ParseEnum(_hero.Get(row, "element"), ElementType.None),
            heroClass = ParseEnum(_hero.Get(row, "heroClass"), HeroClass.None),
            rarity = ParseInt(_hero.Get(row, "rarity"), 1),
            baseHp = ParseInt(_hero.Get(row, "baseHp"), 0),
            baseAtk = ParseInt(_hero.Get(row, "baseAtk"), 0),
            baseDef = ParseInt(_hero.Get(row, "baseDef"), 0),
            baseSpd = ParseInt(_hero.Get(row, "baseSpd"), 0),
            critRate = ParseFloat(_hero.Get(row, "critRate"), 0f),
            critDmg = ParseFloat(_hero.Get(row, "critDmg"), 1f),
            damageReduction = ParseFloat(_hero.Get(row, "damageReduction"), 0f),
            growthHp = ParseFloat(_hero.Get(row, "growthHp"), 0f),
            growthAtk = ParseFloat(_hero.Get(row, "growthAtk"), 0f),
            growthDef = ParseFloat(_hero.Get(row, "growthDef"), 0f),
            attackRange = ParseFloat(_hero.Get(row, "attackRange"), 2.5f)
        };

        /// <summary>Lực chiến — tổng có trọng số (chuẩn mobile RPG) trên stat thiết kế.</summary>
        private static int CombatPower(float hp, float atk, float def, float spd, float critR, float critD, float dr)
        {
            var critMult = 1f + Mathf.Clamp01(critR) * Mathf.Max(0f, critD - 1f);
            var offense = atk * critMult * (1f + spd / 400f);
            var effHp = hp * (1f + def / 500f) * (1f + Mathf.Clamp(dr, 0f, 0.9f));
            var power = offense * 1.8f + effHp * 0.12f;
            return Mathf.RoundToInt(power);
        }

        private static List<string> LoadIdColumn(string path)
        {
            var list = new List<string>();
            var doc = CsvDoc.Load(path);
            if (doc == null) return list;
            foreach (var r in doc.rows)
            {
                var id = doc.Get(r, "id");
                if (!string.IsNullOrEmpty(id)) list.Add(id);
            }

            return list;
        }

        private static int ParseInt(string s, int def) =>
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : def;

        private static float ParseFloat(string s, float def) =>
            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;

        private static T ParseEnum<T>(string s, T def) where T : struct =>
            Enum.TryParse<T>(s, true, out var v) ? v : def;

        private static Color ElementColor(ElementType e)
        {
            switch (e)
            {
                case ElementType.Fire: return new Color(0.90f, 0.30f, 0.20f);
                case ElementType.Water: return new Color(0.25f, 0.55f, 0.95f);
                case ElementType.Wind: return new Color(0.35f, 0.80f, 0.45f);
                case ElementType.Light: return new Color(0.95f, 0.85f, 0.35f);
                case ElementType.Dark: return new Color(0.55f, 0.35f, 0.80f);
                default: return Color.gray;
            }
        }

        private static GUIStyle Selected()
        {
            var s = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
            return s;
        }

        private static GUIStyle Warn()
        {
            var s = new GUIStyle(EditorStyles.boldLabel);
            s.normal.textColor = new Color(0.95f, 0.6f, 0.1f);
            return s;
        }

        #endregion

        #region CSV document (preserve comments + header order)

        /// <summary>Đọc/ghi CSV giữ nguyên các dòng comment (#) + header, mỗi record = dict theo tên cột.</summary>
        private class CsvDoc
        {
            public readonly List<string> preamble = new List<string>(); // các dòng trước header (comment/blank)
            public string[] header = new string[0];
            public readonly List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();

            public string Get(Dictionary<string, string> row, string col) =>
                row != null && row.TryGetValue(col, out var v) ? v : "";

            public Dictionary<string, string> NewRow()
            {
                var d = new Dictionary<string, string>();
                foreach (var c in header) d[c] = "";
                return d;
            }

            public static CsvDoc Load(string path)
            {
                if (!File.Exists(path)) return null;
                var doc = new CsvDoc();
                var lines = File.ReadAllLines(path);
                var headerParsed = false;
                foreach (var raw in lines)
                {
                    if (!headerParsed)
                    {
                        if (string.IsNullOrWhiteSpace(raw) || raw.TrimStart().StartsWith("#"))
                        {
                            doc.preamble.Add(raw);
                            continue;
                        }

                        doc.header = raw.Split(',').Select(c => c.Trim()).ToArray();
                        headerParsed = true;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(raw) || raw.TrimStart().StartsWith("#")) continue;
                    var cells = raw.Split(',');
                    var row = new Dictionary<string, string>();
                    for (int i = 0; i < doc.header.Length; i++)
                        row[doc.header[i]] = i < cells.Length ? cells[i].Trim() : "";
                    doc.rows.Add(row);
                }

                return doc;
            }

            public void Save(string path)
            {
                if (File.Exists(path)) File.Copy(path, path + ".bak", true); // backup

                var sb = new StringBuilder();
                foreach (var p in preamble) sb.Append(p).Append('\n');
                sb.Append(string.Join(",", header)).Append('\n');
                foreach (var row in rows)
                {
                    for (int i = 0; i < header.Length; i++)
                    {
                        if (i > 0) sb.Append(',');
                        var v = row.TryGetValue(header[i], out var vv) ? vv : "";
                        sb.Append(v == null ? "" : v.Replace(",", "_")); // giá trị không được chứa dấu phẩy
                    }

                    sb.Append('\n');
                }

                File.WriteAllText(path, sb.ToString());
            }
        }

        #endregion
    }
}
#endif
