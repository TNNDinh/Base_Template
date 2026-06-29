#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class CustomPADBuildMenu
{
    static CustomPADBuildMenu()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);
    }

    private static void OnBuildPlayer(BuildPlayerOptions options)
    {
        if (options.targetGroup == BuildTargetGroup.Android && !AutoBuild.IsAutoBuildRunning)
        {
            MyPreprocessFunc();
            BuildAssetBundles();
        }

        // Tiếp tục giao quyền lại cho trình build mặc định của Unity
        BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
    }

    [MenuItem("Google/Custom Build AAB")]
    public static void BuildWithCustomPreprocess()
    {
        MyPreprocessFunc();
        //BuildAssetBundles();

        if (!EditorApplication.ExecuteMenuItem("Google/Build Android App Bundle..."))
        {
            Debug.LogError("Không tìm thấy menu gốc!");
        }
    }

    private static void MyPreprocessFunc()
    {
        PlayerSettings.Android.keystorePass = "deviloper2025";
        PlayerSettings.Android.keyaliasPass = "deviloper2025";
    }

    public static void BuildAssetBundles()
    {
        Debug.Log("[BuildInterceptor] Bắt đầu build Asset Bundles...");
        string assetBundleDirectory = "Assets/AssetBundles/Android";

        if (Directory.Exists(assetBundleDirectory))
            Directory.Delete(assetBundleDirectory, true);

        Directory.CreateDirectory(assetBundleDirectory);

        BuildPipeline.BuildAssetBundles(
            assetBundleDirectory,
            BuildAssetBundleOptions.None,
            BuildTarget.Android
        );
        Debug.Log("[BuildInterceptor] Hoàn thành build Asset Bundles!");
    }
}
#endif