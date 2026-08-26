namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Gộp nhiều nguồn cử chỉ, lấy nguồn nào đang có người dùng. Thêm kiểu điều khiển mới
    /// chỉ việc cắm thêm một nguồn, camera không phải biết.
    /// </summary>
    public class CompositeGestureSource : IHomeCameraGestureSource
    {
        #region Fields

        private readonly IHomeCameraGestureSource[] _sources;

        #endregion

        #region Initialize

        public CompositeGestureSource(params IHomeCameraGestureSource[] sources) => _sources = sources;

        #endregion

        #region Public

        public HomeCameraGesture Read()
        {
            HomeCameraGesture result = default;

            // Vẫn đọc hết mọi nguồn dù đã có kết quả: nguồn nào cũng phải được nhịp mỗi khung hình
            // để giữ mốc vị trí, bỏ nhịp một cái là lần vuốt sau nhảy vọt.
            for (int i = 0; i < _sources.Length; i++)
            {
                if (_sources[i] == null) continue;

                HomeCameraGesture gesture = _sources[i].Read();
                if (gesture.IsActive && !result.IsActive) result = gesture;
            }

            return result;
        }

        #endregion
    }
}
