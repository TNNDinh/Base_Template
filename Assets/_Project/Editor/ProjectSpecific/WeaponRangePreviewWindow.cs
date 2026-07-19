using System.Collections.Generic;
using System.Text;
using Ezg.Feature.Gameplay.Battle;
using UnityEditor;
using UnityEngine;

namespace Ezg.EditorTools
{
    /// <summary>
    ///     Designer vũ khí: vẽ lưới tròn (ring × sector), CLICK ô để chọn tầm đánh (triggerShape) tương
    ///     đối hướng nhắm, sửa các stat, xoay xem trước, và LƯU THẲNG vào <c>Resources/ArenaCsv/Weapons.csv</c>
    ///     (đồng thời cập nhật <see cref="WeaponCollection" /> asset). Mở: menu <b>Battle/Arena/Weapon Designer</b>.
    /// </summary>
    public class WeaponRangePreviewWindow : EditorWindow
    {
        private const float RotateStepSeconds = 0.2f;
        private const string CsvPath = "Assets/_Project/Features/Gameplay/Battle/Resources/ArenaCsv/Weapons.csv";
        private const string CollectionPath = "Assets/_Project/Features/Gameplay/Battle/Resources/ArenaCsv/WeaponCollection.asset";

        private WeaponCollection _weapons;
        private TextAsset _csv; // file CSV được link — nguồn config, Save ghi thẳng vào đây
        private readonly List<WeaponModel> _list = new List<WeaponModel>();
        private int _sel = -1;

        // weapon đang sửa
        private WeaponModel _cur;
        private readonly HashSet<Vector2Int> _cells = new HashSet<Vector2Int>(); // (lateral, forward) tương đối hướng nhắm

        private int _ringCount = 6;
        private int _sectors = 24;
        private int _facing;
        private bool _autoRotate;
        private bool _editMode = true;

        private double _lastStep;
        private Material _mat;
        private Rect _gridRect;
        private Vector2 _scroll;

        [MenuItem("Battle/Arena/Weapon Designer")]
        private static void Open()
        {
            var w = GetWindow<WeaponRangePreviewWindow>("Weapon Designer");
            w.minSize = new Vector2(460f, 680f);
        }

        private void OnEnable()
        {
            if (_weapons == null) _weapons = FindCollection();
            if (_csv == null) _csv = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvPath);
            ReloadList();
            EditorApplication.update += OnEditorUpdate;
            _lastStep = EditorApplication.timeSinceStartup;
        }

        private void OnDisable() => EditorApplication.update -= OnEditorUpdate;

        private void OnEditorUpdate()
        {
            if (!_autoRotate || _sectors <= 0) return;
            var now = EditorApplication.timeSinceStartup;
            if (now - _lastStep < RotateStepSeconds) return;
            _lastStep = now;
            _facing = (_facing - 1 + _sectors) % _sectors; // xoay theo chiều kim đồng hồ
            Repaint();
        }

