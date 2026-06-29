#if UNITY_EDITOR
using Google.Android.AppBundle.Editor.AssetPacks;
using Google.Android.AppBundle.Editor.Internal;
using Google.Android.AppBundle.Editor.Internal.Config;
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class AutoBuild
{
    public static bool IsAutoBuildRunning = false;

    static void PerformBuild()
    {
        IsAutoBuildRunning = true;
        AssetDatabase.Refresh();

        var buildPath = "";
        var buildFileName = "";
        var fileNameExtension = "";
        var buildVersion = "";
        var bundleVersionCode = "";
        string[] args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-buildType":
                    EditorUserBuildSettings.buildAppBundle = args[i + 1] == "true";
                    fileNameExtension = EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk";
                    break;

                case "-buildFileName":
                    buildFileName = args[i + 1];
                    break;

                case "-buildPath":
                    buildPath = args[i + 1];
                    break;

                case "-buildVersion":
                    buildVersion = args[i + 1];
                    break;

                case "-bundleVersionCode":
                    bundleVersionCode = args[i + 1];
                    break;
            }
        }

        PlayerSettings.Android.keyaliasName = "deviloperlimited";
        PlayerSettings.Android.keystorePass = "deviloper2025";
        PlayerSettings.Android.keyaliasPass = "deviloper2025";

        string[] defaultScene =
        {
            "Assets/Scenes/SplashScene.unity",
            "Assets/Scenes/HomeScene.unity",
            "Assets/Scenes/BattleScene.unity",
        };

        if (!string.IsNullOrEmpty(buildVersion))
            PlayerSettings.bundleVersion = buildVersion;

        if (!string.IsNullOrEmpty(bundleVersionCode))
            PlayerSettings.Android.bundleVersionCode = Convert.ToInt32(bundleVersionCode);

#if UNITY_IOS
        var options = new BuildPlayerOptions()
        {
            scenes = defaultScene,
            locationPathName = buildPath + buildFileName + fileNameExtension,
            target = BuildTarget.iOS,
            options = BuildOptions.None,
        };
        BuildPipeline.BuildPlayer(options);
#else
        string assetBundleDirectory = "Assets/AssetBundles/Android";

        if (Directory.Exists(assetBundleDirectory))
            Directory.Delete(assetBundleDirectory, true);

        Directory.CreateDirectory(assetBundleDirectory);

        BuildPipeline.BuildAssetBundles(
            assetBundleDirectory,
            BuildAssetBundleOptions.None,
            BuildTarget.Android
        );

        var options = new BuildPlayerOptions()
        {
            scenes = defaultScene,
            locationPathName = buildPath + buildFileName + fileNameExtension,
            target = BuildTarget.Android,
            options = BuildOptions.None,
            targetGroup = BuildTargetGroup.Android,
        }; 

        //Load config (copy từ Library/PlayAssetPackConfig.json vào path)
        string projectPath = Directory.GetCurrentDirectory().Replace("\\", "/");
        string configPath = Path.Combine(projectPath, SerializationHelper.ConfigurationFilePath);

        string json = File.ReadAllText(configPath);

        // 🧩 Regex: bắt tất cả giá trị của key "path"
        var regex = new Regex("\"path\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
        var matches = regex.Matches(json);

        foreach (Match match in matches)
        {
            string originalFullPath = match.Groups[1].Value;
            string fullPath = originalFullPath.Replace("\\", "/");

            // Bỏ qua nếu không phải absolute path
            if (!fullPath.Contains(":/") && !fullPath.StartsWith("/Users/"))
                continue;

            // Detect phần prefix tuyệt đối (trước /Assets/)
            int assetsIndex = fullPath.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex <= 0) continue;

            string prefix = fullPath.Substring(0, assetsIndex + 1); // giữ luôn dấu '/'
            string relativePath = fullPath.Substring(assetsIndex + 1); // Assets/...

            // Normalize lại (Assets/…)
            string fixedPath = "Assets/" + relativePath.Substring("Assets/".Length);

            Debug.Log($"🔧 Normalize path:\n  From: {fullPath}\n  To:   {fixedPath}");

            json = json.Replace("//", "/");
            json = json.Replace("//", "/");
            json = json.Replace("//", "/");
            json = json.Replace("\"/Assets/", "\"Assets/");
            json = json.Replace(originalFullPath, fixedPath);
        }

        string tempPath = Path.Combine(projectPath, "Temp/AssetPackConfig_CI.json");
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath) ?? string.Empty);
        File.WriteAllText(tempPath, json);

        AppBundlePublisher.Build(options, AssetPackConfigSerializer.LoadConfig(tempPath), false);
#endif

    }
}
#endif