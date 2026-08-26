using UnityEngine;
using UnityEngine.EventSystems;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Bọc một nguồn cử chỉ khác và nuốt cử chỉ nào bắt đầu trên UI, để bấm nút không kéo cả cảnh theo.
    /// <para>
    /// Chỉ xét lúc ĐẶT tay xuống: đã bắt đầu vuốt ở ngoài UI thì kéo ngang qua nút vẫn lia bình thường,
    /// nếu không camera sẽ khựng lại giữa chừng mỗi lần tay quét qua thanh UI.
    /// </para>
    /// </summary>
    public class UiBlockingGestureSource : IHomeCameraGestureSource
    {
        #region Fields

        private readonly IHomeCameraGestureSource _inner;
        private bool _wasActive;
        private bool _isBlocked;

        #endregion

        #region Initialize

        public UiBlockingGestureSource(IHomeCameraGestureSource inner) => _inner = inner;

        #endregion

        #region Public

        public HomeCameraGesture Read()
        {
            // Vẫn đọc nguồn trong mọi khung hình dù có chặn hay không, để nó giữ đúng mốc vị trí.
            HomeCameraGesture gesture = _inner.Read();

            if (!gesture.IsActive)
            {
                _wasActive = false;
                _isBlocked = false;
                return gesture;
            }

            if (!_wasActive) _isBlocked = IsPointerOverUi();
            _wasActive = true;

            return _isBlocked ? default : gesture;
        }

        #endregion

        #region Private

        private static bool IsPointerOverUi()
        {
            EventSystem system = EventSystem.current;
            if (system == null) return false;

            if (Input.touchCount > 0) return system.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            return system.IsPointerOverGameObject();
        }

        #endregion
    }
}
