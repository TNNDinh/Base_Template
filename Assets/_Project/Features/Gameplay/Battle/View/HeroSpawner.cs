using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Khởi tạo model 3D cho 1 <see cref="Unit" />: load prefab theo <see cref="Unit.ModelKey" />
    ///     từ <c>Resources/Heroes/</c>, gắn <see cref="UnitView" />, bind logic, đặt vào slot đội hình.
    ///     Thiếu prefab → tạo placeholder capsule (để test khi chưa có model art).
    /// </summary>
    public static class HeroSpawner
    {
        private const string ResourceFolder = "Heroes/";

        /// <summary>Spawn model cho 1 unit, trả về <see cref="IUnitView" /> đã bind.</summary>
        public static IUnitView Spawn(Unit unit, Transform parent = null)
        {
            GameObject go = null;

            if (!string.IsNullOrEmpty(unit.ModelKey))
            {
                var prefab = Resources.Load<GameObject>(ResourceFolder + unit.ModelKey);
                if (prefab != null) go = UnityEngine.Object.Instantiate(prefab);
                else Debug.LogWarning($"[Battle] Không tìm thấy model '{ResourceFolder}{unit.ModelKey}', dùng placeholder.");
            }

            if (go == null) go = CreatePlaceholder(unit);

            go.name = $"Unit_{unit.Team}_{unit.Slot}_{unit.DisplayName}";
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = BattleFormation.SlotPosition(unit.Team, unit.Slot);
            // Game 2D: KHÔNG xoay — hướng nhìn do view flip scaleX (xem SpineUnitView.Bind).
            go.transform.rotation = Quaternion.identity;

            var view = go.GetComponent<IUnitView>();
            if (view == null) view = go.AddComponent<UnitView>();
            view.Bind(unit);
            return view;
        }

        #region Placeholder

        private static GameObject CreatePlaceholder(Unit unit)
        {
            var root = new GameObject();

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);

            var rend = body.GetComponent<Renderer>();
            if (rend != null) rend.material.color = ElementColor(unit.Element);

            return root;
        }

        private static Color ElementColor(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire: return new Color(0.90f, 0.25f, 0.15f);
                case ElementType.Water: return new Color(0.20f, 0.45f, 0.90f);
                case ElementType.Wind: return new Color(0.30f, 0.80f, 0.40f);
                case ElementType.Light: return new Color(0.95f, 0.90f, 0.40f);
                case ElementType.Dark: return new Color(0.55f, 0.30f, 0.70f);
                default: return Color.gray;
            }
        }

        #endregion
    }
}
