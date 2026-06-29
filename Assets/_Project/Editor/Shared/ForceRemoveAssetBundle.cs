#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ForceRemoveAssetBundle
{
    static ForceRemoveAssetBundle()
    {
        EditorApplication.delayCall += RemoveEmptyBundles;
    }

    [MenuItem("Tools/Asset Bundles/Force Remove Empty Bundles")]
    static void RemoveEmptyBundles()
    {
        var allNames = AssetDatabase.GetAllAssetBundleNames();
        int removed = 0;
        foreach (var name in allNames)
        {
            var assets = AssetDatabase.GetAssetPathsFromAssetBundle(name);
            if (assets.Length == 0)
            {
                bool ok = AssetDatabase.RemoveAssetBundleName(name, true);
                if (ok) removed++;
            }
        }
        AssetDatabase.RemoveUnusedAssetBundleNames();
        if (removed > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[ForceRemoveBundle] Đã xóa {removed} bundle name rỗng (stale).");
        }
    }
}
#endif