        private static WeaponCollection FindCollection()
        {
            var guids = AssetDatabase.FindAssets("t:WeaponCollection");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<WeaponCollection>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        /// <summary>Nạp danh sách vũ khí: ưu tiên từ CSV được link; nếu chưa link thì lấy từ collection asset.</summary>
        private void ReloadList()
        {
            _list.Clear();
            if (_csv != null)
            {
                var t = new CsvTable(_csv.text);
                for (int i = 0; i < t.Rows.Count; i++)
                {
                    var r = new CsvRow(t, t.Rows[i]);
                    _list.Add(new WeaponModel
                    {
                        id = r.Str("id"), name = r.Str("name"), rarity = r.Int("rarity"), damage = r.Float("damage"),
                        triggerShape = r.Str("triggerShape"), knockback = r.Str("knockback"),
                        comboBonus = r.Float("comboBonus"), collisionDamage = r.Float("collisionDamage"), modelKey = r.Str("modelKey")
                    });
                }
            }
            else if (_weapons != null && _weapons.dataGroup != null)
            {
                _list.AddRange(_weapons.dataGroup);
            }

            _sel = _list.Count > 0 ? 0 : -1;
            LoadSelected();
        }

        private void LoadSelected()
        {
            _cells.Clear();
            if (_sel < 0 || _sel >= _list.Count)
            {
                _cur = default;
                return;
            }

            _cur = _list[_sel];
            foreach (var c in CellSet.Parse(_cur.triggerShape)) _cells.Add(c);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUI.BeginChangeCheck();
            _csv = (TextAsset)EditorGUILayout.ObjectField("CSV File (nguồn)", _csv, typeof(TextAsset), false);
            if (EditorGUI.EndChangeCheck()) ReloadList(); // đổi file CSV → nạp lại
            _weapons = (WeaponCollection)EditorGUILayout.ObjectField("Weapon Collection (sync)", _weapons, typeof(WeaponCollection), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load CSV")) ReloadList();
                if (GUILayout.Button("＋ New Weapon")) NewWeapon();
                using (new EditorGUI.DisabledScope(_sel < 0))
                    if (GUILayout.Button("🗑 Delete")) DeleteWeapon();
            }

            if (_csv == null)
                EditorGUILayout.HelpBox("Chưa link CSV → sẽ ghi vào Weapons.csv mặc định. Kéo file .csv vào 'CSV File' để config file khác.", MessageType.None);

            DrawWeaponPicker();
            EditorGUILayout.Space(4);
            DrawFields();

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                _ringCount = EditorGUILayout.IntSlider("Rings", _ringCount, 1, 10);
            }

            _sectors = EditorGUILayout.IntSlider("Sectors / Ring", _sectors, 3, 24);
            _facing = EditorGUILayout.IntSlider("Facing (xoay)", Mathf.Clamp(_facing, 0, _sectors - 1), 0, _sectors - 1);
            using (new EditorGUILayout.HorizontalScope())
            {
                _editMode = GUILayout.Toggle(_editMode, "Pick ô (click lưới)", "Button");
                _autoRotate = GUILayout.Toggle(_autoRotate, "Auto Rotate", "Button");
            }

            EditorGUILayout.HelpBox(_editMode
                ? "Đặt Facing làm HƯỚNG NHẮM, click ô trên lưới để bật/tắt ô trong tầm đánh. Ô lưu tương đối hướng nhắm."
                : "Kéo Facing / bật Auto Rotate để xem trigger quét quanh hero.", MessageType.None);

            _gridRect = GUILayoutUtility.GetRect(position.width - 20f, 360f);
            HandleGridClick();
            if (Event.current.type == EventType.Repaint) DrawGrid();

            EditorGUILayout.Space(6);
            using (new EditorGUI.DisabledScope(_sel < 0 || string.IsNullOrEmpty(_cur.id)))
            {
                GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);
                if (GUILayout.Button("💾  Save to CSV", GUILayout.Height(30f))) SaveToCsv();
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawWeaponPicker()
        {
            if (_list.Count == 0)
            {
                EditorGUILayout.HelpBox("Chưa có vũ khí. Bấm ＋ New Weapon.", MessageType.Info);
                return;
            }

            var names = new string[_list.Count];
            for (int i = 0; i < _list.Count; i++) names[i] = $"{_list[i].name} ({_list[i].id})";
            int newSel = EditorGUILayout.Popup("Weapon", Mathf.Clamp(_sel, 0, _list.Count - 1), names);
            if (newSel != _sel)
            {
                _sel = newSel;
                LoadSelected();
            }
        }

        private void DrawFields()
        {
            if (_sel < 0) return;

            _cur.id = EditorGUILayout.TextField("id", _cur.id);
            _cur.name = EditorGUILayout.TextField("name", _cur.name);
            _cur.rarity = EditorGUILayout.IntField("rarity", _cur.rarity);
            _cur.damage = EditorGUILayout.FloatField("damage", _cur.damage);
            _cur.knockback = EditorGUILayout.TextField("knockback (x y;…)", _cur.knockback);
            _cur.comboBonus = EditorGUILayout.FloatField("comboBonus", _cur.comboBonus);
            _cur.collisionDamage = EditorGUILayout.FloatField("collisionDamage", _cur.collisionDamage);
            _cur.modelKey = EditorGUILayout.TextField("modelKey (prefab 41xxx)", _cur.modelKey);
            EditorGUILayout.LabelField("triggerShape", CellsToString(_cells));
            EditorGUILayout.LabelField("range (auto)", CellSet.MaxForward(new List<Vector2Int>(_cells)).ToString());
        }

        private void NewWeapon()
        {
            _cells.Clear();
            _cur = new WeaponModel { id = "wp_new", name = "New Weapon", rarity = 1, damage = 10, knockback = "0 1", comboBonus = 0.2f, collisionDamage = 10 };
            _list.Add(_cur);
            _sel = _list.Count - 1;
        }

        private void DeleteWeapon()
        {
            if (_sel < 0) return;
            _list.RemoveAt(_sel);
            _sel = Mathf.Clamp(_sel, 0, _list.Count - 1);
            if (_list.Count == 0) _sel = -1;
            LoadSelected();
        }

        private void HandleGridClick()
        {
            if (!_editMode || _sel < 0) return;
            var e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || !_gridRect.Contains(e.mousePosition)) return;

            ComputeLayout(out var center, out var hole, out var maxR, out var thick, out var step);
            Vector2 d = e.mousePosition - center;
            float r = d.magnitude;
            if (r < hole || r > maxR) return;

            int ring = Mathf.Clamp(Mathf.FloorToInt((r - hole) / thick), 0, _ringCount - 1);
            float ang = Mathf.Atan2(-d.y, d.x);
            if (ang < 0f) ang += Mathf.PI * 2f;
            int sector = Mathf.FloorToInt(ang / step) % _sectors;

            int lateral = SignedDelta(sector - _facing);
            var cell = new Vector2Int(lateral, ring + 1);
            if (!_cells.Remove(cell)) _cells.Add(cell);

            _cur.triggerShape = CellsToString(_cells);
            e.Use();
            Repaint();
        }

