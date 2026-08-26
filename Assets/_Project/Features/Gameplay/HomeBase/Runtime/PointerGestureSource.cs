using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Đọc cử chỉ từ chuột: giữ trái rê là vuốt, lăn bánh xe là thu/phóng.
    /// Có nguồn này thì thử ngay trong Editor được, khỏi phải build xuống máy.
    /// </summary>
    public class PointerGestureSource : IHomeCameraGestureSource
    {
        #region Constants

        /// <summary>Một nấc lăn chuột quy ra bằng ngần này pixel chụm ngón, cho hai nguồn cùng thang đo.</summary>
        private const float SCROLL_TO_PIXELS = 140f;

        #endregion

        #region Fields

        private Vector2 _previousPosition;
        private bool _isDragging;

        #endregion

        #region Public

        public HomeCameraGesture Read()
        {
            Vector2 position = Input.mousePosition;
            float zoom = Input.mouseScrollDelta.y * SCROLL_TO_PIXELS;

            if (Input.GetMouseButtonDown(0))
            {
                _previousPosition = position;
                _isDragging = true;
            }

            if (!Input.GetMouseButton(0)) _isDragging = false;

            Vector2 pan = Vector2.zero;
            if (_isDragging)
            {
                pan = position - _previousPosition;
                _previousPosition = position;
            }

            bool isActive = _isDragging || !Mathf.Approximately(zoom, 0f);
            return new HomeCameraGesture(position, pan, zoom, isActive);
        }

        #endregion
    }
}
