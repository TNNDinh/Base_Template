using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.PSD;
using System.IO;
using System.Collections.Generic;
// using UnityEditor.AddressableAssets;
// using UnityEditor.AddressableAssets.Settings;
// using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.Rendering;

public class PSDImporterHandler : AssetPostprocessor
{
    // Những file mà ta đã yêu cầu reimport (để tránh loop)
    private static HashSet<string> scheduledForReimport = new HashSet<string>();

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            string ext = Path.GetExtension(assetPath).ToLower();

            bool isPsd = ext == ".psd";
            bool isSkeletonData = assetPath.EndsWith("_SkeletonData.asset");

            if (!isPsd && !isSkeletonData)
                continue;

// 1. Lấy đường dẫn folder chứa file
            string folderPath = Path.GetDirectoryName(assetPath).Replace("\\", "/");
        
            // 2. Lấy TÊN của folder cuối cùng (folder cha trực tiếp)
            string parentFolderName = Path.GetFileName(folderPath); 

            // 3. Kiểm tra điều kiện tên folder
            // - Bắt đầu bằng "Scene_" (ví dụ Scene_01, Scene_02...)
            // - Hoặc chính xác là "Decors"
            bool isSceneSpecific = parentFolderName.StartsWith("Scene_");
            string grandParentPath = Path.GetDirectoryName(folderPath);
            string grandParentName = !string.IsNullOrEmpty(grandParentPath) ? Path.GetFileName(grandParentPath) : "";
            bool isDecorsFolder = parentFolderName.Equals("Decors", StringComparison.OrdinalIgnoreCase) || 
                                  grandParentName.Equals("Decors", StringComparison.OrdinalIgnoreCase);

            // Nếu folder cha trực tiếp không phải 2 loại trên thì bỏ qua hoàn toàn
            if (!isSceneSpecific && !isDecorsFolder)
            {
                // Ví dụ: file nằm trong Scene_01/sprites_decor/file.psd -> parentFolderName là "sprites_decor" -> Sẽ bị bỏ qua ở đây
                continue;
            }

            Debug.Log($"[PSDImporterHandler] Processing validated asset in {parentFolderName}: {assetPath}");

            // =====================
            // SPINE (Decor)
            // =====================
            if (isSkeletonData)
            {
                EditorApplication.delayCall += () => AttachSpineDecorToSceneVariant(assetPath);
                continue;
            }

            // =====================
            // PSD (Scene)
            // =====================
            if (scheduledForReimport.Contains(assetPath))
            {
                scheduledForReimport.Remove(assetPath);
                Debug.Log($"[PSDImporterHandler] Post-reimport callback for: {assetPath}");
                string p = assetPath;
                EditorApplication.delayCall += () => HandlePSDImported(p);
                continue;
            }

            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
                continue;

            Debug.Log($"[PSDImporterHandler] Detected PSD: {assetPath} importer = {importer.GetType().Name}");

