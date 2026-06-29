#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof(AssetBundleConfig))]
public class AssetBundleConfigEditor : Editor
{
    private const string PAD_DEFINE_SYMBOL = "USE_PAD";
    private const string BUNDLE_DEFINE_SYMBOL = "USE_BUNDLE";
    private GUIStyle boxStyle;

    private AssetBundleConfig config;

    private ReorderableList excludedFeaturesList;
    private ReorderableList extraFolderTargetsList;
    private GUIStyle greenLabelStyle;

    // Styles
    private GUIStyle headerStyle;
    private GUIStyle redLabelStyle;
    private Vector2 scrollPosition;
    private GUIStyle sectionHeaderStyle;
    private bool showExcludedFeatures = true;
    private bool showFolderRename;

    private bool showLoadingMode;
    private bool showPreview;
    private bool showPreviewSection;
    private GUIStyle yellowLabelStyle;

    private void OnEnable()
    {
        config = (AssetBundleConfig)target;
        config.useBundles = HasDefineSymbol(BUNDLE_DEFINE_SYMBOL);
        config.usePAD = HasDefineSymbol(PAD_DEFINE_SYMBOL);
        SetupExcludedFeaturesList();
        SetupExtraFolderTargetsList();
    }

    private void SetupExcludedFeaturesList()
    {
        excludedFeaturesList = new ReorderableList(
            serializedObject,
            serializedObject.FindProperty("excludedFeatures"),
            true, true, true, true
        );

        excludedFeaturesList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Feature Names (sẽ giữ nguyên Resources)");
        };

