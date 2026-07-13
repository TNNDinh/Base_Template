using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Nạp toàn bộ config battle từ các file CSV trong <c>Resources/BattleCsv/</c> vào
    ///     <see cref="BattleDatabase" />. Thêm hero/skill/stage = sửa CSV, KHÔNG đụng code.
    /// </summary>
    public static class BattleCsvLoader
    {
        private const string Folder = "BattleCsv/";

        public static void LoadAll()
        {
            LoadHeroes();
            LoadSkills();
            LoadSkillEffects();
            LoadEffects();
            LoadStars();
            LoadStarCosts();
            LoadEvolutions();
            LoadLoadouts();
            LoadPassives();
            LoadHeroStarPassives();
            LoadStages();
            LoadStageBots();
            LoadEquipments();
            LoadEquipStats();
            LoadEquipPassives();
        }

        private static CsvTable Read(string name)
        {
            var ta = Resources.Load<TextAsset>(Folder + name);
            if (ta == null)
            {
                Debug.LogError($"[Battle] Thiếu CSV: Resources/{Folder}{name}.csv");
                return null;
            }

            return new CsvTable(ta.text);
        }

        private static void LoadHeroes()
        {
            var t = Read("Heroes");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterHero(new HeroModel
                {
                    id = r.Str("id"), name = r.Str("name"),
                    element = r.Enum<ElementType>("element"), heroClass = r.Enum<HeroClass>("heroClass"),
                    rarity = r.Int("rarity"),
                    baseHp = r.Int("baseHp"), baseAtk = r.Int("baseAtk"), baseDef = r.Int("baseDef"), baseSpd = r.Int("baseSpd"),
                    critRate = r.Float("critRate"), critDmg = r.Float("critDmg"), damageReduction = r.Float("damageReduction"),
                    growthHp = r.Float("growthHp"), growthAtk = r.Float("growthAtk"), growthDef = r.Float("growthDef"),
                    maxMana = r.Float("maxMana"), startMana = r.Float("startMana"),
                    manaPerAttack = r.Float("manaPerAttack"), manaOnHit = r.Float("manaOnHit"),
                    attackRange = r.Float("attackRange"),
                    basicSkillId = r.Str("basicSkillId"), skill2Id = r.Str("skill2Id"), ultimateId = r.Str("ultimateId"),
                    modelKey = r.Str("modelKey"), baseHeroId = r.Str("baseHeroId"), evolvePath = r.Enum<EvolutionPath>("evolvePath")
                });
            }
        }

        private static void LoadSkills()
        {
            var t = Read("Skills");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterSkill(new SkillModel
                {
                    id = r.Str("id"), name = r.Str("name"), desc = r.Str("desc"),
                    type = r.Enum<SkillType>("type"), targetType = r.Enum<TargetType>("targetType"),
                    energyCost = r.Int("energyCost"), manaCost = r.Int("manaCost"), cooldown = r.Int("cooldown"),
                    hitCount = r.Int("hitCount"), animKey = r.Str("animKey")
                });
            }
        }

        private static void LoadSkillEffects()
        {
            var t = Read("SkillEffects");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterSkillEffect(new SkillEffectModel
                {
                    skillId = r.Str("skillId"), order = r.Int("order"),
                    effectType = r.Enum<EffectType>("effectType"), target = r.Enum<TargetType>("target"),
                    scaleStat = r.Enum<StatType>("scaleStat"), scaleValue = r.Float("scaleValue"),
                    flat = r.Int("flat"), chance = r.Float("chance"),
                    buffId = r.Str("buffId"), duration = r.Int("duration")
                });
            }
        }

        private static void LoadEffects()
        {
            var t = Read("Effects");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterEffect(new EffectModel
                {
                    id = r.Str("id"), category = r.Enum<EffectCategory>("category"),
                    stat = r.Enum<StatType>("stat"), modType = r.Enum<ModType>("modType"), value = r.Float("value"),
                    tickScaleStat = r.Enum<StatType>("tickScaleStat"), tickValue = r.Float("tickValue"),
                    maxStack = r.Int("maxStack"), dispellable = r.Bool("dispellable"), icon = r.Str("icon")
                });
            }
        }

        private static void LoadStars()
        {
            var t = Read("HeroStar");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterStar(new HeroStarModel
                {
                    star = r.Int("star"), statMultiplier = r.Float("statMultiplier"),
                    levelCap = r.Int("levelCap"), unlockSkillId = r.Str("unlockSkillId")
                });
            }
        }

        private static void LoadStarCosts()
        {
            var t = Read("HeroStarCost");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterStarCost(new HeroStarCostModel
                {
                    fromStar = r.Int("fromStar"), costType = r.Enum<CostType>("costType"),
                    dupeCount = r.Int("dupeCount"), materialId = r.Str("materialId"),
                    materialAmount = r.Int("materialAmount"), goldCost = r.Int("goldCost")
                });
            }
        }

        private static void LoadEvolutions()
        {
            var t = Read("HeroEvolution");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterEvolution(new HeroEvolutionModel
                {
                    baseHeroId = r.Str("baseHeroId"), path = r.Enum<EvolutionPath>("path"),
                    targetHeroId = r.Str("targetHeroId"), pathName = r.Str("pathName"), icon = r.Str("icon"),
                    reqStar = r.Int("reqStar"), reqLevel = r.Int("reqLevel"),
                    materialId = r.Str("materialId"), materialAmount = r.Int("materialAmount"), goldCost = r.Int("goldCost")
                });
            }
        }

        private static void LoadLoadouts()
        {
            var t = Read("HeroStarLoadout");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterLoadout(new HeroStarLoadoutModel
                {
                    heroId = r.Str("heroId"), star = r.Int("star"), activeSkillId = r.Str("activeSkillId")
                });
            }
        }

        private static void LoadPassives()
        {
            var t = Read("Passives");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterPassiveConfig(new PassiveConfigModel
                {
                    id = r.Str("id"), type = r.Enum<PassiveType>("type"),
                    value = r.Float("value"), value2 = r.Float("value2"), value3 = r.Float("value3"),
                    flag = r.Bool("flag"), refId = r.Str("refId")
                });
            }
        }

        private static void LoadHeroStarPassives()
        {
            var t = Read("HeroStarPassive");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterHeroStarPassive(new HeroStarPassiveModel
                {
                    heroId = r.Str("heroId"), star = r.Int("star"), passiveId = r.Str("passiveId")
                });
            }
        }

        private static void LoadStages()
        {
            var t = Read("Stages");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterStage(new StageModel
                {
                    id = r.Str("id"), chapter = r.Int("chapter"), index = r.Int("index"), name = r.Str("name"),
                    staminaCost = r.Int("staminaCost"), unlockStageId = r.Str("unlockStageId"),
                    recommendedPower = r.Int("recommendedPower")
                });
            }
        }

        private static void LoadStageBots()
        {
            var t = Read("StageBots");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterStageBot(new StageBotModel
                {
                    stageId = r.Str("stageId"), wave = r.Int("wave"), slot = r.Int("slot"),
                    enemyId = r.Str("enemyId"), level = r.Int("level"), star = r.Int("star"),
                    hpMul = r.Float("hpMul"), atkMul = r.Float("atkMul"), defMul = r.Float("defMul")
                });
            }
        }

        private static void LoadEquipments()
        {
            var t = Read("Equipments");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterEquipment(new EquipmentModel
                {
                    id = r.Str("id"), name = r.Str("name"), slot = r.Enum<EquipSlot>("slot"),
                    rarity = r.Int("rarity"), grantedSkillId = r.Str("grantedSkillId"), icon = r.Str("icon")
                });
            }
        }

        private static void LoadEquipStats()
        {
            var t = Read("EquipStats");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterEquipStat(new EquipStatModel
                {
                    equipId = r.Str("equipId"), stat = r.Enum<StatType>("stat"),
                    modType = r.Enum<ModType>("modType"), value = r.Float("value")
                });
            }
        }

        private static void LoadEquipPassives()
        {
            var t = Read("EquipPassive");
            if (t == null) return;
            foreach (var cells in t.Rows)
            {
                var r = new CsvRow(t, cells);
                BattleDatabase.RegisterEquipPassive(new EquipPassiveModel
                {
                    equipId = r.Str("equipId"), passiveId = r.Str("passiveId")
                });
            }
        }
    }
}
