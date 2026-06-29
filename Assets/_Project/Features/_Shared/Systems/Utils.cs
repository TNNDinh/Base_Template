using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Ezg.Core.Adapter;
using Ezg.Core.Utils;
using Ezg.Feature.Shared;
using Ezg.Package.Audio;
using Ezg.Package.Localize;
using Newtonsoft.Json;
using Spine.Unity;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.Shared.Systems
{
    public static class Utils
    {
        private static readonly NumberFormatInfo DotNumberFormat = new()
        {
            NumberGroupSeparator = ".", // đổi sang dấu .
            NumberDecimalSeparator = "," // (tùy, không dùng bên dưới 10k)
        };

        public static Dictionary<TKey, TValue> CloneDictionaryCloningValues<TKey, TValue>
            (Dictionary<TKey, TValue> original) where TValue : ICloneable
        {
            var ret = new Dictionary<TKey, TValue>(original.Count,
                original.Comparer);
            foreach (var entry in original) ret.Add(entry.Key, (TValue)entry.Value.Clone());

            return ret;
        }

        public static T CloneJson<T>(this T source)
        {
            if (ReferenceEquals(source, null)) return default;

            var deserializeSettings = new JsonSerializerSettings
                { ObjectCreationHandling = ObjectCreationHandling.Replace };
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source), deserializeSettings);
        }

        public static T CloneJsonPolymorphic<T>(this T source)
        {
            if (ReferenceEquals(source, null)) return default;

            var json = JsonConvert.SerializeObject(source, source.GetType(), null);
            return (T)JsonConvert.DeserializeObject(json, source.GetType(), new JsonSerializerSettings
                { ObjectCreationHandling = ObjectCreationHandling.Replace });
        }

        #region Physics

        /// <summary>
        ///     Thay đổi radius của collider 2D theo thời gian
        /// </summary>
        /// <param name="circleCollider"></param>
        /// <param name="targetRadius"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static IEnumerator ChangeRadiusOverTime(this CircleCollider2D circleCollider, float targetRadius,
            float duration)
        {
            if (circleCollider == null) yield break;

            var startRadius = circleCollider.radius;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                circleCollider.radius = Mathf.Lerp(startRadius, targetRadius, elapsed / duration);
                yield return null;
            }

            circleCollider.radius = targetRadius;
        }

        #endregion

        public static int ToMiliseconds(this float value)
        {
            return Convert.ToInt32(value * 1000);
        }

        public static int ToMiliseconds(this int value)
        {
            return Convert.ToInt32(value * 1000);
        }

        public static long ToMiliseconds(this long value)
        {
            return Convert.ToInt64(value * 1000);
        }

        public static byte[] ObjectToByteArray(this object obj)
        {
            if (obj == null)
                return null;
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static T ByteArrayToObject<T>(this byte[] arrBytes)
        {
            var memStream = new MemoryStream();
            var binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            var obj = (T)binForm.Deserialize(memStream);

            return obj;
        }

        /// <summary>
        ///     Trả về tỉ lệ có success với rate này hay ko
        /// </summary>
        /// <param name="rate"></param>
        /// <returns></returns>
        public static bool IsSuccessWithRate(this float rate)
        {
            return Random.value <= rate;
        }

        /// <summary>
        ///     Trả về index của trọng số được chọn
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int GetIndexInRate(this List<float> list)
        {
            float total = 0;

            foreach (var element in list) total += element;

            var randomPoint = Random.value * total;

            for (var i = 0; i < list.Count; i++)
            {
                if (randomPoint < list[i]) return i;

                randomPoint -= list[i];
            }

            return list.Count - 1;
        }

        public static int GetIndexInRate(this IEnumerable<float> list)
        {
            float total = 0;

            foreach (var element in list) total += element;

            var randomPoint = Random.value * total;

            var count = list.Count();
            for (var i = 0; i < count; i++)
            {
                if (randomPoint < list.ElementAt(i)) return i;

                randomPoint -= list.ElementAt(i);
            }

            return count - 1;
        }

        /// <summary>
        ///     Lấy kích thước của game thông qua camera
        /// </summary>
        /// <returns></returns>
        public static Vector2 GetCameraWorldSize()
        {
            var cam = Camera.main;
            var height = 2f * cam.orthographicSize;
            var width = height * cam.aspect;
            return new Vector2(width, height);
        }

        /// <summary>
        ///     Rút ngắn các giá trị tiền tệ trong game với K, M, B...
        /// </summary>
        /// <param name="money"></param>
        /// <returns></returns>
        public static string MoneyConvert(this long money)
        {
            return money switch
            {
                >= 100000000000 => $"{money / 1000000000.0:0.#}B",
                >= 1000000000 => $"{money / 1000000000.0:0.##}B",
                >= 100000000 => $"{money / 1000000.0:0.#}M",
                >= 1000000 => $"{money / 1000000.0:0.##}M",
                >= 100000 => $"{money / 1000.0:0}K",
                >= 10000 => $"{money / 1000.0:0.#}K",
                _ => money.ToString("#,##0", DotNumberFormat)
            };
        }

        /// <summary>
        ///     Trả về vị trí trong đường tròn
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="angel"></param>
        /// <returns></returns>
        public static Vector3 GetPosInCircle(Vector3 center, float radius, float angel)
        {
            Vector3 pos;
            pos.x = center.x + radius * Mathf.Sin(angel * Mathf.Deg2Rad);
            pos.y = center.y + radius * Mathf.Cos(angel * Mathf.Deg2Rad);
            pos.z = center.z;
            return pos;
        }

        public static Vector3 GetPosInCircle(Vector3 center, float radius, int index, int total, float startAngle = 0)
        {
            Vector3 pos;
            var angle = startAngle + 120f / total * index;
            pos.x = center.x + radius * Mathf.Sin(angle * Mathf.Deg2Rad);
            pos.y = center.y + radius * Mathf.Cos(angle * Mathf.Deg2Rad);
            pos.z = center.z;
            return pos;
        }

        /// <summary>
        ///     Trả về vị trí trong đường tròn
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="angel"></param>
        /// <returns></returns>
        public static Vector3 GetPosInCircle(this Transform transform, float radius, float angel)
        {
            Vector3 pos;
            pos.x = transform.position.x + radius * Mathf.Sin(angel * Mathf.Deg2Rad);
            pos.y = transform.position.y + radius * Mathf.Cos(angel * Mathf.Deg2Rad);
            pos.z = transform.position.z;
            return pos;
        }

        /// <summary>
        ///     Tính toán giá trị dựa trên công thức
        /// </summary>
        /// <param name="valueInFormula"></param>
        /// <param name="formula"></param>
        /// <returns></returns>
        public static float GetValueByFormular(this string formula, Dictionary<string, float> valueInFormula)
        {
            if (string.IsNullOrEmpty(formula)) return valueInFormula.ElementAt(0).Value;

            foreach (var value in valueInFormula)
                formula = Regex.Replace(formula, value.Key, value.Value.ToString(CultureInfo.InvariantCulture));

            return Convert.ToSingle(new DataTable().Compute(formula, null));
        }

        public static List<T> MappingWithoutInherit<T>(this IList input) where T : class
        {
            var count = input.Count;

            //Tạo model
            var result = new List<T>();

            //Type xuất ra
            var tProperties =
                typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            for (var c = 0; c < count; c++)
            {
                result.Add(Activator.CreateInstance<T>());
                foreach (var t in tProperties) t.SetValue(result[c], t.GetValue(input[c]));
            }

            return result;
        }

        public static List<T> Mapping<T>(this IList input) where T : class
        {
            var count = input.Count;

            //Tạo model
            var result = new List<T>();

            //Type truyền vào
            var uProperties = input[0].GetType().GetProperties();

            //Type xuất ra
            var tProperties = typeof(T).GetProperties();

            for (var c = 0; c < count; c++)
            {
                result.Add(Activator.CreateInstance<T>());
                for (var i = 0; i < tProperties.Length; i++)
                for (var x = 0; x < uProperties.Length; x++)
                    if (tProperties[i].Name == uProperties[x].Name)
                    {
                        tProperties[i].SetValue(result[c], uProperties[x].GetValue(input[c]));
                        break;
                    }
            }

            return result;
        }

        public static Canvas GetCanvas(this RectTransform rt)
        {
            return rt.gameObject.GetComponentInParent<Canvas>();
        }

        public static float GetWidth(this RectTransform rt)
        {
            var w = (rt.anchorMax.x - rt.anchorMin.x) * Screen.width + rt.sizeDelta.x * rt.GetCanvas().scaleFactor;
            return w;
        }

        public static float GetHeight(this RectTransform rt)
        {
            var h = (rt.anchorMax.y - rt.anchorMin.y) * Screen.height + rt.sizeDelta.y * rt.GetCanvas().scaleFactor;
            return h;
        }

        /// <summary>
        ///     Tính khoảng cách giữa 2 số
        /// </summary>
        /// <param name="number1"></param>
        /// <param name="number2"></param>
        /// <returns></returns>
        public static int GetDistance(this int number1, int number2)
        {
            return Math.Abs(number1 - number2);
        }

        public static float GetDistance(this float number1, float number2)
        {
            return Math.Abs(number1 - number2);
        }

        #region Color

        public static Color GetColorWithHex(string hexColor)
        {
            if (ColorUtility.TryParseHtmlString(hexColor, out var newCol))
                return newCol;

            Debug.Log("GetColorWithHex: Convert hex fail");
            return Color.white;
        }

        #endregion

        public static void SnapTo(this ScrollRect scroller, RectTransform target, Vector2 bonusPos)
        {
            Canvas.ForceUpdateCanvases();

            var contentPos = (Vector2)scroller.transform.InverseTransformPoint(scroller.content.position);
            var childPos = (Vector2)scroller.transform.InverseTransformPoint(target.position);
            var endPos = contentPos - childPos;

            //Cuộn dọc
            if (!scroller.horizontal) endPos.x = contentPos.x;

            //Cuộn ngang
            if (!scroller.vertical) endPos.y = contentPos.y;

            scroller.content.DOAnchorPos(endPos + bonusPos, 0.5f).SetEase(Ease.OutQuad);
        }

        public static void SnapTo(this ScrollRect scrollRect, int index)
        {
            if (scrollRect.content == null) return;

            var itemTransform = scrollRect.content.GetChild(index);

            // Lấy parent của item
            var parentRect = itemTransform.parent as RectTransform;

            // Lấy vị trí của parent trong scroll view
            var parentPos = scrollRect.viewport.InverseTransformPoint(parentRect.position);

            // Lấy vị trí của item trong parent
            var itemPos = parentRect.InverseTransformPoint(itemTransform.position);

            // Tổng vị trí của item trong scroll view
            var viewportPos = new Vector2(parentPos.x + itemPos.x, parentPos.y + itemPos.y);

            // Tính toán normalized 
            var normalizedPos = new Vector2(0, 1 - viewportPos.y / scrollRect.viewport.rect.height);

            // Cuộn đến vị trí
            scrollRect.DONormalizedPos(normalizedPos, 0.5f).SetEase(Ease.OutQuad);
        }

        public static void SnapTo(this ScrollRect scrollRect, RectTransform contentPanel,
            List<RectTransform> rectItemList, int targetIndex)
        {
            Canvas.ForceUpdateCanvases();

            if (rectItemList == null || rectItemList.Count == 0) return;

            // Clamp để tránh ArgumentOutOfRangeException khi targetIndex vượt số item thực tế
            var clampedIndex = Mathf.Clamp(targetIndex, 0, rectItemList.Count);

            var yReduce = 0f;
            for (var i = 0; i < clampedIndex; i++) yReduce += rectItemList[i].sizeDelta.y;

            contentPanel.DOLocalMoveY(yReduce, yReduce == 0 ? 0f : 0.5f, true).SetEase(Ease.OutQuad);
        }

        /// <summary>
        ///     Di chuyển object tới vị trí, thay cho dotween với target đang di chuyển
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="target"></param>
        /// <param name="duration"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public static IEnumerator DoMove(this GameObject obj, Transform target, float duration,
            UnityAction onComplete = null)
        {
            var startPos = obj.transform.position;
            var elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                obj.transform.position = Vector3.Lerp(startPos, target.position, elapsedTime / duration);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            obj.transform.position = target.position;
            onComplete?.Invoke();
        }

        /// <summary>
        ///     Thay thế từ cuối cùng tìm thấy
        /// </summary>
        /// <param name="source"></param>
        /// <param name="find"></param>
        /// <param name="replace"></param>
        /// <returns></returns>
        public static string ReplaceLast(this string source, string find, string replace)
        {
            var place = source.LastIndexOf(find);

            if (place == -1)
                return source;

            return source.Remove(place, find.Length).Insert(place, replace);
        }

        /// <summary>
        ///     Cập nhật vị trí angle theo góc tròn
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="spread"></param>
        /// <param name="index"></param>
        /// <param name="totalObject"></param>
        public static void UpdateAngleCircle(this Transform trans, float spread, int index, int totalObject)
        {
            trans.eulerAngles = new Vector3(trans.eulerAngles.x,
                trans.eulerAngles.y,
                trans.eulerAngles.z + spread * (index + 1) - (totalObject + 1) * spread / 2f);
        }

        /// <summary>
        ///     Convert string to Underscore type
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToUnderscoreCase(this string str)
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLower();
        }

        /// <summary>
        ///     Convert object to dict
        /// </summary>
        /// <param name="source"></param>
        /// <param name="bindingAttr"></param>
        /// <returns></returns>
        public static IDictionary<string, object> ToDictionary(this object source,
            BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
        {
            return source.GetType().GetProperties(bindingAttr).ToDictionary
            (
                propInfo => propInfo.Name,
                propInfo => propInfo.GetValue(source, null)
            );
        }

        public static Dictionary<string, TValue> ToDictionary<TValue>(this object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, TValue>>(json);
            return dictionary;
        }

#if UNITY_EDITOR
        public static void CreateDirectory(string path)
        {
            if (Directory.Exists(path)) return;

            Directory.CreateDirectory(path);
        }
#endif

        public static Vector3 GetWorldPositionFromRectTransform(this RectTransform rectTransform)
        {
            var screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, rectTransform.position);

            var ray = Camera.main.ScreenPointToRay(screenPoint);

            var distance = (0 - ray.origin.z) / ray.direction.z;
            var worldPos = ray.origin + ray.direction * distance;
            return worldPos;
        }

        public static Vector3 GetWorldPositionFromUI(this RectTransform uiElement)
        {
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, uiElement.position);

            var ray = Camera.main.ScreenPointToRay(screenPos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Plane")))
            {
                var distance = hit.distance;
                Debug.Log($"Hit plane at distance: {distance:F2}");

                var worldPos = hit.point;


                return worldPos;
            }

            return Vector3.zero;
        }

        #region Spine

        public static bool ChangeSkin(this SkeletonGraphic skeletonGraphic, string skinName, bool updateNow = true)
        {
            if (skeletonGraphic == null) return false;

            if (skeletonGraphic.Skeleton == null) return false;

            if (string.IsNullOrEmpty(skinName)) skinName = "default";

            var skeleton = skeletonGraphic.Skeleton;

            var skin = skeleton.Data.FindSkin(skinName);
            if (skin == null)
            {
                Debug.LogError($"Không tìm thấy skin: '{skinName}' trong SkeletonData!");
                return false;
            }

            skeleton.SetSkin(skin);
            skeleton.SetSlotsToSetupPose();

            if (skeletonGraphic.AnimationState != null) skeletonGraphic.AnimationState.Apply(skeleton);

            if (updateNow)
            {
                skeletonGraphic.Update(0f);
                skeletonGraphic.SetMaterialDirty();
            }

            Debug.Log($"Đã thay skin thành công: <color=green>{skinName}</color>");
            return true;
        }

        #endregion

        #region AddCanvasGraphicRaycaster

        public static (GraphicRaycaster, Canvas) AddCanvasGraphicRaycasterUpCurrencyBar(this GameObject obj)
        {
            var _graphicRaycaster = obj.GetComponent<GraphicRaycaster>() ?? obj.AddComponent<GraphicRaycaster>();
            var _canvas = obj.GetComponent<Canvas>() ?? obj.AddComponent<Canvas>();
            _canvas.overrideSorting = true;
            _canvas.sortingLayerName = "UI";
            var currencyBarCanvas = UIManager.Instance.currencyBar.GetComponent<Canvas>();
            _canvas.sortingOrder = currencyBarCanvas != null ? currencyBarCanvas.sortingOrder : 1;
            return (_graphicRaycaster, _canvas);
        }

        #endregion

        /// <summary>
        ///     Retrieves mapped internal generic prefab resources efficiently bounds checking metrics seamlessly mapped
        ///     dependencies checking metrics arrays cleanly structural definitions checking limits gracefully limits datasets
        ///     tracking mappings dependencies definitions variables gracefully parameters target sequences target sequences
        ///     matrices safely target gracefully loops structurally targets.
        /// </summary>
        /// <returns>Prefab instance properly limits gracefully dataset bounds limits target mapping references.</returns>
        public static GameObject GetCurrencyPrefab()
        {
            return ResLoader.Load<GameObject>(PathUtils.CurrencyPrefab);
        }

        public static GameObject SpawnCurrencyMoveToTarget(Vector3 pos, Vector2 sizeDelta, int idItem, Transform parent,
            Vector3 targetPos, bool isAnim = true)
        {
            var prefab = GetCurrencyPrefab();
            var objCurrency = Object.Instantiate(prefab, parent);
            objCurrency.GetComponent<RectTransform>().sizeDelta = sizeDelta;
            objCurrency.transform.position = pos;
            objCurrency.gameObject.SetActive(true);
            objCurrency.transform.localScale = Vector3.one;
            // removed: CurrencySpawnController (gameplay removed)
            if (isAnim)
            {
                objCurrency.transform.DOMove(targetPos, 0.5f)
                    .OnComplete(() => { Object.DestroyImmediate(objCurrency); });
                return null;
            }

            return objCurrency;
        }

        #region Coroutine

        public static void DelayMethod(this MonoBehaviour mono, float time, Action callback)
        {
            if (!mono.gameObject.activeSelf) return;
            mono.StartCoroutine(Delay(time, callback));
        }

        public static void DelayMethod(this MonoBehaviour mono, float time, IEnumerator callback)
        {
            if (!mono.gameObject.activeSelf) return;
            mono.StartCoroutine(Delay(time, mono, callback));
        }

        public static void DelayRealTimeMethod(this MonoBehaviour mono, float time, Action callback)
        {
            if (!mono.gameObject.activeSelf) return;
            mono.StartCoroutine(DelayRealTime(time, callback));
        }

        private static IEnumerator Delay(float time, Action callback)
        {
            yield return new WaitForSeconds(time);
            callback?.Invoke();
        }

        private static IEnumerator Delay(float time, MonoBehaviour mono, IEnumerator callback)
        {
            yield return new WaitForSeconds(time);
            mono.StartCoroutine(callback);
        }

        private static IEnumerator DelayRealTime(float time, Action callback)
        {
            yield return new WaitForSecondsRealtime(time);
            callback?.Invoke();
        }

        #endregion

        #region Get Target

        public static Transform GetRandomTarget(this Transform transform, float radius, int layer,
            Collider2D[] hitColliders, string[] tagThrough = null, Transform targetThrough = null)
        {
            var detected = Physics2D.OverlapCircleNonAlloc(transform.position, radius, hitColliders, layer);

            if (detected == 0)
                return null;

            var result = new List<Transform>();

            for (var i = 0; i < detected; i++)
            {
                if (layer == transform.gameObject.layer)
                    continue;

                if (hitColliders[i].transform == targetThrough)
                    continue;

                if (tagThrough != null && tagThrough.Contains(hitColliders[i].gameObject.tag))
                    continue;

                result.Add(hitColliders[i].transform);
            }

            return result.Count == 0 ? null : result[Random.Range(0, result.Count)];
        }

        public static Transform GetNearestTarget(this Transform transform, float radius, int layer,
            Collider2D[] hitColliders, string[] tagThrough = null, Transform targetThrough = null)
        {
            var detected = Physics2D.OverlapCircleNonAlloc(transform.position, radius, hitColliders, layer);

            if (detected == 0)
                return null;

            var bestTarget = default(Transform);
            var currentSpace = Mathf.Infinity;

            for (var i = 0; i < detected; i++)
            {
                if (layer == transform.gameObject.layer)
                    continue;

                if (hitColliders[i].transform == targetThrough)
                    continue;

                if (tagThrough != null && tagThrough.Contains(hitColliders[i].gameObject.tag))
                    continue;

                var space = (transform.position - hitColliders[i].transform.position).sqrMagnitude;
                if (space >= currentSpace) continue;

                currentSpace = space;
                bestTarget = hitColliders[i].transform;
            }

            return float.IsPositiveInfinity(currentSpace) ? null : bestTarget;
        }

        public static Transform GetNearestTarget(this Transform transform, float radius, int layer,
            Collider2D[] hitColliders, string[] tagThrough = null, List<Transform> targetThrough = null)
        {
            var detected = Physics2D.OverlapCircleNonAlloc(transform.position, radius, hitColliders, layer);

            if (detected == 0)
                return null;

            var bestTarget = default(Transform);
            var currentSpace = Mathf.Infinity;

            for (var i = 0; i < detected; i++)
            {
                if (layer == transform.gameObject.layer)
                    continue;

                if (targetThrough != null && targetThrough.Contains(hitColliders[i].transform))
                    continue;

                if (tagThrough != null && tagThrough.Contains(hitColliders[i].gameObject.tag))
                    continue;

                var space = (transform.position - hitColliders[i].transform.position).sqrMagnitude;
                if (space >= currentSpace) continue;

                currentSpace = space;
                bestTarget = hitColliders[i].transform;
            }

            return float.IsPositiveInfinity(currentSpace) ? null : bestTarget;
        }

        public static List<T> FindNearTargetInCircle<T>(this Transform transform, int quantity, float radius, int layer,
            int layerOriginal,
            Collider2D[] hitColliders, string tag = null, Transform targetThrough = null)
        {
            var passList = new List<Transform>();
            if (targetThrough != null)
                passList.Add(targetThrough);
            var detected = Physics2D.OverlapCircleNonAlloc(transform.position, radius, hitColliders, layer);

            if (detected == 0)
                return null;

            var numberFinded = 0;
            var list = new List<T>();
            for (var i = 0; i < detected; i++)
            {
                if ((!string.IsNullOrEmpty(tag) && !hitColliders[i].gameObject.CompareTag(tag)) ||
                    passList.Contains(hitColliders[i].transform))
                    continue;

                var character = hitColliders[i].GetComponent<T>();
                if (character != null)
                {
                    list.Add(character);
                    passList.Add(hitColliders[i].transform);
                    numberFinded++;
                    if (numberFinded >= quantity)
                        break;
                }
            }

            return list;
        }

        public static Vector3 GetRandomPosInRadius(this Transform transform, float range = 0)
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);

            var x = Mathf.Cos(angle) * range;
            var y = Mathf.Sin(angle) * range;

            return transform.position + new Vector3(x, y, 0);
        }

        public static Vector3 GetRandomPosInCircle(this Transform transform, float range = 0)
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);

            var radius = Random.Range(0, range);
            var x = Mathf.Cos(angle) * radius;
            var y = Mathf.Sin(angle) * radius;

            return transform.position + new Vector3(x, y, 0);
        }

        public static Vector2 GetRandomPosInCircle(this Vector2 pos, float range = 0)
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);

            var radius = Random.Range(0, range);
            var x = Mathf.Cos(angle) * radius;
            var y = Mathf.Sin(angle) * radius;

            return pos + new Vector2(x, y);
        }

        #endregion

        #region String handle

        /// <summary>
        ///     Lấy biểu thức
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetOperator(RequireViewType type)
        {
            switch (type)
            {
                case RequireViewType.Mul:
                    return "x";
                case RequireViewType.Div:
                    return ":";
                case RequireViewType.Add:
                    return "+";
                case RequireViewType.Sub:
                    return "- ";
            }

            return "";
        }

        /// <summary>
        ///     Chuyen string sang dạng snake_case
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToSnakeCase(this string str)
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString()))
                .ToLower();
        }

        /// <summary>
        ///     Chuyển chuỗi gạch dưới sang dạng viết hoa chữ cái đầu
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string SnakeCaseToCamelCase(this string name)
        {
            if (string.IsNullOrEmpty(name) || !name.Contains("_")) return name;

            var array = name.Split('_');
            for (var i = 0; i < array.Length; i++)
            {
                var s = array[i];
                var first = string.Empty;
                var rest = string.Empty;
                if (s.Length > 0) first = char.ToUpperInvariant(s[0]).ToString();

                if (s.Length > 1) rest = s.Substring(1).ToLowerInvariant();

                array[i] = first + rest;
            }

            var newName = string.Join("", array);
            return newName;
        }

        public static string ConvertSnakeCaseToPascalCase(this string snakeCase)
        {
            var words = snakeCase.Split('_');

            var pascalCaseBuilder = new StringBuilder();

            foreach (var word in words)
                if (!string.IsNullOrEmpty(word))
                    pascalCaseBuilder.Append(char.ToUpper(word[0])).Append(word.Substring(1));

            return pascalCaseBuilder.ToString();
        }


        public static Vector2 ConvertViewPortToScreen(Vector2 viewport)
        {
            return new Vector2(viewport.x * Screen.width, viewport.y * Screen.height);
        }

        public static bool IsValidateEmail(string email)
        {
            // returns true if the input is a valid email
            return Regex.IsMatch(email,
                @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }

        public static string ChangeColorArticle(string hexColor, string content)
        {
            return $"<color={hexColor}>{content}</color>";
            ;
        }

        public static string GetRemainingTimeToString(float seconds)
        {
            return TimeSpan.FromSeconds(seconds).ToString(seconds > 3600 ? @"hh:mm:ss" : @"mm:ss");
        }

        public static DateTime ConvertStringTime(string time)
        {
            var year = int.Parse(time.Substring(0, 4));
            var month = int.Parse(time.Substring(4, 2));
            var day = int.Parse(time.Substring(6, 2));
            var hour = int.Parse(time.Substring(8, 2));
            var minute = int.Parse(time.Substring(10, 2));
            var second = int.Parse(time.Substring(12, 2));
            return new DateTime(year, month, day, hour, minute, second);
        }

        public static DateTime ConvertToDateTime(string value)
        {
            return Convert.ToDateTime(value);
        }

        public static int GetStageId(int worldId, int mapId)
        {
            return worldId * 1000000 + mapId * 1000;
        }

        public static Tuple<int, int> GetWorldMap(int stageId)
        {
            var worldId = stageId / 1000000;
            var mapId = worldId % 1000;
            return Tuple.Create(worldId, mapId);
        }

        /// <summary>
        ///     Convert string to enum
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        /// <summary>
        ///     Chuyển enum thành list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> ConvertEnumToList<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>().ToList();
        }


        /// <summary>
        ///     Chuyển định dạng từ giây sang 00:00:00
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static string FormatTime(int seconds)
        {
            var hour = seconds / (60 * 60);
            var minutes = (seconds - hour * 60 * 60) / 60;
            var second = seconds % 60;
            var hourString = hour < 10 ? "0" + hour : hour.ToString();
            var minutesString = minutes < 10 ? "0" + minutes : minutes.ToString();
            var secondStr = second < 10 ? "0" + second : second.ToString();
            return $"{hourString}:{minutesString}:{secondStr}";
        }

        /// <summary>
        ///     Chuyen string sang dạng gạch dưới
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToUnderScoreCase(this string str)
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString()))
                .ToLower();
        }

        /// <summary>
        ///     Chuyển chuỗi gạch dưới sang dạng viết hoa chữ cái đầu
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string UnderscoreToCamelCase(this string name)
        {
            if (string.IsNullOrEmpty(name) || !name.Contains("_")) return name;

            var array = name.Split('_');
            for (var i = 0; i < array.Length; i++)
            {
                var s = array[i];
                var first = string.Empty;
                var rest = string.Empty;
                if (s.Length > 0) first = char.ToUpperInvariant(s[0]).ToString();

                if (s.Length > 1) rest = s.Substring(1).ToLowerInvariant();

                array[i] = first + rest;
            }

            var newName = string.Join("", array);
            return newName;
        }

        /// <summary>
        ///     Chuyển chữ cái đầu về lowerCase
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FirstCharToLowerCase(this string str)
        {
            if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
                return str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str[1..];

            return str;
        }

        private static readonly global::System.Random random = new();

        public static string RandomString(this int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        ///     Tạo random một số int
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static int SetRandom()
        {
            return random.Next(int.MaxValue);
        }

        public static void CopyToClipboard(this string str)
        {
            GUIUtility.systemCopyBuffer = str;
        }

        #endregion

        #region Item

        // public static string GetDescriptionItems(MergeEnum.MergeItemTypes types, int id)
        // {
        //     string des = "";
        //     ItemData itemMergeModel = DataManager.ItemMerge.GetByIdAndType(types, id);
        //         if (DataItemCacheManager.ItemGeneratorCache.TryGetValue((types, id),
        //                 out var generator) && generator != null) des = Itemdes.Generator;
        //         else if (DataItemCacheManager.ItemExpandCache.TryGetValue((types, id),
        //                      out var expand) && expand != null) des = Itemdes.Expand;
        //         else if (DataItemCacheManager.ItemToolCache.TryGetValue((types, id),
        //                      out var tool) && tool != null) des = Itemdes.Tool;
        //         else if (DataItemCacheManager.ItemPlusCache.TryGetValue((types, id),
        //                      out var plus) && plus != null) des = Itemdes.Plus;
        //         else if (DataItemCacheManager.ItemDuplicateCamera.TryGetValue((types, id),
        //                      out var camera) && camera != null) des = Itemdes.DuplicateCamera;
        //         else if (DataItemCacheManager.ItemSpeedBoostCache.TryGetValue((types, id),
        //                      out var speedBoost) && speedBoost != null) des = Itemdes.SpeedBoost;
        //         else if (DataItemCacheManager.ItemSandglassCache.TryGetValue((types, id),
        //                      out var sandglass) && sandglass != null) des = Itemdes.Sandglass;
        //         else if (DataItemCacheManager.ItemScissorsCache.TryGetValue((types, id),
        //                      out var scissors) && scissors != null) des = Itemdes.Scissors;
        //         else if (DataItemCacheManager.ItemUnlimitedEnergyCache.TryGetValue(
        //                      (types, id), out var unlimitedEnergy) &&
        //                  unlimitedEnergy != null) des = Itemdes.UnlimitedEnergy;
        //         else if (DataItemCacheManager.ItemChoiceChestCache.TryGetValue((types, id),
        //                      out var choiceChest) && choiceChest != null) des = Itemdes.ChoiceChest;
        //         else if (DataItemCacheManager.ItemFlushGiftCache.TryGetValue((types, id),
        //                      out var flushGift) && flushGift != null) des = Itemdes.FlushGift;
        //         else if (DataItemCacheManager.ItemCurrencyCache.TryGetValue((types, id),
        //                      out var currency) && currency != null) des = Itemdes.Currency;
        //         else if (DataItemCacheManager.ItemDisposableCache.TryGetValue((types, id),
        //                      out var disposable) && disposable != null) des = Itemdes.Disposable;
        //         else if (DataItemCacheManager.ItemBoxGeneratorCache.TryGetValue((types, id),
        //                      out var boxGenerator) && boxGenerator != null) des = Itemdes.BoxGenerator;
        //     return des;
        // }

        #endregion

        #region Tooltip

        // public static void SpawnTooltip(Transform parent, float offsetX, float offsetY, Resource[] reward,
        //     Vector3 worldPos)
        // {
        //     var view = GameObject.Instantiate(DataManager.GeneralAssets.TooltipElement, parent);
        //     RectTransform viewRectTransform = view.GetComponent<RectTransform>();
        //     viewRectTransform.position = worldPos;
        //     viewRectTransform.localPosition = parent.InverseTransformPoint(worldPos);
        //     Vector3 newPos = viewRectTransform.localPosition;
        //     newPos.x -= offsetX;
        //     newPos.y += offsetY;
        //     viewRectTransform.localPosition = newPos;
        //     view.GetComponent<TooltipController>().InitData(reward);
        // }

        #endregion

        #region Asynce

        // public static async void WaitAsync(Action action)
        // {
        //
        // }

        #endregion

        #region Position Key Conversion

        public static int PackPos(this Vector2Int pos)
        {
            return pos.x * 100 + pos.y;
        }

        public static int PackPos(int x, int y)
        {
            return x * 100 + y;
        }

        public static Vector2Int UnpackPos(this int packedKey)
        {
            return new Vector2Int(packedKey / 100, packedKey % 100);
        }

        #endregion
    }
}