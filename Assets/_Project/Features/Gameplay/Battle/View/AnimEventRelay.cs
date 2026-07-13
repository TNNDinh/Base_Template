using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Gắn trên GameObject có <see cref="Animator" /> (model). AnimationEvent trong clip gọi
    ///     <see cref="OnAttackImpact" /> tại đúng frame chạm đòn → chuyển lên <see cref="UnitView" /> cha.
    /// </summary>
    public class AnimEventRelay : MonoBehaviour
    {
        private UnitView _view;

        private void Awake() => _view = GetComponentInParent<UnitView>();

        /// <summary>Gọi từ AnimationEvent "OnAttackImpact" trong clip attack.</summary>
        public void OnAttackImpact()
        {
            if (_view == null) _view = GetComponentInParent<UnitView>();
            if (_view != null) _view.OnAttackImpact();
        }

        /// <summary>Gọi từ AnimationEvent "OnDeathEnd" ở cuối clip Dying → view mờ dần & ẩn.</summary>
        public void OnDeathEnd()
        {
            if (_view == null) _view = GetComponentInParent<UnitView>();
            if (_view != null) _view.OnDeathEnd();
        }
    }
}
