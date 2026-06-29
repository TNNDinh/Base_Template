#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class CustomMaxSizeSetter : EditorWindow
{
    private const int MINIMUM_MAX_SIZE = 32;
    private const int MAXIMUM_MAX_SIZE = 4096;
    private int currentMaxSize = -1;

    private int initialMaxSize = -1;

    private Texture2D[] selection;
    private string selectionLabel;

    private void OnEnable()
    {
        OnSelectionChange();
    }

    private void OnGUI()
    {
        GUILayout.Label(selectionLabel, EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(selection == null || selection.Length == 0);

        EditorGUI.showMixedValue = currentMaxSize < 0;
        EditorGUI.BeginChangeCheck();
        var maxSize = EditorGUILayout.IntSlider(currentMaxSize, MINIMUM_MAX_SIZE, MAXIMUM_MAX_SIZE);
        if (EditorGUI.EndChangeCheck()) // Otherwise, IntSlider clamps value from -1 to MINIMUM_MAX_SIZE with no user input
            currentMaxSize = maxSize;
        EditorGUI.showMixedValue = false;

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(currentMaxSize < 0 || initialMaxSize == currentMaxSize);

        if (GUILayout.Button("Apply"))
        {
            AssetDatabase
                .StartAssetEditing(); // Apart from batching the reimport operations, this also ensures OnProjectChange isn't called in the middle of this for-loop
            try
            {
                for (var i = 0; i < selection.Length; i++)
                    SetMaxSizeOfTexture(selection[i], currentMaxSize);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        EditorGUI.EndDisabledGroup();
        EditorGUI.EndDisabledGroup();
    }

    private void OnProjectChange() // Texture Max Size might be changed from the Inspector and etc.
    {
        OnSelectionChange();
    }

    private void OnSelectionChange()
    {
        selection = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
        if (selection == null || selection.Length == 0)
        {
            selectionLabel = "Max Size of \"Nothing Selected\":";
            initialMaxSize = currentMaxSize = -1;
        }
        else if (selection.Length == 1)
        {
            selectionLabel = "Max Size of \"" + selection[0].name + "\":";
            initialMaxSize = currentMaxSize = GetMaxSizeOfTexture(selection[0]);
        }
        else
        {
            selectionLabel = "Max Size of \"" + selection[0].name + "\" and " + (selection.Length - 1) + " more:";
            initialMaxSize = currentMaxSize = GetMaxSizeOfTexture(selection[0]);
            for (var i = 1; i < selection.Length; i++)
            {
                var maxSize = GetMaxSizeOfTexture(selection[i]);
                if (maxSize != initialMaxSize)
                {
                    initialMaxSize = currentMaxSize = -1;
                    break;
                }
            }
        }

        Repaint();
    }

    [MenuItem("Window/Custom Max Size Setter")]
    private static void Init()
    {
        var window = GetWindow<CustomMaxSizeSetter>();
        window.minSize = new Vector2(250f, 85f);
        window.titleContent = new GUIContent("Custom Max Size");
        window.Show();
    }

    private int GetMaxSizeOfTexture(Texture2D texture)
    {
        var textureImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
        return textureImporter.maxTextureSize;
    }

    private void SetMaxSizeOfTexture(Texture2D texture, int maxSize)
    {
        var textureImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
        textureImporter.maxTextureSize = maxSize;
        textureImporter.npotScale = TextureImporterNPOTScale.None;
        textureImporter.SaveAndReimport();
    }
}

#endif