using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

using System.IO;
using System.Collections.Generic;
using Coffee.UIEffects;

/// <summary>
/// Unity Editor context-menu importer that reads a PSD-export CSV (produced by
/// the Photoshop "Export Layers To Files (Fast)" JSX script) and reconstructs
/// the layer hierarchy as a Unity Canvas UI hierarchy.
/// </summary>
public static class PsdLayerImporter
{
    private const string MenuPath       = "Assets/PSD Layers/Import";
    private const string RemapMenuPath  = "Assets/PSD Layers/Remap";
    private const string CleanupMenuPath = "Assets/PSD Layers/Clean Up PSD Text Files";

    // Asset path to the PsdToUnityConfigCollection ScriptableObject in the project.
    // Create one via Assets > Create > Configs > PsdToUnityConfig, then set the paths there.
    private const string ConfigAssetPath = "Assets/PsdToUnityConfig.asset";

    // Guard flag: prevents the prefab-asset postprocessor from re-entering while
    // ApplyObjectOverride is in-flight (which itself triggers an asset re-import).
    internal static bool s_IsApplyingOverrides = false;

    // -------------------------------------------------------------------------
    // MenuItem action
    // -------------------------------------------------------------------------

    private static PsdToUnityConfigCollection LoadConfig()
    {
        PsdToUnityConfigCollection cfg = AssetDatabase.LoadAssetAtPath<PsdToUnityConfigCollection>(ConfigAssetPath);
        if (cfg == null)
        {
            // Fallback: find any instance in the project
            string[] guids = AssetDatabase.FindAssets("t:PsdToUnityConfigCollection");
            if (guids.Length > 0)
                cfg = AssetDatabase.LoadAssetAtPath<PsdToUnityConfigCollection>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return cfg;
    }

    [MenuItem(MenuPath)]
    static void ImportCsv()
    {
        // 1. Get CSV asset path from the current selection
        string csvAssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        // 2. Convert Unity project-relative path to a full system path
        //    (File.ReadAllLines needs an absolute OS path, not "Assets/…")
        string systemPath = Path.GetFullPath(csvAssetPath);

        // 3. Read all lines
        string[] lines = File.ReadAllLines(systemPath);

        // 4. Defensive check — must have header row + at least 1 data row
        if (lines.Length < 2)
        {
            Debug.LogError("PSD Import: CSV has no layer rows.");
            return;
        }

        // 5. Parse data rows (row 0 = header, rows 1+ = data)
        //    Columns: filename,layer_name,layer_type,parent_group,x,y,width,height,canvas_width,canvas_height,...
        //    ParseCsvLine handles quoted fields (RFC 4180) so values with commas are parsed correctly.
        var layerRows = new List<string[]>();
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] cols = ParseCsvLine(lines[i]);
            if (cols.Length < 12)
            {
                Debug.LogWarning("PSD Import: Skipping malformed row " + i + " (expected 12 columns, got " + cols.Length + "): " + lines[i]);
                continue;
            }
            layerRows.Add(cols);
        }

        if (layerRows.Count == 0)
        {
            Debug.LogError("PSD Import: CSV has no valid layer rows.");
            return;
        }

        // 6. Read canvas size from the first data row (cols 8 and 9)
        float canvasWidth  = float.Parse(layerRows[0][8], System.Globalization.CultureInfo.InvariantCulture);
        float canvasHeight = float.Parse(layerRows[0][9], System.Globalization.CultureInfo.InvariantCulture);

        int layerCount = layerRows.Count;

        // 8. Preview dialog — user must confirm before any scene changes
        bool proceed = EditorUtility.DisplayDialog(
            "PSD Layer Import",
            "Canvas: " + canvasWidth + " x " + canvasHeight + "\nLayers: " + layerCount,
            "Import",
            "Cancel"
        );
        if (!proceed)
            return;

        // 9. Find existing Canvas in scene
#if UNITY_2022_2_OR_NEWER
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
#else
        Canvas canvas = Object.FindObjectOfType<Canvas>();
#endif
        if (canvas == null)
        {
            Debug.LogError("PSD Import: No Canvas found in scene. Add a Canvas before importing.");
            return;
        }

        // 10. Resize Canvas to CSV canvas dimensions
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(canvasWidth, canvasHeight);

        // -------------------------------------------------------------------------
        // 11. Duplicate detection — check BEFORE any GameObject creation
        // -------------------------------------------------------------------------

        string csvFolder = Path.GetDirectoryName(csvAssetPath).Replace('\\', '/');
        string mappingSystemPath = Path.GetFullPath(Path.Combine(csvFolder, "layers_mapping.json"));
        // Read full entries (with prefabRoot/prefabAssetPath) for re-import resolution
        List<LayerMappingEntry> existingEntries = ReadMappingEntries(mappingSystemPath);
        // Build quick lookup: layerName → entry (for re-import resolution)
        Dictionary<string, LayerMappingEntry> existingEntryMap = null;
        if (existingEntries != null)
        {
            existingEntryMap = new Dictionary<string, LayerMappingEntry>();
            foreach (LayerMappingEntry e in existingEntries)
                if (!string.IsNullOrEmpty(e.layerName))
                    existingEntryMap[e.layerName] = e;
        }

        bool replaceAll = false;
        bool keepAll    = false;

        if (existingEntryMap == null)
        {
            // Collect the set of layer names that will be imported
            var layerNames = new HashSet<string>();
            for (int r = 0; r < layerRows.Count; r++)
                layerNames.Add(layerRows[r][1]);

            // Walk existing canvas children recursively to find matching names
            int duplicateCount = 0;
            foreach (Transform child in canvas.transform)
                duplicateCount += CountMatchingChildren(child, layerNames);

            if (duplicateCount > 0)
            {
                int choice = EditorUtility.DisplayDialogComplex(
                    "Duplicate GameObjects Found",
                    duplicateCount + " GameObjects already exist under this Canvas.",
                    "Replace All",   // 0
                    "Cancel",        // 1
                    "Keep All"       // 2
                );
                if (choice == 1) return;  // Cancel — abort, nothing created
                replaceAll = (choice == 0);
                keepAll    = (choice == 2);
            }

            // If replacing, destroy existing duplicates before creating new ones
            if (replaceAll)
            {
                var toDestroy = new List<GameObject>();
                foreach (Transform child in canvas.transform)
                    CollectMatchingChildren(child, layerNames, toDestroy);
                foreach (var go in toDestroy)
                    Undo.DestroyObjectImmediate(go);
            }
        }

        // -------------------------------------------------------------------------
        // 12. Build group GameObjects dictionary
        //     - No mapping (first import): create one empty GO per unique parent_group.
        //     - Mapping exists (re-import): resolve existing group GOs via stored entry.
        //       Never create new group GOs on re-import.
        // -------------------------------------------------------------------------

        PsdToUnityConfigCollection config = LoadConfig();
        if (config == null)
            Debug.LogWarning("PSD Import: PsdToUnityConfigCollection not found. Text/Button prefabs will be skipped. Create one via Assets > Create > Configs > PsdToUnityConfig.");

        // Pre-scan: compute the union bounding box (in PSD space) for every button group.
        // This is used later to position the group GO at the correct canvas location,
        // regardless of which child layer is processed first.
        var groupBounds = new Dictionary<string, Rect>();
        foreach (string[] cols in layerRows)
        {
            string pg = cols[3];
            if (string.IsNullOrEmpty(pg)) continue;
            if (!pg.StartsWith("btn", System.StringComparison.OrdinalIgnoreCase)) continue;
            float lx = float.Parse(cols[4], System.Globalization.CultureInfo.InvariantCulture);
            float ly = float.Parse(cols[5], System.Globalization.CultureInfo.InvariantCulture);
            float lw = float.Parse(cols[6], System.Globalization.CultureInfo.InvariantCulture);
            float lh = float.Parse(cols[7], System.Globalization.CultureInfo.InvariantCulture);
            if (!groupBounds.ContainsKey(pg))
            {
                groupBounds[pg] = new Rect(lx, ly, lw, lh);
            }
            else
            {
                Rect cur = groupBounds[pg];
                float xMin = Mathf.Min(cur.xMin, lx);
                float yMin = Mathf.Min(cur.yMin, ly);
                float xMax = Mathf.Max(cur.xMax, lx + lw);
                float yMax = Mathf.Max(cur.yMax, ly + lh);
                groupBounds[pg] = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            }
        }

