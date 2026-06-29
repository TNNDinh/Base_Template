using UnityEngine;
using System.IO;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.UI;
#if UNITY_EDITOR
public class OptimizeBorderSprite
{
    [MenuItem("Assets/Texture/OptimizeSprite")]
    private static void OptimizeSprite()
    {
        List<Sprite> sprites = new List<Sprite>();
        foreach (var obj in Selection.objects)
        {
            Sprite sprite = null;
            if (obj is Sprite)
            {
                sprite = obj as Sprite;
            }
            else if (obj is Texture2D)
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(obj));
            }
            else
            {
                Debug.Log("Not support type " + obj.GetType().ToString());
            }
            if (sprite != null && ((sprite.border.x > 0 && sprite.border.z > 0) || (sprite.border.y > 0 && sprite.border.w > 0)))
            {
                string path = AssetDatabase.GetAssetPath(sprite.texture);
                int pixelX = sprite.texture.width;
                int pixelY = sprite.texture.height;
                int width = pixelX;
                int height = pixelY;
                int bottom = 0;
                int left = 0;
                if (sprite.border.x > 0 && sprite.border.z > 0)
                {
                    left = (int)sprite.border.x;
                    width = (int)(sprite.border.x + sprite.border.z);
                }
                if (sprite.border.y > 0 && sprite.border.w > 0)
                {
                    bottom = (int)sprite.border.y;
                    height = (int)(sprite.border.y + sprite.border.w);
                }
                Texture2D texture = new Texture2D(width, height);
                int size = width * height;
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!ti.isReadable)
                {
                    ti.isReadable = true;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                Color[] pixel = sprite.texture.GetPixels();
                Color[] pixelNew = new Color[size];
                int c, r;
                for (int i = 0; i < size; i++)
                {
                    c = i % width;
                    r = i / width;
                    if (c >= left)
                    {
                        c = pixelX - (width - c);
                    }
                    if (r >= bottom)
                    {
                        r = pixelY - (height - r);
                    }
                    pixelNew[i] = pixel[r * pixelX + c];
                }
                texture.SetPixels(pixelNew);
                string pathSystem = Application.dataPath;
                pathSystem = pathSystem.Substring(0, pathSystem.Length - 6) + AssetDatabase.GetAssetPath(sprite);
                texture.Apply();
                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(pathSystem, bytes);
                sprites.Add(sprite);
                if (ti.isReadable == true)
                {
                    ti.isReadable = false;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
        }
        AssetDatabase.Refresh();
        if (sprites.Count > 0)
        {
            //FixImageSprite(sprites);
        }
    }

    private static void FixImageSprite(List<Sprite> sprites)
    {
        var referenceCache = new Dictionary<string, List<string>>();

        string[] guids = AssetDatabase.FindAssets("");
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);

