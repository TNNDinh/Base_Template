using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Cử chỉ điều khiển camera đọc được trong một khung hình, đo bằng pixel màn hình.
    /// <para>
    /// Cố tình để ở đơn vị pixel: nguồn cử chỉ chỉ biết màn hình, còn quy ra bao nhiêu mét
    /// trong thế giới là việc của camera — đổi độ cao hay góc nhìn không phải sửa nguồn nhập.
    /// </para>
    /// </summary>
    public readonly struct HomeCameraGesture
    {
        #region Public - Properties

        /// <summary>Vị trí ngón tay / con trỏ, đơn vị pixel.</summary>
        public Vector2 PointerPosition { get; }

        /// <summary>Quãng vuốt của khung hình này, đơn vị pixel.</summary>
        public Vector2 PanPixels { get; }

        /// <summary>Lượng chụm/xoè. Dương là phóng to (zoom in).</summary>
        public float ZoomPixels { get; }

        /// <summary>Có ai đang chạm vào màn hình không.</summary>
        public bool IsActive { get; }

        #endregion

        #region Initialize

        public HomeCameraGesture(Vector2 pointerPosition, Vector2 panPixels, float zoomPixels, bool isActive)
        {
            PointerPosition = pointerPosition;
            PanPixels = panPixels;
            ZoomPixels = zoomPixels;
            IsActive = isActive;
        }

        #endregion
    }

    /// <summary>Nguồn cử chỉ điều khiển camera. Chạm, chuột, hay giả lập đều cắm vừa.</summary>
    public interface IHomeCameraGestureSource
    {
        /// <summary>Đọc cử chỉ của khung hình này. Phải gọi mỗi khung hình để nguồn giữ đúng trạng thái.</summary>
        HomeCameraGesture Read();
    }
}
