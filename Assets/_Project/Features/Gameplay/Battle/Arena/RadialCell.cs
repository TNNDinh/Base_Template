using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Một ô trên lưới tròn (polar grid), xác định bởi cặp (ring, sector).
    ///     Về hình học, ô là một "annular sector" — hình chữ nhật bị bo cạnh ngoài (cung tròn),
    ///     nằm giữa 2 bán kính (Inner/Outer) và 2 góc (Start/End). Vòng càng ngoài ô càng to.
    ///     Toạ độ ở đây là LOCAL so với tâm arena (mặt phẳng XY, Z=0) — đổi sang world qua transform.
    /// </summary>
    public struct RadialCell
    {
        public readonly int Ring;   // vòng: 0 = trong cùng, tăng dần ra ngoài
        public readonly int Sector; // ô thứ mấy trong vòng (0..SectorsPerRing-1)
        public readonly int Id;      // id phẳng = Ring * SectorsPerRing + Sector

        public readonly float InnerRadius;
        public readonly float OuterRadius;
        public readonly float StartAngleRad;
        public readonly float EndAngleRad;

        public RadialCell(int ring, int sector, int id,
            float innerRadius, float outerRadius, float startAngleRad, float endAngleRad)
        {
            Ring = ring;
            Sector = sector;
            Id = id;
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
            StartAngleRad = startAngleRad;
            EndAngleRad = endAngleRad;
        }

        public float MidRadius => (InnerRadius + OuterRadius) * 0.5f;
        public float MidAngleRad => (StartAngleRad + EndAngleRad) * 0.5f;

        /// <summary>Tâm ô ở local space (so với tâm arena).</summary>
        public Vector3 LocalCenter
        {
            get
            {
                float a = MidAngleRad;
                float r = MidRadius;
                return new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
            }
        }
    }
}
