using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>Bốn hướng chính trên mặt lưới. Dùng để chỉ cạnh nào của thành mở cổng.</summary>
    public enum TdCompass
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    /// <summary>Tiện ích đổi hướng sang vector và góc xoay.</summary>
    public static class TdCompassExtensions
    {
        #region Public

        /// <summary>Vector đơn vị chỉ ra ngoài theo hướng này.</summary>
        public static Vector3 ToDirection(this TdCompass compass)
        {
            switch (compass)
            {
                case TdCompass.North: return Vector3.forward;
                case TdCompass.East: return Vector3.right;
                case TdCompass.South: return Vector3.back;
                default: return Vector3.left;
            }
        }

        /// <summary>Góc quanh trục đứng để trục Z cục bộ trỏ theo hướng này.</summary>
        public static float ToYaw(this TdCompass compass) => (int)compass * 90f;

        /// <summary>Hướng ngược lại.</summary>
        public static TdCompass Opposite(this TdCompass compass) => (TdCompass)(((int)compass + 2) % 4);

        /// <summary>Hướng dọc theo cạnh vuông góc với <paramref name="compass"/>, quay trái 90 độ.</summary>
        public static Vector3 ToTangent(this TdCompass compass)
        {
            Vector3 outward = compass.ToDirection();
            return new Vector3(-outward.z, 0f, outward.x);
        }

        /// <summary>Cạnh này chạy theo trục Z (Bắc/Nam) hay trục X.</summary>
        public static bool IsNorthSouth(this TdCompass compass)
        {
            return compass == TdCompass.North || compass == TdCompass.South;
        }

        #endregion
    }
}
