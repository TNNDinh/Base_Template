using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Component gắn trên GameObject của 1 ô lưới tròn — giữ dữ liệu <see cref="RadialCell" /> và
    ///     tham chiếu renderer để gameplay sau này highlight / đổi màu / gắn occupant.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class RadialCellView : MonoBehaviour
    {
        private MeshRenderer _renderer;
        private MeshFilter _filter;

        public RadialCell Cell { get; private set; }

        public void Setup(RadialCell cell, MeshRenderer meshRenderer, MeshFilter meshFilter)
        {
            Cell = cell;
            _renderer = meshRenderer;
            _filter = meshFilter;
        }

        /// <summary>Đổi màu toàn ô bằng cách ghi lại vertex color (dùng 1 material chung).</summary>
        public void SetColor(Color color)
        {
            if (_filter == null) _filter = GetComponent<MeshFilter>();
            var mesh = _filter != null ? _filter.sharedMesh : null;
            if (mesh == null) return;

            var colors = mesh.colors;
            if (colors == null || colors.Length == 0) return;
            for (int i = 0; i < colors.Length; i++) colors[i] = color;
            mesh.colors = colors;
        }
    }
}
