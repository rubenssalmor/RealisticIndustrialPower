using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace Realistic_Industrial_Power_Consumption
{
    /// <summary>
    /// Realistic Industrial Power Consumption Mod
    /// Adjusts electrical consumption of industrial buildings based on their physical lot size
    /// </summary>
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger("Realistic_Industrial_Power_Consumption.Mod").SetShowsErrorsInUI(false);
        public static Setting Settings { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info("========================================");
            log.Info("REALISTIC INDUSTRIAL POWER CONSUMPTION");
            log.Info("Version: 1.2.0");
            log.Info("========================================");

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                log.Info($"Mod loaded from: {asset.path}");
            }

            // Initialize settings
            Settings = new Setting(this);
            Settings.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));
            GameManager.instance.localizationManager.AddSource("es-ES", new LocaleES(Settings));
            AssetDatabase.global.LoadSettings("Realistic_Industrial_Power_Consumption", Settings, Settings);
            log.Info($"Settings initialized - Enabled: {Settings.EnableMod}, Multiplier: {Settings.IndustrialPowerConsumption}%");
            log.Info("Localization support: English (en-US), Spanish (es-ES)");

            // Register power consumption system
            updateSystem.UpdateAt<IndustrialPowerSystem>(SystemUpdatePhase.GameSimulation);
            log.Info("Power consumption system registered successfully");
        }

        public void OnDispose()
        {
            log.Info("Disposing Realistic Industrial Power Consumption mod");

            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }

            log.Info("Mod disposed successfully");
        }
    }
}
