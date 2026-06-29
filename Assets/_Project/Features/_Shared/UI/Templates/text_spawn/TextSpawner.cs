using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TextSpawner : MonoBehaviour
{
    public enum Alignment
    {
        Left,
        Center,
        Right
    }

    [SerializeField] private string text = "Well Done";
    [SerializeField] private float spacing;
    [SerializeField] private float deltaTime = 0.05f;
    [SerializeField] private GameObject template;
    [SerializeField] private Alignment alignment = Alignment.Left;

    [Header("Warp Settings")] [SerializeField]
    private AnimationCurve curveY = AnimationCurve.Linear(0, 0, 1, 0);

    [SerializeField] private float curveScale = 100f;
    [SerializeField] private bool rotateAlongCurve = true;
    [SerializeField] private float rotationScale = 0.2f;

    [Header("Color Settings")] [SerializeField]
    private Gradient gradient;

#if UNITY_EDITOR
    [Header("Editor Preview")] [SerializeField]
    private bool autoPreview; // Automatically preview when values change
#endif

    private readonly List<float> charWidths = new();

    /// <summary>
    ///     Runtime entry point
    /// </summary>
    private void Start()
    {
        if (Application.isPlaying && template != null)
        {
            PrecalculateCharWidths();
            StartCoroutine(SpawnText());
        }
    }

    private IEnumerator SpawnText()
    {
        var totalWidth = CalculateTotalWidth();
        var startOffset = 0f;
        if (alignment == Alignment.Center)
            startOffset = -totalWidth / 2f;
        else if (alignment == Alignment.Right)
            startOffset = -totalWidth;

        var offsetX = startOffset;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var obj = Instantiate(template, transform);

            var uiText = obj.GetComponentInChildren<Text>();
            if (uiText != null)
            {
                uiText.text = c.ToString();
                var colorT = text.Length > 1 ? (float)i / (text.Length - 1) : 0f;
                uiText.color = gradient.Evaluate(colorT);
            }

            var rect = obj.GetComponent<RectTransform>();
            var tNorm = (offsetX - startOffset) / totalWidth;
            var yOffset = curveY.Evaluate(Mathf.Clamp01(tNorm)) * curveScale;
            var pos = new Vector2(offsetX, yOffset);

            if (rect != null)
                rect.anchoredPosition = pos;
            else
                obj.transform.localPosition = new Vector3(pos.x, pos.y, 0f);

            if (rotateAlongCurve)
            {
                var delta = 0.001f;
                var y1 = curveY.Evaluate(Mathf.Clamp01(tNorm - delta));
                var y2 = curveY.Evaluate(Mathf.Clamp01(tNorm + delta));
                var slope = (y2 - y1) / (2f * delta);
                var angle = Mathf.Atan(slope) * Mathf.Rad2Deg * rotationScale;

                if (rect != null)
                    rect.localRotation = Quaternion.Euler(0f, 0f, angle);
                else
                    obj.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (i < text.Length - 1)
            {
                var avgWidth = (charWidths[i] + charWidths[i + 1]) / 2f;
                offsetX += avgWidth + spacing;
            }

            if (c != ' ')
                yield return new WaitForSeconds(deltaTime);
        }
    }

    private void PrecalculateCharWidths()
    {
        charWidths.Clear();
        foreach (var c in text)
        {
            var uiText = template.GetComponentInChildren<Text>();
            var charWidth = 0f;
            if (uiText != null)
            {
                var settings = uiText.GetGenerationSettings(uiText.rectTransform.rect.size);
                charWidth = uiText.cachedTextGenerator.GetPreferredWidth(c.ToString(), settings) / uiText.pixelsPerUnit;
            }

            charWidths.Add(charWidth);
        }
    }

    private float CalculateTotalWidth()
    {
        var total = 0f;
        for (var i = 0; i < text.Length; i++)
            if (i < text.Length - 1)
            {
                var avgWidth = (charWidths[i] + charWidths[i + 1]) / 2f;
                total += avgWidth + spacing;
            }

        return total;
    }

#if UNITY_EDITOR
    /// <summary>
    ///     Generate preview immediately in Edit Mode
    /// </summary>
    public void PreviewInEditor()
    {
        if (PrefabUtility.IsPartOfPrefabAsset(this)) return;

        // Remove old children
        for (var i = transform.childCount - 1; i >= 0; i--) DestroyImmediate(transform.GetChild(i).gameObject);

        if (template == null) return;

        PrecalculateCharWidths();
        GenerateInstantPreview();
    }

    /// <summary>
    ///     Internal preview generation (no coroutine)
    /// </summary>
    private void GenerateInstantPreview()
    {
        var totalWidth = CalculateTotalWidth();
        var startOffset = 0f;
        if (alignment == Alignment.Center)
            startOffset = -totalWidth / 2f;
        else if (alignment == Alignment.Right)
            startOffset = -totalWidth;

        var offsetX = startOffset;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var obj = PrefabUtility.InstantiatePrefab(template, transform) as GameObject;
            if (obj == null) continue;

            var uiText = obj.GetComponentInChildren<Text>();
            if (uiText != null)
            {
                uiText.text = c.ToString();
                var colorT = text.Length > 1 ? (float)i / (text.Length - 1) : 0f;
                uiText.color = gradient.Evaluate(colorT);
            }

            var rect = obj.GetComponent<RectTransform>();

            var tNorm = (offsetX - startOffset) / totalWidth;
            var yOffset = curveY.Evaluate(Mathf.Clamp01(tNorm)) * curveScale;
            var pos = new Vector2(offsetX, yOffset);

            if (rect != null)
                rect.anchoredPosition = pos;
            else
                obj.transform.localPosition = new Vector3(pos.x, pos.y, 0f);

            if (rotateAlongCurve)
            {
                var delta = 0.001f;
                var y1 = curveY.Evaluate(Mathf.Clamp01(tNorm - delta));
                var y2 = curveY.Evaluate(Mathf.Clamp01(tNorm + delta));
                var slope = (y2 - y1) / (2f * delta);

                var angle = Mathf.Atan(slope) * Mathf.Rad2Deg * rotationScale;

                if (rect != null)
                    rect.localRotation = Quaternion.Euler(0f, 0f, angle);
                else
                    obj.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (i < text.Length - 1)
            {
                var avgWidth = (charWidths[i] + charWidths[i + 1]) / 2f;
                offsetX += avgWidth + spacing;
            }
        }
    }

    /// <summary>
    ///     Automatically refresh when inspector values change (Edit Mode only)
    /// </summary>
    private void OnValidate()
    {
        if (!autoPreview || !Application.isEditor || Application.isPlaying)
            return;

        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            PreviewInEditor();
        };
    }
#endif
}