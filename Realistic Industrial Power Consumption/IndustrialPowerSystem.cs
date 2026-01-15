using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Economy;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Game.Common;
using Game.Companies;
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
        private const int MAX_CONSUMPTION_KW = 40000; // Límite máximo de consumo por edificio: 40 MW
        private int m_CappedBuildingsCount = 0; // Contador de edificios que alcanzaron el límite
        private int m_DiagnosticLogCounter = 0; // Contador para logs de diagnóstico
        private const int LOG_DIAGNOSTIC_EVERY = 300; // Log diagnóstico cada 300 updates (~50 segundos)

        // Track size multipliers to detect changes
        private int m_LastSmallMultiplier = -1;
        private int m_LastMediumMultiplier = -1;
        private int m_LastLargeMultiplier = -1;
        private int m_LastVeryLargeMultiplier = -1;
        private int m_LastHugeMultiplier = -1;
        private int m_LastMassiveMultiplier = -1;
        private int m_LastGiganticMultiplier = -1;

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

            // Check if any multiplier changed
            bool multipliersChanged = false;

            // Check global multiplier
            if (System.Math.Abs(multiplier - m_LastMultiplier) > 0.01f)
            {
                log.Info($"Global multiplier changed from {m_LastMultiplier * 100}% to {multiplier * 100}%");
                m_LastMultiplier = multiplier;
                multipliersChanged = true;
            }

            // Check size-based multipliers
            if (m_LastSmallMultiplier != Mod.Settings.SmallMultiplier)
            {
                log.Info($"Small multiplier changed from {m_LastSmallMultiplier}% to {Mod.Settings.SmallMultiplier}%");
                m_LastSmallMultiplier = Mod.Settings.SmallMultiplier;
                multipliersChanged = true;
            }

            if (m_LastMediumMultiplier != Mod.Settings.MediumMultiplier)
            {
                log.Info($"Medium multiplier changed from {m_LastMediumMultiplier}% to {Mod.Settings.MediumMultiplier}%");
                m_LastMediumMultiplier = Mod.Settings.MediumMultiplier;
                multipliersChanged = true;
            }

            if (m_LastLargeMultiplier != Mod.Settings.LargeMultiplier)
            {
                log.Info($"Large multiplier changed from {m_LastLargeMultiplier}% to {Mod.Settings.LargeMultiplier}%");
                m_LastLargeMultiplier = Mod.Settings.LargeMultiplier;
                multipliersChanged = true;
            }

            if (m_LastVeryLargeMultiplier != Mod.Settings.VeryLargeMultiplier)
            {
                log.Info($"Very Large multiplier changed from {m_LastVeryLargeMultiplier}% to {Mod.Settings.VeryLargeMultiplier}%");
                m_LastVeryLargeMultiplier = Mod.Settings.VeryLargeMultiplier;
                multipliersChanged = true;
            }

            if (m_LastHugeMultiplier != Mod.Settings.HugeMultiplier)
            {
                log.Info($"Huge multiplier changed from {m_LastHugeMultiplier}% to {Mod.Settings.HugeMultiplier}%");
                m_LastHugeMultiplier = Mod.Settings.HugeMultiplier;
                multipliersChanged = true;
            }

            if (m_LastMassiveMultiplier != Mod.Settings.MassiveMultiplier)
            {
                log.Info($"Massive multiplier changed from {m_LastMassiveMultiplier}% to {Mod.Settings.MassiveMultiplier}%");
                m_LastMassiveMultiplier = Mod.Settings.MassiveMultiplier;
                multipliersChanged = true;
            }

            if (m_LastGiganticMultiplier != Mod.Settings.GiganticMultiplier)
            {
                log.Info($"Gigantic multiplier changed from {m_LastGiganticMultiplier}% to {Mod.Settings.GiganticMultiplier}%");
                m_LastGiganticMultiplier = Mod.Settings.GiganticMultiplier;
                multipliersChanged = true;
            }

            // If any multiplier changed, reset cached calculations
            if (multipliersChanged)
            {
                log.Info("Settings changed. Resetting prefab modifications and cached calculations.");
                m_ModifiedPrefabs.Clear();
                m_CalculatedConsumption.Clear();
            }

            try
            {
                var entities = m_IndustrialBuildingQuery.ToEntityArray(Allocator.Temp);
                var consumers = m_IndustrialBuildingQuery.ToComponentDataArray<ElectricityConsumer>(Allocator.Temp);
                var prefabRefs = m_IndustrialBuildingQuery.ToComponentDataArray<PrefabRef>(Allocator.Temp);

                int prefabsModified = 0;

                // Statistics for balancing
                int smallCount = 0, mediumCount = 0, largeCount = 0, veryLargeCount = 0, hugeCount = 0, massiveCount = 0, giganticCount = 0;
                long smallTotal = 0, mediumTotal = 0, largeTotal = 0, veryLargeTotal = 0, hugeTotal = 0, massiveTotal = 0, giganticTotal = 0;
                long smallFulfilled = 0, mediumFulfilled = 0, largeFulfilled = 0, veryLargeFulfilled = 0, hugeFulfilled = 0, massiveFulfilled = 0, giganticFulfilled = 0;
                int smallMin = int.MaxValue, smallMax = 0;
                int mediumMin = int.MaxValue, mediumMax = 0;
                int largeMin = int.MaxValue, largeMax = 0;
                int veryLargeMin = int.MaxValue, veryLargeMax = 0;
                int hugeMin = int.MaxValue, hugeMax = 0;
                int massiveMin = int.MaxValue, massiveMax = 0;
                int giganticMin = int.MaxValue, giganticMax = 0;
                int smallAreaMin = int.MaxValue, smallAreaMax = 0;
                int mediumAreaMin = int.MaxValue, mediumAreaMax = 0;
                int largeAreaMin = int.MaxValue, largeAreaMax = 0;
                int veryLargeAreaMin = int.MaxValue, veryLargeAreaMax = 0;
                int hugeAreaMin = int.MaxValue, hugeAreaMax = 0;
                int massiveAreaMin = int.MaxValue, massiveAreaMax = 0;
                int giganticAreaMin = int.MaxValue, giganticAreaMax = 0;

                // Reset contador de edificios limitados
                m_CappedBuildingsCount = 0;

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
                                int calculatedConsumptionBeforeCap = (int)System.Math.Round(baseConsumption * sizeMultiplier);

                                // Aplicar límite máximo antes de aplicar el multiplicador del usuario
                                int calculatedConsumption = System.Math.Min(calculatedConsumptionBeforeCap, MAX_CONSUMPTION_KW);

                                // Log si el edificio fue limitado
                                if (calculatedConsumptionBeforeCap > MAX_CONSUMPTION_KW)
                                {
                                    log.Info($"[LIMIT APPLIED] Prefab capped: Area={lotArea} tiles, Category={sizeCategory}, " +
                                             $"Before cap={calculatedConsumptionBeforeCap} kW ({calculatedConsumptionBeforeCap/1000f:F1} MW), " +
                                             $"After cap={calculatedConsumption} kW ({calculatedConsumption/1000f:F1} MW)");
                                }

                                m_CalculatedConsumption[prefabRef.m_Prefab] = calculatedConsumption;
                            }

                            // Calculate target consumption for this prefab
                            int prefabBaseConsumption = m_CalculatedConsumption[prefabRef.m_Prefab];
                            int prefabTargetBeforeMultiplier = prefabBaseConsumption;
                            int prefabTargetAfterMultiplier = (int)System.Math.Round(prefabBaseConsumption * multiplier);

                            // Aplicar límite máximo después del multiplicador del usuario
                            int prefabTargetConsumption = System.Math.Min(prefabTargetAfterMultiplier, MAX_CONSUMPTION_KW);

                            // Log detallado si se aplicó el límite DESPUÉS del multiplicador
                            if (prefabTargetAfterMultiplier > MAX_CONSUMPTION_KW)
                            {
                                log.Info($"[LIMIT AFTER MULTIPLIER] Base={prefabTargetBeforeMultiplier} kW, " +
                                         $"After multiplier ({multiplier*100}%)={prefabTargetAfterMultiplier} kW ({prefabTargetAfterMultiplier/1000f:F1} MW), " +
                                         $"Final={prefabTargetConsumption} kW ({prefabTargetConsumption/1000f:F1} MW)");
                            }

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
                        int beforeCap = (int)System.Math.Round(m_CalculatedConsumption[prefabRef.m_Prefab] * multiplier);
                        targetConsumption = System.Math.Min(beforeCap, MAX_CONSUMPTION_KW);

                        // Contar edificios que alcanzaron el límite
                        if (beforeCap > MAX_CONSUMPTION_KW)
                        {
                            m_CappedBuildingsCount++;
                        }
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
                        case "Huge":
                            hugeCount++;
                            hugeTotal += targetConsumption;
                            hugeFulfilled += consumer.m_FulfilledConsumption;
                            hugeMin = System.Math.Min(hugeMin, targetConsumption);
                            hugeMax = System.Math.Max(hugeMax, targetConsumption);
                            hugeAreaMin = System.Math.Min(hugeAreaMin, area);
                            hugeAreaMax = System.Math.Max(hugeAreaMax, area);
                            break;
                        case "Massive":
                            massiveCount++;
                            massiveTotal += targetConsumption;
                            massiveFulfilled += consumer.m_FulfilledConsumption;
                            massiveMin = System.Math.Min(massiveMin, targetConsumption);
                            massiveMax = System.Math.Max(massiveMax, targetConsumption);
                            massiveAreaMin = System.Math.Min(massiveAreaMin, area);
                            massiveAreaMax = System.Math.Max(massiveAreaMax, area);
                            break;
                        case "Gigantic":
                            giganticCount++;
                            giganticTotal += targetConsumption;
                            giganticFulfilled += consumer.m_FulfilledConsumption;
                            giganticMin = System.Math.Min(giganticMin, targetConsumption);
                            giganticMax = System.Math.Max(giganticMax, targetConsumption);
                            giganticAreaMin = System.Math.Min(giganticAreaMin, area);
                            giganticAreaMax = System.Math.Max(giganticAreaMax, area);
                            break;
                    }
                }

                // Log de diagnóstico con ejemplos de edificios individuales
                m_DiagnosticLogCounter++;
                if (m_DiagnosticLogCounter >= LOG_DIAGNOSTIC_EVERY && entities.Length > 0)
                {
                    m_DiagnosticLogCounter = 0;
                    log.Info("========== DIAGNOSTIC: SAMPLE BUILDINGS ==========");

                    // Mostrar hasta 5 edificios de ejemplo con sus valores reales
                    int samplesToShow = System.Math.Min(5, entities.Length);
                    for (int sample = 0; sample < samplesToShow; sample++)
                    {
                        var sampleEntity = entities[sample];
                        var sampleConsumer = consumers[sample];
                        var samplePrefabRef = prefabRefs[sample];

                        string cat;
                        int area;
                        float mult = CalculateLotSizeMultiplier(samplePrefabRef, out cat, out area);

                        int baseCalc = m_CalculatedConsumption.ContainsKey(samplePrefabRef.m_Prefab)
                            ? m_CalculatedConsumption[samplePrefabRef.m_Prefab] : 0;
                        int target = (int)System.Math.Round(baseCalc * multiplier);
                        int capped = System.Math.Min(target, MAX_CONSUMPTION_KW);

                        // Obtener también el consumo del prefab
                        int prefabConsumption = 0;
                        if (EntityManager.HasComponent<ConsumptionData>(samplePrefabRef.m_Prefab))
                        {
                            var prefabData = EntityManager.GetComponentData<ConsumptionData>(samplePrefabRef.m_Prefab);
                            prefabConsumption = (int)prefabData.m_ElectricityConsumption;
                        }

                        log.Info($"  Sample {sample+1}: Area={area} tiles, Category={cat}");
                        log.Info($"    Our calculation: Base={baseCalc} kW, Target={target} kW, Capped={capped} kW");
                        log.Info($"    Prefab ConsumptionData.m_ElectricityConsumption={prefabConsumption} kW");
                        log.Info($"    Building ElectricityConsumer: Wanted={sampleConsumer.m_WantedConsumption} kW, Fulfilled={sampleConsumer.m_FulfilledConsumption} kW");

                        // Investigar otros componentes que podrían influir en el consumo
                        try
                        {
                            if (EntityManager.HasComponent<Game.Companies.CompanyData>(sampleEntity))
                            {
                                var companyData = EntityManager.GetComponentData<Game.Companies.CompanyData>(sampleEntity);
                                log.Info($"    CompanyData found (Company exists)");
                                // Explorar propiedades de CompanyData
                            }

                            if (EntityManager.HasComponent<Game.Prefabs.IndustrialProcessData>(samplePrefabRef.m_Prefab))
                            {
                                var processData = EntityManager.GetComponentData<Game.Prefabs.IndustrialProcessData>(samplePrefabRef.m_Prefab);
                                log.Info($"    IndustrialProcessData: MaxWorkersPerCell={processData.m_MaxWorkersPerCell}, Output={processData.m_Output.m_Amount}");
                            }

                            // Buscar si hay un buffer de empleados
                            if (EntityManager.HasBuffer<Game.Companies.Employee>(sampleEntity))
                            {
                                var employees = EntityManager.GetBuffer<Game.Companies.Employee>(sampleEntity);
                                log.Info($"    Employees buffer: Count={employees.Length}");
                            }

                            // Buscar recursos
                            if (EntityManager.HasBuffer<Game.Economy.Resources>(sampleEntity))
                            {
                                var resources = EntityManager.GetBuffer<Game.Economy.Resources>(sampleEntity);
                                log.Info($"    Resources buffer: Count={resources.Length}");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            log.Error($"    Error reading extra components: {ex.Message}");
                        }

                        // Verificar si hay discrepancia
                        if (System.Math.Abs(sampleConsumer.m_WantedConsumption - capped) > 100)
                        {
                            log.Warn($"    ⚠️ DISCREPANCY! Expected {capped} kW but building wants {sampleConsumer.m_WantedConsumption} kW");
                            log.Warn($"    Ratio: Building wants {(float)sampleConsumer.m_WantedConsumption / baseCalc:F2}x our base calculation");
                        }
                    }
                    log.Info("==================================================");
                }

                // Log detailed statistics every ~10 seconds for balancing
                m_LogCounter++;
                if (m_LogCounter >= LOG_EVERY_N_UPDATES)
                {
                    m_LogCounter = 0;
                    long totalWanted = smallTotal + mediumTotal + largeTotal + veryLargeTotal + hugeTotal + massiveTotal + giganticTotal;
                    long totalFulfilled = smallFulfilled + mediumFulfilled + largeFulfilled + veryLargeFulfilled + hugeFulfilled + massiveFulfilled + giganticFulfilled;

                    log.Info("========== POWER CONSUMPTION STATISTICS ==========");
                    log.Info($"Total industrial buildings: {entities.Length}");
                    log.Info($"Modified prefabs this cycle: {prefabsModified} | Total prefabs tracked: {m_ModifiedPrefabs.Count}");
                    log.Info($"User multiplier: {Mod.Settings.IndustrialPowerConsumption}%");
                    log.Info($"Buildings capped at {MAX_CONSUMPTION_KW/1000} MW limit: {m_CappedBuildingsCount} ({(m_CappedBuildingsCount * 100.0 / System.Math.Max(1, entities.Length)):F1}%)");
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
                        log.Info($"VERY LARGE (251-500 tiles): {veryLargeCount} buildings | Lot areas: {veryLargeAreaMin}-{veryLargeAreaMax} tiles");
                        log.Info($"  Wanted: Avg {veryLargeTotal / veryLargeCount} kW | Min {veryLargeMin} kW | Max {veryLargeMax} kW");
                        log.Info($"  Fulfilled: Avg {veryLargeFulfilled / veryLargeCount} kW | Total {veryLargeFulfilled / 1000} MW ({(veryLargeFulfilled * 100.0 / veryLargeTotal):F1}% of wanted)");
                    }

                    if (hugeCount > 0)
                    {
                        log.Info($"HUGE (501-750 tiles): {hugeCount} buildings | Lot areas: {hugeAreaMin}-{hugeAreaMax} tiles");
                        log.Info($"  Wanted: Avg {hugeTotal / hugeCount} kW | Min {hugeMin} kW | Max {hugeMax} kW");
                        log.Info($"  Fulfilled: Avg {hugeFulfilled / hugeCount} kW | Total {hugeFulfilled / 1000} MW ({(hugeFulfilled * 100.0 / hugeTotal):F1}% of wanted)");
                    }

                    if (massiveCount > 0)
                    {
                        log.Info($"MASSIVE (751-1000 tiles): {massiveCount} buildings | Lot areas: {massiveAreaMin}-{massiveAreaMax} tiles");
                        log.Info($"  Wanted: Avg {massiveTotal / massiveCount} kW | Min {massiveMin} kW | Max {massiveMax} kW");
                        log.Info($"  Fulfilled: Avg {massiveFulfilled / massiveCount} kW | Total {massiveFulfilled / 1000} MW ({(massiveFulfilled * 100.0 / massiveTotal):F1}% of wanted)");
                    }

                    if (giganticCount > 0)
                    {
                        log.Info($"GIGANTIC (>1000 tiles): {giganticCount} buildings | Lot areas: {giganticAreaMin}-{giganticAreaMax} tiles");
                        log.Info($"  Wanted: Avg {giganticTotal / giganticCount} kW | Min {giganticMin} kW | Max {giganticMax} kW");
                        log.Info($"  Fulfilled: Avg {giganticFulfilled / giganticCount} kW | Total {giganticFulfilled / 1000} MW ({(giganticFulfilled * 100.0 / giganticTotal):F1}% of wanted)");
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
                    // Multiplicadores configurables por el usuario
                    if (lotArea <= 30)
                    {
                        sizeCategory = "Small";
                        return Mod.Settings.SmallMultiplier / 100f;
                    }
                    else if (lotArea <= 100)
                    {
                        sizeCategory = "Medium";
                        return Mod.Settings.MediumMultiplier / 100f;
                    }
                    else if (lotArea <= 250)
                    {
                        sizeCategory = "Large";
                        return Mod.Settings.LargeMultiplier / 100f;
                    }
                    else if (lotArea <= 500)
                    {
                        sizeCategory = "Very Large";
                        return Mod.Settings.VeryLargeMultiplier / 100f;
                    }
                    else if (lotArea <= 750)
                    {
                        sizeCategory = "Huge";
                        return Mod.Settings.HugeMultiplier / 100f;
                    }
                    else if (lotArea <= 1000)
                    {
                        sizeCategory = "Massive";
                        return Mod.Settings.MassiveMultiplier / 100f;
                    }
                    else
                    {
                        sizeCategory = "Gigantic";
                        return Mod.Settings.GiganticMultiplier / 100f;
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