            foreach (var dependency in dependencies)
            {
                if (referenceCache.ContainsKey(dependency))
                {
                    if (!referenceCache[dependency].Contains(assetPath))
                    {
                        referenceCache[dependency].Add(assetPath);
                    }
                }
                else
                {
                    referenceCache[dependency] = new List<string>() { assetPath };
                }
            }
        }
        List<GameObject> gameObjects = new List<GameObject>();
        foreach (Object child in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(child);
            if (referenceCache.ContainsKey(path))
            {
                foreach (var reference in referenceCache[path])
                {
                    GameObject gmObject = AssetDatabase.LoadAssetAtPath(reference, (typeof(GameObject))) as GameObject;
                    if (gmObject != null && !gameObjects.Contains(gmObject))
                    {
                        gameObjects.Add(gmObject);
                    }
                }
            }
        }
        List<Image> imgs;
        foreach (GameObject gm in gameObjects)
        {
            imgs = GetImage(gm.transform, sprites);
            foreach (Image im in imgs)
            {
                if (im.type == Image.Type.Simple)
                {
                    im.type = Image.Type.Sliced;
                }
            }
            imgs.Clear();
        }
        gameObjects.Clear();
        referenceCache.Clear();
    }

    public static List<Image> GetImage(Transform trans, List<Sprite> sprites)
    {
        var result = new List<Image>();
        bool active = trans.gameObject.activeSelf;
        trans.gameObject.SetActive(true);
        var im = trans.GetComponent<Image>();
        if (im != null && im.sprite != null && sprites.Contains(im.sprite))
        {
            result.Add(im);
        }
        foreach (Transform child in trans)
        {
            result.AddRange(GetImage(child, sprites));
        }
        trans.gameObject.SetActive(active);
        return result;
    }

    //[MenuItem("Assets/FindTexturePVRTC")]
    static void FindTexturePVRTC()
    {
        if (Selection.activeObject != null)
        {
            string[] guids2 = AssetDatabase.FindAssets("t:texture2D", new[] { AssetDatabase.GetAssetPath(Selection.activeObject) });
            List<Object> gos = new List<Object>();
            foreach (string guid2 in guids2)
            {
                if (AssetDatabase.GUIDToAssetPath(guid2).Contains("/PVRTC/"))
                {
                    Object go = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid2));
                    gos.Add(go);
                }
            }
            Selection.objects = gos.ToArray();
        }
    }

    //[MenuItem("Assets/Texture/FindTextureASTC")]
    static void FindTextureASTC()
    {
        Debug.Log("FindTextureASTC");

        string[] guids2 = AssetDatabase.FindAssets("t:texture2D", new[] { "Assets/AssetBundles/SpineModels/UI" });
        Debug.Log(guids2.Length);
        List<Object> gos = new List<Object>();
        foreach (string guid2 in guids2)
        {
            if (!AssetDatabase.GUIDToAssetPath(guid2).Contains("/PVRTC/"))
            {
                Object go = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid2));
                gos.Add(go);
            }
        }
        Debug.Log(gos.Count);
        Selection.objects = gos.ToArray();

    }

    [MenuItem("Assets/Texture/FixSupportETCBottomRight")]
    public static void FixSupportETCBottomRight()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is Texture2D)
            {
                FixSupportETCTexure((Texture2D)obj, SetColorBottonRight, true);
            }
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Texture/FixSupportETCTopRight")]
    public static void FixSupportETCTopRight()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is Texture2D)
            {
                FixSupportETCTexure((Texture2D)obj, SetColorTopRight);
            }
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Texture/FixSupportETCTopLeft")]
    public static void FixSupportETCTopLeft()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is Texture2D)
            {
                FixSupportETCTexure((Texture2D)obj, SetColorTopLeft);
            }
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Texture/FixSupportETCBottomLeft")]
    public static void FixSupportETCBottomLeft()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is Texture2D)
            {
                FixSupportETCTexure((Texture2D)obj, SetColorBottomLeft);
            }
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Texture/FixSupportETCMid")]
    public static void FixSupportETCMid()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is Texture2D)
            {
                FixSupportETCTexure((Texture2D)obj, SetColorMid);
            }
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Texture/FixSupportETCMidBottom")]
    public static void FixSupportETCMidBottom()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is Texture2D)
            {
                FixSupportETCTexure((Texture2D)obj, SetColorMidBottom);
            }
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Texture/FixSupportETCMidTop")]
    public static void FixSupportETCMidTop()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is Texture2D)
            {
                FixSupportETCTexure((Texture2D)obj, SetColorMidTop);
            }
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Texture/FixSupportETCMidLeft")]
    public static void FixSupportETCMidLeft()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is Texture2D)
            {
                FixSupportETCTexure((Texture2D)obj, SetColorMidLeft);
            }
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Texture/FixSupportETCMidRight")]
    public static void FixSupportETCMidRight()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is Texture2D)
            {
                FixSupportETCTexure((Texture2D)obj, SetColorMidRight);
            }
        }
        AssetDatabase.Refresh();
    }

    public static void SetColor(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY, int left, int bottom)
    {
        if (y < bottom || x < left || x >= pixelX + left || y >= pixelY + bottom)
        {
            pixelNew[index] = new Color(0, 0, 0, 0);
        }
        else
        {
            pixelNew[index] = pixel[(y - bottom) * pixelX + x - left];
        }
    }

    public static void SetColorBottonRight(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
    {
        SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, 0, deltaY);
    }

    public static void SetColorTopRight(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
    {
        SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, 0, 0);
    }

    public static void SetColorTopLeft(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
    {
        SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, deltaX, 0);
    }

    public static void SetColorBottomLeft(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
    {
        SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, deltaX, deltaY);
    }

    public static void SetColorMid(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
    {
        SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, Mathf.FloorToInt(deltaX / 2), Mathf.FloorToInt(deltaY / 2));
    }

    public static void SetColorMidTop(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
    {
        SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, Mathf.FloorToInt(deltaX / 2), 0);
    }

    public static void SetColorMidBottom(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
    {
        SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, Mathf.FloorToInt(deltaX / 2), deltaY);
    }

    public static void SetColorMidLeft(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
    {
        SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, deltaX, Mathf.FloorToInt(deltaY / 2));
    }

    public static void SetColorMidRight(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
    {
        SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, 0, Mathf.FloorToInt(deltaY / 2));
    }

    public static void FixSupportETCTexure(Texture2D t, System.Action<int, Color[], Color[], int, int, int, int, int, int> setColor, bool isFixAtlas = false)
    {
        int delta = 4;
        string path = AssetDatabase.GetAssetPath(t);
        int pixelX = t.width;
        int pixelY = t.height;
        int deltaX = (delta - t.width % delta) % delta;
        int deltaY = (delta - t.height % delta) % delta;
        if (deltaX != 0 || deltaY != 0)
        {
            int width = pixelX + deltaX;
            int height = pixelY + deltaY;

            Texture2D texture = new Texture2D(width, height);
            int size = width * height;
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null && !ti.isReadable)
            {
                ti.isReadable = true;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            Color[] pixel = t.GetPixels();
            Color[] pixelNew = new Color[size];
            int x, y;
            for (int i = 0; i < size; i++)
            {
                y = Mathf.FloorToInt(i / width);
                x = i % width;
                setColor(i, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY);
            }
            texture.SetPixels(pixelNew);
            string pathSystem = Application.dataPath;
            pathSystem = pathSystem.Substring(0, pathSystem.Length - 6) + AssetDatabase.GetAssetPath(t);
            texture.Apply();
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(pathSystem, bytes);
            if (ti != null && ti.isReadable == true)
            {
                ti.isReadable = false;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            if (isFixAtlas == true)
            {
                string pathAtlas = pathSystem.Substring(0, pathSystem.Length - 3) + "atlas.txt";
                if (File.Exists(pathAtlas))
                {
                    string aslat = File.ReadAllText(pathAtlas);
                    string format = "size: {0},{1}";
                    aslat = aslat.Replace(string.Format(format, pixelX, pixelY), string.Format(format, width, height));
                    File.WriteAllText(pathAtlas, aslat);
                }
            }
        }
    }

    public static void DisableReadWrite(Texture2D t)
    {
        string path = AssetDatabase.GetAssetPath(t);
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null && ti.isReadable == true)
        {
            ti.isReadable = false;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }

    [MenuItem("Assets/Texture/DisableReadWriteTexture")]
    public static void DisableReadWriteTexture()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is Texture2D)
            {
                DisableReadWrite((Texture2D)obj);
            }
        }
        AssetDatabase.Refresh();
    }
}
#endif