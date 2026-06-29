using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SpineSpritePivotSetter : EditorWindow
{
    private Texture2D atlasTexture;
    private SpriteAlignment pivotAlignment = SpriteAlignment.Center;
    private Vector2 customPivot = new Vector2(0.5f, 0.5f);

    [MenuItem("Tools/Spine/Set Pivot For Atlas Sprites")]
    public static void ShowWindow()
    {
        GetWindow<SpineSpritePivotSetter>("Spine Pivot Setter");
    }

    void OnGUI()
    {
        GUILayout.Label("Spine Atlas Sprite Pivot Setter", EditorStyles.boldLabel);

        atlasTexture = (Texture2D)EditorGUILayout.ObjectField("Atlas Texture", atlasTexture, typeof(Texture2D), false);
        pivotAlignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Pivot", pivotAlignment);

        if (pivotAlignment == SpriteAlignment.Custom)
        {
            customPivot = EditorGUILayout.Vector2Field("Custom Pivot", customPivot);
        }

        if (GUILayout.Button("Apply Pivot"))
        {
            if (atlasTexture != null)
                SetPivot();
            else
                EditorUtility.DisplayDialog("Error", "Please assign an atlas texture!", "OK");
        }
    }

    void SetPivot()
    {
        string path = AssetDatabase.GetAssetPath(atlasTexture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null || !importer.spriteImportMode.Equals(SpriteImportMode.Multiple))
        {
            EditorUtility.DisplayDialog("Error", "Selected texture is not a sprite atlas (Multiple mode).", "OK");
            return;
        }

        SpriteMetaData[] sprites = importer.spritesheet;
        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteMetaData smd = sprites[i];
            smd.alignment = (int)pivotAlignment;

            if (pivotAlignment == SpriteAlignment.Custom)
                smd.pivot = customPivot;

            sprites[i] = smd;
        }

        importer.spritesheet = sprites;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        EditorUtility.DisplayDialog("Done", "Pivot updated for all sprites in atlas!", "OK");
    }
}
