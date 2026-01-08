# Changelog - Realistic Industrial Power Consumption

All notable changes to this project will be documented in this file.

---

## [1.2.0] - 2026-01-07

### 🔧 Fixed
- **Fixed excessive power consumption in very large buildings**: Eliminated double scaling issue where both lot area AND size multiplier were causing exponential consumption growth.
- **Power consumption now scales predictably**: Consumption now scales primarily by lot area with only a gentle size correction multiplier.

### ⚖️ Balanced
- **Simplified power calculation system**:
  - Formula: **Consumption = Lot Area × 5 kW/tile × Size Multiplier × User Multiplier%**
  - Efficiency-based multipliers by building size (larger = more efficient):
    - Small (≤30 tiles): **1.0x**
    - Medium (31-100 tiles): **0.9x**
    - Large (101-250 tiles): **0.8x**
    - Very Large (>250 tiles): **0.7x**
  - Eliminated all hard caps - consumption scales with efficiency bonuses
  - This eliminates the previous double scaling problem while rewarding larger industrial buildings

### 📝 Technical Changes
- Simplified power consumption formula for better maintainability
- Removed all hard caps/clamps - pure linear scaling
- Updated code comments to reflect new calculation methodology

### 🎯 Gameplay Impact
- Power consumption is now predictable with efficiency scaling
- Larger industrial buildings are more power-efficient per tile
- Example: 100-tile building uses 450 kW, 500-tile building uses 1,750 kW (@ 100%)
- User slider (75%-375%) gives full control over difficulty
- Encourages building larger, more efficient industrial zones

---

## [1.1.1] - 2026-01-03

### 🔧 Fixed
- **Fixed office buildings being incorrectly affected**: Office buildings were mistakenly being treated as industrial buildings. Now correctly filters to only affect industrial zone buildings.

---

## [1.1.0] - 2026-01-02

### 🔧 Fixed
- **Fixed power demand not matching consumption**: Previously, the mod only modified the building's consumption component, but the power grid demand was calculated from the prefab. Now modifies `ConsumptionData` in the prefab for accurate demand calculations.
- **Consumption and demand now match**: Power grid shows correct demand values that match actual building consumption.

### ⚖️ Balanced
- **Reduced power consumption across all categories** for more realistic gameplay:
  - Small buildings (≤30 tiles): Max **0.75 MW** (was 1.5 MW)
  - Medium buildings (31-100 tiles): Max **2 MW** (was 4 MW)
  - Large buildings (101-250 tiles): Max **4 MW** (was 8 MW)
  - Very Large buildings (>250 tiles): Max **7.5 MW** (was 15 MW)
- **Changed default multiplier** from 150% to **100%** for better out-of-box balance
- Updated kW/tile ratios:
  - Small: 25 kW/tile (was 33)
  - Medium: 20 kW/tile (was 27)
  - Large: 16 kW/tile (was 21)
  - Very Large: 15 kW/tile (was 20)

### 📝 Changed
- Updated mod description and documentation to reflect new balance
- Updated English and Spanish localization strings
- Improved logging to show prefab modification statistics

### 🎮 Compatibility
- **Updated for Cities Skylines II version 1.5.3+**
- Minimum game version: 1.5.*

---

## [1.0.0] - 2026-01-01

### 🎉 Initial Release

#### Features
- **Size-based power consumption** for industrial buildings
- Power consumption calculated from physical lot size (width × depth in tiles)
- Four building categories with progressive scaling:
  - Small (≤30 tiles)
  - Medium (31-100 tiles)
  - Large (101-250 tiles)
  - Very Large (>250 tiles)
- **Configurable multiplier** slider (75% to 375%, default 150%)
- **Performance optimized**: Updates every 10 frames (~6 times per second)
- **Detailed statistics logging** every 10 seconds
- **Bilingual support**: English and Spanish
- **No save game modifications**: Safe to add/remove at any time
- Compatible with all industrial building types and zones

#### Technical
- Uses Unity ECS (Entity Component System)
- Modifies `ElectricityConsumer` component
- Efficient caching system to minimize recalculations
- Automatic reset when multiplier changes
