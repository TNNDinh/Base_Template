using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public enum PlatformBuildMode
{
    None,
    Android,
    Editor,
    iOS
}

/// <summary>
///     Cấu hình cho Asset Bundle và Play Asset Delivery.
///     Dùng 3 nút Platform trong Inspector để switch mode tự động.
/// </summary>
[CreateAssetMenu(fileName = "AssetBundleConfig", menuName = "Game/Asset Bundle Config")]
public class AssetBundleConfig : ScriptableObject
{
    [HideInInspector] public PlatformBuildMode currentMode = PlatformBuildMode.None;

    [Header("Loading Mode")]
    [Tooltip(
        "Load bundle từ file local (Editor dùng D:/UnityProject/m1/AssetBundles/Android, device dùng StreamingAssets)")]
    public bool useBundles;

    [Tooltip("Bật Play Asset Delivery trên Android device (production)")]
    public bool usePAD;

    [Tooltip("Tự động load bundle khi show screen")]
    public bool autoLoadBundle = true;

    [Tooltip("Bỏ qua bundle, luôn dùng Resources.Load trong ResLoader (debug / fallback Editor)")]
    public bool forceResourceLoad;

    [Header("Excluded Features")]
    [Tooltip("Danh sách feature sẽ KHÔNG chuyển đổi Resources → Prefabs\n(Những feature này sẽ build sẵn trong app)")]
    public List<string> excludedFeatures = new()
    {
        "Confirm",
        "MaskChangeScene",
        "SplashScene"
    };

    [Header("Folder Rename Settings")] [Tooltip("Đường dẫn tới folder Features")]
    public string featuresPath = "Assets/_Project/Features";

    [Header("Scan Result")] [SerializeField]
    public List<FolderRenameInfo> foldersToRename = new();

    // Feature folder roots (e.g. "Assets/_Project/Features/GamePlay") đã được đổi Resources → Prefabs.
    // Dùng để reverse về Resources khi switch sang Editor hoặc iOS.
    [HideInInspector] public List<string> renamedFeatureRoots = new();

#if UNITY_EDITOR
    [Header("Build Extra Rename Targets")]
    [Tooltip(
        "Kéo folder vào đây. OnPreprocessBuild sẽ rename Resources → Prefabs trong các folder này khi build Android.\n" +
        "Dùng cho các path nằm ngoài featuresPath (ví dụ: Assets/_Project/Core/SomeFeature).")]
    public List<Object> buildExtraFolderTargets = new();
#endif

    public bool IsFeatureExcluded(string featureName)
    {
        return excludedFeatures.Contains(featureName);
    }
}

[Serializable]
public class FolderRenameInfo
{
    public string featureName;
    public string oldPath;
    public string newPath;
    public bool willRename;

    public FolderRenameInfo(string feature, string old, string newP, bool rename)
    {
        featureName = feature;
        oldPath = old;
        newPath = newP;
        willRename = rename;
    }
}