        excludedFeaturesList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var element = excludedFeaturesList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, element, GUIContent.none);
        };

        excludedFeaturesList.onAddCallback = list =>
        {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            list.serializedProperty.GetArrayElementAtIndex(index).stringValue = "NewFeature";
        };
    }

    private void SetupExtraFolderTargetsList()
    {
        extraFolderTargetsList = new ReorderableList(
            serializedObject,
            serializedObject.FindProperty("buildExtraFolderTargets"),
            true, true, true, true
        );

        extraFolderTargetsList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Folder targets (kéo folder vào đây)");
        };

        extraFolderTargetsList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var element = extraFolderTargetsList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;

            var obj = EditorGUI.ObjectField(rect, element.objectReferenceValue, typeof(Object), false);

            // Chỉ chấp nhận folder
            if (obj != null)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (!AssetDatabase.IsValidFolder(path))
                {
                    Debug.LogWarning($"[AssetBundleConfig] Chỉ chấp nhận folder, bỏ qua: {path}");
                    obj = element.objectReferenceValue;
                }
            }

            element.objectReferenceValue = obj;
        };
    }

    private void InitStyles()
    {
        if (headerStyle == null)
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 10, 10)
            };

        if (sectionHeaderStyle == null)
            sectionHeaderStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

        if (boxStyle == null)
            boxStyle = new GUIStyle("helpbox")
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 5, 5)
            };

        if (greenLabelStyle == null)
            greenLabelStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                fontStyle = FontStyle.Bold
            };

        if (redLabelStyle == null)
            redLabelStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.9f, 0.3f, 0.3f) },
                fontStyle = FontStyle.Bold
            };

        if (yellowLabelStyle == null)
            yellowLabelStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.9f, 0.7f, 0.1f) },
                fontStyle = FontStyle.Bold
            };
    }

    public override void OnInspectorGUI()
    {
        InitStyles();
        serializedObject.Update();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("🎮 Asset Bundle Configuration", headerStyle);
        EditorGUILayout.Space(5);

        DrawPlatformSelector();
        DrawLoadingModeSection();
        DrawExcludedFeaturesSection();
        DrawFolderRenameSection();
        DrawPreviewSection();
        DrawActionButtons();

        serializedObject.ApplyModifiedProperties();
    }

    // ─────────────────────────────────────────────────────────────
    // Platform Selector
    // ─────────────────────────────────────────────────────────────

    private void DrawPlatformSelector()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("🖥  Platform Mode", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();
        DrawPlatformButton("🤖  Android", PlatformBuildMode.Android, new Color(0.2f, 0.65f, 0.3f));
        DrawPlatformButton("💻  Editor", PlatformBuildMode.Editor, new Color(0.25f, 0.5f, 0.85f));
        DrawPlatformButton("🍎  iOS", PlatformBuildMode.iOS, new Color(0.75f, 0.35f, 0.1f));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // Status summary
        switch (config.currentMode)
        {
            case PlatformBuildMode.Android:
                EditorGUILayout.HelpBox(
                    "Android  •  USE_BUNDLE ✗  |  USE_PAD ✓  |  ForceResources ✗\n" +
                    "Folders : Resources  →  Prefabs",
                    MessageType.None);
                break;
            case PlatformBuildMode.Editor:
                EditorGUILayout.HelpBox(
                    "Editor  •  USE_BUNDLE ✓  |  USE_PAD ✗  |  ForceResources ✗\n" +
                    "Folders : Prefabs  →  Resources",
                    MessageType.None);
                break;
            case PlatformBuildMode.iOS:
                EditorGUILayout.HelpBox(
                    "iOS  •  USE_BUNDLE ✓  |  USE_PAD ✗  |  ForceResources ✗\n" +
                    "Folders : Resources  →  Prefabs  |  Load từ StreamingAssets",
                    MessageType.None);
                break;
            default:
                EditorGUILayout.HelpBox("Chưa chọn platform. Nhấn một trong 3 nút trên để apply.", MessageType.Warning);
                break;
        }

        if (config.renamedFeatureRoots != null && config.renamedFeatureRoots.Count > 0)
            EditorGUILayout.LabelField(
                $"📋  {config.renamedFeatureRoots.Count} feature(s) đang ở trạng thái Prefabs (sẽ được restore khi switch sang Editor / iOS)",
                EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    private void DrawPlatformButton(string label, PlatformBuildMode mode, Color activeColor)
    {
        var isActive = config.currentMode == mode;

        GUI.backgroundColor = isActive ? activeColor : new Color(0.45f, 0.45f, 0.45f);

        var style = new GUIStyle(GUI.skin.button)
        {
            fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal,
            fontSize = isActive ? 13 : 12,
            fixedHeight = 40
        };

        if (GUILayout.Button(label, style) && !isActive)
            ConfirmAndSwitch(mode);

        GUI.backgroundColor = Color.white;
    }

    private void ConfirmAndSwitch(PlatformBuildMode mode)
    {
        var name = mode.ToString();
        var details = mode switch
        {
            PlatformBuildMode.Android =>
                "• Rename Resources → Prefabs cho các feature\n" +
                "• Set USE_PAD (remove USE_BUNDLE)\n" +
                "• Lưu danh sách các feature đã đổi",
            PlatformBuildMode.Editor =>
                "• Rename Prefabs → Resources (từ danh sách đã lưu)\n" +
                "• Set USE_BUNDLE, remove USE_PAD\n" +
                "• ForceResources = false",
            _ =>
                "• Rename Resources → Prefabs cho các feature\n" +
                "• Set USE_BUNDLE (remove USE_PAD)\n" +
                "• Load bundle từ StreamingAssets trên device iOS"
        };

        if (!EditorUtility.DisplayDialog(
                $"Switch to {name}",
                $"Các thay đổi sẽ được thực hiện:\n\n{details}",
                "Xác nhận", "Hủy"))
            return;

        switch (mode)
        {
            case PlatformBuildMode.Android: ExecuteSwitchToAndroid(); break;
            case PlatformBuildMode.Editor: ExecuteSwitchToEditor(); break;
            case PlatformBuildMode.iOS: ExecuteSwitchToIOS(); break;
        }

        config.currentMode = mode;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    // ─────────────────────────────────────────────────────────────
    // Switch implementations
    // ─────────────────────────────────────────────────────────────

    private void ExecuteSwitchToAndroid()
    {
        // Rename Resources → Prefabs (đệ quy để bắt được Resources lồng, vd Shop/Store/Resources)
        var renamed = new List<string>();
        int success = 0, fail = 0;

        if (!Directory.Exists(config.featuresPath))
        {
            Debug.LogError($"[AssetBundleConfig] featuresPath not found: {config.featuresPath}");
            return;
        }

        foreach (var featureFolder in Directory.GetDirectories(config.featuresPath))
        {
            var featureName = Path.GetFileName(featureFolder);
            if (config.IsFeatureExcluded(featureName)) continue;

            var normalized = featureFolder.Replace("\\", "/");
            RenameResourcesToPrefabsRecursive(normalized, renamed, ref success, ref fail);
        }

        // Apply cho các extra folder targets — cùng pattern đệ quy
        foreach (var obj in config.buildExtraFolderTargets)
        {
            if (obj == null) continue;
            var p = AssetDatabase.GetAssetPath(obj);
            if (!AssetDatabase.IsValidFolder(p)) continue;
            RenameResourcesToPrefabsRecursive(p.Replace("\\", "/"), renamed, ref success, ref fail);
        }

        config.renamedFeatureRoots = renamed;

        SetDefine(BUNDLE_DEFINE_SYMBOL, false);
        SetDefine(PAD_DEFINE_SYMBOL, true);

        AssetDatabase.Refresh();
        config.useBundles = false;
        config.usePAD = true;

        Debug.Log($"[AssetBundleConfig] → Android done. Renamed {success} folders. Defines: USE_PAD only.");
        if (fail > 0)
            EditorUtility.DisplayDialog("Cảnh báo",
                $"Renamed {success}, thất bại {fail}. Xem Console để biết chi tiết.", "OK");
    }

    private void ExecuteSwitchToEditor()
    {
        RenameBackToResources();

        SetDefine(BUNDLE_DEFINE_SYMBOL, true);
        SetDefine(PAD_DEFINE_SYMBOL, false);

        AssetDatabase.Refresh();
        config.useBundles = true;
        config.usePAD = false;

        Debug.Log("[AssetBundleConfig] → Editor done. Folders restored. Defines: USE_BUNDLE only.");
    }

    private void ExecuteSwitchToIOS()
    {
        // Rename Resources → Prefabs giống Android để tránh Unity pack vào app
        var renamed = new List<string>();
        int success = 0, fail = 0;

        if (!Directory.Exists(config.featuresPath))
        {
            Debug.LogError($"[AssetBundleConfig] featuresPath not found: {config.featuresPath}");
            return;
        }

        foreach (var featureFolder in Directory.GetDirectories(config.featuresPath))
        {
            var featureName = Path.GetFileName(featureFolder);
            if (config.IsFeatureExcluded(featureName)) continue;

            var normalized = featureFolder.Replace("\\", "/");
            RenameResourcesToPrefabsRecursive(normalized, renamed, ref success, ref fail);
        }

        foreach (var obj in config.buildExtraFolderTargets)
        {
            if (obj == null) continue;
            var p = AssetDatabase.GetAssetPath(obj);
            if (!AssetDatabase.IsValidFolder(p)) continue;
            RenameResourcesToPrefabsRecursive(p.Replace("\\", "/"), renamed, ref success, ref fail);
        }

        config.renamedFeatureRoots = renamed;

        SetDefine(BUNDLE_DEFINE_SYMBOL, true);
        SetDefine(PAD_DEFINE_SYMBOL, false);

        AssetDatabase.Refresh();
        config.useBundles = true;
        config.usePAD = false;

        Debug.Log(
            $"[AssetBundleConfig] → iOS done. Renamed {success} folders. Defines: USE_BUNDLE only. Load từ StreamingAssets.");
        if (fail > 0)
            EditorUtility.DisplayDialog("Cảnh báo",
                $"Renamed {success}, thất bại {fail}. Xem Console để biết chi tiết.", "OK");
    }

    /// <summary>
    ///     Đệ quy tìm Resources trong folderPath. Khi gặp Resources thì rename parent/Resources → parent/Prefabs
    ///     và thêm parent vào danh sách renamed (để restore lại sau).
    ///     Bỏ qua nhánh đã đổi (Prefabs).
    ///     Dùng Directory.* (file system) thay vì AssetDatabase để tránh stale cache khi batch.
    /// </summary>
    private void RenameResourcesToPrefabsRecursive(string folderPath, List<string> renamed, ref int success,
        ref int fail)
    {
        folderPath = folderPath.Replace("\\", "/").TrimEnd('/');
        if (!Directory.Exists(folderPath)) return;

        // Dedup — đã rename folder này thì không xử lý lại
        if (renamed.Contains(folderPath)) return;

        var subDirs = Directory.GetDirectories(folderPath);
        if (subDirs == null || subDirs.Length == 0) return;

        foreach (var sub in subDirs)
        {
            var subPath = sub.Replace("\\", "/");
            var subName = Path.GetFileName(subPath);
            if (subName == "Resources")
            {
                var resourcesPath = folderPath + "/Resources";
                var prefabsPath = folderPath + "/Prefabs";
                if (Directory.Exists(prefabsPath)) continue; // đã có Prefabs, skip

                var err = AssetDatabase.RenameAsset(resourcesPath, "Prefabs");
                if (string.IsNullOrEmpty(err))
                {
                    renamed.Add(folderPath);
                    success++;
                    Debug.Log($"[AssetBundleConfig] ✓ Android: {folderPath}/Resources → Prefabs");
                }
                else
                {
                    fail++;
                    Debug.LogError($"[AssetBundleConfig] ✗ Rename failed: {folderPath} — {err}");
                }
            }
            else if (subName != "Prefabs")
            {
                RenameResourcesToPrefabsRecursive(subPath, renamed, ref success, ref fail);
            }
        }
    }

    private void RenameBackToResources()
    {
        if (config.renamedFeatureRoots == null || config.renamedFeatureRoots.Count == 0)
        {
            Debug.Log("[AssetBundleConfig] Không có feature nào cần restore.");
            return;
        }

        int success = 0, fail = 0;
        foreach (var root in config.renamedFeatureRoots)
        {
            var prefabsPath = Path.Combine(root, "Prefabs").Replace("\\", "/");
            if (!Directory.Exists(prefabsPath))
            {
                Debug.LogWarning($"[AssetBundleConfig] Prefabs folder không tồn tại, bỏ qua: {prefabsPath}");
                continue;
            }

            var err = AssetDatabase.RenameAsset(prefabsPath, "Resources");
            if (string.IsNullOrEmpty(err))
            {
                success++;
                Debug.Log($"[AssetBundleConfig] ✓ Restored: {Path.GetFileName(root)}/Prefabs → Resources");
            }
            else
            {
                Debug.LogError($"[AssetBundleConfig] ✗ Restore failed: {root} — {err}");
                fail++;
            }
        }

        config.renamedFeatureRoots.Clear();
        Debug.Log($"[AssetBundleConfig] Restored {success} folders to Resources.");
        if (fail > 0)
            EditorUtility.DisplayDialog("Cảnh báo", $"Restored {success}, thất bại {fail}. Xem Console.", "OK");
    }

    // ─────────────────────────────────────────────────────────────
    // Existing sections
    // ─────────────────────────────────────────────────────────────

    private void DrawLoadingModeSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        showLoadingMode =
            EditorGUILayout.Foldout(showLoadingMode, "📦 Loading Mode (hiện tại)", true, sectionHeaderStyle);

        if (showLoadingMode)
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("USE_BUNDLE:", GUILayout.Width(110));
            var newUseBundles = EditorGUILayout.Toggle(config.useBundles, GUILayout.Width(20));
            if (newUseBundles != config.useBundles)
            {
                config.useBundles = newUseBundles;
                SetDefine(BUNDLE_DEFINE_SYMBOL, newUseBundles);
                EditorUtility.SetDirty(config);
            }

            EditorGUILayout.LabelField(
                config.useBundles
                    ? "Local bundles (Editor: AssetBundles/Android, iOS/Android device: StreamingAssets)"
                    : "Resources.Load (fallback)",
                config.useBundles ? greenLabelStyle : yellowLabelStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("USE_PAD:", GUILayout.Width(110));
            var newUsePAD = EditorGUILayout.Toggle(config.usePAD, GUILayout.Width(20));
            if (newUsePAD != config.usePAD)
            {
                config.usePAD = newUsePAD;
                SetDefine(PAD_DEFINE_SYMBOL, newUsePAD);
                EditorUtility.SetDirty(config);
            }

            EditorGUILayout.LabelField(
                config.usePAD ? "Play Asset Delivery (Android production)" : "Disabled",
                config.usePAD ? greenLabelStyle : redLabelStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            config.autoLoadBundle = EditorGUILayout.Toggle(
                new GUIContent("Auto Load Bundle", "Tự động load bundle khi show screen"),
                config.autoLoadBundle);

            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Force Resources:", GUILayout.Width(110));
            var newForceRes = EditorGUILayout.Toggle(config.forceResourceLoad, GUILayout.Width(20));
            if (newForceRes != config.forceResourceLoad)
            {
                config.forceResourceLoad = newForceRes;
                EditorUtility.SetDirty(config);
            }

            EditorGUILayout.LabelField(
                config.forceResourceLoad ? "Bỏ qua bundle, dùng Resources.Load (debug Editor)" : "Disabled",
                config.forceResourceLoad ? yellowLabelStyle : redLabelStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Define Symbols:", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            var symbols = GetCurrentDefineSymbols();
            EditorGUILayout.SelectableLabel(string.IsNullOrEmpty(symbols) ? "(none)" : symbols, EditorStyles.miniLabel,
                GUILayout.Height(20));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawExcludedFeaturesSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        showExcludedFeatures = EditorGUILayout.Foldout(showExcludedFeatures, "🔒 Excluded Features (Không chuyển đổi)",
            true, sectionHeaderStyle);

        if (showExcludedFeatures)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Những feature này sẽ giữ nguyên folder 'Resources' và được build sẵn trong app.\nKhông cần download qua PAD.",
                MessageType.Info);
            EditorGUILayout.Space(5);
            excludedFeaturesList.DoLayoutList();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Common", GUILayout.Height(20)))
            {
                AddExcludedIfNotExists("Confirm");
                AddExcludedIfNotExists("MaskChangeScene");
            }

            if (GUILayout.Button("+ Add Splash", GUILayout.Height(20)))
                AddExcludedIfNotExists("SplashScene");
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void AddExcludedIfNotExists(string featureName)
    {
        if (!config.excludedFeatures.Contains(featureName))
        {
            config.excludedFeatures.Add(featureName);
            EditorUtility.SetDirty(config);
        }
    }

    private void DrawFolderRenameSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        showFolderRename = EditorGUILayout.Foldout(showFolderRename, "📁 Folder Settings", true, sectionHeaderStyle);

        if (showFolderRename)
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Features Path:", GUILayout.Width(100));
            config.featuresPath = EditorGUILayout.TextField(config.featuresPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Features Folder", config.featuresPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                        path = "Assets" + path.Substring(Application.dataPath.Length);
                    config.featuresPath = path;
                    EditorUtility.SetDirty(config);
                }
            }

            EditorGUILayout.EndHorizontal();

            // Show renamed list if any
            if (config.renamedFeatureRoots != null && config.renamedFeatureRoots.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Features đang ở Prefabs ({config.renamedFeatureRoots.Count}):",
                    EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                foreach (var r in config.renamedFeatureRoots)
                    EditorGUILayout.LabelField(Path.GetFileName(r), EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }

            // Extra build targets
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Build Extra Targets", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(
                "OnPreprocessBuild sẽ rename Resources → Prefabs trong các folder này khi build Android.\n" +
                "Dùng cho path nằm ngoài featuresPath.",
                MessageType.Info);
            EditorGUILayout.Space(3);
            if (extraFolderTargetsList != null)
                extraFolderTargetsList.DoLayoutList();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        showPreviewSection =
            EditorGUILayout.Foldout(showPreviewSection, "👁 Manual Scan & Preview", true, sectionHeaderStyle);

        if (showPreviewSection)
        {
            EditorGUILayout.Space(5);

            if (GUILayout.Button("🔍 Scan Features Folders", GUILayout.Height(28)))
            {
                ScanFeatureFolders();
                showPreview = true;
            }

            EditorGUILayout.Space(5);

            if (showPreview && config.foldersToRename != null && config.foldersToRename.Count > 0)
            {
                var total = config.foldersToRename.Count;
                var selected = config.foldersToRename.Count(f => f.willRename);
                EditorGUILayout.LabelField($"Found: {total} | Selected: {selected}", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
                foreach (var folder in config.foldersToRename)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    folder.willRename = EditorGUILayout.Toggle(folder.willRename, GUILayout.Width(18));
                    EditorGUILayout.LabelField(folder.featureName, EditorStyles.boldLabel, GUILayout.Width(120));
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField($"📂 {folder.oldPath}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"➜  {folder.newPath}", greenLabelStyle);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("✓ Select All", GUILayout.Height(22)))
                    foreach (var f in config.foldersToRename)
                        f.willRename = true;
                if (GUILayout.Button("✗ Deselect All", GUILayout.Height(22)))
                    foreach (var f in config.foldersToRename)
                        f.willRename = false;
                EditorGUILayout.EndHorizontal();
            }
            else if (showPreview)
            {
                EditorGUILayout.HelpBox(
                    $"Không tìm thấy folder nào cần rename.\n(Excluded: {string.Join(", ", config.excludedFeatures)})",
                    MessageType.Info);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("⚡ Actions", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        var selectedCount = config.foldersToRename?.Count(f => f.willRename) ?? 0;

        if (selectedCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"⚠️ Sẽ rename {selectedCount} folder(s). Hành động này không thể hoàn tác dễ dàng!",
                MessageType.Warning);
            EditorGUILayout.Space(3);
        }

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = selectedCount > 0;
        GUI.backgroundColor = selectedCount > 0 ? new Color(0.3f, 0.7f, 0.3f) : Color.gray;
        if (GUILayout.Button($"🔄 Rename ({selectedCount}) Folders", GUILayout.Height(30)))
            if (EditorUtility.DisplayDialog("Xác nhận Rename",
                    $"Bạn có chắc muốn rename {selectedCount} folder(s)?", "Rename", "Hủy"))
                RenameSelectedFolders();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        if (GUILayout.Button("📂 Open", GUILayout.Width(60), GUILayout.Height(30)))
        {
            if (Directory.Exists(config.featuresPath))
                EditorUtility.RevealInFinder(config.featuresPath);
            else
                EditorUtility.DisplayDialog("Error", "Folder không tồn tại!", "OK");
        }

        if (GUILayout.Button("🔄", GUILayout.Width(30), GUILayout.Height(30)))
            AssetDatabase.Refresh();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Restore button — quét featuresPath + extraFolderTargets để rename Prefabs → Resources
        GUI.backgroundColor = new Color(0.85f, 0.45f, 0.1f);
        if (GUILayout.Button("🔁 Restore All Folders (Prefabs → Resources)", GUILayout.Height(30)))
            if (EditorUtility.DisplayDialog("Restore Folders",
                    "Sẽ tìm tất cả folder tên 'Prefabs' trong featuresPath và extraFolderTargets, rename lại thành 'Resources'.\n\nTiếp tục?",
                    "Restore", "Hủy"))
                ForceRestoreAllFolders();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
        if (GUILayout.Button("⚙ Generate C# AssetRefs", GUILayout.Height(26)))
            AssetBundleNamesGenerator.GenerateAssetRefs();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndVertical();
    }

    // ─────────────────────────────────────────────────────────────
    // Scan / Rename helpers
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    ///     Quét featuresPath + extraFolderTargets, tìm tất cả folder "Prefabs" và rename về "Resources".
    ///     Không phụ thuộc vào renamedFeatureRoots — dùng được kể cả khi tracking bị mất.
    /// </summary>
    private void ForceRestoreAllFolders()
    {
        int success = 0, fail = 0;

        // Collect all parent directories to scan (top-level)
        var scanRoots = new List<string>();

        if (Directory.Exists(config.featuresPath))
            foreach (var d in Directory.GetDirectories(config.featuresPath))
                scanRoots.Add(d.Replace("\\", "/"));

        foreach (var obj in config.buildExtraFolderTargets)
        {
            if (obj == null) continue;
            var p = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(p))
                scanRoots.Add(p.Replace("\\", "/"));
        }

        // Đệ quy tìm tất cả Prefabs lồng (vd Shop/Store/Prefabs) và rename về Resources
        foreach (var root in scanRoots)
            RestorePrefabsRecursive(root, ref success, ref fail);

        config.renamedFeatureRoots?.Clear();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var msg = $"✅ Restored: {success}\n❌ Failed: {fail}";
        EditorUtility.DisplayDialog("Restore Complete", msg, "OK");
        Debug.Log($"[AssetBundleConfig] ForceRestore done — {success} restored, {fail} failed.");
    }

    private void RestorePrefabsRecursive(string folderPath, ref int success, ref int fail)
    {
        folderPath = folderPath.Replace("\\", "/").TrimEnd('/');
        if (!AssetDatabase.IsValidFolder(folderPath)) return;

        var subFolders = AssetDatabase.GetSubFolders(folderPath);
        if (subFolders == null || subFolders.Length == 0) return;

        foreach (var sub in subFolders)
        {
            var subName = Path.GetFileName(sub);
            if (subName == "Prefabs")
            {
                var prefabsPath = folderPath + "/Prefabs";
                var resourcesPath = folderPath + "/Resources";
                if (AssetDatabase.IsValidFolder(resourcesPath)) continue;

                var err = AssetDatabase.RenameAsset(prefabsPath, "Resources");
                if (string.IsNullOrEmpty(err))
                {
                    success++;
                    Debug.Log($"[AssetBundleConfig] ✓ Restored: {folderPath}/Prefabs → Resources");
                }
                else
                {
                    fail++;
                    Debug.LogError($"[AssetBundleConfig] ✗ Restore failed: {folderPath} — {err}");
                }
            }
            else if (subName != "Resources")
            {
                RestorePrefabsRecursive(sub, ref success, ref fail);
            }
        }
    }

    private void ScanFeatureFolders()
    {
        config.foldersToRename.Clear();

        // 1. Scan featuresPath
        if (Directory.Exists(config.featuresPath))
            foreach (var featureFolder in Directory.GetDirectories(config.featuresPath))
            {
                var featureName = Path.GetFileName(featureFolder);
                if (config.IsFeatureExcluded(featureName)) continue;

                var normalized = featureFolder.Replace("\\", "/").TrimEnd('/');
                AddScanResult(normalized); // direct: <Feature>/Resources
                ScanForResourcesRecursive(normalized); // nested: <Feature>/.../Resources (vd Shop/SubA/Resources)
            }
        else
            Debug.LogWarning($"[AssetBundleConfig] featuresPath không tồn tại: {config.featuresPath}");

        // 2. Scan extraFolderTargets
        foreach (var obj in config.buildExtraFolderTargets)
        {
            if (obj == null) continue;
            var folderPath = AssetDatabase.GetAssetPath(obj).Replace("\\", "/").TrimEnd('/');
            if (string.IsNullOrEmpty(folderPath)) continue;

            // Case A: kéo thẳng folder "Resources" → dùng parent làm feature root
            if (Path.GetFileName(folderPath).Equals("Resources", StringComparison.OrdinalIgnoreCase))
            {
                AddScanResult(Path.GetDirectoryName(folderPath).Replace("\\", "/"));
                continue;
            }

            // Scan đệ quy — tìm tất cả folder có "Resources" ở mọi độ sâu
            ScanForResourcesRecursive(folderPath);
        }

        EditorUtility.SetDirty(config);
        Debug.Log($"[AssetBundleConfig] Scan: {config.foldersToRename.Count} folder(s) found.");
    }

    private void ScanForResourcesRecursive(string folderPath)
    {
        folderPath = folderPath.Replace("\\", "/").TrimEnd('/');

        // Dùng AssetDatabase.GetSubFolders — Unity-native, không phụ thuộc working directory
        var subFolders = AssetDatabase.GetSubFolders(folderPath);
        if (subFolders == null || subFolders.Length == 0) return;

        foreach (var sub in subFolders)
        {
            var subName = Path.GetFileName(sub);
            if (subName == "Resources")
                // Tìm thấy Resources → thêm parent vào danh sách
                AddScanResult(folderPath);
            else if (subName != "Prefabs")
                // Không phải Resources/Prefabs → tiếp tục đệ quy
                ScanForResourcesRecursive(sub);
        }
    }

    private void AddScanResult(string folderPath)
    {
        folderPath = folderPath.Replace("\\", "/").TrimEnd('/');
        var oldPath = folderPath + "/Resources";
        var newPath = folderPath + "/Prefabs";

        // Dùng Directory.Exists — nhất quán với ScanFeatureFolders gốc đang hoạt động
        if (!Directory.Exists(oldPath) || Directory.Exists(newPath))
            return;

        // Dedup — tránh thêm cùng 1 path 2 lần
        if (config.foldersToRename.Any(f => f.oldPath == oldPath))
            return;

        var featureName = Path.GetFileName(folderPath);
        config.foldersToRename.Add(new FolderRenameInfo(featureName, oldPath, newPath, true));
    }

    private void RenameSelectedFolders()
    {
        int success = 0, fail = 0;
        var errors = new List<string>();

        foreach (var folder in config.foldersToRename)
        {
            if (!folder.willRename) continue;

            var err = AssetDatabase.RenameAsset(folder.oldPath, "Prefabs");
            if (string.IsNullOrEmpty(err))
            {
                success++;
                Debug.Log($"[AssetBundleConfig] ✓ Renamed: {folder.featureName}");
            }
            else
            {
                fail++;
                errors.Add($"{folder.featureName}: {err}");
            }
        }

        AssetDatabase.Refresh();
        ScanFeatureFolders();

        var msg = $"✅ Thành công: {success}\n❌ Thất bại: {fail}";
        if (errors.Count > 0) msg += "\n\nLỗi:\n" + string.Join("\n", errors);
        EditorUtility.DisplayDialog("Kết quả", msg, "OK");
    }

    // ─────────────────────────────────────────────────────────────
    // Define Symbol helpers
    // ─────────────────────────────────────────────────────────────

    private static void SetDefine(string symbol, bool enable)
    {
        if (enable) AddDefineSymbol(symbol);
        else RemoveDefineSymbol(symbol);
    }

    private static bool HasDefineSymbol(string symbol)
    {
        var target = EditorUserBuildSettings.selectedBuildTargetGroup;
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(target).Split(';').Contains(symbol);
    }

    private static void AddDefineSymbol(string symbol)
    {
        var target = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defs = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
        if (!defs.Split(';').Contains(symbol))
        {
            defs = string.IsNullOrEmpty(defs) ? symbol : defs + ";" + symbol;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defs);
            Debug.Log($"[AssetBundleConfig] ✓ Added define: {symbol}");
        }
    }

    private static void RemoveDefineSymbol(string symbol)
    {
        var target = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defs = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
        var list = defs.Split(';').ToList();
        if (list.Remove(symbol))
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", list));
            Debug.Log($"[AssetBundleConfig] ✗ Removed define: {symbol}");
        }
    }

    private static string GetCurrentDefineSymbols()
    {
        var target = EditorUserBuildSettings.selectedBuildTargetGroup;
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
    }
}
#endif