            if (importer is TextureImporter)
            {
                Debug.Log($"[PSDImporterHandler] Overriding importer -> PSDImporter for: {assetPath}");
                AssetDatabase.SetImporterOverride<PSDImporter>(assetPath);
                scheduledForReimport.Add(assetPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                continue;
            }

            if (importer is PSDImporter psdImporter)
            {
                Debug.Log($"[PSDImporterHandler] Configuring PSDImporter for: {assetPath}");

                var so = new SerializedObject(psdImporter);
                bool changed = false;

                // ======= GIỮ NGUYÊN TOÀN BỘ ĐOẠN CHỈNH MIPMAP / PPU / MULTIPLE / PIVOT =======

                // 1) tắt mipmap
                var mip = so.FindProperty("m_MipMaps") ??
                          so.FindProperty("mipmapEnabled") ??
                          so.FindProperty("m_MipmapEnabled");
                if (mip != null && mip.propertyType == SerializedPropertyType.Boolean && mip.boolValue)
                {
                    mip.boolValue = false;
                    changed = true;
                    Debug.Log("[PSDImporterHandler] Disabled mipmaps");
                }

                // 2) set Pixels Per Unit = 100
                var ppu = so.FindProperty("m_SpritePixelsPerUnit") ?? so.FindProperty("spritePixelsPerUnit");
                if (ppu != null && (ppu.propertyType == SerializedPropertyType.Float ||
                                    ppu.propertyType == SerializedPropertyType.Integer))
                {
                    float cur = (ppu.propertyType == SerializedPropertyType.Float) ? ppu.floatValue : ppu.intValue;
                    if (Mathf.Abs(cur - 100f) > 0.01f)
                    {
                        if (ppu.propertyType == SerializedPropertyType.Float) ppu.floatValue = 100f;
                        else ppu.intValue = 100;
                        changed = true;
                        Debug.Log("[PSDImporterHandler] Set PPU = 100");
                    }
                }

                // 3) SpriteImportMode = Multiple
                var sim = so.FindProperty("m_SpriteImportMode") ?? so.FindProperty("spriteImportMode");
                if (sim != null)
                {
                    if (sim.propertyType == SerializedPropertyType.Enum)
                    {
                        if (sim.enumValueIndex != (int)SpriteImportMode.Multiple)
                        {
                            sim.enumValueIndex = (int)SpriteImportMode.Multiple;
                            changed = true;
                            Debug.Log("[PSDImporterHandler] Set SpriteImportMode = Multiple");
                        }
                    }
                    else if (sim.propertyType == SerializedPropertyType.Integer)
                    {
                        if (sim.intValue != (int)SpriteImportMode.Multiple)
                        {
                            sim.intValue = (int)SpriteImportMode.Multiple;
                            changed = true;
                            Debug.Log("[PSDImporterHandler] Set SpriteImportMode (int) = Multiple");
                        }
                    }
                }

                // 4) Pivot + Alignment trong m_SpriteImportData
                var spriteImportData = so.FindProperty("m_SpriteImportData");
                if (spriteImportData != null && spriteImportData.isArray)
                {
                    for (int i = 0; i < spriteImportData.arraySize; i++)
                    {
                        var elem = spriteImportData.GetArrayElementAtIndex(i);
                        if (elem == null) continue;

                        var pivotProp = elem.FindPropertyRelative("pivot");
                        if (pivotProp != null && pivotProp.propertyType == SerializedPropertyType.Vector2)
                        {
                            pivotProp.vector2Value = new Vector2(0.5f, 0.5f);
                            changed = true;
                        }

                        var alignProp = elem.FindPropertyRelative("alignment");
                        if (alignProp != null)
                        {
                            if (alignProp.propertyType == SerializedPropertyType.Enum)
                                alignProp.enumValueIndex = (int)SpriteAlignment.Center;
                            else if (alignProp.propertyType == SerializedPropertyType.Integer)
                                alignProp.intValue = (int)SpriteAlignment.Center;
                            changed = true;
                        }
                    }

                    if (changed) Debug.Log("[PSDImporterHandler] Set per-sprite pivot/alignment in m_SpriteImportData");
                }

                // 5) Heuristic: dò property chứa "pivot"/"alignment"
                var iter = so.GetIterator();
                while (iter.NextVisible(true))
                {
                    string pname = iter.propertyPath.ToLower();
                    if (pname.Contains("pivot") || pname.Contains("alignment") || pname.Contains("character") ||
                        pname.Contains("rig"))
                    {
                        if (iter.propertyType == SerializedPropertyType.Vector2)
                        {
                            iter.vector2Value = new Vector2(0.5f, 0.5f);
                            changed = true;
                            Debug.Log("[PSDImporterHandler] Set Vector2 pivot for: " + iter.propertyPath);
                        }
                        else if (iter.propertyType == SerializedPropertyType.Enum)
                        {
                            iter.enumValueIndex = (int)SpriteAlignment.Center;
                            changed = true;
                            Debug.Log("[PSDImporterHandler] Set enum alignment for: " + iter.propertyPath);
                        }
                        else if (iter.propertyType == SerializedPropertyType.Integer)
                        {
                            iter.intValue = (int)SpriteAlignment.Center;
                            changed = true;
                            Debug.Log("[PSDImporterHandler] Set int alignment for: " + iter.propertyPath);
                        }
                    }
                }

                // Nếu có thay đổi → reimport lại 1 lần
                if (changed)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    scheduledForReimport.Add(assetPath);

                    string p = assetPath;
                    EditorApplication.delayCall += () =>
                    {
                        var imp = AssetImporter.GetAtPath(p) as PSDImporter;
                        if (imp != null)
                        {
                            imp.SaveAndReimport();
                            Debug.Log("[PSDImporterHandler] SaveAndReimport called for: " + p);
                        }
                        else
                        {
                            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
                        }
                    };
                    continue;
                }

                // Không cần reimport → tạo prefab luôn
                string localCopy = assetPath;
                EditorApplication.delayCall += () => HandlePSDImported(localCopy);
            }
        }
    }


    private static bool FolderIs(string path, string keyword)
    {
        if (string.IsNullOrEmpty(path)) return false;

        // tách tất cả folder ra
        string[] parts = path.Split('/', '\\');

        foreach (var part in parts)
        {
            if (part.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }


    private static void HandlePSDImported(string psdPath)
    {
        // Lấy folder chứa trực tiếp file PSD
        string folder = Path.GetDirectoryName(psdPath).Replace("\\", "/");

        // Chỉ xử lý khi folder là "Scene" hoặc "Decors"
        if (FolderIs(folder, "Scene"))
        {
            // File nằm trong thư mục Scene -> tạo scene variant
            CreateSceneVariant(psdPath);
            return;
        }
        else if (FolderIs(folder, "Decors"))
        {
            // File nằm trong thư mục Decors -> tạo decor variant và attach
            AttachSpineDecorToSceneVariant(psdPath);
            return;
        }

        // Không phải Scene/Decors -> bỏ qua
        Debug.Log($"[PSDImporterHandler] Bỏ qua vì không nằm trong folder Scene/Decors: {psdPath}");
    }

    private static void CreateSceneVariant(string scenePsdPath)
    {
        GameObject psdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(scenePsdPath);
        if (psdPrefab == null) return;

        string folder = Path.GetDirectoryName(scenePsdPath).Replace("\\", "/"); // .../Scene_<Theme>
        string variantPath = Path.Combine(folder, Path.GetFileNameWithoutExtension(scenePsdPath) + "_Variant.prefab")
            .Replace("\\", "/");

        if (!File.Exists(variantPath))
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(psdPrefab);
            var spriteRenderers = instance.GetComponentsInChildren<SpriteRenderer>(true);

            foreach (var sr in spriteRenderers)
            {
                string goName = sr.gameObject.name;

                // Giữ bật nếu:
                // - Tên bắt đầu bằng "0" (layer cố định như cũ)
                // - HOẶC tên chứa từ khóa "background" (khống phân biệt hoa/thường)
                // bool shouldKeepEnabled = goName.StartsWith("0")
                //                          || goName.IndexOf("background", StringComparison.OrdinalIgnoreCase) >= 0;
                bool shouldKeepEnabled = false;

                if (shouldKeepEnabled)
                    continue; // giữ nguyên enabled = true

                // Các layer còn lại → tắt đi
                sr.enabled = false;
            }

            PrefabUtility.SaveAsPrefabAsset(instance, variantPath);
            GameObject.DestroyImmediate(instance);
            Debug.Log("[PSDImporterHandler] Created Scene Variant: " + variantPath);
        }


        // ✅ Addressable theo nhóm theme
        // ThemeType theme = InferThemeFromPath(folder);
        // string themeName = ThemeName(theme);
        // string themeGroup = $"Theme_{themeName}";
        // string address = $"Scenes/Scene_{themeName}_Variant";
        // MarkAsAddressable(variantPath, themeGroup, address, $"Theme:{themeName}", "Scene");

        // string psdAddress = $"Scenes/Scene_{themeName}_PSD";
        // MarkAsAddressable(scenePsdPath, themeGroup, psdAddress, $"Theme:{themeName}", "Scene", "RawPSD");

        // ✅ Cập nhật SO (có worldSize)
        var sceneVariant = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
        Vector2 worldSize = GetWorldSizeFromAsset(scenePsdPath, sceneVariant);

        // Fallback to texture importer if prefab method fails
        if (worldSize == Vector2.zero)
        {
            worldSize = GetWorldSizeFromAsset(scenePsdPath);
            Debug.Log($"[PSDImporterHandler] Fallback to TextureImporter size: {worldSize}");
        }

        // removed: UpdateAddressBuildUpSO (BuildUpGoal gameplay removed)
    }

    private static Vector2 GetWorldSizeFromAsset(string assetPath)
    {
        Debug.Log($"[PSDImporterHandler] Getting world size for: {assetPath}");

        // Thử TextureImporter trước (PNG/JPG)
        var texImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (texImporter != null)
        {
            int w, h;
            texImporter.GetSourceTextureWidthAndHeight(out w, out h);
            float ppu = texImporter.spritePixelsPerUnit > 0 ? texImporter.spritePixelsPerUnit : 100f;
            Debug.Log($"[PSDImporterHandler] TextureImporter size = {w}x{h}, ppu={ppu}");
            return new Vector2(w / ppu, h / ppu);
        }

        // PSDImporter - lấy size trực tiếp từ texture
        var psdImporter = AssetImporter.GetAtPath(assetPath) as UnityEditor.U2D.PSD.PSDImporter;
        if (psdImporter != null)
        {
            // Load texture trực tiếp từ PSD
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture != null)
            {
                float ppu = psdImporter.spritePixelsPerUnit > 0 ? psdImporter.spritePixelsPerUnit : 100f;
                Debug.Log($"[PSDImporterHandler] PSD texture raw size = {texture.width}x{texture.height}, ppu={ppu}");
                Debug.Log($"[PSDImporterHandler] Expected: {2535f / ppu:F2} x {3803f / ppu:F2}");

                // Đúng theo PSD: width = 2535, height = 3803
                float expectedWidth = 2535f / ppu;
                float expectedHeight = 3803f / ppu;

                Debug.Log($"[PSDImporterHandler] Calculated: {expectedWidth:F2} x {expectedHeight:F2}");
                return new Vector2(expectedWidth, expectedHeight);
            }
            else
            {
                Debug.LogWarning($"[PSDImporterHandler] Không load được texture từ {assetPath}");
            }
        }

        Debug.LogWarning($"[PSDImporterHandler] Không tìm thấy importer hợp lệ cho {assetPath}");
        return Vector2.zero;
    }


    private static void AttachSpineDecorToSceneVariant(string skeletonDataPath)
    {
        var skeletonData = AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>(skeletonDataPath);
        if (skeletonData == null)
        {
            Debug.LogWarning($"[PSDImporterHandler] Không tìm thấy SkeletonDataAsset: {skeletonDataPath}");
            return;
        }

        string decorFolder = Path.GetDirectoryName(skeletonDataPath).Replace("\\", "/");
        string decorsRoot = Path.GetDirectoryName(decorFolder).Replace("\\", "/");
        string sceneFolder = Path.GetDirectoryName(decorsRoot).Replace("\\", "/");

        string[] sceneVariants = Directory.GetFiles(sceneFolder, "*_Variant.prefab");
        if (sceneVariants.Length == 0)
        {
            Debug.LogWarning($"[PSDImporterHandler] Không tìm thấy scene variant trong {sceneFolder}");
            return;
        }

        string sceneVariantPath = sceneVariants[0];
        GameObject sceneInstance = PrefabUtility.LoadPrefabContents(sceneVariantPath);

        string parentName = Path.GetFileName(decorFolder);
        Transform target = sceneInstance.transform.Find(parentName);
        if (target == null)
        {
            Debug.LogWarning($"[PSDImporterHandler] Không tìm thấy child '{parentName}' trong {sceneVariantPath}");
            PrefabUtility.UnloadPrefabContents(sceneInstance);
            return;
        }

        if (target.GetComponentInChildren<Spine.Unity.SkeletonAnimation>() != null)
        {
            Debug.Log($"[PSDImporterHandler] SkeletonAnimation cho '{parentName}' đã tồn tại, bỏ qua.");
            PrefabUtility.UnloadPrefabContents(sceneInstance);
            return;
        }

        // Tạo Spine GameObject
        GameObject spineGO = new GameObject(skeletonData.name);
        var skeletonAnim = spineGO.AddComponent<Spine.Unity.SkeletonAnimation>();
        skeletonAnim.skeletonDataAsset = skeletonData;
        skeletonAnim.Initialize(true);

        // Đồng bộ sorting từ parent
        var mr = skeletonAnim.GetComponent<MeshRenderer>();
        var srParent = target.GetComponent<SpriteRenderer>();
        if (mr != null && srParent != null)
        {
            mr.sortingLayerID = srParent.sortingLayerID;
            mr.sortingOrder = srParent.sortingOrder;
        }

        spineGO.transform.SetParent(target, false);

        // === QUY TẮC MỚI: active dựa vào tên có chứa "_destroy" hay không ===
        string assetName = spineGO.transform.parent.gameObject.name;
        bool hasDestroy = assetName.IndexOf("_destroy", StringComparison.OrdinalIgnoreCase) >= 0;
        bool hasBoth = assetName.IndexOf("_both", StringComparison.OrdinalIgnoreCase) >= 0;
        bool isDestroyVersion = hasDestroy || hasBoth;

        //spineGO.SetActive(isDestroyVersion);

        Debug.LogWarning($"[CHECK ACTIVE] Asset: '{assetName}' | hasDestroy: {hasDestroy} | hasBoth: {hasBoth} | Final Active: {isDestroyVersion}");

        // Lưu lại prefab
        PrefabUtility.SaveAsPrefabAsset(sceneInstance, sceneVariantPath);
        PrefabUtility.UnloadPrefabContents(sceneInstance);

        Debug.Log(
            $"[PSDImporterHandler] Attached Spine decor '{skeletonData.name}' vào Scene Variant {sceneVariantPath}");
    }


    private static void CreateDecorVariantAndAttach(string decorJsonPath)
    {
        // Load SkeletonDataAsset từ file json
        var skeletonData = AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>(decorJsonPath);
        if (skeletonData == null)
        {
            Debug.LogWarning($"[PSDImporterHandler] Không tìm thấy SkeletonDataAsset cho {decorJsonPath}");
            return;
        }

        string decorFolder = Path.GetDirectoryName(decorJsonPath).Replace("\\", "/"); // .../Scene_<Theme>/Decors
        string sceneFolder = Path.GetDirectoryName(decorFolder).Replace("\\", "/"); // .../Scene_<Theme>

        string decorVariantFolder = Path.Combine(sceneFolder, "DecorVariants").Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(decorVariantFolder))
            AssetDatabase.CreateFolder(sceneFolder, "DecorVariants");

        string variantPath = Path.Combine(decorVariantFolder, skeletonData.name + "_Variant.prefab").Replace("\\", "/");

        // Tạo GameObject instance từ SkeletonAnimation
        GameObject go = new GameObject(skeletonData.name);
        var skeletonAnim = go.AddComponent<Spine.Unity.SkeletonAnimation>();
        skeletonAnim.skeletonDataAsset = skeletonData;
        skeletonAnim.Initialize(true);

        // ✅ Bảo đảm có SortingGroup
        var sg = go.GetComponent<SortingGroup>();
        if (sg == null) sg = go.AddComponent<SortingGroup>();
        sg.sortingLayerID = SortingLayer.NameToID("Default");
        sg.sortingOrder = 0;

        GameObject variantPrefab = PrefabUtility.SaveAsPrefabAsset(go, variantPath);
        GameObject.DestroyImmediate(go);

        Debug.Log("[PSDImporterHandler] Created Decor Variant (Spine): " + variantPath);

        // ✅ Addressable theo nhóm theme
        // ThemeType theme = InferThemeFromPath(sceneFolder);
        // string themeName = ThemeName(theme);
        // string themeGroup = $"Theme_{themeName}";
        // string address = $"Decors/{themeName}/{Path.GetFileNameWithoutExtension(variantPath)}";
        // // Labels: Theme:<Name>, Decor
        // MarkAsAddressable(variantPath, themeGroup, address, $"Theme:{themeName}", "Decor");

        // ✅ Cập nhật SO
        // UpdateAddressBuildUpSO(sceneFolder, null, variantPrefab);

        // Attach vào Scene Variant như cũ
        string[] sceneVariants = Directory.GetFiles(sceneFolder, "*_Variant.prefab");
        if (sceneVariants.Length == 0)
        {
            Debug.LogWarning($"[PSDImporterHandler] Không tìm thấy scene variant trong {sceneFolder}");
            return;
        }

        string sceneVariantPath = sceneVariants[0];
        AttachDecorToSceneVariant(variantPrefab, sceneVariantPath, skeletonData.name);
    }


    private static void AttachDecorToSceneVariant(GameObject decorVariant, string sceneVariantPath, string childName)
    {
        GameObject sceneInstance = PrefabUtility.LoadPrefabContents(sceneVariantPath);

        Transform target = sceneInstance.transform.Find(childName);
        if (target == null)
        {
            Debug.LogWarning($"[PSDImporterHandler] Không tìm thấy child '{childName}' trong {sceneVariantPath}");
            PrefabUtility.UnloadPrefabContents(sceneInstance);
            return;
        }

        // kiểm tra xem đã có child với đúng tên chưa
        string expectedName = decorVariant.name;
        Transform existing = target.Find(expectedName);
        if (existing != null)
        {
            Debug.Log($"[PSDImporterHandler] {expectedName} đã tồn tại trong {sceneVariantPath}, bỏ qua spawn mới.");
            PrefabUtility.UnloadPrefabContents(sceneInstance);
            return;
        }

        // chưa có -> spawn thêm
        GameObject newChild = (GameObject)PrefabUtility.InstantiatePrefab(decorVariant, target);
        newChild.name = expectedName;

// ✅ Đồng bộ Sorting theo parent (target)
        if (TryGetSorting(target, out int parentLayerId, out int parentOrder))
        {
            var childSG = newChild.GetComponent<SortingGroup>();
            if (childSG == null) childSG = newChild.AddComponent<SortingGroup>();
            childSG.sortingLayerID = parentLayerId;
            childSG.sortingOrder = parentOrder;
        }
        else
        {
            // fallback: vẫn bảo đảm có SortingGroup (nếu chưa có)
            var childSG = newChild.GetComponent<SortingGroup>();
            if (childSG == null) newChild.AddComponent<SortingGroup>();
        }

        PrefabUtility.SaveAsPrefabAsset(sceneInstance, sceneVariantPath);
        PrefabUtility.UnloadPrefabContents(sceneInstance);

        Debug.Log($"[PSDImporterHandler] Attached {decorVariant.name} as child of {childName} in {sceneVariantPath}");
    }

    private static bool TryGetSorting(Transform t, out int layerId, out int order)
    {
        var sg = t.GetComponent<SortingGroup>();
        if (sg != null)
        {
            layerId = sg.sortingLayerID;
            order = sg.sortingOrder;
            return true;
        }

        var sr = t.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            layerId = sr.sortingLayerID;
            order = sr.sortingOrder;
            return true;
        }

        layerId = SortingLayer.NameToID("Default");
        order = 0;
        return false;
    }


    #region Addressable

    // private static AddressableAssetSettings GetOrCreateSettings()
    // {
    //     return AddressableAssetSettingsDefaultObject.GetSettings(true);
    // }

    // private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
    // {
    //     var group = settings.FindGroup(groupName);
    //     if (group == null)
    //     {
    //         group = settings.CreateGroup(
    //             groupName,
    //             setAsDefaultGroup: false,
    //             readOnly: false,
    //             postEvent: true,
    //             schemasToCopy: settings.DefaultGroup != null ? settings.DefaultGroup.Schemas : null
    //         );

    //         if (group.Schemas.Count == 0)
    //         {
    //             group.AddSchema<BundledAssetGroupSchema>();
    //             group.AddSchema<ContentUpdateGroupSchema>();
    //         }

    //         // Gợi ý cấu hình runtime Android
    //         var bundled = group.GetSchema<BundledAssetGroupSchema>();
    //         if (bundled != null)
    //         {
    //             // Tuỳ nhu cầu: PackTogether để prefetch 1 phát cả theme, hoặc PackSeparately để stream từng decor
    //             if (bundled.BundleMode != BundledAssetGroupSchema.BundlePackingMode.PackTogether)
    //                 bundled.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;

    //             if (bundled.Compression != BundledAssetGroupSchema.BundleCompressionMode.LZ4)
    //                 bundled.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
    //         }

    //         Debug.Log($"[PSDImporterHandler] Created Addressables Group: {groupName}");
    //     }

    //     return group;
    // }

    // private static void EnsureLabels(AddressableAssetSettings settings, params string[] labels)
    // {
    //     if (labels == null) return;
    //     var existing = settings.GetLabels();
    //     foreach (var l in labels)
    //     {
    //         if (string.IsNullOrEmpty(l)) continue;
    //         if (!existing.Contains(l)) settings.AddLabel(l);
    //     }
    // }

    // private static void MarkAsAddressable(string assetPath, string groupName, string address = null,
    //     params string[] labels)
    // {
    //     var settings = GetOrCreateSettings();
    //     var group = GetOrCreateGroup(settings, groupName);

    //     string guid = AssetDatabase.AssetPathToGUID(assetPath);
    //     if (string.IsNullOrEmpty(guid))
    //     {
    //         Debug.LogWarning($"[PSDImporterHandler] Cannot mark as addressable, invalid GUID: {assetPath}");
    //     }

    //     var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: true);
    //     entry.address = string.IsNullOrEmpty(address)
    //         ? Path.GetFileNameWithoutExtension(assetPath)
    //         : address;

    //     if (labels != null && labels.Length > 0)
    //     {
    //         EnsureLabels(settings, labels);
    //         foreach (var l in labels) entry.SetLabel(l, true);
    //     }

    //     EditorUtility.SetDirty(settings);
    //     AssetDatabase.SaveAssets();

    //     Debug.Log($"[PSDImporterHandler] Addressable: '{entry.address}' -> Group '{groupName}' | Path: {assetPath}");
    // }


    // removed: GetOrCreateSOForSceneFolder (AddressBuildUpGoalSO — BuildUpGoal gameplay removed)

    // Cố gắng suy ra ThemeType từ đường dẫn: ưu tiên tên folder cha của sceneFolder (thường là theme)
    private static ThemeType InferThemeFromPath(string sceneFolder)
    {
        string folderName = Path.GetFileName(sceneFolder); // "Scene_Farm"
        if (string.IsNullOrEmpty(folderName)) return default;

        string[] parts = folderName.Split('_');
        if (parts.Length > 1)
        {
            string themePart = parts[parts.Length - 1]; // "Farm"
            if (Enum.TryParse(themePart, ignoreCase: true, out ThemeType parsed))
                return parsed;

            Debug.LogWarning($"[PSDImporterHandler] Không parse được ThemeType từ '{themePart}' trong {folderName}");
        }

        return default;
    }

    private static string ThemeName(ThemeType theme) => theme.ToString();


    // removed: UpdateAddressBuildUpSO (AddressBuildUpGoalSO/Model — BuildUpGoal gameplay removed)

    #endregion

    private static Vector2 GetWorldSizeFromAsset(string assetPath, GameObject prefab)
    {
        if (prefab != null)
        {
            var bg = FindDeepChild(prefab.transform, "BackGround");
            if (bg != null)
            {
                var sr = bg.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    float w = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                    float h = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
                    Debug.Log(
                        $"[PSDImporterHandler] Prefab BackGround size = {sr.sprite.rect.width}x{sr.sprite.rect.height}, ppu={sr.sprite.pixelsPerUnit}");
                    return new Vector2(w, h);
                }
            }
        }

        Debug.LogWarning($"[PSDImporterHandler] Không tìm thấy BackGround trong prefab của {assetPath}");
        return Vector2.zero;
    }

// Utility tìm child sâu
    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var result = FindDeepChild(child, name);
            if (result != null) return result;
        }

        return null;
    }
}