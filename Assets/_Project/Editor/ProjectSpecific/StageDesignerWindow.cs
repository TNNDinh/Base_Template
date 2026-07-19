using System.Collections.Generic;
using System.Text;
using Ezg.Feature.Gameplay.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ezg.EditorTools
{
    /// <summary>
    ///     Designer stage arena: sửa stage (id/name/maxRounds) + cấu hình enemy spawn theo round, lưu CSV,
    ///     và <b>Test nhanh</b> (set stage → mở BattleScene → vào Play). Mở: menu <b>Battle/Arena/Stage Designer</b>.
    /// </summary>
    public class StageDesignerWindow : EditorWindow
    {
        private const string Dir = "Assets/_Project/Features/Gameplay/Battle/Resources/ArenaCsv/";
        private const string ScenePath = "Assets/_Project/Scenes/BattleScene.unity";

        private readonly List<ArenaStageModel> _stages = new List<ArenaStageModel>();
        private readonly List<ArenaSpawnModel> _spawns = new List<ArenaSpawnModel>();
        private int _sel = -1;
        private string[] _enemyIds = new string[0];
        private string[] _patternIds = new string[0];
        private Vector2 _scroll;

        private ArenaPrefabRegistry _registry;
        private readonly RadialGridConfig _cfg = new RadialGridConfig();
        private RadialGrid _grid;
        private int _previewRound = 1;
        private Material _mat;

        [MenuItem("Battle/Arena/Stage Designer")]
        private static void Open()
        {
            var w = GetWindow<StageDesignerWindow>("Stage Designer");
            w.minSize = new Vector2(560f, 560f);
        }

        private void OnEnable() => Reload();

        private void Reload()
        {
            _stages.Clear();
            _spawns.Clear();

            var st = ReadCsv("ArenaStages");
            if (st != null)
                for (int i = 0; i < st.Rows.Count; i++)
                {
                    var r = new CsvRow(st, st.Rows[i]);
                    _stages.Add(new ArenaStageModel { id = r.Str("id"), name = r.Str("name"), maxRounds = r.Int("maxRounds") });
                }

            var sp = ReadCsv("ArenaSpawns");
            if (sp != null)
                for (int i = 0; i < sp.Rows.Count; i++)
                {
                    var r = new CsvRow(sp, sp.Rows[i]);
                    _spawns.Add(new ArenaSpawnModel
                    {
                        stageId = r.Str("stageId"), round = r.Int("round"), enemyId = r.Str("enemyId"),
                        ring = r.Int("ring"), sector = r.Int("sector"), count = r.Int("count"),
                        patternId = r.Str("patternId"), wanderPatternId = r.Str("wanderPatternId"),
                        level = r.Int("level"), hpMul = r.Float("hpMul"), atkMul = r.Float("atkMul"), defMul = r.Float("defMul")
                    });
                }

            _enemyIds = Ids(AssetDatabase.LoadAssetAtPath<EnemyStatCollection>(Dir + "EnemyStatCollection.asset")?.dataGroup);
            _patternIds = PatternIds(AssetDatabase.LoadAssetAtPath<EnemyMovePatternCollection>(Dir + "EnemyMovePatternCollection.asset")?.dataGroup);
            _registry = AssetDatabase.LoadAssetAtPath<ArenaPrefabRegistry>(Dir + "ArenaPrefabRegistry.asset");
            _grid = new RadialGrid(_cfg);
            _sel = _stages.Count > 0 ? 0 : -1;
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload")) Reload();
                if (GUILayout.Button("＋ New Stage")) NewStage();
                using (new EditorGUI.DisabledScope(_sel < 0))
                    if (GUILayout.Button("🗑 Delete Stage")) DeleteStage();
            }

            if (_stages.Count == 0)
            {
                EditorGUILayout.HelpBox("Chưa có stage. Bấm ＋ New Stage.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            var names = new string[_stages.Count];
            for (int i = 0; i < _stages.Count; i++) names[i] = $"{_stages[i].id}  ({_stages[i].name})";
            _sel = EditorGUILayout.Popup("Stage", Mathf.Clamp(_sel, 0, _stages.Count - 1), names);

            var s = _stages[_sel];
            s.id = EditorGUILayout.TextField("id", s.id);
            s.name = EditorGUILayout.TextField("name", s.name);
            s.maxRounds = EditorGUILayout.IntField("maxRounds", s.maxRounds);
            _stages[_sel] = s;

            EditorGUILayout.Space(6);
            DrawSpawns(s.id);

            EditorGUILayout.Space(8);
            DrawGridPreview(s.id);

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);
                if (GUILayout.Button("💾  Save CSV", GUILayout.Height(30f))) Save();
                GUI.backgroundColor = new Color(0.5f, 0.75f, 1f);
                if (GUILayout.Button("▶  Test nhanh (Save + Play)", GUILayout.Height(30f))) QuickTest();
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSpawns(string stageId)
        {
            EditorGUILayout.LabelField("Enemy spawn theo round", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("round", GUILayout.Width(45));
                EditorGUILayout.LabelField("enemy", GUILayout.Width(90));
                EditorGUILayout.LabelField("ring", GUILayout.Width(38));
                EditorGUILayout.LabelField("sector", GUILayout.Width(45));
                EditorGUILayout.LabelField("count", GUILayout.Width(42));
                EditorGUILayout.LabelField("pattern", GUILayout.Width(120));
                EditorGUILayout.LabelField("lvl", GUILayout.Width(30));
                EditorGUILayout.LabelField("", GUILayout.Width(24));
            }

            for (int i = 0; i < _spawns.Count; i++)
            {
                if (_spawns[i].stageId != stageId) continue;
                var sp = _spawns[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    sp.round = EditorGUILayout.IntField(sp.round, GUILayout.Width(45));
                    sp.enemyId = PopupOrText(sp.enemyId, _enemyIds, 90);
                    DrawEnemyThumb(sp.enemyId, 26);
                    sp.ring = EditorGUILayout.IntField(sp.ring, GUILayout.Width(38));
                    sp.sector = EditorGUILayout.IntField(sp.sector, GUILayout.Width(45));
                    sp.count = EditorGUILayout.IntField(sp.count, GUILayout.Width(42));
                    sp.patternId = PopupOrText(sp.patternId, _patternIds, 120);
                    sp.level = EditorGUILayout.IntField(Mathf.Max(1, sp.level), GUILayout.Width(30));
                    _spawns[i] = sp;
                    if (GUILayout.Button("✕", GUILayout.Width(24))) { _spawns.RemoveAt(i); GUIUtility.ExitGUI(); }
                }
            }

            if (GUILayout.Button("＋ Thêm dòng spawn", GUILayout.Width(180)))
                _spawns.Add(new ArenaSpawnModel
                {
                    stageId = stageId, round = 1, enemyId = _enemyIds.Length > 0 ? _enemyIds[0] : "",
                    ring = 5, sector = 0, count = 1,
                    patternId = _patternIds.Length > 0 ? _patternIds[0] : "", wanderPatternId = "wander_circle",
                    level = 1, hpMul = 1, atkMul = 1, defMul = 1
                });
        }

        private static string PopupOrText(string value, string[] options, float width)
        {
            if (options == null || options.Length == 0)
                return EditorGUILayout.TextField(value, GUILayout.Width(width));
            int idx = System.Array.IndexOf(options, value);
            if (idx < 0) idx = 0;
            int newIdx = EditorGUILayout.Popup(idx, options, GUILayout.Width(width));
            return options[Mathf.Clamp(newIdx, 0, options.Length - 1)];
        }

        private void NewStage()
        {
            _stages.Add(new ArenaStageModel { id = "arena_1_1", name = "New Stage", maxRounds = 15 });
            _sel = _stages.Count - 1;
        }

        private void DeleteStage()
        {
            if (_sel < 0) return;
            string id = _stages[_sel].id;
            _spawns.RemoveAll(x => x.stageId == id);
            _stages.RemoveAt(_sel);
            _sel = _stages.Count > 0 ? 0 : -1;
        }

        private void Save()
        {
            var st = new StringBuilder();
            st.AppendLine("# id,name,maxRounds");
            st.AppendLine("id,name,maxRounds");
            foreach (var s in _stages) st.AppendLine($"{s.id},{s.name},{s.maxRounds}");
            System.IO.File.WriteAllText(Dir + "ArenaStages.csv", st.ToString());

            var sp = new StringBuilder();
            sp.AppendLine("stageId,round,enemyId,ring,sector,count,patternId,wanderPatternId,level,hpMul,atkMul,defMul");
            foreach (var x in _spawns)
                sp.AppendLine($"{x.stageId},{x.round},{x.enemyId},{x.ring},{x.sector},{x.count},{x.patternId},{x.wanderPatternId},{x.level},{Mul(x.hpMul)},{Mul(x.atkMul)},{Mul(x.defMul)}");
            System.IO.File.WriteAllText(Dir + "ArenaSpawns.csv", sp.ToString());

            AssetDatabase.ImportAsset(Dir + "ArenaStages.csv");
            AssetDatabase.ImportAsset(Dir + "ArenaSpawns.csv");

            RebuildStageCollection();
            RebuildSpawnCollection();
            AssetDatabase.SaveAssets();
            Debug.Log($"[StageDesigner] Đã lưu {_stages.Count} stage + {_spawns.Count} spawn.");
        }

        private static float Mul(float v) => v <= 0f ? 1f : v;

        private void RebuildStageCollection()
        {
            var c = ScriptableObject.CreateInstance<ArenaStageCollection>();
            c.dataGroup = _stages.ToArray();
            c.Convert();
            SaveAsset(c, "ArenaStageCollection");
        }

        private void RebuildSpawnCollection()
        {
            var c = ScriptableObject.CreateInstance<ArenaSpawnCollection>();
            c.dataGroup = _spawns.ToArray();
            c.Convert();
            SaveAsset(c, "ArenaSpawnCollection");
        }

        private static void SaveAsset(ScriptableObject so, string name)
        {
            var p = Dir + name + ".asset";
            AssetDatabase.DeleteAsset(p);
            AssetDatabase.CreateAsset(so, p);
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private void QuickTest()
        {
            if (_sel < 0) return;
            Save();

            SessionState.SetString(ArenaStageTestLauncher.Key, _stages[_sel].id); // bootstrap set PendingStageId sau khi vào play

            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
            }

            EditorApplication.EnterPlaymode();
        }

        #region Preview ảnh + round

        private void DrawEnemyThumb(string enemyId, float size)
        {
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            var tex = EnemyThumb(enemyId);
            if (tex != null) GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
            else EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.08f));
        }

        private Texture EnemyThumb(string enemyId)
        {
            var prefab = _registry != null ? _registry.Enemy(enemyId) : null;
            if (prefab == null) return null;
            var t = AssetPreview.GetAssetPreview(prefab);
            if (t == null) Repaint(); // preview đang render
            return t;
        }

        private void DrawGridPreview(string stageId)
        {
            int maxRound = 1;
            foreach (var sp in _spawns)
                if (sp.stageId == stageId && sp.round > maxRound) maxRound = sp.round;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Xem theo round (dồn tới round):", EditorStyles.boldLabel, GUILayout.Width(210));
                if (GUILayout.Button("◀", GUILayout.Width(30))) _previewRound = Mathf.Max(1, _previewRound - 1);
                _previewRound = EditorGUILayout.IntSlider(Mathf.Clamp(_previewRound, 1, maxRound), 1, maxRound);
                if (GUILayout.Button("▶", GUILayout.Width(30))) _previewRound = Mathf.Min(maxRound, _previewRound + 1);
            }

            var rect = GUILayoutUtility.GetRect(position.width - 24f, 320f);
            if (Event.current.type != EventType.Repaint) return;

            DrawGrid(rect);

            Vector2 center = new Vector2(rect.center.x, rect.center.y);
            float maxR = Mathf.Min(rect.width, rect.height) * 0.46f;
            float scale = maxR / Mathf.Max(0.01f, _cfg.OuterRadius);
            if (_grid == null) _grid = new RadialGrid(_cfg);

            foreach (var sp in _spawns)
            {
                if (sp.stageId != stageId || sp.round > _previewRound) continue;
                int ring = Mathf.Clamp(sp.ring, 0, _cfg.RingCount - 1);
                int sector = ((sp.sector % _cfg.SectorsPerRing) + _cfg.SectorsPerRing) % _cfg.SectorsPerRing;
                var local = _grid.GetCell(ring, sector).LocalCenter;
                Vector2 p = center + new Vector2(local.x, -local.y) * scale;

                bool current = sp.round == _previewRound;
                var tex = EnemyThumb(sp.enemyId);
                float sz = 40f;
                var ir = new Rect(p.x - sz * 0.5f, p.y - sz * 0.5f, sz, sz);
                if (tex != null)
                {
                    var c = GUI.color;
                    GUI.color = current ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                    GUI.DrawTexture(ir, tex, ScaleMode.ScaleToFit);
                    GUI.color = c;
                }
                else
                {
                    EditorGUI.DrawRect(new Rect(p.x - 6, p.y - 6, 12, 12), current ? Color.red : new Color(1f, 0f, 0f, 0.4f));
                }

                if (sp.count > 1) GUI.Label(new Rect(p.x + 6, p.y + 6, 30, 16), "x" + sp.count);
            }
        }

        private void DrawGrid(Rect rect)
        {
            EnsureMaterial();
            Vector2 center = new Vector2(rect.center.x, rect.center.y);
            float maxR = Mathf.Min(rect.width, rect.height) * 0.46f;
            float hole = maxR * 0.12f;
            float thick = (maxR - hole) / _cfg.RingCount;
            float step = Mathf.PI * 2f / _cfg.SectorsPerRing;

            GL.PushMatrix();
            _mat.SetPass(0);
            GL.LoadPixelMatrix();
            for (int ring = 0; ring < _cfg.RingCount; ring++)
            {
                float r0 = hole + ring * thick + 1f;
                float r1 = hole + (ring + 1) * thick - 1f;
                for (int spk = 0; spk < _cfg.SectorsPerRing; spk++)
                {
                    var col = new Color(0.4f, 0.72f, 0.52f, ring % 2 == 0 ? 0.13f : 0.07f);
                    DrawSector(center, r0, r1, spk * step + step * 0.06f, (spk + 1) * step - step * 0.06f, col);
                }
            }

            DrawDisc(center, hole * 0.9f, new Color(0.3f, 0.62f, 1f, 0.85f)); // hero ở tâm
            GL.PopMatrix();
        }

        private static void DrawSector(Vector2 c, float r0, float r1, float a0, float a1, Color col)
        {
            int seg = Mathf.Max(2, Mathf.CeilToInt((a1 - a0) / 0.14f));
            GL.Begin(GL.TRIANGLES);
            GL.Color(col);
            for (int i = 0; i < seg; i++)
            {
                float t0 = Mathf.Lerp(a0, a1, (float)i / seg);
                float t1 = Mathf.Lerp(a0, a1, (float)(i + 1) / seg);
                Vector2 i0 = Polar(c, r0, t0), o0 = Polar(c, r1, t0), i1 = Polar(c, r0, t1), o1 = Polar(c, r1, t1);
                Vert(i0); Vert(o0); Vert(o1);
                Vert(i0); Vert(o1); Vert(i1);
            }

            GL.End();
        }

        private static void DrawDisc(Vector2 c, float r, Color col)
        {
            const int seg = 24;
            GL.Begin(GL.TRIANGLES);
            GL.Color(col);
            for (int i = 0; i < seg; i++)
            {
                float t0 = (float)i / seg * Mathf.PI * 2f;
                float t1 = (float)(i + 1) / seg * Mathf.PI * 2f;
                Vert(c); Vert(Polar(c, r, t0)); Vert(Polar(c, r, t1));
            }

            GL.End();
        }

        private static Vector2 Polar(Vector2 c, float r, float a) => new Vector2(c.x + Mathf.Cos(a) * r, c.y - Mathf.Sin(a) * r);
        private static void Vert(Vector2 p) => GL.Vertex3(p.x, p.y, 0f);

        private void EnsureMaterial()
        {
            if (_mat != null) return;
            _mat = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
            _mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _mat.SetInt("_ZWrite", 0);
        }

        #endregion

        private static string[] Ids(UnitStatModel[] arr)
        {
            if (arr == null) return new string[0];
            var l = new string[arr.Length];
            for (int i = 0; i < arr.Length; i++) l[i] = arr[i].id;
            return l;
        }

        private static string[] PatternIds(EnemyMovePatternModel[] arr)
        {
            if (arr == null) return new string[0];
            var l = new string[arr.Length];
            for (int i = 0; i < arr.Length; i++) l[i] = arr[i].id;
            return l;
        }

        private static CsvTable ReadCsv(string name)
        {
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(Dir + name + ".csv");
            return ta == null ? null : new CsvTable(ta.text);
        }
    }

    /// <summary>
    ///     Cầu nối "Test nhanh": entering play reset static nên set <see cref="ArenaLaunch.PendingStageId" />
    ///     sau khi ĐÃ vào play mode (đọc từ SessionState — sống sót qua domain reload).
    /// </summary>
    [InitializeOnLoad]
    public static class ArenaStageTestLauncher
    {
        public const string Key = "ArenaTestStageId";

        static ArenaStageTestLauncher()
        {
            EditorApplication.playModeStateChanged += OnChange;
        }

        private static void OnChange(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            var id = SessionState.GetString(Key, "");
            if (string.IsNullOrEmpty(id)) return;
            ArenaLaunch.PendingStageId = id;
            SessionState.EraseString(Key);
        }
    }
}
