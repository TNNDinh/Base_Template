using System;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Log chiến đấu (debug) + điểm móc cho view/VFX. View 3D đăng ký <see cref="OnDamage" /> /
    ///     <see cref="OnMessage" /> để render số sát thương, animation... (giữ logic tách khỏi presentation).
    /// </summary>
    public static class BattleLog
    {
        /// <summary>(caster, target, amount, isCrit)</summary>
        public static event Action<Unit, Unit, float, bool> OnDamage;

        public static event Action<string> OnMessage;

        /// <summary>Bắt đầu 1 round mới (số round). View đăng ký để cập nhật text "Round X".</summary>
        public static event Action<int> OnRound;

        /// <summary>1 unit được thêm vào trận giữa chừng (vd Bóng thay hero chết) — view spawn model.</summary>
        public static event Action<Unit> OnUnitSpawned;

        /// <summary>1 unit rời trận giữa chừng (bóng về / bị gỡ) — view huỷ model.</summary>
        public static event Action<Unit> OnUnitDespawned;

        /// <summary>Bật/tắt Debug.Log (tắt khi chạy production để khỏi spam).</summary>
        public static bool EnableConsole = true;

        /// <summary>Báo bắt đầu round mới.</summary>
        public static void Round(int round)
        {
            OnRound?.Invoke(round);
            if (EnableConsole) Debug.Log($"[Battle] --- Round {round} ---");
        }

        public static void SpawnUnit(Unit u) => OnUnitSpawned?.Invoke(u);
        public static void DespawnUnit(Unit u) => OnUnitDespawned?.Invoke(u);

        public static void Damage(Unit caster, Unit target, float amount, bool isCrit)
        {
            OnDamage?.Invoke(caster, target, amount, isCrit);
            if (EnableConsole)
                Debug.Log(
                    $"[Battle] {caster.DisplayName} → {target.DisplayName}: {amount:0} dmg{(isCrit ? " (CRIT)" : "")} | HP {target.CurrentHp:0}/{target.MaxHp:0}");
        }

        public static void Info(string msg)
        {
            OnMessage?.Invoke(msg);
            if (EnableConsole) Debug.Log($"[Battle] {msg}");
        }
    }
}
