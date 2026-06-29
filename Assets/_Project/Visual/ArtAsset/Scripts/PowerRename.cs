#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class PowerRename : EditorWindow
{
    private string find = "";
    private string prefix = "";
    private string replaceWith = "";
    private string suffix = "";
    private int trimEnd;
    private int trimStart;

    private void OnGUI()
    {
        GUILayout.Label("Power Rename", EditorStyles.boldLabel);

        // Prefix Field
        prefix = EditorGUILayout.TextField("Prefix", prefix);

        // Suffix Field
        suffix = EditorGUILayout.TextField("Suffix", suffix);

        // Trim Fields
        GUILayout.Label("Trim Characters", EditorStyles.boldLabel);
        trimStart = EditorGUILayout.IntField("Trim Start", trimStart);
        trimEnd = EditorGUILayout.IntField("Trim End", trimEnd);

        // Find and Replace Fields
        find = EditorGUILayout.TextField("Find", find);
        replaceWith = EditorGUILayout.TextField("Replace with", replaceWith);

        if (GUILayout.Button("Rename"))
        {
            foreach (var obj in Selection.objects)
            {
                var newName = obj.name;

                // Find and Replace
                if (!string.IsNullOrEmpty(find)) newName = newName.Replace(find, replaceWith);

                // Trim Start
                if (trimStart > 0 && newName.Length > trimStart) newName = newName.Substring(trimStart);

                // Trim End
                if (trimEnd > 0 && newName.Length > trimEnd) newName = newName.Substring(0, newName.Length - trimEnd);

                // Add Prefix and Suffix
                newName = prefix + newName + suffix;

                // Rename GameObjects in the scene
                if (obj is GameObject)
                {
                    var go = obj as GameObject;
                    Undo.RecordObject(go, "Power Rename");
                    go.name = newName;
                }
                // Rename Assets in the project folder
                else if (AssetDatabase.Contains(obj))
                {
                    var path = AssetDatabase.GetAssetPath(obj);
                    AssetDatabase.RenameAsset(path, newName);
                    AssetDatabase.SaveAssets();
                }
            }

            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Tools/Power Rename")]
    public static void ShowWindow()
    {
        GetWindow(typeof(PowerRename), false, "Power Rename");
    }
}
#endif