        var groupObjects = new Dictionary<string, GameObject>();

        if (existingEntryMap == null)
        {
            // First import — create group GOs (bottom PSD layer first = created first = rendered below)
            for (int r = layerRows.Count - 1; r >= 0; r--)
            {
                string[] cols = layerRows[r];
                string parentGroup = cols[3];

                if (!string.IsNullOrEmpty(parentGroup) && !groupObjects.ContainsKey(parentGroup))
                {
                    bool isBtnGroup = parentGroup.StartsWith("btn", System.StringComparison.OrdinalIgnoreCase);
                    GameObject grpGo = null;

                    if (isBtnGroup && config != null && !string.IsNullOrEmpty(config.buttonPrefabPath))
                    {
                        GameObject btnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(config.buttonPrefabPath);
                        if (btnPrefab != null)
                        {
                            grpGo = (GameObject)PrefabUtility.InstantiatePrefab(btnPrefab, canvas.transform);
                            grpGo.name = parentGroup;
                            Undo.RegisterCreatedObjectUndo(grpGo, "PSD Import");
                            Debug.Log("PSD Import: Instantiated button prefab for group '" + parentGroup + "'");
                        }
                        else
                        {
                            Debug.LogWarning("PSD Import: Button prefab not found at '" + config.buttonPrefabPath + "' for group '" + parentGroup + "'");
                        }
                    }

                    if (grpGo == null)
                    {
                        grpGo = new GameObject(parentGroup);
                        Undo.RegisterCreatedObjectUndo(grpGo, "PSD Import");
                        grpGo.transform.SetParent(canvas.transform, false);
                        RectTransform grpRt = grpGo.AddComponent<RectTransform>();
                        grpRt.anchorMin = Vector2.zero;
                        grpRt.anchorMax = Vector2.one;
                        grpRt.offsetMin = Vector2.zero;
                        grpRt.offsetMax = Vector2.zero;
                    }

                    groupObjects[parentGroup] = grpGo;
                }
            }
        }
        else
        {
            // Re-import — resolve existing group GOs from mapping (no creation)
            for (int r = layerRows.Count - 1; r >= 0; r--)
            {
                string parentGroup = layerRows[r][3];
                if (string.IsNullOrEmpty(parentGroup) || groupObjects.ContainsKey(parentGroup)) continue;

                LayerMappingEntry grpEntry;
                if (existingEntryMap.TryGetValue(parentGroup, out grpEntry))
                {
                    GameObject grpGo = ResolveGameObject(grpEntry);
                    if (grpGo != null)
                        groupObjects[parentGroup] = grpGo;
                    // If not found (deleted) — leave out of dict; layers parented here will be skipped
                }
            }
        }

        // -------------------------------------------------------------------------
        // 12b. Configure sprite import settings for all image files before creating GOs
        // -------------------------------------------------------------------------

        var imageFilenames = new HashSet<string>();
        foreach (string[] cols in layerRows)
        {
            string fn = cols[0];
            string lt = cols[2];
            if (lt == "image" && !string.IsNullOrEmpty(fn))
                imageFilenames.Add(fn);
        }
        ConfigureSpriteImportSettings(csvFolder, imageFilenames);

        // -------------------------------------------------------------------------
        // 13. Layer GameObject creation loop (per-row Image / TextMeshProUGUI creation)
        // -------------------------------------------------------------------------

        int createdCount    = 0;
        int spriteMissCount = 0;
        // layerName → LayerMappingEntry (with full path info)
        var createdLayerEntries = new Dictionary<string, LayerMappingEntry>();
        // Tracks which button groups have already had their position + sibling index set
        var btnGroupPositioned = new HashSet<string>();

