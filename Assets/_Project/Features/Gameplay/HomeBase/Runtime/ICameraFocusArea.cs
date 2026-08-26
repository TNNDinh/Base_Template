using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Vùng mà camera được phép lia quanh. Camera chỉ cần biết chừng này, không cần biết
    /// bên dưới là căn cứ nhà hay bất cứ thứ gì khác.
    /// </summary>
    public interface ICameraFocusArea
    {
        /// <summary>Tâm vùng, world space.</summary>
        Vector3 AreaCenter { get; }

        /// <summary>Nửa cạnh vùng, tính trên mặt phẳng ngang.</summary>
        float AreaHalfExtent { get; }
    }
}
