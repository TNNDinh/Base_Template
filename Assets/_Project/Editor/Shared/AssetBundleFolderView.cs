#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Asset Bundle browser that displays bundles in a folder tree.
/// Convention: first "_" in bundle name = folder separator.
///   "gameplay_booster"        → folder "gameplay",  bundle "booster"
///   "events_petal_plate_party"→ folder "events",    bundle "petal_plate_party"
///   "common"                  → root-level bundle (no folder)
/// </summary>
public class AssetBundleFolderView : EditorWindow
{
    // ── tree state ────────────────────────────────────────────────────
    class FolderNode
    {
        public string folderName;
        public bool expanded = true;
        public List<string> bundleNames = new(); // full bundle names under this folder
    }

    List<FolderNode> _folders = new();
    List<string> _rootBundles = new(); // bundles with no folder prefix

    string _selectedBundle;
    string[] _selectedAssets = System.Array.Empty<string>();

    // ── rename / create state ─────────────────────────────────────────
    bool _renameMode;
    string _renameOldName;
    string _renameFolder;
    string _renameBundlePart;

    bool _createMode;
    string _newFolder = "";
    string _newBundlePart = "";

    Vector2 _leftScroll, _rightScroll;

    [MenuItem("Tools/Asset Bundles/Folder View")]
    public static void Open() => GetWindow<AssetBundleFolderView>("Bundle Folders").Show();

    void OnEnable() => Refresh();

    void Refresh()
    {
        _folders.Clear();
        _rootBundles.Clear();
        _selectedBundle = null;
        _selectedAssets = System.Array.Empty<string>();

        var folderMap = new Dictionary<string, FolderNode>();

        foreach (string name in AssetDatabase.GetAllAssetBundleNames().OrderBy(n => n))
        {
            int sep = name.IndexOf('_');
            if (sep > 0)
            {
                string folder = name[..sep];
                if (!folderMap.TryGetValue(folder, out var node))
                {
                    node = new FolderNode { folderName = folder };
                    folderMap[folder] = node;
                    _folders.Add(node);
                }
                node.bundleNames.Add(name);
            }
            else
            {
                _rootBundles.Add(name);
            }
        }

        _folders.Sort((a, b) => string.Compare(a.folderName, b.folderName));
    }

