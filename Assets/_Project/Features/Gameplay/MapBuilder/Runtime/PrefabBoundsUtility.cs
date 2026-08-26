using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.MapBuilder
{
    /// <summary>
    /// Đo kích thước thật của prefab từ mesh của chính nó.
    /// <para>
    /// Nhờ vậy các bộ dựng cảnh xếp/rải prefab mà không phải gõ tay cạnh tile hay độ lệch pivot —
    /// đổi prefab khác kích thước là bố cục tự khớp lại, khỏi chỉnh số.
    /// </para>
    /// </summary>
    public static class PrefabBoundsUtility
    {
        #region Fields

        // Đo mesh không rẻ, mà một lần dựng cảnh hỏi cùng một prefab hàng trăm lần.
        private static readonly Dictionary<int, Bounds> _cache = new Dictionary<int, Bounds>();

        #endregion

        #region Public

        /// <summary>Xoá cache. Gọi trước mỗi lần dựng lại để ăn theo prefab vừa sửa.</summary>
        public static void ClearCache() => _cache.Clear();

        /// <summary>
        /// Hộp bao của prefab, tính trong không gian cha của nó — tức đúng toạ độ mà
        /// bản thể sinh ra sẽ chiếm, đã gồm cả transform gốc của prefab.
        /// </summary>
        /// <returns><c>false</c> nếu prefab rỗng hoặc không có mesh nào.</returns>
        public static bool TryMeasure(GameObject prefab, out Bounds bounds)
        {
            bounds = default;
            if (prefab == null) return false;

            int key = prefab.GetInstanceID();
            if (_cache.TryGetValue(key, out bounds)) return bounds.size != Vector3.zero;

            bool hasAny = false;
            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i].sharedMesh;
                if (mesh == null) continue;
                Accumulate(ref bounds, ref hasAny, mesh.bounds, filters[i].transform.localToWorldMatrix);
            }

            var skinned = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinned.Length; i++)
            {
                Mesh mesh = skinned[i].sharedMesh;
                if (mesh == null) continue;
                Accumulate(ref bounds, ref hasAny, mesh.bounds, skinned[i].transform.localToWorldMatrix);
            }

            if (!hasAny) bounds = new Bounds(Vector3.zero, Vector3.zero);
            _cache[key] = bounds;
            return hasAny;
        }

        /// <summary>Cạnh phủ trên mặt phẳng ngang. Trả về <see cref="Vector2.zero"/> nếu không đo được.</summary>
        public static Vector2 GetFootprint(GameObject prefab)
        {
            if (!TryMeasure(prefab, out Bounds bounds)) return Vector2.zero;
            return new Vector2(bounds.size.x, bounds.size.z);
        }

        /// <summary>
        /// Tâm hộp bao lệch khỏi pivot bao nhiêu. Trừ giá trị này khi đặt vị trí là tâm mesh
        /// rơi đúng chỗ mình muốn, dù prefab có pivot ở góc.
        /// </summary>
        public static Vector3 GetPivotOffset(GameObject prefab)
        {
            return TryMeasure(prefab, out Bounds bounds) ? bounds.center : Vector3.zero;
        }

        /// <summary>Bán kính chiếm chỗ trên mặt ngang — nửa cạnh dài hơn của hộp bao.</summary>
        public static float GetPlanarRadius(GameObject prefab)
        {
            Vector2 size = GetFootprint(prefab);
            return Mathf.Max(size.x, size.y) * 0.5f;
        }

        /// <summary>Prefab nằm dài theo trục Z cục bộ hay trục X. Dùng để xoay mảnh hàng rào cho đúng chiều.</summary>
        public static bool RunsAlongZ(GameObject prefab)
        {
            Vector2 size = GetFootprint(prefab);
            return size.y > size.x;
        }

        #endregion

        #region Private

        private static void Accumulate(ref Bounds total, ref bool hasAny, Bounds local, Matrix4x4 toPrefab)
        {
            Vector3 center = local.center;
            Vector3 extents = local.extents;

            for (int corner = 0; corner < 8; corner++)
            {
                var offset = new Vector3(
                    (corner & 1) == 0 ? -extents.x : extents.x,
                    (corner & 2) == 0 ? -extents.y : extents.y,
                    (corner & 4) == 0 ? -extents.z : extents.z);

                Vector3 point = toPrefab.MultiplyPoint3x4(center + offset);
                if (!hasAny)
                {
                    total = new Bounds(point, Vector3.zero);
                    hasAny = true;
                    continue;
                }

                total.Encapsulate(point);
            }
        }

        #endregion
    }
}
