using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Helper rename Resources ↔ Prefabs.
///
/// QUAN TRỌNG: KHÔNG dùng IPreprocessBuildWithReport / IPostprocessBuildWithReport.
/// Lý do: AssetDatabase.Refresh trong preprocess sẽ queue compile/import. Compile chạy
/// giữa chừng khi Google Play Plugin đã start background thread → domain reload →
/// ThreadAbortException ở WaitHandle.WaitOne.
///
/// Workflow đúng (sequential, không interleave với build pipeline):
///   1. Mở AssetBundleConfig inspector → bấm 🤖 Android   (rename Resources → Prefabs)
///   2. Đợi Unity idle hoàn toàn (không còn icon compile/import ở góc dưới phải)
///   3. Trigger build AAB qua Google Play Plugin
///   4. Sau khi build xong → bấm 💻 Editor                (restore Prefabs → Resources)
///
/// Nếu build crash giữa chừng và folder bị stuck ở Prefabs → dùng menu
/// "Tools/Build/Restore Resources Folders".
/// </summary>
public static class BuildResourcesFolderSwap
{
    private const string ConfigPath = "Assets/_Project/Resources/AssetBundle/AssetBundleConfig.asset";

    [MenuItem("Tools/Build/Restore Resources Folders (nếu build crash)")]
    public static void RestoreFolders()
    {
        var config = AssetDatabase.LoadAssetAtPath<AssetBundleConfig>(ConfigPath);
        var rootsToRestore = new List<string>();

        // 1. Lấy danh sách roots đã track trong config
        if (config?.renamedFeatureRoots != null)
            rootsToRestore.AddRange(config.renamedFeatureRoots);

        // 2. Scan thêm featuresPath + extra targets phòng trường hợp config bị mất tracking
        if (config != null)
        {
            if (Directory.Exists(config.featuresPath))
            {
                foreach (var d in Directory.GetDirectories(config.featuresPath))
                    ScanForStuckPrefabs(d.Replace("\\", "/"), rootsToRestore);
            }

            foreach (var obj in config.buildExtraFolderTargets)
            {
                if (obj == null) continue;
                string p = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(p))
                    ScanForStuckPrefabs(p.Replace("\\", "/"), rootsToRestore);
            }
        }

        if (rootsToRestore.Count == 0)
        {
            Debug.Log("[BuildResourcesFolderSwap] Không có gì cần restore.");
            return;
        }

        int restored = 0;
        EditorApplication.LockReloadAssemblies();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var root in rootsToRestore)
                if (TryRenameBack(root)) restored++;
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorApplication.UnlockReloadAssemblies();
        }

        if (config != null)
        {
            config.renamedFeatureRoots?.Clear();
            EditorUtility.SetDirty(config);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Debug.Log($"[BuildResourcesFolderSwap] Restored {restored} Prefabs → Resources.");
    }

    private static void ScanForStuckPrefabs(string folderPath, List<string> result)
    {
        folderPath = folderPath.Replace("\\", "/").TrimEnd('/');
        if (!Directory.Exists(folderPath)) return;

        string[] subDirs = Directory.GetDirectories(folderPath);
        if (subDirs == null || subDirs.Length == 0) return;

        foreach (string sub in subDirs)
        {
            string subPath = sub.Replace("\\", "/");
            string subName = Path.GetFileName(subPath);
            if (subName == "Prefabs")
            {
                if (!result.Contains(folderPath)) result.Add(folderPath);
            }
            else if (subName != "Resources")
            {
                ScanForStuckPrefabs(subPath, result);
            }
        }
    }

    private static bool TryRenameBack(string featureFolderPath)
    {
        featureFolderPath = featureFolderPath.Replace("\\", "/").TrimEnd('/');
        string prefabsPath   = featureFolderPath + "/Prefabs";
        string resourcesPath = featureFolderPath + "/Resources";

        if (!Directory.Exists(prefabsPath) || Directory.Exists(resourcesPath)) return false;

        string err = AssetDatabase.MoveAsset(prefabsPath, resourcesPath);
        if (string.IsNullOrEmpty(err))
        {
            Debug.Log($"[BuildResourcesFolderSwap] ✓ {Path.GetFileName(featureFolderPath)}/Prefabs → Resources");
            return true;
        }

        Debug.LogError($"[BuildResourcesFolderSwap] ✗ Restore failed: {featureFolderPath} — {err}");
        return false;
    }
}