    void OnGUI()
    {
        // toolbar
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70))) Refresh();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+ New Bundle", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            _createMode = true;
            _renameMode = false;
            _newFolder = "";
            _newBundlePart = "";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        // ── LEFT: tree ────────────────────────────────────────────────
        _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUILayout.Width(220), GUILayout.ExpandHeight(true));

        DrawRootBundles();
        foreach (var folder in _folders)
            DrawFolder(folder);

        EditorGUILayout.EndScrollView();

        // divider
        GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));

        // ── RIGHT: assets / edit panel ────────────────────────────────
        _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        if (_createMode)     DrawCreatePanel();
        else if (_renameMode) DrawRenamePanel();
        else                  DrawAssetList();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndHorizontal();
    }

    // ── left panel helpers ─────────────────────────────────────────────

    void DrawRootBundles()
    {
        foreach (string name in _rootBundles)
            DrawBundleRow(name, name, 0);
    }

    void DrawFolder(FolderNode folder)
    {
        // folder header
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(4);
        string arrow = folder.expanded ? "▾" : "▸";
        if (GUILayout.Button($"{arrow} {folder.folderName}", EditorStyles.boldLabel))
            folder.expanded = !folder.expanded;
        EditorGUILayout.EndHorizontal();

        if (!folder.expanded) return;

        foreach (string fullName in folder.bundleNames)
        {
            string displayName = fullName[(folder.folderName.Length + 1)..]; // strip "folder_"
            DrawBundleRow(fullName, displayName, 16);
        }
    }

    void DrawBundleRow(string fullName, string displayName, int indent)
    {
        bool selected = fullName == _selectedBundle;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(indent);

        var style = selected ? EditorStyles.whiteLabel : EditorStyles.label;
        var bg = selected ? new Color(0.24f, 0.49f, 0.91f) : Color.clear;

        var rect = GUILayoutUtility.GetRect(new GUIContent(displayName), style, GUILayout.ExpandWidth(true));
        if (selected) EditorGUI.DrawRect(rect, bg);
        GUI.Label(rect, displayName, style);

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            SelectBundle(fullName);
            _renameMode = false;
            _createMode = false;
            Event.current.Use();
        }

        // rename button
        if (selected && GUILayout.Button("✎", GUILayout.Width(20)))
        {
            _renameMode = true;
            _createMode = false;
            _renameOldName = fullName;
            int sep = fullName.IndexOf('_');
            _renameFolder     = sep > 0 ? fullName[..sep] : "";
            _renameBundlePart = sep > 0 ? fullName[(sep + 1)..] : fullName;
        }

        EditorGUILayout.EndHorizontal();
    }

    void SelectBundle(string name)
    {
        _selectedBundle = name;
        _selectedAssets = AssetDatabase.GetAssetPathsFromAssetBundle(name);
    }

    // ── right panel: asset list ────────────────────────────────────────

    void DrawAssetList()
    {
        if (string.IsNullOrEmpty(_selectedBundle))
        {
            EditorGUILayout.HelpBox("Chọn một bundle bên trái để xem assets.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Bundle: {_selectedBundle}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{_selectedAssets.Length} assets", EditorStyles.miniLabel);
        EditorGUILayout.Space(4);

        foreach (string path in _selectedAssets)
        {
            EditorGUILayout.BeginHorizontal();
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            EditorGUILayout.ObjectField(asset, typeof(Object), false);
            EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
    }

    // ── right panel: rename ────────────────────────────────────────────

    void DrawRenamePanel()
    {
        EditorGUILayout.LabelField("Đổi tên bundle", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _renameFolder     = EditorGUILayout.TextField("Folder (prefix)", _renameFolder);
        _renameBundlePart = EditorGUILayout.TextField("Bundle name", _renameBundlePart);

        string preview = string.IsNullOrWhiteSpace(_renameFolder)
            ? _renameBundlePart
            : $"{_renameFolder}_{_renameBundlePart}";
        EditorGUILayout.LabelField("Tên bundle mới:", preview, EditorStyles.helpBox);

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Áp dụng"))
        {
            ApplyRename(_renameOldName, preview);
            _renameMode = false;
            Refresh();
        }
        if (GUILayout.Button("Huỷ"))
            _renameMode = false;

        EditorGUILayout.EndHorizontal();
    }

    void ApplyRename(string oldName, string newName)
    {
        if (oldName == newName || string.IsNullOrWhiteSpace(newName)) return;

        foreach (string path in AssetDatabase.GetAssetPathsFromAssetBundle(oldName))
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null) importer.assetBundleName = newName;
        }

        AssetDatabase.RemoveAssetBundleName(oldName, true);
        AssetDatabase.SaveAssets();
        Debug.Log($"[BundleView] Renamed: {oldName} → {newName}");
    }

    // ── right panel: create ────────────────────────────────────────────

    void DrawCreatePanel()
    {
        EditorGUILayout.LabelField("Tạo bundle mới", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Bundle mới sẽ được đăng ký tên nhưng chưa có asset nào. " +
            "Gán asset vào bundle này bằng Inspector hoặc kéo vào bundle trong Browser.",
            MessageType.Info);
        EditorGUILayout.Space(4);

        _newFolder     = EditorGUILayout.TextField("Folder (prefix)", _newFolder);
        _newBundlePart = EditorGUILayout.TextField("Bundle name", _newBundlePart);

        string preview = string.IsNullOrWhiteSpace(_newFolder)
            ? _newBundlePart
            : $"{_newFolder}_{_newBundlePart}";
        EditorGUILayout.LabelField("Tên bundle:", preview, EditorStyles.helpBox);

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();

        bool valid = !string.IsNullOrWhiteSpace(_newBundlePart) &&
                     System.Text.RegularExpressions.Regex.IsMatch(preview, @"^[a-zA-Z0-9_]+$");

        GUI.enabled = valid;
        if (GUILayout.Button("Tạo"))
        {
            // Unity tạo bundle khi có ít nhất 1 asset, ở đây chỉ log hướng dẫn
            Debug.Log($"[BundleView] Bundle name sẵn sàng: \"{preview}\" — gán asset qua Inspector.");
            _createMode = false;
            GUIUtility.keyboardControl = 0;
        }
        GUI.enabled = true;

        if (GUILayout.Button("Huỷ"))
            _createMode = false;

        EditorGUILayout.EndHorizontal();

        if (!valid && !string.IsNullOrWhiteSpace(_newBundlePart))
            EditorGUILayout.HelpBox("Tên chỉ được dùng chữ cái, số và dấu _", MessageType.Warning);
    }
}
#endif
