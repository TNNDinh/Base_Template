using System.Collections.Generic;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Một đơn vị chiến đấu trên field. Bản thân class RỖNG về cấu hình: chỉ giữ state chiến đấu
    ///     (HP/energy/shield/buff/cooldown) và được khởi tạo hoàn toàn qua <see cref="SetData" />.
    ///     Mọi hành vi đặc thù (đỡ đòn, phản công, né...) nằm trong <see cref="Passives" /> — không hardcode field.
    ///     Logic thuần, KHÔNG phụ thuộc UI.
    /// </summary>
    public class Unit
    {
        #region Constants

        public const float MaxEnergy = 100f;

        #endregion

        #region Config (chỉ set qua SetData)

        public string HeroId { get; private set; }
        public string ModelKey { get; private set; }
        public string DisplayName { get; private set; }
        public ElementType Element { get; private set; }
        public BattleTeam Team { get; private set; }
        public int Slot { get; private set; }

        public string BasicSkillId { get; private set; }
        public string ActiveSkillId { get; private set; }
        public string UltimateId { get; private set; }

        private readonly List<PassiveBase> _passives = new List<PassiveBase>();
        public IReadOnlyList<PassiveBase> Passives => _passives;

        private readonly List<string> _extraSkillIds = new List<string>(); // skill từ trang bị
        public IReadOnlyList<string> ExtraSkillIds => _extraSkillIds;

        /// <summary>Chỉ số gốc đã tính theo level + star (chưa cộng buff/debuff).</summary>
        private readonly Dictionary<StatType, float> _baseStats = new Dictionary<StatType, float>();

        #endregion

        #region State (runtime)

        public float CurrentHp;
        public float Energy;
        public float Shield;

        /// <summary>Mana hiện tại (tài nguyên dùng Active skill). Ultimate dùng <see cref="Energy" /> riêng.</summary>
        public float Mana;
        public float MaxMana;
        public float ManaPerAttack; // hồi mỗi lần hành động
        public float ManaOnHit;     // hồi khi bị tấn công
        private float _startMana;    // mana khởi điểm (nạp lại ở InitVitals)

        /// <summary>Tầm đánh: nhỏ = cận chiến (view lao vào target), lớn = tầm xa (đứng yên).</summary>
        public float AttackRange;

        /// <summary>Pool tốc độ tiêu hao trong 1 round: đầu round = SPD, mỗi đòn trừ ActionCost, còn &gt;0 thì được đánh tiếp.</summary>
        public float SpeedPool;

        public readonly List<ActiveBuff> Buffs = new List<ActiveBuff>();
        private readonly Dictionary<string, int> _cooldowns = new Dictionary<string, int>();

        #endregion

        #region Properties

        public bool IsAlive => CurrentHp > 0f;
        public float MaxHp => GetStat(StatType.Hp);
        public bool IsUltimateReady => Energy >= MaxEnergy;

        #endregion

        #region Setup

        /// <summary>Khởi tạo/tái sử dụng unit từ data. Xoá sạch state cũ trước khi nạp.</summary>
        public void SetData(UnitRuntimeData data)
        {
            HeroId = data.heroId;
            ModelKey = data.modelKey;
            DisplayName = data.displayName;
            Element = data.element;
            Team = data.team;
            Slot = data.slot;

            BasicSkillId = data.basicSkillId;
            ActiveSkillId = data.activeSkillId;
            UltimateId = data.ultimateId;

            MaxMana = data.maxMana;
            ManaPerAttack = data.manaPerAttack;
            ManaOnHit = data.manaOnHit;
            _startMana = data.startMana;
            AttackRange = data.attackRange;

            _baseStats.Clear();
            if (data.baseStats != null)
                foreach (var kv in data.baseStats)
                    _baseStats[kv.Key] = kv.Value;

            _passives.Clear();
            if (data.passives != null)
                for (int i = 0; i < data.passives.Count; i++)
                    if (data.passives[i] != null)
                        _passives.Add(data.passives[i]);

            _extraSkillIds.Clear();
            if (data.extraSkillIds != null)
                for (int i = 0; i < data.extraSkillIds.Count; i++)
                    if (!string.IsNullOrEmpty(data.extraSkillIds[i]))
                        _extraSkillIds.Add(data.extraSkillIds[i]);

            Buffs.Clear();
            _cooldowns.Clear();
            InitVitals();
        }

        /// <summary>Nhân chỉ số gốc (dùng cho config bot theo ải: hpMul/atkMul/defMul). Nhớ InitVitals sau đó.</summary>
        public void MultiplyBaseStat(StatType stat, float mul)
        {
            if (_baseStats.TryGetValue(stat, out var v)) _baseStats[stat] = v * mul;
        }

        /// <summary>
        ///     Tạo data cho "bóng-hero" (clone chiến đấu của unit này): chỉ số ×<paramref name="mul" />,
        ///     cùng skill cơ bản, KHÔNG mang passive (tránh đệ quy bóng). Đặt tại <paramref name="slot" />.
        /// </summary>
        public UnitRuntimeData MakeShadowData(int slot, float mul)
        {
            var stats = new Dictionary<StatType, float>();
            foreach (var kv in _baseStats)
            {
                var scale = kv.Key == StatType.Hp || kv.Key == StatType.Atk || kv.Key == StatType.Def ? mul : 1f;
                stats[kv.Key] = kv.Value * scale;
            }

            return new UnitRuntimeData
            {
                heroId = "shadow_of_" + HeroId,
                modelKey = ModelKey,
                displayName = "Bóng " + DisplayName,
                element = Element,
                team = Team,
                slot = slot,
                basicSkillId = BasicSkillId,
                activeSkillId = null,
                ultimateId = null,
                maxMana = MaxMana, startMana = 0f, manaPerAttack = ManaPerAttack, manaOnHit = ManaOnHit,
                attackRange = AttackRange,
                baseStats = stats,
                passives = new List<PassiveBase>() // bóng không tự sinh bóng
            };
        }

        public void InitVitals()
        {
            CurrentHp = MaxHp;
            Energy = 0f;
            Shield = 0f;
            Mana = _startMana < 0f ? 0f : _startMana > MaxMana ? MaxMana : _startMana; // mana khởi điểm
        }

        #endregion

        #region Stats (base + buff/debuff)

        /// <summary>
        ///     Chỉ số hiệu dụng = base × (1 + Σ percent mod) + Σ flat mod, gộp toàn bộ buff/debuff cùng stat.
        /// </summary>
        public float GetStat(StatType stat)
        {
            var baseV = _baseStats.TryGetValue(stat, out var b) ? b : 0f;
            float flat = 0f;
            float percent = 0f;

            for (int i = 0; i < Buffs.Count; i++)
            {
                var e = Buffs[i].Effect;
                bool isStatMod = e.category == EffectCategory.Buff || e.category == EffectCategory.Debuff;
                if (!isStatMod || e.stat != stat) continue;

                if (e.modType == ModType.Flat) flat += e.value * Buffs[i].Stacks;
                else percent += e.value * Buffs[i].Stacks;
            }

            var result = baseV * (1f + percent) + flat;
            return result < 0f ? 0f : result;
        }

        #endregion

        #region Passive hooks

        /// <summary>Gọi các passive khi bắt đầu lượt của unit.</summary>
        public void RunPassivesTurnStart(BattleContext ctx)
        {
            for (int i = 0; i < _passives.Count; i++) _passives[i].OnTurnStart(ctx, this);
        }

        #endregion

        #region Combat ops

        /// <summary>Nhận sát thương, trừ shield trước. Trả sát thương thực vào máu.</summary>
        public float TakeDamage(float amount)
        {
            if (amount <= 0f) return 0f;

            if (Shield > 0f)
            {
                var absorbed = Shield >= amount ? amount : Shield;
                Shield -= absorbed;
                amount -= absorbed;
            }

            CurrentHp -= amount;
            if (CurrentHp < 0f) CurrentHp = 0f;

            if (IsAlive) GainMana(ManaOnHit); // hồi mana khi bị tấn công
            return amount;
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || !IsAlive) return;
            CurrentHp += amount;
            if (CurrentHp > MaxHp) CurrentHp = MaxHp;
        }

        public void AddShield(float amount)
        {
            if (amount <= 0f) return;
            Shield += amount;
        }

        public void GainEnergy(float amount)
        {
            Energy += amount;
            if (Energy > MaxEnergy) Energy = MaxEnergy;
            if (Energy < 0f) Energy = 0f;
        }

        /// <summary>Cộng/trừ mana, kẹp trong [0, MaxMana].</summary>
        public void GainMana(float amount)
        {
            if (amount == 0f) return;
            Mana += amount;
            if (Mana > MaxMana) Mana = MaxMana;
            if (Mana < 0f) Mana = 0f;
        }

        /// <summary>Trừ mana khi dùng Active skill.</summary>
        public void SpendMana(float amount)
        {
            if (amount <= 0f) return;
            Mana -= amount;
            if (Mana < 0f) Mana = 0f;
        }

        #endregion

        #region Cooldown

        public void SetCooldown(string skillId, int turns)
        {
            if (turns > 0) _cooldowns[skillId] = turns;
        }

        public bool IsOnCooldown(string skillId) => _cooldowns.TryGetValue(skillId, out var t) && t > 0;

        public void TickCooldowns()
        {
            if (_cooldowns.Count == 0) return;
            var keys = new List<string>(_cooldowns.Keys);
            foreach (var k in keys)
            {
                _cooldowns[k] -= 1;
                if (_cooldowns[k] <= 0) _cooldowns.Remove(k);
            }
        }

        #endregion

        #region Buffs

        public void AddBuff(ActiveBuff buff)
        {
            var existing = Buffs.Find(x => x.Effect.id == buff.Effect.id);
            if (existing != null)
            {
                if (existing.Stacks < existing.Effect.maxStack) existing.Stacks++;
                existing.RemainingTurns = buff.RemainingTurns; // refresh duration
                existing.TickAmount = buff.TickAmount;
                return;
            }

            Buffs.Add(buff);
        }

        public void CleanseDebuffs()
        {
            Buffs.RemoveAll(b =>
                (b.Effect.category == EffectCategory.Debuff || b.Effect.category == EffectCategory.Dot) &&
                b.Effect.dispellable);
        }

        /// <summary>Tick mọi buff đầu turn: DoT trừ máu, HoT hồi máu, giảm duration, xoá hết hạn.</summary>
        public void TickBuffsTurnStart()
        {
            for (int i = Buffs.Count - 1; i >= 0; i--)
            {
                var b = Buffs[i];
                if (b.Effect.category == EffectCategory.Dot) TakeDamage(b.TickAmount);
                else if (b.Effect.category == EffectCategory.Hot) Heal(b.TickAmount);

                b.RemainingTurns--;
                if (b.IsExpired) Buffs.RemoveAt(i);
            }
        }

        #endregion
    }
}
