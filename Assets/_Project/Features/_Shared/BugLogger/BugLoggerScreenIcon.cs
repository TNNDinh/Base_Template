using System;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Ezg.Feature.Shared.Config;

namespace Assets._Game._4.CORE.Modules.BugLogger
{
    public class BugLoggerScreenIcon : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler,
        IBeginDragHandler, IEndDragHandler
    {
        #region Initialize

        private void Awake()
        {
            _thisRect = GetComponent<RectTransform>();
            _thisImage = GetComponent<Image>();

            if (_parentCanvas == null) _parentCanvas = GetComponentInParent<Canvas>();

            _canvasRect = _parentCanvas.GetComponent<RectTransform>();
            _canvasCamera = _parentCanvas.worldCamera;
            DontDestroyOnLoad(transform.parent.gameObject);
        }

        #endregion

        #region Fields

        [SerializeField] private Canvas _parentCanvas;

        [SerializeField] [Tooltip("Thời gian animation snap về cạnh màn hình")]
        private float _snapDuration = 0.3f;

        [SerializeField] [Tooltip("Khoảng cách min từ button đến cạnh màn hình")]
        private float _edgePadding = 10f;

        private RectTransform _thisRect;
        private RectTransform _canvasRect;
        private Camera _canvasCamera;
        private Image _thisImage;
        private bool _isDragging;

        #endregion

        #region Drag Handlers

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Convert delta từ screen space sang local space của parent
            Vector2 localDelta;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                eventData.position,
                _canvasCamera,
                out var currentLocalPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                eventData.position - eventData.delta,
                _canvasCamera,
                out var previousLocalPoint);

