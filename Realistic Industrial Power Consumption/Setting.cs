using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using System.Collections.Generic;

namespace Realistic_Industrial_Power_Consumption
{
    [FileLocation("Realistic_Industrial_Power_Consumption")]
    [SettingsUIGroupOrder(kMainGroup, kPowerGroup)]
    [SettingsUIShowGroupName(kMainGroup, kPowerGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "RealisticIndustrialPower";
        public const string kMainGroup = "General";
        public const string kPowerGroup = "PowerConsumption";

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
        /// Default: 150% (balanced gameplay)
        /// </summary>
        [SettingsUISlider(min = 75, max = 375, step = 25, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kSection, kPowerGroup)]
        public int IndustrialPowerConsumption { get; set; }

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

                // Enable/Disable toggle
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableMod)), "Enable Mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableMod)), "Enable or disable realistic power consumption based on building size and production efficiency." },

                // Power consumption slider
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IndustrialPowerConsumption)), "Power Consumption Multiplier" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IndustrialPowerConsumption)), "Adjust global power consumption. Default 100% provides balanced gameplay. Larger buildings consume more power based on their lot size." },

                // Reset button
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetToDefaults)), "Reset to Defaults" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetToDefaults)), "Reset all settings to their default values (Enabled: true, Multiplier: 100%)." },
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

                // Enable/Disable toggle
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableMod)), "Activar Mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableMod)), "Activa o desactiva el consumo eléctrico realista basado en tamaño y eficiencia de producción." },

                // Power consumption slider
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IndustrialPowerConsumption)), "Multiplicador de Consumo Eléctrico" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IndustrialPowerConsumption)), "Ajusta el consumo eléctrico global. El valor predeterminado de 100% proporciona juego balanceado. Edificios más grandes consumen más energía según su tamaño." },

                // Reset button
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetToDefaults)), "Restaurar Valores Predeterminados" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetToDefaults)), "Restaura todas las configuraciones a sus valores predeterminados (Activado: verdadero, Multiplicador: 100%)." },
            };
        }

        public void Unload()
        {
        }
    }
}
