using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Dựng mesh cho 1 ô annular-sector trên mặt phẳng XY (Z=0). Màu ô nằm ở vertex color nên
    ///     chỉ cần 1 material dùng chung (vd Sprites/Default) là mỗi ô hiện đúng màu riêng.
    ///     Vertex được đặt tương đối với TÂM Ô (<see cref="RadialCell.LocalCenter" />) để GameObject
    ///     của ô có thể đặt localPosition ngay tại tâm ô.
    /// </summary>
    public static class RadialGridMeshBuilder
    {
        public static Mesh Build(in RadialCell cell, int arcSegments, Color color)
        {
            int seg = Mathf.Max(1, arcSegments);
            Vector3 center = cell.LocalCenter;

            int vCount = (seg + 1) * 2;
            var verts = new Vector3[vCount];
            var colors = new Color[vCount];
            var uvs = new Vector2[vCount];

            for (int i = 0; i <= seg; i++)
            {
                float t = (float)i / seg;
                float a = Mathf.Lerp(cell.StartAngleRad, cell.EndAngleRad, t);
                float cos = Mathf.Cos(a);
                float sin = Mathf.Sin(a);

                Vector3 inner = new Vector3(cos * cell.InnerRadius, sin * cell.InnerRadius, 0f) - center;
                Vector3 outer = new Vector3(cos * cell.OuterRadius, sin * cell.OuterRadius, 0f) - center;

                verts[i * 2] = inner;
                verts[i * 2 + 1] = outer;
                colors[i * 2] = color;
                colors[i * 2 + 1] = color;
                uvs[i * 2] = new Vector2(t, 0f);
                uvs[i * 2 + 1] = new Vector2(t, 1f);
            }

            var tris = new int[seg * 6];
            int ti = 0;
            for (int i = 0; i < seg; i++)
            {
                int innerA = i * 2;
                int outerA = i * 2 + 1;
                int innerB = (i + 1) * 2;
                int outerB = (i + 1) * 2 + 1;

                tris[ti++] = innerA;
                tris[ti++] = outerA;
                tris[ti++] = outerB;
                tris[ti++] = innerA;
                tris[ti++] = outerB;
                tris[ti++] = innerB;
            }

            var mesh = new Mesh { name = "RadialCell" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
