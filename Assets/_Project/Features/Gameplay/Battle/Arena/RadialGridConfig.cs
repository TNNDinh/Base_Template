using System;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Thông số hình học của lưới tròn quanh hero. Đơn vị bán kính/độ dày = world unit.
    ///     Bán kính lưới = <see cref="InnerRadius" /> + <see cref="RingCount" /> * <see cref="RingThickness" />.
    /// </summary>
    [Serializable]
    public class RadialGridConfig
    {
        [Tooltip("Số vòng tính từ trong ra (bán kính = số grid). Mặc định 6.")]
        [Min(1)] public int RingCount = 6;

        [Tooltip("Số ô mỗi vòng. Ô vòng ngoài rộng hơn vòng trong (cùng số ô).")]
        [Min(3)] public int SectorsPerRing = 24;

        [Tooltip("Bán kính lỗ trống ở giữa — chỗ hero đứng.")]
        [Min(0f)] public float InnerRadius = 0.6f;

        [Tooltip("Độ dày (bán kính) của mỗi vòng.")]
        [Min(0.05f)] public float RingThickness = 0.75f;

        [Tooltip("Khe hở giữa 2 ô cùng vòng (độ) — để các ô tách rời nhau.")]
        [Range(0f, 30f)] public float AngularGapDeg = 2f;

        [Tooltip("Khe hở giữa 2 vòng liền nhau (world unit).")]
        [Min(0f)] public float RadialGap = 0.06f;

        [Tooltip("Độ mịn cung cong ở cạnh trong/ngoài của mỗi ô (số đoạn chia).")]
        [Min(1)] public int ArcSegments = 6;

        public float OuterRadius => InnerRadius + RingCount * RingThickness;
    }
}
