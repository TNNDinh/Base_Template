#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Ezg.Core.Editor
{
    public class BatchRenameAssetTool : EditorWindow
{
    private string _pasteText = "";
    private Vector2 _leftScroll;
    private Vector2 _rightScroll;
    private List<Object> _selectedAssets = new List<Object>();
    private List<string> _newNames = new List<string>();

    [MenuItem("Tools/Batch Rename Assets")]
    static void OpenWindow()
    {
        var window = GetWindow<BatchRenameAssetTool>("Batch Rename Assets");
        window.minSize = new Vector2(600, 400);
        window.RefreshSelection();
    }

    void OnFocus()
    {
        RefreshSelection();
    }

    void OnSelectionChange()
    {
        RefreshSelection();
        Repaint();
    }

    void RefreshSelection()
    {
        _selectedAssets.Clear();
        foreach (var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
                _selectedAssets.Add(obj);
        }
    }

    void ParsePasteText()
    {
        _newNames.Clear();
        if (string.IsNullOrEmpty(_pasteText)) return;

        var lines = _pasteText.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                _newNames.Add(trimmed);
        }
    }

    void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Batch Rename Assets", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();

        // Left panel — selected assets
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.45f));
        EditorGUILayout.LabelField($"Selected Assets ({_selectedAssets.Count})", EditorStyles.miniBoldLabel);
        _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, EditorStyles.helpBox, GUILayout.ExpandHeight(true));
        for (int i = 0; i < _selectedAssets.Count; i++)
        {
            string currentName = _selectedAssets[i] != null ? _selectedAssets[i].name : "(missing)";
            string newName = i < _newNames.Count ? _newNames[i] : "";

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{i + 1}.", GUILayout.Width(28));

            bool hasNew = !string.IsNullOrEmpty(newName);
            var style = hasNew ? EditorStyles.label : EditorStyles.miniLabel;
            GUILayout.Label(currentName, style, GUILayout.ExpandWidth(true));

            if (hasNew)
            {
                GUILayout.Label("→", GUILayout.Width(18));
                var nameStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.2f, 0.7f, 0.3f) } };
                GUILayout.Label(newName, nameStyle, GUILayout.ExpandWidth(true));
            }
            EditorGUILayout.EndHorizontal();
        }
        if (_selectedAssets.Count == 0)
            EditorGUILayout.HelpBox("Select assets in the Project window.", MessageType.Info);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        GUILayout.Space(8);

        // Right panel — paste area
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("New Names (paste from clipboard)", EditorStyles.miniBoldLabel);
        EditorGUILayout.HelpBox("One name per line. Order matches selected assets.", MessageType.None);

        EditorGUI.BeginChangeCheck();
        _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll, GUILayout.ExpandHeight(true));
        _pasteText = EditorGUILayout.TextArea(_pasteText, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        EditorGUILayout.EndScrollView();
        if (EditorGUI.EndChangeCheck())
            ParsePasteText();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Paste from Clipboard", GUILayout.Height(22)))
        {
            _pasteText = GUIUtility.systemCopyBuffer;
            ParsePasteText();
        }
        if (GUILayout.Button("Clear", GUILayout.Width(60), GUILayout.Height(22)))
        {
            _pasteText = "";
            _newNames.Clear();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // Validation summary
        int pairCount = Mathf.Min(_selectedAssets.Count, _newNames.Count);
        if (_selectedAssets.Count > 0 && _newNames.Count > 0)
        {
            if (_newNames.Count < _selectedAssets.Count)
                EditorGUILayout.HelpBox($"Only {_newNames.Count} names provided for {_selectedAssets.Count} assets. First {pairCount} will be renamed.", MessageType.Warning);
            else if (_newNames.Count > _selectedAssets.Count)
                EditorGUILayout.HelpBox($"{_newNames.Count} names for {_selectedAssets.Count} assets. Last {_newNames.Count - _selectedAssets.Count} name(s) will be ignored.", MessageType.Warning);
            else
                EditorGUILayout.HelpBox($"Ready to rename {pairCount} asset(s).", MessageType.Info);
        }

        bool canRename = pairCount > 0;
        GUI.enabled = canRename;
        if (GUILayout.Button("Rename", GUILayout.Height(32)))
            DoRename();
        GUI.enabled = true;
    }

    void DoRename()
    {
        int count = Mathf.Min(_selectedAssets.Count, _newNames.Count);
        int success = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < count; i++)
            {
                if (_selectedAssets[i] == null) continue;

                string assetPath = AssetDatabase.GetAssetPath(_selectedAssets[i]);
                string error = AssetDatabase.RenameAsset(assetPath, _newNames[i]);
                if (string.IsNullOrEmpty(error))
                    success++;
                else
                    Debug.LogWarning($"[BatchRename] Failed to rename '{_selectedAssets[i].name}' → '{_newNames[i]}': {error}");
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("Batch Rename", $"Renamed {success} / {count} asset(s) successfully.", "OK");
        RefreshSelection();
        _pasteText = "";
        _newNames.Clear();
        Repaint();
    }
}
}
#endif
