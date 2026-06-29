#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Migration helpers for PAD-compatible bundle naming.
///
/// Convention used by the custom Asset Bundle Browser:
///   "__" (double underscore) = hierarchy level separator  →  displayed as folder in tree
///   "_"  (single underscore) within a token               →  part of the bundle name
///
/// Examples:
///   "gameplay/item_raw/coffee"  →  "gameplay__item_raw__coffee"
///   "gameplay/booster"          →  "gameplay__booster"
///   "common"                    →  "common"  (no change)
/// </summary>
public static class RenameAssetBundlesForPAD
{
    // ── Step 1: / → __ (run this first, converts slash-based names to PAD+browser convention)
    [MenuItem("Tools/Asset Bundles/Migration/1. Rename bundles: slash  /  →  double-underscore  __")]
    public static void RenameSlashToDoubleUnderscore()
    {
        BatchRename(
            oldName => oldName.Contains("/"),
            oldName => oldName.Replace("/", "__"),
            "slash → __");
    }

    // ── Step 2 (optional): single _ → __ for bundles that were previously migrated with single _
    // Only converts the FIRST underscore of each segment boundary.
    // Use this if you previously ran the old "replace / with _" script.
    [MenuItem("Tools/Asset Bundles/Migration/2. Migrate bundles: single-underscore  _  →  double-underscore  __")]
    public static void MigrateSingleToDoubleUnderscore()
    {
        string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();
        var toRename = new List<(string oldName, string newName)>();

        foreach (string bundleName in allBundleNames)
        {
            // Skip bundles that already use __ or have no _
            if (bundleName.Contains("__") || !bundleName.Contains("_"))
                continue;

            // Convert first "_" to "__"
            int sep = bundleName.IndexOf('_');
            string newName = bundleName[..sep] + "__" + bundleName[(sep + 1)..];
            toRename.Add((bundleName, newName));
        }

        ApplyRenames(toRename, "single_ prefix → __");
    }

    // ── Utility ─────────────────────────────────────────────────────────────

    private static void BatchRename(
        System.Func<string, bool> filter,
        System.Func<string, string> transform,
        string label)
    {
        string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();
        var toRename = new List<(string oldName, string newName)>();

        foreach (string bundleName in allBundleNames)
        {
            if (filter(bundleName))
                toRename.Add((bundleName, transform(bundleName)));
        }

        ApplyRenames(toRename, label);
    }

    private static void ApplyRenames(List<(string oldName, string newName)> list, string label)
    {
        if (list.Count == 0)
        {
            Debug.Log($"[PAD:{label}] Không có bundle nào cần đổi tên.");
            return;
        }

        int renamed = 0;
        foreach (var (oldName, newName) in list)
        {
            if (oldName == newName) continue;

            string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(oldName);
            foreach (string path in assetPaths)
            {
                AssetImporter importer = AssetImporter.GetAtPath(path);
                if (importer != null)
                    importer.assetBundleName = newName;
            }

            AssetDatabase.RemoveAssetBundleName(oldName, true);
            renamed++;
            Debug.Log($"[PAD:{label}] {oldName} → {newName}  ({assetPaths.Length} assets)");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PAD:{label}] Hoàn tất: đổi tên {renamed} bundles.");
    }
}
#endif
