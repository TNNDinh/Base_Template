using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Giao diện chung cho view của 1 unit — để logic (BattleSceneController) không phụ thuộc
    ///     cài đặt cụ thể: placeholder capsule (<see cref="UnitView" />) hay spine 2D (<see cref="SpineUnitView" />).
    /// </summary>
    public interface IUnitView
    {
        /// <summary>Gắn unit logic vào view.</summary>
        void Bind(Unit unit);

        void PlayIdle();
        void PlayAttack();
        void PlayHit();
        void PlayDie();

        /// <summary>Hồi sinh: gỡ trạng thái chết, về Idle.</summary>
        void Revive();

        /// <summary>
        ///     Đánh 1 target ở <paramref name="targetPos" />: ngoài <paramref name="range" /> (cận chiến) thì lao lại gần rồi đánh; trong tầm thì đứng đánh.
        ///     <paramref name="onImpact" /> được gọi tại ĐÚNG lúc chạm đòn (giữa anim vung) — dùng để cho target flinch/tụt máu đúng nhịp.
        /// </summary>
        void AttackTarget(Vector3 targetPos, float range, global::System.Action onImpact);

        /// <summary>Cập nhật thanh máu (tỉ lệ current/max).</summary>
        void SetHp(float current, float max);

        /// <summary>Cập nhật thanh mana (tỉ lệ current/max).</summary>
        void SetMana(float current, float max);

        /// <summary>Điểm neo VFX trúng đòn.</summary>
        Transform HitPoint { get; }
    }
}
