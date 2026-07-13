using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Xử lý tiến hóa hero: liệt kê nhánh khả dụng, kiểm tra điều kiện, thực thi tiến hóa.
    ///     Tiến hóa = đổi <see cref="OwnedHero.heroId" /> sang id đích (giữ nguyên level/star);
    ///     skill tự đổi vì skill gắn theo hero id. Mặc định 1 chiều, mở <see cref="ResetEvolution" /> nếu muốn re-spec.
    /// </summary>
    public static class HeroEvolutionService
    {
        /// <summary>Các nhánh tiến hóa của hero (theo id gốc, kể cả khi đã tiến hóa rồi).</summary>
        public static IReadOnlyList<HeroEvolutionModel> GetAvailablePaths(OwnedHero owned)
        {
            return BattleDatabase.GetEvolutions(owned.BaseHeroId);
        }

        public static bool IsEvolved(OwnedHero owned) => owned.evolvePath != EvolutionPath.None;

        /// <summary>Kiểm tra có thể tiến hóa theo nhánh <paramref name="path" /> không.</summary>
        public static bool CanEvolve(OwnedHero owned, EvolutionPath path, out HeroEvolutionModel evo, out string reason)
        {
            reason = null;
            evo = BattleDatabase.GetEvolution(owned.BaseHeroId, path);

            if (string.IsNullOrEmpty(evo.targetHeroId))
            {
                reason = "Không có nhánh tiến hóa này";
                return false;
            }

            if (IsEvolved(owned))
            {
                reason = "Hero đã tiến hóa (reset trước nếu muốn đổi nhánh)";
                return false;
            }

            if (owned.star < evo.reqStar)
            {
                reason = $"Cần đạt {evo.reqStar}★";
                return false;
            }

            if (owned.level < evo.reqLevel)
            {
                reason = $"Cần đạt level {evo.reqLevel}";
                return false;
            }

            // TODO: [Battle] - check đủ material/gold qua Inventory/Currency của base.
            return true;
        }

        /// <summary>Thực thi tiến hóa theo nhánh user chọn. Trả về true nếu thành công.</summary>
        public static bool Evolve(OwnedHero owned, EvolutionPath path)
        {
            if (!CanEvolve(owned, path, out var evo, out var reason))
            {
                Debug.LogWarning($"[Battle] Không thể tiến hóa: {reason}");
                return false;
            }

            // TODO: [Battle] - trừ material/gold thật ở đây (ConsumeCost).
            if (string.IsNullOrEmpty(owned.originHeroId)) owned.originHeroId = owned.heroId;
            owned.heroId = evo.targetHeroId;
            owned.evolvePath = path;
            owned.unlockedSkillIds.Clear(); // skill của dạng mới lấy từ HeroModel + star-unlock của dạng mới

            // TODO: [Battle] - PlayerDataManager.HeroData.Save() + EmitEvent(HeroEvolved) khi wire persistence.
            Debug.Log($"[Battle] {owned.originHeroId} tiến hóa → {owned.heroId} (nhánh {path})");
            return true;
        }

        /// <summary>Reset về dạng gốc (nếu game cho phép re-spec). Không hoàn chi phí ở MVP.</summary>
        public static bool ResetEvolution(OwnedHero owned)
        {
            if (!IsEvolved(owned)) return false;

            owned.heroId = owned.BaseHeroId;
            owned.originHeroId = null;
            owned.evolvePath = EvolutionPath.None;
            owned.unlockedSkillIds.Clear();
            Debug.Log($"[Battle] {owned.heroId} reset về dạng gốc");
            return true;
        }
    }
}
