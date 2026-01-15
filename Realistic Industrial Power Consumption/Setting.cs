using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using System.Collections.Generic;

namespace Realistic_Industrial_Power_Consumption
{
    [FileLocation("Realistic_Industrial_Power_Consumption")]
    [SettingsUIGroupOrder(kMainGroup, kPowerGroup, kSizeMultipliers)]
    [SettingsUIShowGroupName(kMainGroup, kPowerGroup, kSizeMultipliers)]
    public class Setting : ModSetting
    {
        public const string kSection = "RealisticIndustrialPower";
        public const string kMainGroup = "General";
        public const string kPowerGroup = "PowerConsumption";
        public const string kSizeMultipliers = "SizeMultipliers";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        /// <summary>
        /// Master toggle to enable/disable the mod
        /// </summary>
        [SettingsUISection(kSection, kMainGroup)]
        public bool EnableMod { get; set; }

        /// <summary>
        /// Power consumption multiplier for industrial buildings
        /// Range: 75% (reduced) to 375% (extreme)
        /// Default: 100% (balanced gameplay)
        /// </summary>
        [SettingsUISlider(min = 75, max = 375, step = 25, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kSection, kPowerGroup)]
        public int IndustrialPowerConsumption { get; set; }

        // Size-based multipliers (affect base consumption calculation)

        /// <summary>
        /// Multiplier for Small buildings (≤30 tiles)
        /// Default: 100%
        /// </summary>
        [SettingsUISlider(min = 50, max = 150, step = 5, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kSection, kSizeMultipliers)]
        public int SmallMultiplier { get; set; }

        /// <summary>
        /// Multiplier for Medium buildings (31-100 tiles)
        /// Default: 100%
        /// </summary>
        [SettingsUISlider(min = 50, max = 150, step = 5, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kSection, kSizeMultipliers)]
        public int MediumMultiplier { get; set; }

        /// <summary>
        /// Multiplier for Large buildings (101-250 tiles)
        /// Default: 100%
        /// </summary>
        [SettingsUISlider(min = 50, max = 150, step = 5, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kSection, kSizeMultipliers)]
        public int LargeMultiplier { get; set; }

        /// <summary>
        /// Multiplier for Very Large buildings (251-500 tiles)
        /// Default: 80%
        /// </summary>
        [SettingsUISlider(min = 30, max = 100, step = 5, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kSection, kSizeMultipliers)]
        public int VeryLargeMultiplier { get; set; }

        /// <summary>
        /// Multiplier for Huge buildings (501-750 tiles)
        /// Default: 60%
        /// </summary>
        [SettingsUISlider(min = 20, max = 100, step = 5, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kSection, kSizeMultipliers)]
        public int HugeMultiplier { get; set; }

