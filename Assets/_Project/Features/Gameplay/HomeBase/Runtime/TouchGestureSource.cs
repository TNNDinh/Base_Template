using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Đọc cử chỉ từ màn cảm ứng: một ngón là vuốt, hai ngón chụm/xoè là thu/phóng.
    /// </summary>
    public class TouchGestureSource : IHomeCameraGestureSource
    {
        #region Fields

        private float _lastPinchDistance;
        private bool _wasPinching;

        #endregion

        #region Public

        public HomeCameraGesture Read()
        {
            int count = Input.touchCount;
            if (count == 0)
            {
                _wasPinching = false;
                return default;
            }

            if (count == 1) return ReadSingleTouch();
            return ReadPinch();
        }

        #endregion

        #region Private

        private HomeCameraGesture ReadSingleTouch()
        {
            _wasPinching = false;

            Touch touch = Input.GetTouch(0);
            bool moved = touch.phase == TouchPhase.Moved;
            return new HomeCameraGesture(touch.position, moved ? touch.deltaPosition : Vector2.zero, 0f, true);
        }

        private HomeCameraGesture ReadPinch()
        {
            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);

            Vector2 center = (first.position + second.position) * 0.5f;
            float distance = Vector2.Distance(first.position, second.position);

            // Khung hình đầu của cú chụm chưa có mốc để so, bỏ qua kẻo camera giật một phát.
            float zoom = _wasPinching ? distance - _lastPinchDistance : 0f;
            _lastPinchDistance = distance;
            _wasPinching = true;

            Vector2 pan = (first.deltaPosition + second.deltaPosition) * 0.5f;
            return new HomeCameraGesture(center, pan, zoom, true);
        }

        #endregion
    }
}