        // Iterate in REVERSE so that layers at the top of the PSD (index 0) are created last
        // and appear on top in Unity (last sibling renders on top in Canvas).
        for (int r = layerRows.Count - 1; r >= 0; r--)
        {
            string[] cols = layerRows[r];
            string filename    = cols[0];
            string layerName   = cols[1];
            string layerType   = cols[2];
            string parentGroup = cols[3];
            // x,y = top-left corner of the layer in PSD space (origin top-left, Y down)
            float  layerX      = float.Parse(cols[4], System.Globalization.CultureInfo.InvariantCulture);
            float  layerY      = float.Parse(cols[5], System.Globalization.CultureInfo.InvariantCulture);
            float  layerW      = float.Parse(cols[6], System.Globalization.CultureInfo.InvariantCulture);
            float  layerH      = float.Parse(cols[7], System.Globalization.CultureInfo.InvariantCulture);
            string fontSizeStr    = cols.Length > 10 ? cols[10].Trim() : "";
            string shadowColorStr = cols.Length > 11 ? cols[11].Trim() : "";
            string textContent    = cols.Length > 12 ? cols[12].Trim().Trim('"') : "";
            string textColorStr   = cols.Length > 13 ? cols[13].Trim() : "";

            // Resolve parent transform
            Transform parentTransform = canvas.transform;
            if (!string.IsNullOrEmpty(parentGroup) && groupObjects.ContainsKey(parentGroup))
                parentTransform = groupObjects[parentGroup].transform;

            // keepAll: skip if a child with this name already exists under the resolved parent
            if (keepAll && ChildExists(parentTransform, layerName))
                continue;

            // Resolve-or-create layer GameObject
            GameObject layerGo = null;
            bool isExistingObject = false;
            LayerMappingEntry resolvedEntry = null;

            if (existingEntryMap != null)
            {
                // Re-import mode: only update objects already in the mapping.
                // Never create new objects — skip anything not in the mapping.
                LayerMappingEntry entry;
                if (!existingEntryMap.TryGetValue(layerName, out entry))
                    continue; // New layer in CSV not yet mapped — skip

                layerGo = ResolveGameObject(entry);
                if (layerGo == null)
                {
                    // Object was deleted from scene — skip, do not recreate.
                    Debug.LogWarning("PSD Import: Mapped object '" + entry.scenePath + "' not found — skipping.");
                    continue;
                }
                isExistingObject = true;
                resolvedEntry = entry;
            }
            else
            {
                // First import — create new GameObject
                layerGo = new GameObject(layerName);
                Undo.RegisterCreatedObjectUndo(layerGo, "PSD Import");
                layerGo.transform.SetParent(parentTransform, false);
                createdCount++;
            }

            // Update-only visual block — for re-imports with mapping file present.
            // Updates sprite, text content, font size, and shadow color without touching RectTransform.
            if (isExistingObject)
            {
                // Image update
                if (layerType == "image")
                {
                    Image img = layerGo.GetComponent<Image>();
                    if (img != null)
                    {
                        string spritePath = csvFolder + "/" + filename;
                        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                        if (sprite != null)
                        {
                            img.sprite = sprite;
                            img.raycastTarget = false;
                            // Apply change back to prefab asset if this GO lives inside a nested prefab
                            ApplyComponentOverrideToPrefab(img, resolvedEntry, config);
                        }
                    }
                }
                // Text update
                if (layerType == "text")
                {
                    Text txt = layerGo.GetComponentInChildren<Text>();
                    if (txt != null)
                    {
                        bool textChanged = false;
                        if (!string.IsNullOrEmpty(textContent)) { txt.text = textContent; textChanged = true; }
                        float fs;
                        if (!string.IsNullOrEmpty(fontSizeStr) && float.TryParse(fontSizeStr,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out fs))
                        { txt.fontSize = Mathf.RoundToInt(fs); textChanged = true; }
                        if (!string.IsNullOrEmpty(textColorStr))
                        {
                            Color textCol;
                            string hexTextColor = textColorStr.StartsWith("#") ? textColorStr : "#" + textColorStr;
                            if (ColorUtility.TryParseHtmlString(hexTextColor, out textCol))
                            { txt.color = textCol; textChanged = true; }
                        }
                        if (textChanged)
                            ApplyComponentOverrideToPrefab(txt, resolvedEntry, config);
                    }
                }
                // Shadow update
                if (!string.IsNullOrEmpty(shadowColorStr))
                {
                    Color c;
                    string hex = shadowColorStr.StartsWith("#") ? shadowColorStr : "#" + shadowColorStr;
                    if (ColorUtility.TryParseHtmlString(hex, out c))
                    {
                        var shadow = layerGo.GetComponentInChildren<UIShadow>();
                        if (shadow != null)
                        {
                            shadow.effectColor = c;
                            ApplyComponentOverrideToPrefab(shadow, resolvedEntry, config);
                        }
                    }
                }
                // Record scene path and skip RectTransform setup — DO NOT touch position/size
                if (layerGo != null)
                {
                    ScenePathInfo info = GetScenePath(layerGo.transform);
                    createdLayerEntries[layerName] = new LayerMappingEntry
                    {
                        layerName      = layerName,
                        scenePath      = info.scenePath,
                        prefabRoot     = info.prefabRoot,
                        prefabAssetPath = info.prefabAssetPath
                    };
                }
                continue;
            }
            // else: fall through to full component + RectTransform create path (unchanged from Phase 2)

            // Determine if this layer's parent group is a button (prefix "btn")
            bool parentIsButton = parentGroup.StartsWith("btn", System.StringComparison.OrdinalIgnoreCase);

            // Add component by layer type
            if (parentIsButton && groupObjects.ContainsKey(parentGroup))
            {
                // This layer is a child of a button group.
                // Layer name prefix determines role:
                //   "btn" prefix → image layer → set sprite on prefab child named "btn"
                //   "txt" prefix → text layer  → set content on prefab child named "text_button_CTA"
                // Only the first matching layer in the group is used (subsequent ones are ignored).
                GameObject btnGroupGo = groupObjects[parentGroup];

                string btnImgPrefix = (config != null && !string.IsNullOrEmpty(config.buttonImagePrefix)) ? config.buttonImagePrefix : "btn";
                string btnTxtPrefix = (config != null && !string.IsNullOrEmpty(config.buttonTextPrefix)) ? config.buttonTextPrefix : "txt";
                string btnImgChild  = (config != null && !string.IsNullOrEmpty(config.buttonImageChildName)) ? config.buttonImageChildName : "btn";
                string btnTxtChild  = (config != null && !string.IsNullOrEmpty(config.buttonTextChildName))  ? config.buttonTextChildName  : "text_button_CTA";

                bool isImgSlot = layerName.StartsWith(btnImgPrefix, System.StringComparison.OrdinalIgnoreCase);
                bool isTxtSlot = layerName.StartsWith(btnTxtPrefix, System.StringComparison.OrdinalIgnoreCase);

                if (isImgSlot && layerType == "image")
                {
                    // Set sprite on the designated image child in the prefab (with fallback)
                    Transform btnChild = FindChildWithImage(btnGroupGo.transform, btnImgChild);
                    if (btnChild != null)
                    {
                        Image childImg = btnChild.GetComponent<Image>();
                        if (childImg != null)
                        {
                            string spritePath = csvFolder + "/" + filename;
                            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                            if (sprite != null)
                            {
                                childImg.sprite = sprite;
                                childImg.preserveAspect = true;
                                childImg.raycastTarget = false;
                            }
                            else
                            {
                                Debug.LogWarning("PSD Import: Sprite not found for button child: " + spritePath);
                                spriteMissCount++;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("PSD Import: No Image component found in button prefab for group '" + parentGroup + "'. Expected child name: '" + btnImgChild + "'.");
                    }
                }
                else if (isTxtSlot && layerType == "text")
                {
                    // Set text content on the designated text child in the prefab (with fallback)
                    Transform txtChild = FindChildWithText(btnGroupGo.transform, btnTxtChild);
                    if (txtChild != null)
                    {
                        Text btnTxt = txtChild.GetComponent<Text>();
                        if (btnTxt != null)
                        {
                            btnTxt.text = !string.IsNullOrEmpty(textContent) ? textContent : layerName;
                            float fontSize;
                            if (!string.IsNullOrEmpty(fontSizeStr) && float.TryParse(fontSizeStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fontSize))
                                btnTxt.fontSize = Mathf.RoundToInt(fontSize);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("PSD Import: No Text component found in button prefab for group '" + parentGroup + "'. Expected child name: '" + btnTxtChild + "'.");
                    }
                }

                // Position + sibling order: set once per group using the pre-computed union bounding box.
                // Runs regardless of which child layer (image or text) triggers the first visit.
                if (!btnGroupPositioned.Contains(parentGroup))
                {
                    btnGroupPositioned.Add(parentGroup);

                    RectTransform grpRt = btnGroupGo.GetComponent<RectTransform>();
                    if (grpRt != null)
                    {
                        Rect bounds;
                        if (!groupBounds.TryGetValue(parentGroup, out bounds))
                            bounds = new Rect(layerX, layerY, layerW, layerH);

                        grpRt.anchorMin = new Vector2(0.5f, 0.5f);
                        grpRt.anchorMax = new Vector2(0.5f, 0.5f);
                        grpRt.pivot     = new Vector2(0.5f, 0.5f);
                        grpRt.sizeDelta = new Vector2(bounds.width, bounds.height);
                        float grpX = bounds.x + bounds.width  / 2f - canvasWidth  / 2f;
                        float grpY = -(bounds.y + bounds.height / 2f - canvasHeight / 2f);
                        grpRt.anchoredPosition = new Vector2(grpX, grpY);
                        Debug.Log("PSD Import [" + parentGroup + "] group bounds psd=(" + bounds.x + "," + bounds.y + " " + bounds.width + "x" + bounds.height + ") unity=(" + grpX + "," + grpY + ")");
                    }

                    // Sibling index: loop runs reverse (r = layerRows.Count-1 → 0),
                    // Moving the group GO to the last sibling slot puts it in the correct Z-order.
                    btnGroupGo.transform.SetAsLastSibling();
                }

                // Don't create a separate GO — we mapped into the prefab instance
                Undo.DestroyObjectImmediate(layerGo);
                layerGo = null;
                createdCount--;
            }
            else if (layerType == "image")
            {
                Image img = layerGo.AddComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;

                // Sprite lookup
                string spritePath = csvFolder + "/" + filename;
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
                    if (tex != null)
                        Debug.LogWarning("PSD Import: '" + filename + "' found but not imported as Sprite. Change Texture Type to 'Sprite (2D and UI)' in Inspector.");
                    else
                        Debug.LogWarning("PSD Import: Sprite not found: " + spritePath);
                    spriteMissCount++;
                }
                else
                {
                    img.sprite = sprite;
                }
            }
            else if (layerType == "text")
            {
                // Load text prefab path from PsdToUnityConfigCollection
                string textPrefabPath = (config != null) ? config.textPrefabPath : null;
                GameObject textPrefab = null;
                if (!string.IsNullOrEmpty(textPrefabPath))
                    textPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(textPrefabPath);

                if (textPrefab == null)
                {
                    Debug.LogWarning("PSD Import: Text prefab not found at: " + textPrefabPath + " — skipping text layer '" + layerName + "'. Check textPrefabPath in PsdToUnityConfigCollection.");
                    Undo.DestroyObjectImmediate(layerGo);
                    createdCount--;
                }
                else
                {
                    // Replace the empty GO with a prefab instance
                    Undo.DestroyObjectImmediate(layerGo);
                    GameObject textGo = (GameObject)PrefabUtility.InstantiatePrefab(textPrefab, parentTransform);
                    textGo.name = layerName;
                    Undo.RegisterCreatedObjectUndo(textGo, "PSD Import");
                    layerGo = textGo;
                    Text tmp = layerGo.GetComponentInChildren<Text>();
                    if (tmp != null)
                    {
                        tmp.raycastTarget = false;
                        tmp.text = !string.IsNullOrEmpty(textContent) ? textContent : layerName;
                        // Set font size from PSD
                        float fontSize;
                        if (!string.IsNullOrEmpty(fontSizeStr) && float.TryParse(fontSizeStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fontSize))
                            tmp.fontSize = Mathf.RoundToInt(fontSize);
                        // Set text fill color from PSD
                        if (!string.IsNullOrEmpty(textColorStr))
                        {
                            Color textCol;
                            string hexTextColor = textColorStr.StartsWith("#") ? textColorStr : "#" + textColorStr;
                            if (ColorUtility.TryParseHtmlString(hexTextColor, out textCol))
                                tmp.color = textCol;
                        }
                    }
                    // Set UIShadow color from PSD stroke color (hex with #)
                    if (!string.IsNullOrEmpty(shadowColorStr))
                    {
                        Color shadowCol;
                        string hexColor = shadowColorStr.StartsWith("#") ? shadowColorStr : "#" + shadowColorStr;
                        if (ColorUtility.TryParseHtmlString(hexColor, out shadowCol))
                        {
                            var shadow = layerGo.GetComponentInChildren<UIShadow>();
                            if (shadow != null)
                                shadow.effectColor = shadowCol;
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("PSD Import: Unknown layer_type '" + layerType + "' for layer '" + layerName + "' — skipping component.");
            }

            // RectTransform setup — set anchors FIRST, then sizeDelta, then anchoredPosition
            // layerGo may have been replaced by a prefab instance (text layers) or destroyed (prefab missing)
            if (layerGo == null) continue;
            RectTransform rt = layerGo.GetComponent<RectTransform>();
            if (rt == null) rt = layerGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(layerW, layerH);
            // PSD origin: top-left, Y down. Unity Canvas origin: center, Y up.
            // anchoredPosition = center of layer relative to canvas center.
            float unityX = layerX + layerW / 2f - canvasWidth  / 2f;
            float unityY = -(layerY + layerH / 2f - canvasHeight / 2f);
            rt.anchoredPosition = new Vector2(unityX, unityY);
            Debug.Log("PSD Import [" + layerName + "] psd=(" + layerX + "," + layerY + " " + layerW + "x" + layerH + ") unity=(" + unityX + "," + unityY + ") canvas=" + canvasWidth + "x" + canvasHeight);

            // Record scene path for mapping file (skip button-children that were destroyed)
            if (layerGo != null)
            {
                ScenePathInfo info = GetScenePath(layerGo.transform);
                createdLayerEntries[layerName] = new LayerMappingEntry
                {
                    layerName       = layerName,
                    scenePath       = info.scenePath,
                    prefabRoot      = info.prefabRoot,
                    prefabAssetPath = info.prefabAssetPath
                };
            }
        }

        // -------------------------------------------------------------------------
        // 14. Post-import — refresh AssetDatabase, log summary, select Canvas
        // -------------------------------------------------------------------------

        // Build combined mapping: group GOs + layer GOs
        var allMappedEntries = new Dictionary<string, LayerMappingEntry>();
        foreach (var kvp in groupObjects)
        {
            if (kvp.Value != null)
            {
                ScenePathInfo info = GetScenePath(kvp.Value.transform);
                allMappedEntries[kvp.Key] = new LayerMappingEntry
                {
                    layerName       = kvp.Key,
                    scenePath       = info.scenePath,
                    prefabRoot      = info.prefabRoot,
                    prefabAssetPath = info.prefabAssetPath
                };
            }
        }
        foreach (var kvp in createdLayerEntries)
            allMappedEntries[kvp.Key] = kvp.Value;

        WriteMappingFile(mappingSystemPath, canvasWidth, canvasHeight, allMappedEntries);

        AssetDatabase.Refresh();
        Debug.Log("PSD Import complete: " + createdCount + " GameObjects created, " + spriteMissCount + " sprites missing.");
        Selection.activeGameObject = canvas.gameObject;
    }

    // -------------------------------------------------------------------------
    // MenuItem validate — enables item only when a .csv is selected
    // -------------------------------------------------------------------------

    [MenuItem(MenuPath, true)]
    static bool ValidateImportCsv()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return !string.IsNullOrEmpty(path) &&
               Path.GetExtension(path).Equals(".csv", System.StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // Remap menu — scan Canvas by name, rebuild mapping with scene paths
    // -------------------------------------------------------------------------

    [MenuItem(RemapMenuPath)]
    static void RemapLayers()
    {
        string csvAssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        string systemPath   = Path.GetFullPath(csvAssetPath);
        string[] lines = File.ReadAllLines(systemPath);

        if (lines.Length < 2)
        {
            Debug.LogError("PSD Remap: CSV has no layer rows.");
            return;
        }

        string csvFolder         = Path.GetDirectoryName(csvAssetPath).Replace('\\', '/');
        string mappingSystemPath = Path.GetFullPath(Path.Combine(csvFolder, "layers_mapping.json"));

        // Read canvas size from first data row
        string[] firstCols = ParseCsvLine(lines[1]);
        float canvasW = firstCols.Length > 8 ? float.Parse(firstCols[8], System.Globalization.CultureInfo.InvariantCulture) : 0;
        float canvasH = firstCols.Length > 9 ? float.Parse(firstCols[9], System.Globalization.CultureInfo.InvariantCulture) : 0;

        // Collect CSV layer names for adding new layers not yet in the mapping
        var csvLayerNames = new List<string>();
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] cols = ParseCsvLine(lines[i]);
            if (cols.Length < 2) continue;
            string name = cols[1]; // ParseCsvLine already strips quotes
            if (!string.IsNullOrEmpty(name))
                csvLayerNames.Add(name);
        }

        // Read existing mapping entries
        List<LayerMappingEntry> existingEntries = ReadMappingEntries(mappingSystemPath);

        var newMapping = new Dictionary<string, LayerMappingEntry>();
        int kept    = 0;
        int removed = 0;
        int added   = 0;

        if (existingEntries != null)
        {
            // Verify each existing entry by resolving the GO.
            // - Found → keep, recompute all path fields (covers moves/renames already applied by watcher)
            // - Not found → the GO was deleted → remove from mapping.
            foreach (LayerMappingEntry entry in existingEntries)
            {
                GameObject go = ResolveGameObject(entry);
                if (go != null)
                {
                    ScenePathInfo info = GetScenePath(go.transform);
                    newMapping[entry.layerName] = new LayerMappingEntry
                    {
                        layerName       = entry.layerName,
                        scenePath       = info.scenePath,
                        prefabRoot      = info.prefabRoot,
                        prefabAssetPath = info.prefabAssetPath
                    };
                    kept++;
                }
                else
                {
                    Debug.LogWarning("PSD Remap: '" + entry.layerName + "' at '" + entry.scenePath + "' not found in scene — removing.");
                    removed++;
                }
            }
        }

        // Add new CSV layers that have no mapping entry yet (e.g. layers added to the PSD
        // after the first import). Search Canvas by original layer name.
#if UNITY_2022_2_OR_NEWER
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
#else
        Canvas canvas = Object.FindObjectOfType<Canvas>();
#endif
        if (canvas != null)
        {
            var goByName = new Dictionary<string, GameObject>();
            CollectAllChildren(canvas.transform, goByName);

            foreach (string layerName in csvLayerNames)
            {
                if (!newMapping.ContainsKey(layerName) && goByName.ContainsKey(layerName))
                {
                    ScenePathInfo info = GetScenePath(goByName[layerName].transform);
                    newMapping[layerName] = new LayerMappingEntry
                    {
                        layerName       = layerName,
                        scenePath       = info.scenePath,
                        prefabRoot      = info.prefabRoot,
                        prefabAssetPath = info.prefabAssetPath
                    };
                    added++;
                }
            }
        }

        WriteMappingFile(mappingSystemPath, canvasW, canvasH, newMapping);
        AssetDatabase.Refresh();

        Debug.Log("PSD Remap complete: " + kept + " kept, " + added + " added, " + removed + " removed. File: " + mappingSystemPath);
        EditorUtility.DisplayDialog("Remap Complete",
            kept    + " entries kept\n" +
            added   + " new layers added\n" +
            removed + " deleted objects removed",
            "OK");
    }

    [MenuItem(RemapMenuPath, true)]
    static bool ValidateRemapLayers()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return !string.IsNullOrEmpty(path) &&
               Path.GetExtension(path).Equals(".csv", System.StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // Clean Up PSD Text Files — delete exported PSD image files for text layers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads the selected CSV, collects the filename of every row where layer_type == "text",
    /// then deletes those image files from the same folder as the CSV.
    ///
    /// Background: the JSX exporter writes a flat image file for every layer including text
    /// layers. After import those images are replaced by text prefab instances and are no
    /// longer needed. This menu item removes them so the project stays clean.
    /// </summary>
    [MenuItem(CleanupMenuPath)]
    static void CleanUpPsdTextFiles()
    {
        string csvAssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        string systemPath   = Path.GetFullPath(csvAssetPath);
        string[] lines      = File.ReadAllLines(systemPath);

        if (lines.Length < 2)
        {
            Debug.LogError("PSD Cleanup: CSV has no layer rows.");
            return;
        }

        string csvFolder = Path.GetDirectoryName(csvAssetPath).Replace('\\', '/');

        // Collect filenames for text layers (col 0 = filename, col 2 = layer_type)
        var textFiles = new List<string>();
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] cols = ParseCsvLine(lines[i]);
            if (cols.Length < 3) continue;
            string filename  = cols[0]; // ParseCsvLine already strips quotes
            string layerType = cols[2];
            if (layerType == "text" && !string.IsNullOrEmpty(filename))
                textFiles.Add(filename);
        }

        if (textFiles.Count == 0)
        {
            EditorUtility.DisplayDialog("Clean Up PSD Text Files", "No text layer files found in CSV.", "OK");
            return;
        }

        // Confirm before deleting
        bool confirm = EditorUtility.DisplayDialog(
            "Clean Up PSD Text Files",
            "Delete " + textFiles.Count + " PSD text image file(s) from:\n" + csvFolder + "\n\nThis cannot be undone.",
            "Delete",
            "Cancel"
        );
        if (!confirm) return;

        int deleted = 0;
        int missing = 0;
        foreach (string filename in textFiles)
        {
            string assetPath = csvFolder + "/" + filename;
            // AssetDatabase.LoadMainAssetAtPath returns null when the asset does not exist
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            {
                bool ok = AssetDatabase.DeleteAsset(assetPath);
                if (ok) deleted++;
                else    Debug.LogWarning("PSD Cleanup: Failed to delete '" + assetPath + "'");
            }
            else
            {
                missing++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("PSD Cleanup: " + deleted + " text image file(s) deleted, " + missing + " already missing.");
        EditorUtility.DisplayDialog("Clean Up Complete",
            deleted + " file(s) deleted\n" + missing + " file(s) already missing",
            "OK");
    }

    [MenuItem(CleanupMenuPath, true)]
    static bool ValidateCleanUpPsdTextFiles()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return !string.IsNullOrEmpty(path) &&
               Path.GetExtension(path).Equals(".csv", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Collects all GameObjects in the subtree of <paramref name="parent"/> into a name→GO dictionary.
    /// If multiple GOs share a name, the last one wins.
    /// </summary>
    private static void CollectAllChildren(Transform parent, Dictionary<string, GameObject> result)
    {
        foreach (Transform child in parent)
        {
            result[child.name] = child.gameObject;
            CollectAllChildren(child, result);
        }
    }

    // -------------------------------------------------------------------------
    // Sprite import settings configurator
    // -------------------------------------------------------------------------

    /// <summary>
    /// Ensures every image file referenced in the CSV is imported as a Sprite with
    /// Full Rect mesh, Remove PSD Matte enabled, and ASTC 6x6 for Android and iOS.
    /// Only re-imports files whose settings differ from the target values.
    /// </summary>
    private static void ConfigureSpriteImportSettings(string csvFolder, IEnumerable<string> filenames)
    {
        foreach (string filename in filenames)
        {
            string assetPath = csvFolder + "/" + filename;
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("PSD Import: Cannot configure sprite settings — asset not found in AssetDatabase: " + assetPath);
                continue;
            }

            // Read current texture settings struct (contains spriteMode, spriteMeshType, etc.)
            TextureImporterSettings texSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(texSettings);

            // Read psdRemoveMatte via SerializedObject (not exposed in public API)
            SerializedObject so = new SerializedObject(importer);
            so.Update();
            SerializedProperty psdRemoveMatteProp = so.FindProperty("m_PSDRemoveMatte");
            bool currentPsdRemoveMatte = psdRemoveMatteProp != null && psdRemoveMatteProp.boolValue;

            // Check default settings
            bool defaultOk = importer.textureType        == TextureImporterType.Sprite
                          && importer.spriteImportMode   == SpriteImportMode.Single
                          && texSettings.spriteMeshType  == SpriteMeshType.FullRect
                          && currentPsdRemoveMatte;

            // Check platform overrides
            TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings(BuildTarget.Android.ToString());
            TextureImporterPlatformSettings iosSettings     = importer.GetPlatformTextureSettings(BuildTarget.iOS.ToString());

            bool platformOk = androidSettings.overridden && androidSettings.format == TextureImporterFormat.ASTC_6x6
                           && iosSettings.overridden     && iosSettings.format     == TextureImporterFormat.ASTC_6x6;

            if (defaultOk && platformOk)
                continue;  // Already correct — skip reimport

            // Apply textureType and spriteImportMode directly on importer first
            importer.textureType      = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;

            // Apply spriteMeshType via TextureImporterSettings (must be set AFTER textureType)
            texSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(texSettings);

            // Set psdRemoveMatte via SerializedObject
            if (psdRemoveMatteProp != null)
            {
                psdRemoveMatteProp.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Apply Android platform override
            androidSettings = importer.GetPlatformTextureSettings(BuildTarget.Android.ToString());
            androidSettings.overridden = true;
            androidSettings.format     = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(androidSettings);

            // Apply iOS platform override
            iosSettings = importer.GetPlatformTextureSettings(BuildTarget.iOS.ToString());
            iosSettings.overridden = true;
            iosSettings.format     = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(iosSettings);

            importer.SaveAndReimport();
            Debug.Log("PSD Import: Configured sprite import settings for " + assetPath);
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Counts the number of GameObjects in the subtree rooted at <paramref name="t"/>
    /// whose names appear in <paramref name="names"/>.
    /// </summary>
    private static int CountMatchingChildren(Transform t, HashSet<string> names)
    {
        int count = names.Contains(t.name) ? 1 : 0;
        foreach (Transform child in t)
            count += CountMatchingChildren(child, names);
        return count;
    }

    /// <summary>
    /// Appends to <paramref name="result"/> every GameObject in the subtree rooted at
    /// <paramref name="t"/> whose name is present in <paramref name="names"/>.
    /// </summary>
    private static void CollectMatchingChildren(Transform t, HashSet<string> names, List<GameObject> result)
    {
        if (names.Contains(t.name))
            result.Add(t.gameObject);
        foreach (Transform child in t)
            CollectMatchingChildren(child, names, result);
    }

    /// <summary>
    /// Tokenizes a single CSV line, respecting RFC 4180 double-quoted fields.
    /// Quoted fields have their surrounding quotes stripped; the content is returned as-is
    /// (embedded commas inside quotes become part of the field value).
    /// </summary>
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        int i = 0;
        while (i <= line.Length)
        {
            if (i == line.Length)
            {
                // Trailing empty field after a final comma
                fields.Add("");
                break;
            }
            if (line[i] == '"')
            {
                // Quoted field — read until closing quote
                int start = i + 1;
                int end = start;
                while (end < line.Length && line[end] != '"')
                    end++;
                fields.Add(line.Substring(start, end - start));
                i = end + 1; // skip closing quote
                // Skip optional comma separator
                if (i < line.Length && line[i] == ',')
                    i++;
            }
            else
            {
                // Unquoted field — read until comma
                int start = i;
                while (i < line.Length && line[i] != ',')
                    i++;
                fields.Add(line.Substring(start, i - start).Trim());
                if (i < line.Length)
                    i++; // skip comma
                else
                    break;
            }
        }
        return fields.ToArray();
    }

    /// <summary>
    /// Removes surrounding double-quote characters added by csvQuote() in the JSX script.
    /// e.g. "\"layer_name\"" → "layer_name"
    /// </summary>
    private static string StripQuotes(string s)
    {
        s = s.Trim();
        if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
            return s.Substring(1, s.Length - 2);
        return s;
    }

    /// <summary>
    /// Returns true if any immediate child of <paramref name="parent"/> has the given <paramref name="name"/>.
    /// </summary>
    private static bool ChildExists(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Recursively searches for a child Transform whose name matches <paramref name="name"/>.
    /// </summary>
    private static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    /// <summary>
    /// Finds the child Transform that should receive the button image sprite.
    /// First tries to find a child named <paramref name="preferredName"/>; if not found,
    /// falls back to the first descendant that has an Image component.
    /// Logs a warning when falling back so the user knows to update their config.
    /// </summary>
    private static Transform FindChildWithImage(Transform root, string preferredName)
    {
        Transform named = FindChildRecursive(root, preferredName);
        if (named != null && named.GetComponent<Image>() != null)
            return named;

        // Fallback: depth-first search for first Image component
        Transform fallback = FindChildWithComponentRecursive<Image>(root);
        if (fallback != null)
        {
            if (named == null)
                Debug.LogWarning("PSD Import: Child named '" + preferredName + "' not found in '" + root.name + "'. Using fallback child '" + fallback.name + "' (has Image). Update buttonImageChildName in PsdToUnityConfig to suppress this warning.");
            else
                Debug.LogWarning("PSD Import: Child '" + preferredName + "' in '" + root.name + "' has no Image component. Using fallback child '" + fallback.name + "'. Update buttonImageChildName in PsdToUnityConfig.");
            return fallback;
        }
        return null;
    }

    /// <summary>
    /// Finds the child Transform that should receive the button text content.
    /// First tries to find a child named <paramref name="preferredName"/>; if not found,
    /// falls back to the first descendant that has a Text component.
    /// Logs a warning when falling back so the user knows to update their config.
    /// </summary>
    private static Transform FindChildWithText(Transform root, string preferredName)
    {
        Transform named = FindChildRecursive(root, preferredName);
        if (named != null && named.GetComponent<Text>() != null)
            return named;

        // Fallback: depth-first search for first Text component
        Transform fallback = FindChildWithComponentRecursive<Text>(root);
        if (fallback != null)
        {
            if (named == null)
                Debug.LogWarning("PSD Import: Child named '" + preferredName + "' not found in '" + root.name + "'. Using fallback child '" + fallback.name + "' (has Text). Update buttonTextChildName in PsdToUnityConfig to suppress this warning.");
            else
                Debug.LogWarning("PSD Import: Child '" + preferredName + "' in '" + root.name + "' has no Text component. Using fallback child '" + fallback.name + "'. Update buttonTextChildName in PsdToUnityConfig.");
            return fallback;
        }
        return null;
    }

    /// <summary>
    /// Depth-first search for the first descendant that has a component of type T.
    /// Does NOT check <paramref name="parent"/> itself — only descendants.
    /// </summary>
    private static Transform FindChildWithComponentRecursive<T>(Transform parent) where T : Component
    {
        foreach (Transform child in parent)
        {
            if (child.GetComponent<T>() != null)
                return child;
            Transform found = FindChildWithComponentRecursive<T>(child);
            if (found != null)
                return found;
        }
        return null;
    }

    /// <summary>
    /// Result of GetScenePath — contains all three path fields needed by LayerMappingEntry.
    /// </summary>
    private struct ScenePathInfo
    {
        public string scenePath;
        public string prefabRoot;
        public string prefabAssetPath;
    }

    /// <summary>
    /// Computes scene path info for a Transform.
    ///
    /// If the GO (or any of its ancestors) is a prefab instance root, the innermost
    /// such root is used as the anchor:
    ///   - prefabRoot      = full scene path to that root GO
    ///   - scenePath       = path from below the root down to t (exclusive of root name)
    ///                       If t IS the root, scenePath = t.name
    ///   - prefabAssetPath = Unity project-relative path to the .prefab asset
    ///
    /// For plain scene GOs (no prefab ancestor):
    ///   - prefabRoot      = ""
    ///   - prefabAssetPath = ""
    ///   - scenePath       = full hierarchy path from scene root (legacy behavior)
    /// </summary>
    private static ScenePathInfo GetScenePath(Transform t)
    {
        // Walk up to find the innermost prefab instance root
        Transform innermostPrefabRoot = null;
        Transform cur = t;
        while (cur != null)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(cur.gameObject))
            {
                innermostPrefabRoot = cur;
                break; // stop at the first (innermost) root found walking upward
            }
            cur = cur.parent;
        }

        if (innermostPrefabRoot == null)
        {
            // Plain scene GO — build full path from root (legacy behavior)
            string fullPath = t.name;
            Transform c = t.parent;
            while (c != null)
            {
                fullPath = c.name + "/" + fullPath;
                c = c.parent;
            }
            return new ScenePathInfo { scenePath = fullPath, prefabRoot = "", prefabAssetPath = "" };
        }

        // Build full scene path to the prefab root
        string prefabRootPath = innermostPrefabRoot.name;
        Transform rc = innermostPrefabRoot.parent;
        while (rc != null)
        {
            prefabRootPath = rc.name + "/" + prefabRootPath;
            rc = rc.parent;
        }

        // Build relative path from below the prefab root to t
        // If t == innermostPrefabRoot, the relative path is just the root's own name
        string relPath;
        if (t == innermostPrefabRoot)
        {
            relPath = t.name;
        }
        else
        {
            relPath = t.name;
            Transform walker = t.parent;
            while (walker != null && walker != innermostPrefabRoot)
            {
                relPath = walker.name + "/" + relPath;
                walker = walker.parent;
            }
            // walker == innermostPrefabRoot at this point; relPath is the child path below the root
        }

        string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(innermostPrefabRoot.gameObject);
        return new ScenePathInfo
        {
            scenePath       = relPath,
            prefabRoot      = prefabRootPath,
            prefabAssetPath = assetPath ?? ""
        };
    }

    /// <summary>
    /// Resolves a LayerMappingEntry to a live GameObject in the scene.
    ///
    /// If prefabRoot is non-empty (nested prefab entry):
    ///   1. Find the prefab root GO via GameObject.Find(prefabRoot)
    ///   2. Navigate to the child via Transform.Find(scenePath)
    ///
    /// If prefabRoot is empty (legacy / plain scene entry):
    ///   - Fall back to GameObject.Find(scenePath)
    ///
    /// Returns null and logs a warning if resolution fails.
    /// </summary>
    private static GameObject ResolveGameObject(LayerMappingEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.prefabRoot))
        {
            // Two-step prefab resolution
            GameObject rootGo = GameObject.Find(entry.prefabRoot);
            if (rootGo == null)
            {
                Debug.LogWarning("[PSD Import] Prefab root not found: " + entry.prefabRoot);
                return null;
            }
            // If the entry IS the prefab root itself, scenePath == root name — return root directly
            if (entry.scenePath == rootGo.name)
                return rootGo;

            Transform child = rootGo.transform.Find(entry.scenePath);
            if (child == null)
            {
                Debug.LogWarning("[PSD Import] Child not found in prefab: " + entry.prefabRoot + "/" + entry.scenePath);
                return null;
            }
            return child.gameObject;
        }

        // Legacy: full scene path resolution (backward-compatible with old mapping files that have no prefabRoot)
        // JsonUtility deserializes missing fields as empty string, so old files work automatically.
        return GameObject.Find(entry.scenePath);
    }

    /// <summary>
    /// After updating a component on a GO that lives inside a nested prefab instance,
    /// records the property modification and applies it back to the prefab asset.
    ///
    /// Uses ApplyObjectOverride (per-component) rather than ApplyPrefabInstance (entire instance)
    /// to avoid accidentally promoting unrelated scene overrides (position, scale, etc.).
    ///
    /// Skipped when prefabRoot is empty (plain scene GO — no prefab to apply to).
    /// Skipped when the prefab asset is the text prefab or button prefab defined in config —
    /// these are shared template assets and should not be auto-modified.
    /// </summary>
    private static void ApplyComponentOverrideToPrefab(Component component, LayerMappingEntry entry, PsdToUnityConfigCollection config)
    {
        if (entry == null || string.IsNullOrEmpty(entry.prefabRoot)) return;
        if (string.IsNullOrEmpty(entry.prefabAssetPath)) return;

        // Skip if the prefab asset is the text or button template prefab from config.
        // Those are shared assets — auto-applying overrides would corrupt the template.
        if (config != null)
        {
            if (!string.IsNullOrEmpty(config.textPrefabPath) &&
                string.Equals(entry.prefabAssetPath, config.textPrefabPath, System.StringComparison.OrdinalIgnoreCase))
                return;
            if (!string.IsNullOrEmpty(config.buttonPrefabPath) &&
                string.Equals(entry.prefabAssetPath, config.buttonPrefabPath, System.StringComparison.OrdinalIgnoreCase))
                return;
        }

        s_IsApplyingOverrides = true;
        try
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            PrefabUtility.ApplyObjectOverride(component, entry.prefabAssetPath, InteractionMode.AutomatedAction);
        }
        finally
        {
            s_IsApplyingOverrides = false;
        }
    }

    /// <summary>
    /// Serializes a dictionary of LayerMappingEntry objects to layers_mapping.json.
    /// Entries carry full path info: scenePath, prefabRoot, prefabAssetPath.
    /// </summary>
    private static void WriteMappingFile(string systemPath, float canvasW, float canvasH,
        Dictionary<string, LayerMappingEntry> entryMap)
    {
        var entries = new List<LayerMappingEntry>(entryMap.Values);
        var file = new LayerMappingFile
        {
            importDate   = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            canvasWidth  = canvasW,
            canvasHeight = canvasH,
            layers       = entries
        };
        File.WriteAllText(systemPath, JsonUtility.ToJson(file, true));
    }

    /// <summary>
    /// Reads layers_mapping.json and returns the list of LayerMappingEntry objects,
    /// or null if the file does not exist.
    ///
    /// Backward-compatible: JsonUtility deserializes missing prefabRoot / prefabAssetPath
    /// fields as empty string, so old mapping files continue to work via the legacy
    /// GameObject.Find(scenePath) path in ResolveGameObject.
    /// </summary>
    private static List<LayerMappingEntry> ReadMappingEntries(string systemPath)
    {
        if (!File.Exists(systemPath))
            return null;
        string json = File.ReadAllText(systemPath);
        LayerMappingFile loaded = JsonUtility.FromJson<LayerMappingFile>(json);
        if (loaded == null || loaded.layers == null)
            return null;
        var result = new List<LayerMappingEntry>();
        foreach (LayerMappingEntry e in loaded.layers)
            if (!string.IsNullOrEmpty(e.layerName) && !string.IsNullOrEmpty(e.scenePath))
                result.Add(e);
        return result;
    }

}

/// <summary>
/// Serializable container written to layers_mapping.json alongside the CSV after every import.
/// </summary>
[System.Serializable]
public class LayerMappingFile
{
    public string importDate;
    public float  canvasWidth;
    public float  canvasHeight;
    public List<LayerMappingEntry> layers;
}

/// <summary>
/// One entry per created GameObject: records the layer name, scene path info,
/// and optional prefab anchor fields for nested-prefab resolution.
///
/// prefabRoot and prefabAssetPath are optional (empty = plain scene GO, legacy behavior).
/// Old mapping files without these fields are loaded with empty strings by JsonUtility —
/// backward-compatible with the existing GameObject.Find(scenePath) resolution path.
/// </summary>
[System.Serializable]
public class LayerMappingEntry
{
    public string layerName;
    public string scenePath;      // Full scene path (no prefab) OR relative path below prefab root
    public string prefabRoot;     // Full scene path to innermost prefab instance root (empty if not in prefab)
    public string prefabAssetPath; // Unity project-relative path to .prefab asset (empty if not in prefab)
}

/// <summary>
/// Listens for GameObject renames in the Hierarchy and automatically updates all
/// layers_mapping.json files in the project so that re-imports still resolve correctly.
///
/// Flow:
///   1. User selects a GameObject → cache its instance ID and name.
///   2. User renames it → EditorApplication.hierarchyChanged fires.
///   3. Watcher detects the old name differs from the new name.
///   4. Scans all layers_mapping.json files in Assets/ for matching layerName entries.
///   5. Updates their scenePath, prefabRoot, and prefabAssetPath to the new values and saves.
/// </summary>
[UnityEditor.InitializeOnLoad]
public static class PsdMappingWatcher
{
    private static int    _cachedId;
    private static string _cachedName;

    static PsdMappingWatcher()
    {
        UnityEditor.Selection.selectionChanged    += OnSelectionChanged;
        UnityEditor.EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private static void OnSelectionChanged()
    {
        GameObject go = UnityEditor.Selection.activeGameObject;
        if (go != null)
        {
            _cachedId   = go.GetInstanceID();
            _cachedName = go.name;
        }
        else
        {
            _cachedId   = 0;
            _cachedName = null;
        }
    }

    private static void OnHierarchyChanged()
    {
        if (_cachedId == 0 || string.IsNullOrEmpty(_cachedName)) return;

        // Look up the cached GO by instance ID
        Object obj = UnityEditor.EditorUtility.InstanceIDToObject(_cachedId);
        GameObject go = obj as GameObject;
        if (go == null) return;

        // No name change — nothing to do
        if (go.name == _cachedName) return;

        string oldName = _cachedName;

        // Update cache so further renames on the same object keep working
        _cachedName = go.name;

        UpdateAllMappingFiles(oldName, go.transform);
    }

    /// <summary>
    /// Finds every layers_mapping.json in the project and updates any entry whose
    /// layerName matches <paramref name="oldLayerName"/> with freshly computed path info
    /// (scenePath, prefabRoot, prefabAssetPath) from the current transform state.
    /// This correctly handles both the renamed GO itself and renaming the prefab root GO.
    /// </summary>
    private static void UpdateAllMappingFiles(string oldLayerName, Transform renamedTransform)
    {
        // Compute the new path info for the renamed GO
        ScenePathInfo newInfo = ComputeScenePath(renamedTransform);

        string[] guids = UnityEditor.AssetDatabase.FindAssets("layers_mapping");
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (!assetPath.EndsWith("layers_mapping.json",
                    System.StringComparison.OrdinalIgnoreCase)) continue;

            string systemPath = System.IO.Path.GetFullPath(assetPath);
            string json = System.IO.File.ReadAllText(systemPath);
            LayerMappingFile mapping = JsonUtility.FromJson<LayerMappingFile>(json);
            if (mapping == null || mapping.layers == null) continue;

            bool changed = false;
            foreach (LayerMappingEntry entry in mapping.layers)
            {
                if (entry.layerName == oldLayerName)
                {
                    entry.scenePath       = newInfo.scenePath;
                    entry.prefabRoot      = newInfo.prefabRoot;
                    entry.prefabAssetPath = newInfo.prefabAssetPath;
                    changed = true;
                }
            }

            if (changed)
            {
                System.IO.File.WriteAllText(systemPath, JsonUtility.ToJson(mapping, true));
                UnityEditor.AssetDatabase.ImportAsset(assetPath);
                Debug.Log("PSD Mapping: '" + oldLayerName + "' renamed → paths updated (scenePath='" + newInfo.scenePath + "', prefabRoot='" + newInfo.prefabRoot + "')");
            }
        }
    }

    /// <summary>
    /// Duplicates the GetScenePath logic from PsdLayerImporter as a static helper
    /// accessible to PsdMappingWatcher without requiring an instance.
    /// </summary>
    private struct ScenePathInfo
    {
        public string scenePath;
        public string prefabRoot;
        public string prefabAssetPath;
    }

    private static ScenePathInfo ComputeScenePath(Transform t)
    {
        // Find innermost prefab instance root
        Transform innermostPrefabRoot = null;
        Transform cur = t;
        while (cur != null)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(cur.gameObject))
            {
                innermostPrefabRoot = cur;
                break;
            }
            cur = cur.parent;
        }

        if (innermostPrefabRoot == null)
        {
            string fullPath = t.name;
            Transform c = t.parent;
            while (c != null)
            {
                fullPath = c.name + "/" + fullPath;
                c = c.parent;
            }
            return new ScenePathInfo { scenePath = fullPath, prefabRoot = "", prefabAssetPath = "" };
        }

        string prefabRootPath = innermostPrefabRoot.name;
        Transform rc = innermostPrefabRoot.parent;
        while (rc != null)
        {
            prefabRootPath = rc.name + "/" + prefabRootPath;
            rc = rc.parent;
        }

        string relPath;
        if (t == innermostPrefabRoot)
        {
            relPath = t.name;
        }
        else
        {
            relPath = t.name;
            Transform walker = t.parent;
            while (walker != null && walker != innermostPrefabRoot)
            {
                relPath = walker.name + "/" + relPath;
                walker = walker.parent;
            }
        }

        string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(innermostPrefabRoot.gameObject);
        return new ScenePathInfo
        {
            scenePath       = relPath,
            prefabRoot      = prefabRootPath,
            prefabAssetPath = assetPath ?? ""
        };
    }
}