            localDelta = currentLocalPoint - previousLocalPoint;
            _thisRect.anchoredPosition += localDelta;
            ClampToScreen();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            SnapToNearestEdge();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Chỉ xử lý click nếu không phải đang drag
            if (!_isDragging) OnClick();
        }

        #endregion

        #region Functions

        private void OnClick()
        {
            _thisImage.enabled = false; // Ẩn button để không bị chụp vào screenshot
            CaptureAndShowBugLogger().Forget();
        }

        private async UniTaskVoid CaptureAndShowBugLogger()
        {
            var pngBytes = await CaptureScreenshotPngSafe();

            // Show BugLogger và truyền screenshot bytes
            await UIManager.Instance.Show(GameEnums.Features.BugLogger,
                data: pngBytes);

            UIManager.Instance.AddActionCloseFeature(GameEnums.Features.BugLogger, () => _thisImage.enabled = true);
        }

        private async UniTask<byte[]> CaptureScreenshotPngSafe()
        {
            // ReadPixels/screenshot from framebuffer must run at EndOfFrame.
            await UniTask.WaitForEndOfFrame(this);

            try
            {
                var pngBytes = CaptureScreenshotByReadPixels();
                if (pngBytes != null && pngBytes.Length > 0) return pngBytes;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[BugLogger] ReadPixels capture failed, fallback to RenderTexture. Error: {ex.Message}");
            }

            try
            {
                var pngBytes = CaptureScreenshotByRenderTexture();
                if (pngBytes != null && pngBytes.Length > 0) return pngBytes;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BugLogger] RenderTexture fallback capture failed. Error: {ex.Message}");
            }

            return null;
        }

        private static byte[] CaptureScreenshotByRenderTexture()
        {
            RenderTexture activeBefore = null;
            RenderTexture captureRt = null;
            Texture2D tex = null;
            try
            {
                GetCaptureResolution(out var width, out var height);

                // Use ARGB32/RGBA32 to avoid driver-specific packed format issues on emulators.
                captureRt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                ScreenCapture.CaptureScreenshotIntoRenderTexture(captureRt);

                activeBefore = RenderTexture.active;
                RenderTexture.active = captureRt;

                tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                tex.Apply(false, false);
                return tex.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = activeBefore;

                if (tex != null) Destroy(tex);

                if (captureRt != null) RenderTexture.ReleaseTemporary(captureRt);
            }
        }

        private static byte[] CaptureScreenshotByReadPixels()
        {
            Texture2D tex = null;
            try
            {
                GetCaptureResolution(out var width, out var height);

                tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                tex.Apply(false, false);
                return tex.EncodeToPNG();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BugLogger] Fallback screenshot capture failed: {ex.Message}");
                return null;
            }
            finally
            {
                if (tex != null) Destroy(tex);
            }
        }

        private static void GetCaptureResolution(out int width, out int height)
        {
            width = Screen.width;
            height = Screen.height;

            if (width > 0 && height > 0) return;

            var mainDisplay = Display.main;
            width = mainDisplay != null ? mainDisplay.renderingWidth : width;
            height = mainDisplay != null ? mainDisplay.renderingHeight : height;

            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
        }

        /// <summary>
        ///     Lấy bounds của canvas trong local space
        /// </summary>
        private void GetCanvasBounds(out float minX, out float maxX, out float minY, out float maxY)
        {
            var canvasSize = _canvasRect.rect.size;
            var pivot = _canvasRect.pivot;

            // Tính bounds dựa trên pivot của canvas
            minX = -canvasSize.x * pivot.x;
            maxX = canvasSize.x * (1 - pivot.x);
            minY = -canvasSize.y * pivot.y;
            maxY = canvasSize.y * (1 - pivot.y);
        }

        /// <summary>
        ///     Giữ button trong màn hình
        /// </summary>
        private void ClampToScreen()
        {
            var halfWidth = _thisRect.rect.width * _thisRect.pivot.x;
            var halfHeight = _thisRect.rect.height * _thisRect.pivot.y;
            var halfWidthRight = _thisRect.rect.width * (1 - _thisRect.pivot.x);
            var halfHeightTop = _thisRect.rect.height * (1 - _thisRect.pivot.y);

            GetCanvasBounds(out var canvasMinX, out var canvasMaxX, out var canvasMinY, out var canvasMaxY);

            var minX = canvasMinX + halfWidth + _edgePadding;
            var maxX = canvasMaxX - halfWidthRight - _edgePadding;
            var minY = canvasMinY + halfHeight + _edgePadding;
            var maxY = canvasMaxY - halfHeightTop - _edgePadding;

            var clampedPos = _thisRect.anchoredPosition;
            clampedPos.x = Mathf.Clamp(clampedPos.x, minX, maxX);
            clampedPos.y = Mathf.Clamp(clampedPos.y, minY, maxY);

            _thisRect.anchoredPosition = clampedPos;
        }

        /// <summary>
        ///     Snap button về cạnh màn hình gần nhất
        /// </summary>
        private void SnapToNearestEdge()
        {
            var halfWidth = _thisRect.rect.width * _thisRect.pivot.x;
            var halfHeight = _thisRect.rect.height * _thisRect.pivot.y;
            var halfWidthRight = _thisRect.rect.width * (1 - _thisRect.pivot.x);
            var halfHeightTop = _thisRect.rect.height * (1 - _thisRect.pivot.y);

            GetCanvasBounds(out var canvasMinX, out var canvasMaxX, out var canvasMinY, out var canvasMaxY);

            var edgeLeft = canvasMinX + halfWidth + _edgePadding;
            var edgeRight = canvasMaxX - halfWidthRight - _edgePadding;
            var edgeBottom = canvasMinY + halfHeight + _edgePadding;
            var edgeTop = canvasMaxY - halfHeightTop - _edgePadding;

            var currentPos = _thisRect.anchoredPosition;

            // Tính khoảng cách đến 4 cạnh
            var distanceToLeft = Mathf.Abs(currentPos.x - edgeLeft);
            var distanceToRight = Mathf.Abs(currentPos.x - edgeRight);
            var distanceToBottom = Mathf.Abs(currentPos.y - edgeBottom);
            var distanceToTop = Mathf.Abs(currentPos.y - edgeTop);

            // Tìm cạnh gần nhất
            var minDistance = Mathf.Min(distanceToLeft, distanceToRight, distanceToBottom, distanceToTop);

            var targetPos = currentPos;

            if (Mathf.Approximately(minDistance, distanceToLeft))
                targetPos.x = edgeLeft;
            else if (Mathf.Approximately(minDistance, distanceToRight))
                targetPos.x = edgeRight;
            else if (Mathf.Approximately(minDistance, distanceToBottom))
                targetPos.y = edgeBottom;
            else if (Mathf.Approximately(minDistance, distanceToTop)) targetPos.y = edgeTop;

            // Animate snap
            _thisRect.DOAnchorPos(targetPos, _snapDuration).SetEase(Ease.OutBack);
        }

        #endregion
    }
}