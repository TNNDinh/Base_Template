using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class PsdToUnity
{
    private const string ConfigPath = "Assets/_Project/Resources/Collection/General/";
    struct SkinData
    {
        public string[] bone;
        public Vector2 pos;
        public Vector2 rect;
    }

    [MenuItem("Assets/PsdToUnity")]
    private static void ImportPsdAsset()
    {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        var pathFolder = "";
        for (int i = path.Length - 1; i >= 0; i--)
        {
            if (path[i] == '/')
            {
                pathFolder = path.Substring(0, i + 1);
                break;
            }
        }

        if (path.ToLower().EndsWith(".csv"))
        {
            string dataPosition = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
            var lines = dataPosition.Split('\n', '\r');

            List<SkinData> skinData = new List<SkinData>();

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line) == false)
                {
                    var datas = line.Split(',');
                    if (datas.Length != 5)
                    {
                        Debug.Log("Line is not valid: " + line);
                    }
                    else
                    {
                        skinData.Add(new SkinData()
                        {
                            bone = datas[0].Split('/'), pos = new Vector2(float.Parse(datas[1]), float.Parse(datas[2])),
                            rect = new Vector2(float.Parse(datas[3]), float.Parse(datas[4]))
                        });
                    }
                }
            }

            if (skinData.Count > 0)
            {
                Canvas canvas = GameObject.FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    Debug.LogError("Null Canvas");
                    return;
                }
                else
                {
                    Transform root = (new GameObject("root")).transform;
                    root = root.gameObject.AddComponent<RectTransform>();
                    root.SetParent(canvas.transform);
                    root.localPosition = Vector3.zero;
                    root.localScale = Vector3.one;
                    ((RectTransform)root).anchorMin = Vector2.zero;
                    ((RectTransform)root).anchorMax = Vector2.one;
                    ((RectTransform)root).sizeDelta = Vector2.zero;
                    PsdToUnityConfigCollection config =
                        AssetDatabase.LoadAssetAtPath<PsdToUnityConfigCollection>(ConfigPath + "PsdToUnityConfig.asset");
                    if (config == null)
                    {
                        Debug.LogError(
                            "SpriteFolderConfig not found at Assets/SpriteFolderConfig.asset. Please create it.");
                        return;
                    }

                    for (int i = skinData.Count - 1; i >= 0; i--)
                    {
                        SkinData skin = skinData[i];
                        Transform parent = root;
                        // Thay đổi vòng lặp tạo hierarchy: chỉ tạo đến intermediate (j < skin.bone.Length - 1)
                        for (int j = 1; j < skin.bone.Length - 1; j++)
                        {
                            Transform currentBone = parent.Find(skin.bone[j]);
                            if (currentBone == null)
                            {
                                currentBone = (new GameObject(skin.bone[j])).transform;
                                if (currentBone.GetComponent<RectTransform>() == null)
                                {
                                    currentBone = currentBone.gameObject.AddComponent<RectTransform>();
                                    ((RectTransform)currentBone).sizeDelta = Vector2.zero;
                                    ((RectTransform)currentBone).localScale = Vector3.one;
                                }
                                currentBone.position = root.position;
                                currentBone.SetParent(parent);
                            }
                            parent = currentBone;
                        }

                        // Sau vòng lặp, parent là intermediate cuối (hoặc root nếu không có intermediate)
                        // Bây giờ tạo leaf trực tiếp dưới parent với logic replace, không tạo thêm GO rỗng cho leaf
                        string leafName = skin.bone[skin.bone.Length - 1];
                        string n = leafName;
                        for (int j = n.Length - 2; j >= 0; j--)
                        {
                            if (n[j] == '[')
                            {
                                n = n.Substring(0, j);
                                break;
                            }
                        }
                        string cleanName = n.Replace(" ", "").ToLower();
                        string prefix = cleanName.Length >= 3 ? cleanName.Substring(0, 3) : cleanName;

                        GameObject leafGO = null;
                        RectTransform leafRect = null;
                        if (prefix == "btn")
                        {
                            GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(config.buttonPrefabPath);
                            if (buttonPrefab != null)
                            {
                                leafGO = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, parent);
                                leafRect = leafGO.GetComponent<RectTransform>();
                                leafRect.sizeDelta = skin.rect;
                                leafRect.position = new Vector3(skin.pos.x, skin.pos.y, 0);
                                // Check và set sprite nếu tồn tại trong folder config
                                string spritePath = config.spriteFolderPath + cleanName + ".png";
                                Sprite btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                                if (btnSprite != null)
                                {
                                    Image btnImage = leafGO.GetComponent<Image>();
                                    if (btnImage != null)
                                    {
                                        btnImage.sprite = btnSprite;
                                    }
                                }
                                Button btn = leafGO.GetComponent<Button>();
                                if (btn != null)
                                {
                                    btn.interactable = true;
                                }
                                leafGO.name = cleanName;
                            }
                            else
                            {
                                Debug.LogError("Button prefab not found at: " + config.buttonPrefabPath);
                            }
                        }
                        else if (prefix == "txt")
                        {
                            GameObject textPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(config.textPrefabPath);
                            if (textPrefab != null)
                            {
                                leafGO = (GameObject)PrefabUtility.InstantiatePrefab(textPrefab, parent);
                                leafRect = leafGO.GetComponent<RectTransform>();
                                leafRect.sizeDelta = skin.rect;
                                leafRect.position = new Vector3(skin.pos.x, skin.pos.y, 0);
                                Text txt = leafGO.GetComponent<Text>();
                                if (txt != null)
                                {
                                    var content = cleanName.Replace("txt_", "");
                                    txt.text = content;
                                }
                                leafGO.name = cleanName;
                            }
                            else
                            {
                                Debug.LogError("Text prefab not found at: " + config.textPrefabPath);
                            }
                        }
                        else
                        {
                            // Fallback: Tạo leaf như cũ (Image đơn giản)
                            leafGO = new GameObject(cleanName);
                            leafRect = leafGO.AddComponent<RectTransform>();
                            leafRect.SetParent(parent);
                            Image image = leafGO.AddComponent<Image>();
                            image.raycastTarget = false;
                            // Khởi tạo sprite = null
                            Sprite sprite = null;

                            // Check đặc biệt cho currency
                            if (cleanName.StartsWith("currency_"))
                            {
                                string id = cleanName.Substring(9); // Xóa "currency_" (9 ký tự)
                                string currencyPath = config.currencyFolderPath + id + ".psd";
                                Sprite currencySprite = AssetDatabase.LoadAssetAtPath<Sprite>(currencyPath);
                                if (currencySprite != null)
                                {
                                    sprite = currencySprite;
                                }
                            }

                            // Nếu không phải currency hoặc không tìm thấy, check spriteFolderPath trước, fallback pathFolder
                            if (sprite == null)
                            {
                                string spritePath = config.spriteFolderPath + cleanName + ".psd";
                                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                                if (sprite == null)
                                {
                                    string fallbackPath = pathFolder + cleanName + ".psd";
                                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fallbackPath);
                                }
                            }

                            if (sprite != null)
                            {
                                image.sprite = sprite;
                                leafRect.sizeDelta = skin.rect;
                            }
                            leafRect.position = new Vector3(skin.pos.x, skin.pos.y, 0);
                        }

                        if (leafRect != null)
                        {
                            leafRect.localScale = Vector3.one;
                        }
                    }

                    Transform[] childs = root.GetComponentsInChildren<Transform>();
                    for (int k = 0; k < childs.Length; k++) // Thay i=1 bằng k=0 để clean tất cả nếu cần
                    {
                        if (childs[k] != root) // Bỏ qua root
                        {
                            string n = childs[k].name;
                            for (int j = n.Length - 2; j >= 0; j--)
                            {
                                if (n[j] == '[')
                                {
                                    childs[k].name = n.Substring(0, j);
                                    break;
                                }
                            }
                        }
                    }

                    EditorGUIUtility.PingObject(root.gameObject);
                }
            }
        }
    }

    [MenuItem("GameObject/Extension/Remove All Raycast", false, 0)]
    public static void RemoveAllRaycast()
    {
        var selected = Selection.gameObjects;
        if (selected.Length == 1)
        {
            Graphic[] childs = selected[0].GetComponentsInChildren<Graphic>();
            for (int i = 0; i < childs.Length; i++)
            {
                Graphic graphic = childs[i];
                if (graphic != null)
                {
                    Undo.RecordObject(graphic, "RemoveRaycast");
                    graphic.raycastTarget = false;
                }
            }
        }
    }
}