/// <summary>
/// Listens for prefab asset saves (via AssetPostprocessor) and automatically
/// re-syncs any layers_mapping.json that references the saved prefab via prefabAssetPath.
///
/// This means developers do not need to press "Remap PSD Layers" after editing and
/// saving a prefab — the mapping is updated automatically.
///
/// Guard: skipped when PsdLayerImporter.s_IsApplyingOverrides is true to prevent
/// an infinite loop caused by ApplyObjectOverride triggering a re-import.
/// </summary>
public class PsdPrefabAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        // Guard against re-entrant calls triggered by ApplyObjectOverride during import
        if (PsdLayerImporter.s_IsApplyingOverrides)
            return;

        // Collect prefab paths that were saved/re-imported
        var changedPrefabs = new List<string>();
        foreach (string path in importedAssets)
            if (path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                changedPrefabs.Add(path);

        if (changedPrefabs.Count == 0)
            return;

        // Find all layers_mapping.json files in the project
        string[] guids = AssetDatabase.FindAssets("layers_mapping");
        foreach (string guid in guids)
        {
            string mappingAssetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!mappingAssetPath.EndsWith("layers_mapping.json",
                    System.StringComparison.OrdinalIgnoreCase)) continue;

            string mappingSystemPath = Path.GetFullPath(mappingAssetPath);
            string json = File.ReadAllText(mappingSystemPath);
            LayerMappingFile mapping = JsonUtility.FromJson<LayerMappingFile>(json);
            if (mapping == null || mapping.layers == null) continue;

            bool changed = false;
            foreach (LayerMappingEntry entry in mapping.layers)
            {
                if (string.IsNullOrEmpty(entry.prefabAssetPath)) continue;

                // Check if this entry's prefab was among those saved
                bool prefabChanged = false;
                foreach (string prefabPath in changedPrefabs)
                {
                    if (string.Equals(entry.prefabAssetPath, prefabPath,
                            System.StringComparison.OrdinalIgnoreCase))
                    {
                        prefabChanged = true;
                        break;
                    }
                }
                if (!prefabChanged) continue;

                // Resolve the GO and recompute its path
                GameObject rootGo = GameObject.Find(entry.prefabRoot);
                if (rootGo == null) continue;

                Transform child = null;
                if (entry.scenePath == rootGo.name)
                {
                    child = rootGo.transform;
                }
                else
                {
                    Transform found = rootGo.transform.Find(entry.scenePath);
                    if (found != null)
                        child = found;
                }
                if (child == null) continue;

                // Recompute via PsdLayerImporter's GetScenePath by re-resolving from the live hierarchy
                // We call PsdMappingWatcher's internal helper indirectly through the GO's transform.
                // Since ScenePathInfo is private, we rebuild the path here inline.
                Transform innermostRoot = null;
                Transform cur = child;
                while (cur != null)
                {
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(cur.gameObject))
                    {
                        innermostRoot = cur;
                        break;
                    }
                    cur = cur.parent;
                }

                if (innermostRoot == null) continue; // no longer in a prefab — skip

                string prefabRootPath = innermostRoot.name;
                Transform rc = innermostRoot.parent;
                while (rc != null)
                {
                    prefabRootPath = rc.name + "/" + prefabRootPath;
                    rc = rc.parent;
                }

                string relPath;
                if (child == innermostRoot)
                {
                    relPath = child.name;
                }
                else
                {
                    relPath = child.name;
                    Transform walker = child.parent;
                    while (walker != null && walker != innermostRoot)
                    {
                        relPath = walker.name + "/" + relPath;
                        walker = walker.parent;
                    }
                }

                string newAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(innermostRoot.gameObject) ?? "";

                entry.scenePath       = relPath;
                entry.prefabRoot      = prefabRootPath;
                entry.prefabAssetPath = newAssetPath;
                changed = true;
            }

            if (changed)
            {
                File.WriteAllText(mappingSystemPath, JsonUtility.ToJson(mapping, true));
                AssetDatabase.ImportAsset(mappingAssetPath);
                Debug.Log("[PSD Mapping] Auto-synced mapping for prefab: " + string.Join(", ", changedPrefabs.ToArray()));
            }
        }
    }
}
