using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class TextureFormatOverrideWindow : EditorWindow
{
    private DefaultAsset targetFolder;
    private List<string> texturePaths = new List<string>();
    private bool forceSetTextureSize = false;
    private bool removePsdMatte = true;

    // Shared Android/iOS Settings
    private int platformMaxTextureSize = 2048;
    private TextureImporterFormat platformFormat = TextureImporterFormat.ASTC_4x4;
    private int platformCompressionQuality = 100;
    private bool platformOverride = true;

    private Vector2 scrollPos;

    [MenuItem("Window/Custom/Texture Format Override")]
    public static void ShowWindow()
    {
        GetWindow<TextureFormatOverrideWindow>("Texture Override");
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });

        EditorGUILayout.LabelField("Texture Format Override Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Target Folder", targetFolder, typeof(DefaultAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            ScanFolder();
        }

        if (targetFolder != null)
        {
            EditorGUILayout.HelpBox($"Found {texturePaths.Count} PNG/PSD files in folder.", MessageType.Info);
        }

        forceSetTextureSize = EditorGUILayout.Toggle(
            new GUIContent("Force Set Texture Size", "Bật để ép đặt Max Texture Size cho Android và iOS. Tắt để giữ nguyên kích thước texture hiện tại. NÊN TẮT!"),
            forceSetTextureSize);
        removePsdMatte = EditorGUILayout.Toggle(
            new GUIContent("Remove PSD Matte", "Bật để loại bỏ viền matte trên file PSD. Tắt để giữ nguyên thiết lập matte hiện tại của PSD. Lưu ý: Tùy chọn này chỉ áp dụng cho file PSD và sẽ không ảnh hưởng đến file PNG. NÊN BẬT!"),
            removePsdMatte);
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawPlatformSettings("Android and iOS", ref platformOverride, ref platformMaxTextureSize, ref platformFormat, ref platformCompressionQuality);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Settings to All PNG/PSD Files", GUILayout.Height(40)))
        {
            ApplySettings();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPlatformSettings(string platform, ref bool overrideEnabled, ref int maxSize, ref TextureImporterFormat format, ref int quality)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
        overrideEnabled = EditorGUILayout.BeginToggleGroup($"Override for {platform}", overrideEnabled);

        EditorGUI.BeginDisabledGroup(!forceSetTextureSize);
        maxSize = EditorGUILayout.IntPopup("Max Texture Size", maxSize,
            new string[] { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" },
            new int[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 });
        EditorGUI.EndDisabledGroup();

        format = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", format);
        quality = EditorGUILayout.IntSlider("Compression Quality", quality, 0, 100);

        EditorGUILayout.EndToggleGroup();
        EditorGUILayout.EndVertical();
    }

    private void ScanFolder()
    {
        texturePaths.Clear();
        if (targetFolder == null) return;

        string folderPath = AssetDatabase.GetAssetPath(targetFolder);
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

        string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { folderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string lowerPath = path.ToLower();
            if (lowerPath.EndsWith(".png") || lowerPath.EndsWith(".psd"))
            {
                texturePaths.Add(path);
            }
        }
    }

    private void ApplySettings()
    {
        if (texturePaths.Count == 0)
        {
            EditorUtility.DisplayDialog("No Textures Found", "Please select a folder containing PNG or PSD images first.", "OK");
            return;
        }

        int count = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (string path in texturePaths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool isPsd = path.EndsWith(".psd", System.StringComparison.OrdinalIgnoreCase);
                bool changed = false;

                if (isPsd && ApplyPsdRemoveMatte(importer))
                {
                    changed = true;
                }

                if (platformOverride)
                {
                    ApplyPlatformSettings(importer, "Android");
                    ApplyPlatformSettings(importer, "iPhone");
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.DisplayProgressBar("Applying Settings", $"Processing {Path.GetFileName(path)}", (float)count / texturePaths.Count);
                    importer.SaveAndReimport();
                    count++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"[TextureFormatOverrideWindow] Applied settings to {count} textures.");
    }

    private void ApplyPlatformSettings(TextureImporter importer, string platform)
    {
        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
        settings.overridden = true;
        if (forceSetTextureSize)
        {
            settings.maxTextureSize = platformMaxTextureSize;
        }
        settings.format = platformFormat;
        settings.compressionQuality = platformCompressionQuality;
        importer.SetPlatformTextureSettings(settings);
    }

    private bool ApplyPsdRemoveMatte(TextureImporter importer)
    {
        SerializedObject serializedImporter = new SerializedObject(importer);
        serializedImporter.Update();

        SerializedProperty psdRemoveMatteProperty = serializedImporter.FindProperty("m_PSDRemoveMatte");
        if (psdRemoveMatteProperty == null)
        {
            Debug.LogWarning($"[TextureFormatOverrideWindow] Could not find m_PSDRemoveMatte on {importer.assetPath}.");
            return false;
        }

        if (psdRemoveMatteProperty.boolValue == removePsdMatte)
        {
            return false;
        }

        psdRemoveMatteProperty.boolValue = removePsdMatte;
        serializedImporter.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }
}
