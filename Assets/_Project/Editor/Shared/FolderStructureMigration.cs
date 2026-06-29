using Ezg.Feature.Shared.Systems;
#if UNITY_EDITOR
using System.IO;
using Ezg.Core.Extensions;
using UnityEditor;
using UnityEngine;
using Ezg.Feature.System.OverviewCanvas;

/// <summary>
/// Folder Structure Migration — Merge Two
/// Run each phase IN ORDER from the Tools/Migration/ menu.
/// Each phase uses raw file-system moves (Directory.Move + .meta move) to avoid
/// Unity's "Access is denied" lock on AssetDatabase.MoveAsset for large folders.
/// GUIDs are preserved because .meta files travel with the assets.
/// Check Console for [Migration] FAIL lines after each phase before proceeding.
/// </summary>
public static class FolderStructureMigration
{
    private const string ROOT_ASSET = "Assets/_Project/";
    static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    static string ToFull(string assetPath) => Path.Combine(ProjectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));

    // ─── HELPERS ──────────────────────────────────────────────────────────────

    /// Move a folder (or file) using raw file system. Moves item + its .meta file.
    /// dst must NOT exist yet (use MergeFS when dst exists).
    /// NOTE: uses recursive file-by-file move instead of Directory.Move to avoid
    /// Windows "Access is denied" caused by Unity's file watcher holding directory handles.
    static bool MoveFS(string srcAsset, string dstAsset)
    {
        var src     = ToFull(srcAsset);
        var dst     = ToFull(dstAsset);
        var srcMeta = src + ".meta";
        var dstMeta = dst + ".meta";

        if (!Directory.Exists(src) && !File.Exists(src))
        { Debug.Log($"[Migration] SKIP {srcAsset} (not found — already moved)"); return true; }

        if (Directory.Exists(dst) || File.Exists(dst))
        { Debug.LogError($"[Migration] FAIL {srcAsset} — destination already exists: {dstAsset}. Use SafeMove."); return false; }

        var dstParent = Path.GetDirectoryName(dst);
        if (!Directory.Exists(dstParent)) Directory.CreateDirectory(dstParent);

        // ── FILE ──
        if (File.Exists(src))
        {
            File.Move(src, dst);
            if (File.Exists(srcMeta)) File.Move(srcMeta, dstMeta);
            Debug.Log($"[Migration] OK   {srcAsset} → {dstAsset}");
            return true;
        }

        // ── DIRECTORY ──
        // Do NOT use Directory.Move — Unity's file watcher holds the directory handle on Windows.
        // Instead: create dst, recursively move all contents, move folder .meta, delete empty src.
        Directory.CreateDirectory(dst);

        foreach (var sub in Directory.GetDirectories(src))
            MoveFS(srcAsset + "/" + Path.GetFileName(sub), dstAsset + "/" + Path.GetFileName(sub));

        foreach (var file in Directory.GetFiles(src))
        {
            if (file.EndsWith(".meta")) continue;
            MoveFileFS(srcAsset + "/" + Path.GetFileName(file), dstAsset + "/" + Path.GetFileName(file));
        }

        // Move any leftover files (orphaned .meta or other stragglers)
        foreach (var file in Directory.GetFiles(src))
        {
            var name    = Path.GetFileName(file);
            var dstFile = Path.Combine(dst, name);
            if (!File.Exists(dstFile)) File.Move(file, dstFile);
        }

        // Move folder's own .meta AFTER contents so dst folder inherits src's GUID
        if (File.Exists(srcMeta)) File.Move(srcMeta, dstMeta);

        try { Directory.Delete(src); }
        catch { Debug.LogWarning($"[Migration] Warning: could not delete (not empty): {srcAsset}"); }

        Debug.Log($"[Migration] OK   {srcAsset} → {dstAsset}");
        return true;
    }

    /// Move a single file using raw file system. Moves file + its .meta file.
    static bool MoveFileFS(string srcAsset, string dstAsset)
    {
        var src     = ToFull(srcAsset);
        var dst     = ToFull(dstAsset);
        var srcMeta = src + ".meta";
        var dstMeta = dst + ".meta";

        if (!File.Exists(src))
        { Debug.Log($"[Migration] SKIP {srcAsset} (not found — already moved)"); return true; }

        var dstParent = Path.GetDirectoryName(dst);
        if (!Directory.Exists(dstParent)) Directory.CreateDirectory(dstParent);

        if (File.Exists(dst)) { Debug.Log($"[Migration] SKIP {srcAsset} (dst file already exists)"); return true; }

        File.Move(src, dst);
        if (File.Exists(srcMeta)) File.Move(srcMeta, dstMeta);

        Debug.Log($"[Migration] OK   {srcAsset} → {dstAsset}");
        return true;
    }

    /// Merge all direct children (files + subdirs) of src into dst, then remove empty src.
    static void MergeFS(string srcAsset, string dstAsset)
    {
        var srcFull = ToFull(srcAsset);
        var dstFull = ToFull(dstAsset);

        if (!Directory.Exists(srcFull)) { Debug.Log($"[Migration] SKIP MergeFS {srcAsset} (not found)"); return; }
        if (!Directory.Exists(dstFull)) Directory.CreateDirectory(dstFull);

        foreach (var sub in Directory.GetDirectories(srcFull))
        {
            var name   = Path.GetFileName(sub);
            var srcSub = srcAsset + "/" + name;
            var dstSub = dstAsset + "/" + name;
            if (Directory.Exists(Path.Combine(dstFull, name))) MergeFS(srcSub, dstSub);
            else                                                MoveFS(srcSub, dstSub);
        }

        foreach (var file in Directory.GetFiles(srcFull))
        {
            if (file.EndsWith(".meta")) continue;
            var name = Path.GetFileName(file);
            MoveFileFS(srcAsset + "/" + name, dstAsset + "/" + name);
        }

        // Delete source folder only when truly empty (never recursive — prevents data loss)
        if (Directory.Exists(srcFull))
        {
            try
            {
                Directory.Delete(srcFull); // throws if not empty — that's intentional
                var metaFull = ToFull(srcAsset) + ".meta";
                if (File.Exists(metaFull)) File.Delete(metaFull);
                Debug.Log($"[Migration] MERGE done (src deleted): {srcAsset} → {dstAsset}");
            }
            catch
            {
                Debug.LogWarning($"[Migration] MERGE partial — src not empty after merge, left in place: {srcAsset}");
            }
        }
        else Debug.Log($"[Migration] MERGE done: {srcAsset} → {dstAsset}");
    }

    /// SafeMove: MoveFS if dst absent, MergeFS if dst present, skip if src absent.
    static void SafeMove(string srcAsset, string dstAsset)
    {
        var srcFull = ToFull(srcAsset);
        var dstFull = ToFull(dstAsset);
        if (!Directory.Exists(srcFull) && !File.Exists(srcFull))
        { Debug.Log($"[Migration] SKIP {srcAsset} (already moved)"); return; }
        if (Directory.Exists(dstFull)) MergeFS(srcAsset, dstAsset);
        else                           MoveFS(srcAsset, dstAsset);
    }

    // ─── PHASE 1 — Core/Localization ──────────────────────────────────────────
    [MenuItem("Tools/Migration/Phase 1 — Core Localization")]
    static void Phase1_CoreLocalization()
    {
        SafeMove(ROOT_ASSET + "Core/Localize", ROOT_ASSET + "Core/Localization");

        foreach (var f in new[] { "LanguageConfig.cs", "LocalizationCollection.cs", "LocalizationModel.cs" })
            MoveFileFS(ROOT_ASSET + "Core/Languages/" + f, ROOT_ASSET + "Core/Localization/" + f);

        // Delete empty Languages folder
        var langFull = ToFull(ROOT_ASSET + "Core/Languages");
        if (Directory.Exists(langFull)) { Directory.Delete(langFull, true); File.Delete(langFull + ".meta"); }

        AssetDatabase.Refresh();
        Debug.Log("[Migration] ✓ Phase 1 complete — Core/Localization");
    }

    // ─── PHASE 2 — Tutorial → Onboarding ──────────────────────────────────────
    [MenuItem("Tools/Migration/Phase 2 — Tutorial to Onboarding")]
    static void Phase2_Tutorial()
    {
        SafeMove(ROOT_ASSET + "Core/Modules/Tutorial", ROOT_ASSET + "Features/Onboarding/Tutorials");
        AssetDatabase.Refresh();
        Debug.Log("[Migration] ✓ Phase 2 complete — Tutorial to Onboarding");
    }

    // ─── PHASE 3 — Editor Scripts Cleanup ─────────────────────────────────────
    [MenuItem("Tools/Migration/Phase 3 — Editor Scripts Cleanup")]
    static void Phase3_EditorScripts()
    {
        MoveFileFS(ROOT_ASSET + "Core/Extensions/EditorExtensions.cs",     ROOT_ASSET + "Editor/EditorExtensions.cs");
        MoveFileFS(ROOT_ASSET + "Core/Extensions/BatchRenameAssetTool.cs", ROOT_ASSET + "Editor/BatchRenameAssetTool.cs");
        AssetDatabase.Refresh();
        Debug.Log("[Migration] ✓ Phase 3 complete — Editor Scripts Cleanup");
    }

    // ─── PHASE 4 — Core/Infrastructure ────────────────────────────────────────
    [MenuItem("Tools/Migration/Phase 4 — Core Infrastructure")]
    static void Phase4_CoreInfrastructure()
    {
        SafeMove(ROOT_ASSET + "Core/Modules/FirebaseModule",     ROOT_ASSET + "Core/Infrastructure/Firebase");
        SafeMove(ROOT_ASSET + "Core/Modules/Tracking",           ROOT_ASSET + "Core/Infrastructure/Analytics");
        SafeMove(ROOT_ASSET + "Core/Modules/Networking",         ROOT_ASSET + "Core/Infrastructure/Networking");
        SafeMove(ROOT_ASSET + "Core/Modules/NotificationNative", ROOT_ASSET + "Core/Infrastructure/Notifications");
        MergeFS( ROOT_ASSET + "Core/Modules/BaseNotification",   ROOT_ASSET + "Core/Infrastructure/Notifications");

        SafeMove(ROOT_ASSET + "Core/Modules/UIModule",  ROOT_ASSET + "Core/UI/Framework");
        SafeMove(ROOT_ASSET + "Core/Modules/UI",        ROOT_ASSET + "Core/UI/SharedComponents");
        SafeMove(ROOT_ASSET + "Core/Modules/CsvReader", ROOT_ASSET + "Core/Data/CsvReader");
        SafeMove(ROOT_ASSET + "Core/GameSystem",        ROOT_ASSET + "Core/Utils");
        SafeMove(ROOT_ASSET + "Core/DesignPattern",     ROOT_ASSET + "Core/Patterns");
        MergeFS( ROOT_ASSET + "Core/UpdateManager",     ROOT_ASSET + "Core/Utils");

        AssetDatabase.Refresh();
        Debug.Log("[Migration] ✓ Phase 4 complete — Core Infrastructure");
    }

    // ─── PHASE 5 — Features/UI Regrouping ─────────────────────────────────────
    [MenuItem("Tools/Migration/Phase 5 — Features UI Regrouping")]
    static void Phase5_UIRegrouping()
    {
        var ui = ROOT_ASSET + "Features/UI/";

        SafeMove(ui + "GamePlay", ROOT_ASSET + "Features/Gameplay");

        foreach (var f in new[] {
            "BattleRoyale", "FortuneMeetsCookie", "HappinessExpress",
            "OrderMania", "OrderManiaReward", "PetalPlateParty",
            "PizzaTowerRace", "SpeedFeastRace"
        }) SafeMove(ui + f, ROOT_ASSET + "Features/Events/" + f);

        foreach (var f in new[] {
            "ChefsBook", "HomeScene", "Inventory", "ItemInfo", "LevelUp",
            "MapEditor", "NewEquipment", "OpenZoneBuildUpGoal",
            "RenovationCompleted", "SelectTheme"
        }) SafeMove(ui + f, ROOT_ASSET + "Features/Meta/" + f);

        foreach (var f in new[] {
            "BattlePass", "BattlePassPackage", "BigFlashSale", "BuyCurrency",
            "ChainPack", "ChoiceChest", "DiscountGemRaw", "EnergyPack",
            "EnergyTrilogy", "InfinityPack", "LuckySpin", "OpenningPack",
            "OpenStarChest", "PiggyBank", "QuickBreak", "Shop",
            "SpeedPackage", "StarterPack", "WatchRateItemGen"
        }) SafeMove(ui + f, ROOT_ASSET + "Features/Monetization/" + f);

        foreach (var f in new[] { "SplashScene", "SelectLanguage", "VideoIntro" })
            SafeMove(ui + f, ROOT_ASSET + "Features/Onboarding/" + f);

        // Tutorials — SafeMove handles merge if Phase 2 already created the destination
        SafeMove(ui + "Tutorials", ROOT_ASSET + "Features/Onboarding/Tutorials");

        foreach (var f in new[] { "Account", "AvatarSelect", "ChangeName", "GiftCode" })
            SafeMove(ui + f, ROOT_ASSET + "Features/Social/" + f);

        foreach (var f in new[] {
            "Admin", "ConfirmExpandItem", "ConfirmSellRareItem", "DailyReward",
            "GameCheat", "NotEnoughCurrency", "OverviewCanvas", "PopupConfirm",
            "PopupNextTheme", "Rating", "RequireInternet", "RewardEventPopup",
            "RewardPopup", "SaveFound", "ScreenMaskChangeScene", "Settings",
            "Tooltip", "UnlockFeature", "VideoBonuses", "x_CompleteScene"
        }) SafeMove(ui + f, ROOT_ASSET + "Features/System/" + f);

        SafeMove(ui + "--Item", ROOT_ASSET + "Core/UI/SharedComponents/Items");

        AssetDatabase.Refresh();
        Debug.Log("[Migration] ✓ Phase 5 complete — Features/UI Regrouping");
    }

    // ─── PHASE 6 — Features/Systems → Features/_Shared ────────────────────────
    [MenuItem("Tools/Migration/Phase 6 — Systems to _Shared")]
    static void Phase6_SystemsToShared()
    {
        SafeMove(ROOT_ASSET + "Features/Systems", ROOT_ASSET + "Features/_Shared");
        AssetDatabase.Refresh();
        Debug.Log("[Migration] ✓ Phase 6 complete — Features/_Shared");
        Debug.Log("[Migration]   ACTION NEEDED: Check AssetBundleNamesGenerator.cs path reference to AssetRefs.cs");
    }

    // ─── PHASE 7 — 3rdParty EpicToonFX Consolidation ──────────────────────────
    [MenuItem("Tools/Migration/Phase 7 — 3rdParty EpicToonFX")]
    static void Phase7_EpicToonFX()
    {
        var tp = ROOT_ASSET + "3rdParty/";
        SafeMove(tp + "Epic Toon FX", tp + "EpicToonFX");
        foreach (var f in new[] { "Epic Toon FX1", "Epic Toon FX2", "Epic Toon FX3" })
            MergeFS(tp + f, tp + "EpicToonFX");
        AssetDatabase.Refresh();
        Debug.Log("[Migration] ✓ Phase 7 complete — EpicToonFX consolidation");
    }

    // ─── PHASE 8 — Visual/ Restructure ────────────────────────────────────────
    [MenuItem("Tools/Migration/Phase 8 — Visual Restructure")]
    static void Phase8_Visual()
    {
        var art = ROOT_ASSET + "Visual/ArtAsset/";
        var vis = ROOT_ASSET + "Visual/";

        MergeFS(art + "GUI", vis + "Sprites");
        MergeFS(art + "UI",  vis + "Sprites");
        SafeMove(art + "Materials",     vis + "Materials");
        SafeMove(art + "Shader",        vis + "Shaders");
        SafeMove(art + "ThirdPartyVFX", vis + "VFX");
        SafeMove(art + "Scripts",       vis + "Scripts");

        AssetDatabase.Refresh();
        Debug.Log("[Migration] ✓ Phase 8 complete — Visual Restructure");
    }

    // ─── PHASE 9 — Core/Build → Editor/Build ──────────────────────────────────
    [MenuItem("Tools/Migration/Phase 9 — Core Build to Editor")]
    static void Phase9_BuildToEditor()
    {
        SafeMove(ROOT_ASSET + "Core/Build", ROOT_ASSET + "Editor/Build");
        AssetDatabase.Refresh();
        Debug.Log("[Migration] ✓ Phase 9 complete — Core/Build to Editor/Build");
    }

    // ─── PHASE 10 — BugLogger + Discord → Core/DevTools ──────────────────────
    [MenuItem("Tools/Migration/Phase 10 — Core DevTools")]
    static void Phase10_DevTools()
    {
        SafeMove(ROOT_ASSET + "Core/Modules/BugLogger", ROOT_ASSET + "Core/DevTools/BugLogger");
        SafeMove(ROOT_ASSET + "Core/Modules/Discord",   ROOT_ASSET + "Core/DevTools/Discord");
        AssetDatabase.Refresh();
        Debug.Log("[Migration] ✓ Phase 10 complete — BugLogger+Discord to Core/DevTools");
    }

    // ─── PHASE 11 — Core/Utils.cs → Features/_Shared ─────────────────────────
    [MenuItem("Tools/Migration/Phase 11 — Utils to Features Shared")]
    static void Phase11_UtilsToFeatures()
    {
        MoveFileFS(ROOT_ASSET + "Core/Extensions/Utils.cs",     ROOT_ASSET + "Features/_Shared/Utils.cs");
        AssetDatabase.Refresh();
        Debug.Log("[Migration] ✓ Phase 11 complete — Utils.cs to Features/_Shared");
    }

    // ─── PHASE 12 — Core/ItemPreviewController → Features/_Shared ────────────
    [MenuItem("Tools/Migration/Phase 12 — ItemPreviewController to Features Shared")]
    static void Phase12_ItemPreviewToFeatures()
    {
        SafeMove(ROOT_ASSET + "Core/UI/SharedComponents/Items", ROOT_ASSET + "Features/_Shared/UI/Items");
        AssetDatabase.Refresh();
        Debug.Log("[Migration] ✓ Phase 12 complete — Items to Features/_Shared/UI/Items");
    }
}
#endif
