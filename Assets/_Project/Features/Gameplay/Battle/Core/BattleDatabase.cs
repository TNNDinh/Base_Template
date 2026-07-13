using System.Collections.Generic;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Kho config in-memory cho battle (Hero/Skill/SkillEffect/Effect/Star/Stage/Passive...).
    ///     Data nằm trong CSV (Resources/BattleCsv) và được <see cref="BattleCsvLoader" /> nạp vào đây;
    ///     class này chỉ giữ dictionary + API tra cứu/đăng ký. KHÔNG hardcode data.
    /// </summary>
    public static class BattleDatabase
    {
        #region Fields

        private static readonly Dictionary<string, HeroModel> _heroes = new Dictionary<string, HeroModel>();
        private static readonly Dictionary<string, SkillModel> _skills = new Dictionary<string, SkillModel>();
        private static readonly Dictionary<string, List<SkillEffectModel>> _skillEffects =
            new Dictionary<string, List<SkillEffectModel>>();
        private static readonly Dictionary<string, EffectModel> _effects = new Dictionary<string, EffectModel>();
        private static readonly Dictionary<int, HeroStarModel> _stars = new Dictionary<int, HeroStarModel>();
        private static readonly Dictionary<int, HeroStarCostModel> _starCosts = new Dictionary<int, HeroStarCostModel>();
        private static readonly Dictionary<string, List<HeroEvolutionModel>> _evolutions =
            new Dictionary<string, List<HeroEvolutionModel>>();
        private static readonly Dictionary<string, StageModel> _stages = new Dictionary<string, StageModel>();
        private static readonly Dictionary<string, List<StageBotModel>> _stageBots =
            new Dictionary<string, List<StageBotModel>>();
        private static readonly Dictionary<string, HeroStarLoadoutModel> _loadouts =
            new Dictionary<string, HeroStarLoadoutModel>();
        private static readonly Dictionary<string, PassiveConfigModel> _passiveConfigs =
            new Dictionary<string, PassiveConfigModel>();
        private static readonly Dictionary<string, List<string>> _heroStarPassives =
            new Dictionary<string, List<string>>();
        private static readonly Dictionary<string, EquipmentModel> _equipments =
            new Dictionary<string, EquipmentModel>();
        private static readonly Dictionary<string, List<EquipStatModel>> _equipStats =
            new Dictionary<string, List<EquipStatModel>>();
        private static readonly Dictionary<string, List<string>> _equipPassives =
            new Dictionary<string, List<string>>();

        public static bool IsLoaded { get; private set; }

        #endregion

        #region Public Lookups

        public static HeroModel GetHero(string id) => _heroes.TryGetValue(id, out var v) ? v : default;
        public static SkillModel GetSkill(string id) => _skills.TryGetValue(id, out var v) ? v : default;
        public static EffectModel GetEffect(string id) => _effects.TryGetValue(id, out var v) ? v : default;
        public static HeroStarModel GetStar(int star) => _stars.TryGetValue(star, out var v) ? v : default;

        public static HeroStarCostModel GetStarCost(int fromStar) =>
            _starCosts.TryGetValue(fromStar, out var v) ? v : default;

        /// <summary>Các nhánh tiến hóa của 1 hero gốc (rỗng nếu hero không tiến hóa được).</summary>
        public static IReadOnlyList<HeroEvolutionModel> GetEvolutions(string baseHeroId) =>
            _evolutions.TryGetValue(baseHeroId, out var v) ? v : global::System.Array.Empty<HeroEvolutionModel>();

        /// <summary>1 nhánh tiến hóa cụ thể (targetHeroId rỗng nếu không có).</summary>
        public static HeroEvolutionModel GetEvolution(string baseHeroId, EvolutionPath path)
        {
            if (_evolutions.TryGetValue(baseHeroId, out var list))
                for (int i = 0; i < list.Count; i++)
                    if (list[i].path == path)
                        return list[i];
            return default;
        }

        public static StageModel GetStage(string id) => _stages.TryGetValue(id, out var v) ? v : default;

        public static IEnumerable<StageModel> AllStages() => _stages.Values;

        /// <summary>Bot của 1 ải, đã sort theo wave rồi slot (rỗng nếu không có).</summary>
        public static IReadOnlyList<StageBotModel> GetStageBots(string stageId) =>
            _stageBots.TryGetValue(stageId, out var v) ? v : global::System.Array.Empty<StageBotModel>();

        private static string LoadoutKey(string heroId, int star) => heroId + "#" + star;

        public static bool HasLoadout(string heroId, int star) => _loadouts.ContainsKey(LoadoutKey(heroId, star));

        public static HeroStarLoadoutModel GetLoadout(string heroId, int star) =>
            _loadouts.TryGetValue(LoadoutKey(heroId, star), out var v) ? v : default;

        public static PassiveConfigModel GetPassiveConfig(string id) =>
            _passiveConfigs.TryGetValue(id, out var v) ? v : default;

        /// <summary>
        ///     Danh sách passiveId của hero tại bậc sao. Gộp passive theo sao cụ thể + passive
        ///     khai ở star=0 (áp cho MỌI sao). Nhờ vậy 1 hero cấu hình tuỳ ý số passive.
        /// </summary>
        public static IReadOnlyList<string> GetHeroStarPassives(string heroId, int star)
        {
            var hasStar = _heroStarPassives.TryGetValue(LoadoutKey(heroId, star), out var perStar);
            var hasAny = _heroStarPassives.TryGetValue(LoadoutKey(heroId, 0), out var anyStar);

            if (hasAny && hasStar)
            {
                var merged = new List<string>(anyStar);
                merged.AddRange(perStar);
                return merged;
            }

            if (hasAny) return anyStar;
            if (hasStar) return perStar;
            return global::System.Array.Empty<string>();
        }

        /// <summary>Các effect của 1 skill, đã sort theo order (rỗng nếu không có).</summary>
        public static IReadOnlyList<SkillEffectModel> GetSkillEffects(string skillId) =>
            _skillEffects.TryGetValue(skillId, out var v) ? v : global::System.Array.Empty<SkillEffectModel>();

        public static EquipmentModel GetEquipment(string id) =>
            _equipments.TryGetValue(id, out var v) ? v : default;

        /// <summary>Các dòng cộng chỉ số của 1 món trang bị (rỗng nếu không có).</summary>
        public static IReadOnlyList<EquipStatModel> GetEquipStats(string equipId) =>
            _equipStats.TryGetValue(equipId, out var v) ? v : global::System.Array.Empty<EquipStatModel>();

        /// <summary>Các passive (effect) 1 món trang bị cấp (rỗng nếu không có).</summary>
        public static IReadOnlyList<string> GetEquipPassives(string equipId) =>
            _equipPassives.TryGetValue(equipId, out var v) ? v : global::System.Array.Empty<string>();

        #endregion

        #region Register API (dùng bởi BattleCsvLoader)

        public static void RegisterHero(HeroModel h) => _heroes[h.id] = h;
        public static void RegisterSkill(SkillModel s) => _skills[s.id] = s;
        public static void RegisterEffect(EffectModel e) => _effects[e.id] = e;
        public static void RegisterStar(HeroStarModel s) => _stars[s.star] = s;
        public static void RegisterStarCost(HeroStarCostModel c) => _starCosts[c.fromStar] = c;

        public static void RegisterEvolution(HeroEvolutionModel e)
        {
            if (!_evolutions.TryGetValue(e.baseHeroId, out var list))
            {
                list = new List<HeroEvolutionModel>();
                _evolutions[e.baseHeroId] = list;
            }

            list.Add(e);
        }

        public static void RegisterStage(StageModel s) => _stages[s.id] = s;

        public static void RegisterStageBot(StageBotModel b)
        {
            if (!_stageBots.TryGetValue(b.stageId, out var list))
            {
                list = new List<StageBotModel>();
                _stageBots[b.stageId] = list;
            }

            list.Add(b);
            list.Sort((x, y) => x.wave != y.wave ? x.wave.CompareTo(y.wave) : x.slot.CompareTo(y.slot));
        }

        public static void RegisterLoadout(HeroStarLoadoutModel l) => _loadouts[LoadoutKey(l.heroId, l.star)] = l;

        public static void RegisterPassiveConfig(PassiveConfigModel p) => _passiveConfigs[p.id] = p;

        public static void RegisterHeroStarPassive(HeroStarPassiveModel m)
        {
            var key = LoadoutKey(m.heroId, m.star);
            if (!_heroStarPassives.TryGetValue(key, out var list))
            {
                list = new List<string>();
                _heroStarPassives[key] = list;
            }

            list.Add(m.passiveId);
        }

        public static void RegisterEquipment(EquipmentModel e) => _equipments[e.id] = e;

        public static void RegisterEquipStat(EquipStatModel s)
        {
            if (!_equipStats.TryGetValue(s.equipId, out var list))
            {
                list = new List<EquipStatModel>();
                _equipStats[s.equipId] = list;
            }

            list.Add(s);
        }

        public static void RegisterEquipPassive(EquipPassiveModel m)
        {
            if (!_equipPassives.TryGetValue(m.equipId, out var list))
            {
                list = new List<string>();
                _equipPassives[m.equipId] = list;
            }

            list.Add(m.passiveId);
        }

        public static void RegisterSkillEffect(SkillEffectModel se)
        {
            if (!_skillEffects.TryGetValue(se.skillId, out var list))
            {
                list = new List<SkillEffectModel>();
                _skillEffects[se.skillId] = list;
            }

            list.Add(se);
            list.Sort((a, b) => a.order.CompareTo(b.order));
        }

        public static void Clear()
        {
            _heroes.Clear();
            _skills.Clear();
            _skillEffects.Clear();
            _effects.Clear();
            _stars.Clear();
            _starCosts.Clear();
            _evolutions.Clear();
            _stages.Clear();
            _stageBots.Clear();
            _loadouts.Clear();
            _passiveConfigs.Clear();
            _heroStarPassives.Clear();
            _equipments.Clear();
            _equipStats.Clear();
            _equipPassives.Clear();
            IsLoaded = false;
        }

        #endregion

        #region Load

        /// <summary>Nạp toàn bộ config battle từ CSV (Resources/BattleCsv). Chạy 1 lần (idempotent).</summary>
        public static void LoadSample()
        {
            if (IsLoaded) return;
            BattleCsvLoader.LoadAll();
            IsLoaded = true;
        }

        /// <summary>Nạp lại từ CSV (xoá sạch rồi load) — dùng khi sửa CSV lúc chạy để test.</summary>
        public static void Reload()
        {
            Clear();
            BattleCsvLoader.LoadAll();
            IsLoaded = true;
        }

        #endregion
    }
}
