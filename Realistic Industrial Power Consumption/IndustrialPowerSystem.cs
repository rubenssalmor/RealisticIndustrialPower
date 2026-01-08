using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Economy;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Game.Common;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;

namespace Realistic_Industrial_Power_Consumption
{
    public partial class IndustrialPowerSystem : GameSystemBase
    {
        private ILog log;
        private bool m_Initialized = false;
        private EntityQuery m_IndustrialBuildingQuery;
        private EntityQuery m_PrefabQuery;
        private int m_UpdateCounter = 0;
        private Dictionary<Entity, int> m_CalculatedConsumption = new Dictionary<Entity, int>(); // Consumo calculado por prefab
        private Dictionary<Entity, float> m_OriginalPrefabConsumption = new Dictionary<Entity, float>(); // Consumo original del juego
        private HashSet<Entity> m_ModifiedPrefabs = new HashSet<Entity>();
        private int m_LogCounter = 0;
        private float m_LastMultiplier = 0f;
        private bool m_WasEnabled = true; // Track previous enabled state
        private const int LOG_EVERY_N_UPDATES = 60; // Log detailed stats every 60 updates (~10 seconds)

        protected override void OnCreate()
        {
            base.OnCreate();
            log = Mod.log;
            log.Info("IndustrialPowerSystem created");

            m_IndustrialBuildingQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<ElectricityConsumer>(),
                    ComponentType.ReadOnly<IndustrialProperty>(),
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<Building>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Game.Tools.Temp>(),
                    ComponentType.ReadOnly<OfficeProperty>() // Exclude office buildings
                }
            });

            m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<ConsumptionData>(),
                    ComponentType.ReadOnly<BuildingData>()
                }
            });

            log.Info("Industrial building and prefab queries created (excluding office buildings)");
        }

        protected override void OnUpdate()
        {
            if (!m_Initialized && Mod.Settings != null)
            {
                m_Initialized = true;
                m_WasEnabled = Mod.Settings.EnableMod;
                log.Info($"Mod active: {Mod.Settings.EnableMod}, Power consumption: {Mod.Settings.IndustrialPowerConsumption}%");
                log.Info("Update frequency: every 10 frames (~6 times per second)");

                if (Mod.Settings.EnableMod)
                {
                    log.Info("Realistic Industrial Power Consumption mod is now active!");
                    log.Info("Monitoring all industrial buildings with lot size-based consumption");
                }
            }

            if (Mod.Settings == null)
                return;

            // Detect enable/disable state changes
            if (m_WasEnabled != Mod.Settings.EnableMod)
            {
                if (!Mod.Settings.EnableMod)
                {
                    log.Info("========================================");
                    log.Info("MOD DISABLED - Restoring original power consumption values");
                    RestoreOriginalConsumption();
                    log.Info("All prefabs restored to original values");
                    log.Info("========================================");
                }
                else
                {
                    log.Info("========================================");
                    log.Info("MOD ENABLED - Applying realistic power consumption");
                    log.Info($"Power consumption multiplier: {Mod.Settings.IndustrialPowerConsumption}%");
                    log.Info("========================================");
                }
                m_WasEnabled = Mod.Settings.EnableMod;
            }

            if (!Mod.Settings.EnableMod)
                return;

            // Update every 10 frames to override game's reset behavior
            m_UpdateCounter++;
            if (m_UpdateCounter < 10)
                return;

            m_UpdateCounter = 0;

            float multiplier = Mod.Settings.IndustrialPowerConsumption / 100f;

            // If multiplier changed, reset modified prefabs so they get updated
            if (System.Math.Abs(multiplier - m_LastMultiplier) > 0.01f)
            {
                log.Info($"Multiplier changed from {m_LastMultiplier * 100}% to {multiplier * 100}%. Resetting prefab modifications.");
                m_ModifiedPrefabs.Clear();
                m_LastMultiplier = multiplier;
            }

            try
            {
                var entities = m_IndustrialBuildingQuery.ToEntityArray(Allocator.Temp);
                var consumers = m_IndustrialBuildingQuery.ToComponentDataArray<ElectricityConsumer>(Allocator.Temp);
                var prefabRefs = m_IndustrialBuildingQuery.ToComponentDataArray<PrefabRef>(Allocator.Temp);

                int prefabsModified = 0;

                // Statistics for balancing
                int smallCount = 0, mediumCount = 0, largeCount = 0, veryLargeCount = 0;
                long smallTotal = 0, mediumTotal = 0, largeTotal = 0, veryLargeTotal = 0;
                long smallFulfilled = 0, mediumFulfilled = 0, largeFulfilled = 0, veryLargeFulfilled = 0;
                int smallMin = int.MaxValue, smallMax = 0;
                int mediumMin = int.MaxValue, mediumMax = 0;
                int largeMin = int.MaxValue, largeMax = 0;
                int veryLargeMin = int.MaxValue, veryLargeMax = 0;
                int smallAreaMin = int.MaxValue, smallAreaMax = 0;
                int mediumAreaMin = int.MaxValue, mediumAreaMax = 0;
                int largeAreaMin = int.MaxValue, largeAreaMax = 0;
                int veryLargeAreaMin = int.MaxValue, veryLargeAreaMax = 0;

                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var consumer = consumers[i];
                    var prefabRef = prefabRefs[i];

                    // CRITICAL: Modify the PREFAB data to change BOTH consumption AND demand
                    if (!m_ModifiedPrefabs.Contains(prefabRef.m_Prefab))
                    {
                        if (EntityManager.HasComponent<ConsumptionData>(prefabRef.m_Prefab))
                        {
                            // Store original prefab consumption from the game
                            if (!m_OriginalPrefabConsumption.ContainsKey(prefabRef.m_Prefab))
                            {
                                var originalConsumption = EntityManager.GetComponentData<ConsumptionData>(prefabRef.m_Prefab);
                                m_OriginalPrefabConsumption[prefabRef.m_Prefab] = originalConsumption.m_ElectricityConsumption;
                            }

                            // Calculate our custom consumption based on lot size (ONE TIME per prefab)
                            if (!m_CalculatedConsumption.ContainsKey(prefabRef.m_Prefab))
                            {
                                // Get lot size and category
                                string sizeCategory;
                                int lotArea;
                                float sizeMultiplier = CalculateLotSizeMultiplier(prefabRef, out sizeCategory, out lotArea);

                                // SISTEMA SIMPLIFICADO: Consumo = área × 5 kW/tile × sizeMultiplier
                                // El multiplicador del usuario (slider) se aplica después

                                // Factor base: 5 kW por tile de área
                                int baseConsumption = (int)System.Math.Round(lotArea * 5f);

                                // Consumo final = base × sizeMultiplier (actualmente 1.0x)
                                int calculatedConsumption = (int)System.Math.Round(baseConsumption * sizeMultiplier);

                                m_CalculatedConsumption[prefabRef.m_Prefab] = calculatedConsumption;
                            }

                            // Calculate target consumption for this prefab
                            int prefabBaseConsumption = m_CalculatedConsumption[prefabRef.m_Prefab];
                            int prefabTargetConsumption = (int)System.Math.Round(prefabBaseConsumption * multiplier);

                            // Modify the prefab's ConsumptionData - This changes BOTH demand AND consumption
                            var consumptionData = EntityManager.GetComponentData<ConsumptionData>(prefabRef.m_Prefab);
                            consumptionData.m_ElectricityConsumption = prefabTargetConsumption;
                            EntityManager.SetComponentData(prefabRef.m_Prefab, consumptionData);

                            m_ModifiedPrefabs.Add(prefabRef.m_Prefab);
                            prefabsModified++;
                        }
                    }

                    // Collect statistics using the prefab's calculated consumption
                    string cat;
                    int area;
                    CalculateLotSizeMultiplier(prefabRef, out cat, out area);

                    // Get the target consumption for this building (from the prefab calculation)
                    int targetConsumption = 0;
                    if (m_CalculatedConsumption.ContainsKey(prefabRef.m_Prefab))
                    {
                        targetConsumption = (int)System.Math.Round(m_CalculatedConsumption[prefabRef.m_Prefab] * multiplier);
                    }

                    switch (cat)
                    {
                        case "Small":
                            smallCount++;
                            smallTotal += targetConsumption;
                            smallFulfilled += consumer.m_FulfilledConsumption;
                            smallMin = System.Math.Min(smallMin, targetConsumption);
                            smallMax = System.Math.Max(smallMax, targetConsumption);
                            smallAreaMin = System.Math.Min(smallAreaMin, area);
                            smallAreaMax = System.Math.Max(smallAreaMax, area);
                            break;
                        case "Medium":
                            mediumCount++;
                            mediumTotal += targetConsumption;
                            mediumFulfilled += consumer.m_FulfilledConsumption;
                            mediumMin = System.Math.Min(mediumMin, targetConsumption);
                            mediumMax = System.Math.Max(mediumMax, targetConsumption);
                            mediumAreaMin = System.Math.Min(mediumAreaMin, area);
                            mediumAreaMax = System.Math.Max(mediumAreaMax, area);
                            break;
                        case "Large":
                            largeCount++;
                            largeTotal += targetConsumption;
                            largeFulfilled += consumer.m_FulfilledConsumption;
                            largeMin = System.Math.Min(largeMin, targetConsumption);
                            largeMax = System.Math.Max(largeMax, targetConsumption);
                            largeAreaMin = System.Math.Min(largeAreaMin, area);
                            largeAreaMax = System.Math.Max(largeAreaMax, area);
                            break;
                        case "Very Large":
                            veryLargeCount++;
                            veryLargeTotal += targetConsumption;
                            veryLargeFulfilled += consumer.m_FulfilledConsumption;
                            veryLargeMin = System.Math.Min(veryLargeMin, targetConsumption);
                            veryLargeMax = System.Math.Max(veryLargeMax, targetConsumption);
                            veryLargeAreaMin = System.Math.Min(veryLargeAreaMin, area);
                            veryLargeAreaMax = System.Math.Max(veryLargeAreaMax, area);
                            break;
                    }
                }

                // Log detailed statistics every ~10 seconds for balancing
                m_LogCounter++;
                if (m_LogCounter >= LOG_EVERY_N_UPDATES)
                {
                    m_LogCounter = 0;
                    long totalWanted = smallTotal + mediumTotal + largeTotal + veryLargeTotal;
                    long totalFulfilled = smallFulfilled + mediumFulfilled + largeFulfilled + veryLargeFulfilled;

                    log.Info("========== POWER CONSUMPTION STATISTICS ==========");
                    log.Info($"Total industrial buildings: {entities.Length}");
                    log.Info($"Modified prefabs this cycle: {prefabsModified} | Total prefabs tracked: {m_ModifiedPrefabs.Count}");
                    log.Info($"User multiplier: {Mod.Settings.IndustrialPowerConsumption}%");
                    log.Info($"TOTAL: Wanted {totalWanted / 1000} MW | Fulfilled {totalFulfilled / 1000} MW ({(totalFulfilled * 100.0 / System.Math.Max(1, totalWanted)):F1}% satisfaction)");
                    log.Info("");

                    if (smallCount > 0)
                    {
                        log.Info($"SMALL (≤30 tiles): {smallCount} buildings | Lot areas: {smallAreaMin}-{smallAreaMax} tiles");
                        log.Info($"  Wanted: Avg {smallTotal / smallCount} kW | Min {smallMin} kW | Max {smallMax} kW");
                        log.Info($"  Fulfilled: Avg {smallFulfilled / smallCount} kW | Total {smallFulfilled / 1000} MW ({(smallFulfilled * 100.0 / smallTotal):F1}% of wanted)");
                    }

                    if (mediumCount > 0)
                    {
                        log.Info($"MEDIUM (31-100 tiles): {mediumCount} buildings | Lot areas: {mediumAreaMin}-{mediumAreaMax} tiles");
                        log.Info($"  Wanted: Avg {mediumTotal / mediumCount} kW | Min {mediumMin} kW | Max {mediumMax} kW");
                        log.Info($"  Fulfilled: Avg {mediumFulfilled / mediumCount} kW | Total {mediumFulfilled / 1000} MW ({(mediumFulfilled * 100.0 / mediumTotal):F1}% of wanted)");
                    }

                    if (largeCount > 0)
                    {
                        log.Info($"LARGE (101-250 tiles): {largeCount} buildings | Lot areas: {largeAreaMin}-{largeAreaMax} tiles");
                        log.Info($"  Wanted: Avg {largeTotal / largeCount} kW | Min {largeMin} kW | Max {largeMax} kW");
                        log.Info($"  Fulfilled: Avg {largeFulfilled / largeCount} kW | Total {largeFulfilled / 1000} MW ({(largeFulfilled * 100.0 / largeTotal):F1}% of wanted)");
                    }

                    if (veryLargeCount > 0)
                    {
                        log.Info($"VERY LARGE (>250 tiles): {veryLargeCount} buildings | Lot areas: {veryLargeAreaMin}-{veryLargeAreaMax} tiles");
                        log.Info($"  Wanted: Avg {veryLargeTotal / veryLargeCount} kW | Min {veryLargeMin} kW | Max {veryLargeMax} kW");
                        log.Info($"  Fulfilled: Avg {veryLargeFulfilled / veryLargeCount} kW | Total {veryLargeFulfilled / 1000} MW ({(veryLargeFulfilled * 100.0 / veryLargeTotal):F1}% of wanted)");
                    }

                    log.Info("==================================================");
                }

                entities.Dispose();
                consumers.Dispose();
                prefabRefs.Dispose();
            }
            catch (System.Exception ex)
            {
                log.Error($"Critical error in IndustrialPowerSystem.OnUpdate: {ex.Message}");
                log.Error($"Stack trace: {ex.StackTrace}");
            }
        }


        private float CalculateLotSizeMultiplier(PrefabRef prefabRef, out string sizeCategory, out int lotArea)
        {
            try
            {
                // Get building lot size from prefab
                if (EntityManager.HasComponent<BuildingData>(prefabRef.m_Prefab))
                {
                    var buildingData = EntityManager.GetComponentData<BuildingData>(prefabRef.m_Prefab);
                    lotArea = buildingData.m_LotSize.x * buildingData.m_LotSize.y;

                    // Size categories based on lot area (width × depth in tiles)
                    // MULTIPLICADOR UNIFORME: 1.0x para todas las categorías
                    // El consumo escala puramente por área del lote
                    if (lotArea <= 30)
                    {
                        sizeCategory = "Small";
                        return 1.0f;
                    }
                    else if (lotArea <= 100)
                    {
                        sizeCategory = "Medium";
                        return 0.9f;
                    }
                    else if (lotArea <= 250)
                    {
                        sizeCategory = "Large";
                        return 0.8f;
                    }
                    else
                    {
                        sizeCategory = "Very Large";
                        return 0.7f;
                    }
                }
                else
                {
                    sizeCategory = "Unknown (no BuildingData)";
                    lotArea = 0;
                    return 1.0f;
                }
            }
            catch (System.Exception ex)
            {
                log.Error($"Error calculating lot size multiplier: {ex.Message}");
                sizeCategory = "Error";
                lotArea = 0;
                return 1.0f;
            }
        }

        /// <summary>
        /// Restores all modified prefabs to their original power consumption values
        /// Called when the mod is disabled
        /// </summary>
        private void RestoreOriginalConsumption()
        {
            try
            {
                int restoredCount = 0;
                foreach (var kvp in m_OriginalPrefabConsumption)
                {
                    Entity prefabEntity = kvp.Key;
                    float originalConsumption = kvp.Value;

                    if (EntityManager.Exists(prefabEntity) && EntityManager.HasComponent<ConsumptionData>(prefabEntity))
                    {
                        var consumptionData = EntityManager.GetComponentData<ConsumptionData>(prefabEntity);
                        consumptionData.m_ElectricityConsumption = (int)originalConsumption;
                        EntityManager.SetComponentData(prefabEntity, consumptionData);
                        restoredCount++;
                    }
                }

                log.Info($"Restored {restoredCount} prefabs to original consumption values");

                // Clear tracking data
                m_ModifiedPrefabs.Clear();
                m_CalculatedConsumption.Clear();
            }
            catch (System.Exception ex)
            {
                log.Error($"Error restoring original consumption: {ex.Message}");
                log.Error($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