        /// <summary>
        /// Multiplier for Massive buildings (751-1000 tiles)
        /// Default: 40%
        /// </summary>
        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kSection, kSizeMultipliers)]
        public int MassiveMultiplier { get; set; }

        /// <summary>
        /// Multiplier for Gigantic buildings (>1000 tiles)
        /// Default: 22%
        /// </summary>
        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kSection, kSizeMultipliers)]
        public int GiganticMultiplier { get; set; }

        /// <summary>
        /// Reset all settings to their default values
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kSection, kMainGroup)]
        public bool ResetToDefaults
        {
            set
            {
                SetDefaults();
                Mod.log.Info("Settings reset to default values");
            }
        }

        public override void SetDefaults()
        {
            EnableMod = true;
            IndustrialPowerConsumption = 100;

            // Size multipliers (percentage values)
            SmallMultiplier = 100;      // ≤30 tiles: 100%
            MediumMultiplier = 100;     // 31-100 tiles: 100%
            LargeMultiplier = 100;      // 101-250 tiles: 100%
            VeryLargeMultiplier = 80;   // 251-500 tiles: 80%
            HugeMultiplier = 60;        // 501-750 tiles: 60%
            MassiveMultiplier = 40;     // 751-1000 tiles: 40%
            GiganticMultiplier = 22;    // >1000 tiles: 22%
        }
    }

    /// <summary>
    /// English localization for mod settings UI
    /// </summary>
    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Main header
                { m_Setting.GetSettingsLocaleID(), "Realistic Industrial Power Consumption" },

                // Section groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kMainGroup), "General Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kPowerGroup), "Power Consumption" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSizeMultipliers), "Size-Based Multipliers" },

                // Enable/Disable toggle
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableMod)), "Enable Mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableMod)), "Enable or disable realistic power consumption based on building size and production efficiency." },

                // Power consumption slider
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IndustrialPowerConsumption)), "Power Consumption Multiplier" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IndustrialPowerConsumption)), "Adjust global power consumption. Default 100% provides balanced gameplay. Larger buildings consume more power based on their lot size." },

                // Reset button
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetToDefaults)), "Reset to Defaults" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetToDefaults)), "Reset all settings to their default values." },

                // Size multipliers
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.SmallMultiplier)), "Small Buildings (≤30 tiles)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.SmallMultiplier)), "Consumption multiplier for small industrial buildings. Default: 100%" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MediumMultiplier)), "Medium Buildings (31-100 tiles)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.MediumMultiplier)), "Consumption multiplier for medium industrial buildings. Default: 100%" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.LargeMultiplier)), "Large Buildings (101-250 tiles)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.LargeMultiplier)), "Consumption multiplier for large industrial buildings. Default: 100%" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VeryLargeMultiplier)), "Very Large Buildings (251-500 tiles)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VeryLargeMultiplier)), "Consumption multiplier for very large industrial buildings. Default: 80%" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.HugeMultiplier)), "Huge Buildings (501-750 tiles)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.HugeMultiplier)), "Consumption multiplier for huge industrial buildings. Default: 60%" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MassiveMultiplier)), "Massive Buildings (751-1000 tiles)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.MassiveMultiplier)), "Consumption multiplier for massive industrial buildings. Default: 40% (Range: 0%-30%)" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.GiganticMultiplier)), "Gigantic Buildings (>1000 tiles)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.GiganticMultiplier)), "Consumption multiplier for gigantic industrial buildings. Default: 22% (Range: 0%-30%)" },
            };
        }

        public void Unload()
        {
        }
    }

    /// <summary>
    /// Spanish localization for mod settings UI
    /// </summary>
    public class LocaleES : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleES(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Main header
                { m_Setting.GetSettingsLocaleID(), "Consumo Eléctrico Industrial Realista" },

                // Section groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kMainGroup), "Configuración General" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kPowerGroup), "Consumo Eléctrico" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSizeMultipliers), "Multiplicadores por Tamaño" },

                // Enable/Disable toggle
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableMod)), "Activar Mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableMod)), "Activa o desactiva el consumo eléctrico realista basado en tamaño y eficiencia de producción." },

                // Power consumption slider
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IndustrialPowerConsumption)), "Multiplicador de Consumo Eléctrico" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IndustrialPowerConsumption)), "Ajusta el consumo eléctrico global. El valor predeterminado de 100% proporciona juego balanceado. Edificios más grandes consumen más energía según su tamaño." },

                // Reset button
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetToDefaults)), "Restaurar Valores Predeterminados" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetToDefaults)), "Restaura todas las configuraciones a sus valores predeterminados." },

                // Size multipliers
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.SmallMultiplier)), "Edificios Pequeños (≤30 celdas)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.SmallMultiplier)), "Multiplicador de consumo para edificios industriales pequeños. Predeterminado: 100%" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MediumMultiplier)), "Edificios Medianos (31-100 celdas)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.MediumMultiplier)), "Multiplicador de consumo para edificios industriales medianos. Predeterminado: 100%" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.LargeMultiplier)), "Edificios Grandes (101-250 celdas)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.LargeMultiplier)), "Multiplicador de consumo para edificios industriales grandes. Predeterminado: 100%" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VeryLargeMultiplier)), "Edificios Muy Grandes (251-500 celdas)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VeryLargeMultiplier)), "Multiplicador de consumo para edificios industriales muy grandes. Predeterminado: 80%" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.HugeMultiplier)), "Edificios Enormes (501-750 celdas)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.HugeMultiplier)), "Multiplicador de consumo para edificios industriales enormes. Predeterminado: 60%" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MassiveMultiplier)), "Edificios Masivos (751-1000 celdas)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.MassiveMultiplier)), "Multiplicador de consumo para edificios industriales masivos. Predeterminado: 40% (Rango: 0%-30%)" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.GiganticMultiplier)), "Edificios Gigantes (>1000 celdas)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.GiganticMultiplier)), "Multiplicador de consumo para edificios industriales gigantes. Predeterminado: 22% (Rango: 0%-30%)" },
            };
        }

        public void Unload()
        {
        }
    }
}
