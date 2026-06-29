using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Ezg.Core.Adapter;
using Ezg.Core.Utils;
using UnityEngine;
// Đóng các generic của package về RPGStatType của project để thân hàm giữ nguyên cú pháp cũ.
using RPGAttribute = Ezg.Package.RpgStats.RPGAttribute<Ezg.Package.RpgStats.RPGStatType>;
using RPGStatCollection = Ezg.Package.RpgStats.RPGStatCollection<Ezg.Package.RpgStats.RPGStatType>;
using RPGVital = Ezg.Package.RpgStats.RPGVital<Ezg.Package.RpgStats.RPGStatType>;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Package.RpgStats
{
    public static class StatManager
    {
        #region Fields

        private const string CONFIG_RESOURCE_NAME = "RpgStatsConfig";
        private const string DEFAULT_ICON_FOLDER = "Images/StatsIcon/";
        private const string DEFAULT_ICON_PREFIX = "stat_";

        /// <summary>
        ///     Default stats of the character configured by GD in CSV.
        /// </summary>
        public static RPGStatCollection StatsBaseConfig;

        private static RpgStatsConfig _config;

        /// <summary>
        ///     Project config asset (lazy-loaded from Resources, cached). Drives which stats are
        ///     vitals and the stat-icon resource path, so this class holds no per-project hardcode.
        /// </summary>
        private static RpgStatsConfig Config
        {
            get
            {
                if (_config == null) _config = Resources.Load<RpgStatsConfig>(CONFIG_RESOURCE_NAME);
                return _config;
            }
        }

        #endregion

        #region Initialize

        /// <summary>
        ///     Initializes the base player stats from a dictionary of values.
        /// </summary>
        /// <param name="defaultData">Dictionary containing default values for each stat type.</param>
        public static void SetPlayerStatBase(Dictionary<RPGStatType, float> defaultData)
        {
            InitNewStatCollection(ref StatsBaseConfig);

            foreach (var stat in StatsBaseConfig.StatDict) stat.Value.StatBaseValue = defaultData[stat.Key];
        }

        /// <summary>
        ///     Initializes a new stat collection by reference.
        /// </summary>
        /// <param name="result">The stat collection reference to initialize.</param>
        public static void InitNewStatCollection(ref RPGStatCollection result)
        {
            result = new RPGStatCollection();
            foreach (var stat in (RPGStatType[])Enum.GetValues(typeof(RPGStatType)))
                CreateStat(result, stat, 0);
        }

        /// <summary>
        ///     Extension method to initialize a new stat collection.
        /// </summary>
        /// <param name="result">The RPGStatCollection instance to initialize.</param>
        public static void InitNewStatCollection(this RPGStatCollection result)
        {
            foreach (var stat in (RPGStatType[])Enum.GetValues(typeof(RPGStatType)))
                CreateStat(result, stat, 0);
        }

        /// <summary>
        ///     Creates and initializes a new stat collection.
        /// </summary>
        /// <returns>A new RPGStatCollection instance.</returns>
        public static RPGStatCollection InitNewStatCollection()
        {
            var result = new RPGStatCollection();
            foreach (var stat in (RPGStatType[])Enum.GetValues(typeof(RPGStatType)))
                CreateStat(result, stat, 0);

            return result;
        }

        /// <summary>
        ///     Creates and initializes a new stat collection with default data.
        /// </summary>
        /// <param name="defaultData">Dictionary containing default values for each stat type.</param>
        /// <returns>A new RPGStatCollection instance.</returns>
        public static RPGStatCollection InitNewStatCollection(Dictionary<RPGStatType, float> defaultData)
        {
            var result = new RPGStatCollection();
            foreach (var stat in (RPGStatType[])Enum.GetValues(typeof(RPGStatType)))
                CreateStat(result, stat, defaultData[stat]);

            return result;
        }

        /// <summary>
        ///     Creates and initializes a new stat collection with default character stats.
        /// </summary>
        /// <param name="defaultData">Array of default character stats.</param>
        /// <param name="name">Optional modifier name.</param>
        /// <returns>A new RPGStatCollection instance.</returns>
        public static RPGStatCollection InitNewStatCollection(CharacterStats[] defaultData, string name = "")
        {
            var result = new RPGStatCollection();
            InitNewStatCollection(ref result);

            foreach (var stat in defaultData)
            {
                RPGAttribute statInfo;
                if (IsVital(stat.statType))
                {
                    statInfo = result.CreateOrGetStat<RPGVital>(stat.statType);
                    statInfo.StatType = stat.statType;
                    ApplyDefaultValue(statInfo, stat, name);
                    ((RPGVital)statInfo).SetCurrentValueToMax();
                }
                else
                {
                    statInfo = result.CreateOrGetStat<RPGAttribute>(stat.statType);
                    statInfo.StatType = stat.statType;
                    ApplyDefaultValue(statInfo, stat, name);
                }
            }

            return result;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Gets the sprite icon for a specific RPG stat type.
        /// </summary>
        /// <param name="type">The RPGStatType.</param>
        /// <returns>The sprite for the stat icon.</returns>
        public static Sprite GetStatIcon(RPGStatType type)
        {
            var folder = Config != null ? Config.StatIconFolder : DEFAULT_ICON_FOLDER;
            var prefix = Config != null ? Config.StatIconPrefix : DEFAULT_ICON_PREFIX;
            return ResLoader.Load<Sprite>(folder, prefix + (int)type);
        }

        /// <summary>
        ///     Generates an RPGStatModifier from character stats input data.
        /// </summary>
        /// <param name="inputData">The character stats input data.</param>
        /// <param name="name">Optional name for the modifier.</param>
        /// <returns>A new RPGStatModifier instance.</returns>
        public static RPGStatModifier GenerateStatModifier(CharacterStats inputData, string name = null)
        {
            RPGStatModifier mod = StatConfigs<RPGStatType>.ListStatsPercentOnly.Contains(inputData.statType)
                ? new RPGStatModTotalAdd(inputData.value)
                : inputData.valueType == EnumBase.StatModTypes.Number
                    ? new RPGStatModTotalAdd(inputData.value)
                    : new RPGStatModTotalPercent(inputData.value);
            if (!string.IsNullOrEmpty(name))
                mod._name = name;

            return mod;
        }

        /// <summary>
        ///     Generates an RPGStatModifier for a specific stat type and value.
        /// </summary>
        /// <param name="type">The RPGStatType.</param>
        /// <param name="modeType">The type of statistic modifier.</param>
        /// <param name="value">The modifier value.</param>
        /// <param name="name">Optional name for the modifier.</param>
        /// <returns>A new RPGStatModifier instance.</returns>
        public static RPGStatModifier GenerateStatModifier(RPGStatType type, EnumBase.StatModTypes modeType,
            float value, string name = null)
        {
            RPGStatModifier mod = StatConfigs<RPGStatType>.ListStatsPercentOnly.Contains(type)
                ? new RPGStatModTotalAdd(value)
                : modeType == EnumBase.StatModTypes.Number
                    ? new RPGStatModTotalAdd(value)
                    : new RPGStatModTotalPercent(value);
            if (!string.IsNullOrEmpty(name))
                mod._name = name;

            return mod;
        }

        /// <summary>
        ///     Merges stats and modifiers from a destination stat collection into a source collection.
        /// </summary>
        /// <param name="source">The source stat collection.</param>
        /// <param name="dest">The destination stat collection to merge from.</param>
        /// <param name="isUpdate">Flag indicating if modifiers should be updated after merge.</param>
        public static void MergeStats(this RPGStatCollection source, RPGStatCollection dest, bool isUpdate = true)
        {
            if (dest == null) return;

            foreach (var item in dest.StatDict)
            {
                source.GetStat(item.Key).StatBaseValue += dest.GetStat(item.Key).StatBaseValue;
                source.AddStatModifier(item.Key, dest.GetStatModifiers(item.Key), false);
            }

            if (isUpdate) source.UpdateStatModifiers();
        }

        /// <summary>
        ///     Calculates and returns stats using formulas defined in the CSV configuration.
        /// </summary>
        /// <param name="formula">The mathematical formula string.</param>
        /// <param name="valueInFormula">Variables and values mapping for the formula.</param>
        /// <returns>The calculated value as a float.</returns>
        public static float CalculatorStatByFormula(string formula, Dictionary<string, float> valueInFormula)
        {
            if (string.IsNullOrEmpty(formula)) return valueInFormula.ElementAt(0).Value;

            foreach (var value in valueInFormula)
                formula = Regex.Replace(formula, value.Key, value.Value.ToString(CultureInfo.InvariantCulture));

            return Convert.ToSingle(new DataTable().Compute(formula, null));
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Whether a stat type is a vital (has current/max), driven by the config's vital list
        ///     loaded into <see cref="StatConfigs{TKey}.ListVitalStats" /> via <c>RpgStatsConfig.Apply()</c>.
        /// </summary>
        private static bool IsVital(RPGStatType stat)
        {
            return StatConfigs<RPGStatType>.ListVitalStats.Contains(stat);
        }

        /// <summary>
        ///     Creates a stat as a vital or a plain attribute (per config) with a flat base value.
        /// </summary>
        private static void CreateStat(RPGStatCollection result, RPGStatType stat, float baseValue)
        {
            RPGAttribute statInfo;
            if (IsVital(stat))
            {
                statInfo = result.CreateOrGetStat<RPGVital>(stat);
                statInfo.StatType = stat;
                statInfo.StatBaseValue = baseValue;
                ((RPGVital)statInfo).SetCurrentValueToMax();
            }
            else
            {
                statInfo = result.CreateOrGetStat<RPGAttribute>(stat);
                statInfo.StatType = stat;
                statInfo.StatBaseValue = baseValue;
            }
        }

        /// <summary>
        ///     Applies a <see cref="CharacterStats" /> default value either as a base value or as a
        ///     percent/flat modifier, mirroring the original per-stat rules.
        /// </summary>
        private static void ApplyDefaultValue(RPGAttribute statInfo, CharacterStats stat, string name)
        {
            if (StatConfigs<RPGStatType>.ListStatsPercentOnly.Contains(stat.statType))
                statInfo.AddModifier(new RPGStatModTotalAdd(stat.value, true, name));
            else if (stat.valueType == EnumBase.StatModTypes.Number)
                statInfo.StatBaseValue = stat.value;
            else
                statInfo.AddModifier(new RPGStatModTotalPercent(stat.value, true, name));
        }

        #endregion
    }
}