        private int SignedDelta(int raw)
        {
            int d = ((raw % _sectors) + _sectors) % _sectors;
            if (d > _sectors / 2) d -= _sectors;
            return d;
        }

        private void SaveToCsv()
        {
            if (_sel >= 0) _list[_sel] = _cur; // ghi lại weapon đang sửa

            var sb = new StringBuilder();
            sb.AppendLine("# Vu khi hero. triggerShape = tap o nham \"x y\" (x=ngang so voi huong nham, y=tien ra 1..range)");
            sb.AppendLine("# knockback = cac buoc day lui \"x y\". comboBonus=cong don moi lan chuyen enemy. collisionDamage=dmg khi tong enemy khac");
            sb.AppendLine("id,name,rarity,damage,triggerShape,knockback,comboBonus,collisionDamage,modelKey");
            foreach (var w in _list)
                sb.AppendLine($"{w.id},{w.name},{w.rarity},{w.damage},{w.triggerShape},{w.knockback},{w.comboBonus},{w.collisionDamage},{w.modelKey}");

            string path = _csv != null ? AssetDatabase.GetAssetPath(_csv) : CsvPath;
            System.IO.File.WriteAllText(path, sb.ToString());
            AssetDatabase.ImportAsset(path);
            if (_csv == null) _csv = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

            // cập nhật asset collection để runtime/preview dùng ngay
            var col = _weapons != null ? _weapons : AssetDatabase.LoadAssetAtPath<WeaponCollection>(CollectionPath);
            if (col == null)
            {
                col = CreateInstance<WeaponCollection>();
                AssetDatabase.CreateAsset(col, CollectionPath);
                _weapons = col;
            }

            col.dataGroup = _list.ToArray();
            col.Convert();
            EditorUtility.SetDirty(col);
            AssetDatabase.SaveAssets();
            Debug.Log($"[WeaponDesigner] Đã lưu {_list.Count} vũ khí vào {path} + cập nhật collection.");
        }

