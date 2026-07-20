using System.Collections.Generic;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Ngữ cảnh 1 trận để thẻ áp hiệu ứng: hiệu ứng lên ENEMY qua <see cref="Combat" />, lên HERO qua
    ///     các callback (heal/buff — hero state nằm ở scene controller), thao tác bài qua <see cref="Deck" />.
    ///     Tách interface để <see cref="CardResolver" /> test được với ngữ cảnh giả (không cần scene).
    /// </summary>
    public interface ICardContext
    {
        ArenaCombat Combat { get; }
        CardDeckRuntime Deck { get; }
        int AimSector { get; } // hướng người chơi đang nhắm (0..sectors-1)

        void HealHero(float amount);
        void AddHeroDamageBuff(float mulAdd, int rounds);
        void AddHeroStatBuff(string stat, float amount, int rounds); // cộng chỉ số (atk/def/maxHp/crit…); rounds<=0 = cả trận
    }

    /// <summary>
    ///     Áp hiệu ứng 1 thẻ bài lên trận. Tái dùng nguyên primitive combat sẵn có (DamageEnemy, ApplyKnockback,
    ///     PlaceTrap) — KHÔNG nhồi logic thẻ vào <see cref="ArenaCombat" />.
    /// </summary>
    public static class CardResolver
    {
        /// <summary>Thực thi hiệu ứng của <paramref name="card" /> trong ngữ cảnh <paramref name="ctx" />.</summary>
        public static void Resolve(CardModel card, ICardContext ctx)
        {
            if (ctx == null) return;

            switch ((CardType)card.type)
            {
                case CardType.Damage:
                    ApplyDamage(card, ctx);
                    break;

                case CardType.Heal:
                    ctx.HealHero(card.power);
                    break;

                case CardType.BuffDamage:
                    ctx.AddHeroDamageBuff(card.power, card.dur > 0 ? card.dur : 1);
                    break;

                case CardType.StatBuff:
                    ctx.AddHeroStatBuff(card.stat, card.power, card.dur);
                    break;

                case CardType.Trap:
                    ApplyTrap(card, ctx);
                    break;

                case CardType.DrawCards:
                    ctx.Deck?.DrawExtra((int)card.power);
                    break;

                case CardType.GainEnergy:
                    ctx.Deck?.GainEnergy((int)card.power);
                    break;
            }
        }

        private static void ApplyDamage(CardModel card, ICardContext ctx)
        {
            var combat = ctx.Combat;
            if (combat == null) return;

            var cells = CardShapes.Cells((CardTargetShape)card.shape, ctx.AimSector, combat.Occupancy,
                card.ring, card.shapeSize);
            var kb = CellSet.Parse(card.knockback);

            // Gom enemy trước (tránh sửa occupancy giữa vòng lặp cell), rồi damage + đẩy lùi.
            var hit = new List<ArenaEnemyUnit>();
            for (int i = 0; i < cells.Count; i++)
            {
                var e = combat.EnemyAt(cells[i]);
                if (e != null && !hit.Contains(e)) hit.Add(e);
            }

            for (int i = 0; i < hit.Count; i++)
            {
                var e = hit[i];
                combat.DamageEnemy(e, card.power);
                if (e.IsAlive && kb.Count > 0)
                    combat.ApplyKnockback(e, kb, 0f, triggerTrapOnLand: true);
            }
        }

        private static void ApplyTrap(CardModel card, ICardContext ctx)
        {
            var combat = ctx.Combat;
            if (combat == null) return;
            int ring = card.ring < 0 ? 1 : card.ring;
            var cell = new GridCell(ring, combat.Occupancy.Wrap(ctx.AimSector));
            var kb = CellSet.Parse(card.knockback);
            combat.PlaceTrap(cell, card.power, kb, card.dur, card.hits, card.power);
        }
    }
}