        private static string CellsToString(HashSet<Vector2Int> cells)
        {
            var listed = new List<Vector2Int>(cells);
            listed.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            var sb = new StringBuilder();
            for (int i = 0; i < listed.Count; i++)
            {
                if (i > 0) sb.Append(';');
                sb.Append(listed[i].x).Append(' ').Append(listed[i].y);
            }

            return sb.ToString();
        }

        #region Draw (GL)

        private void ComputeLayout(out Vector2 center, out float hole, out float maxR, out float thick, out float step)
        {
            center = new Vector2(_gridRect.center.x, _gridRect.center.y);
            maxR = Mathf.Min(_gridRect.width, _gridRect.height) * 0.46f;
            hole = maxR * 0.13f;
            thick = (maxR - hole) / _ringCount;
            step = Mathf.PI * 2f / _sectors;
        }

        private void DrawGrid()
        {
            EnsureMaterial();
            ComputeLayout(out var center, out var hole, out var maxR, out var thick, out var step);

            var hi = new HashSet<int>();
            foreach (var c in _cells)
            {
                int ring = c.y - 1;
                int sector = ((_facing + c.x) % _sectors + _sectors) % _sectors;
                if (ring >= 0 && ring < _ringCount) hi.Add(ring * _sectors + sector);
            }

            float angGap = step * 0.06f;
            float radGap = thick * 0.08f;

            GL.PushMatrix();
            _mat.SetPass(0);
            GL.LoadPixelMatrix();

            for (int ring = 0; ring < _ringCount; ring++)
            {
                float r0 = hole + ring * thick + radGap;
                float r1 = hole + (ring + 1) * thick - radGap;
                for (int s = 0; s < _sectors; s++)
                {
                    bool on = hi.Contains(ring * _sectors + s);
                    Color col = on
                        ? new Color(1f, 0.5f, 0.12f, 0.92f)
                        : new Color(1f, 1f, 1f, ring % 2 == 0 ? 0.10f : 0.055f);
                    DrawSector(center, r0, r1, s * step + angGap, (s + 1) * step - angGap, col);
                }
            }

            DrawDisc(center, hole * 0.9f, new Color(0.30f, 0.68f, 1f, 1f));
            DrawFacingArrow(center, hole, maxR, step);
            GL.PopMatrix();
        }

        private void DrawFacingArrow(Vector2 c, float hole, float maxR, float step)
        {
            float mid = (_facing + 0.5f) * step;
            var col = new Color(0.30f, 0.68f, 1f, 0.9f);
            Vector2 dir = new Vector2(Mathf.Cos(mid), -Mathf.Sin(mid));
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector2 baseP = c + dir * hole;
            Vector2 tip = c + dir * (maxR + 10f);

            GL.Begin(GL.TRIANGLES);
            GL.Color(col);
            Vector2 b0 = baseP + perp * 3f, b1 = baseP - perp * 3f;
            Vector2 e0 = tip - dir * 14f + perp * 3f, e1 = tip - dir * 14f - perp * 3f;
            Vert(b0); Vert(e0); Vert(e1);
            Vert(b0); Vert(e1); Vert(b1);
            Vert(tip); Vert(tip - dir * 16f + perp * 8f); Vert(tip - dir * 16f - perp * 8f);
            GL.End();
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
                Vector2 i0 = Polar(c, r0, t0), o0 = Polar(c, r1, t0);
                Vector2 i1 = Polar(c, r0, t1), o1 = Polar(c, r1, t1);
                Vert(i0); Vert(o0); Vert(o1);
                Vert(i0); Vert(o1); Vert(i1);
            }

            GL.End();
        }

        private static void DrawDisc(Vector2 c, float r, Color col)
        {
            const int seg = 28;
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

        private static Vector2 Polar(Vector2 c, float r, float ang) =>
            new Vector2(c.x + Mathf.Cos(ang) * r, c.y - Mathf.Sin(ang) * r);

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
    }
}
