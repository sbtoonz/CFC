## Craft From Containers Redux

A comprehensive crafting mod that allows you to craft, build, cook, and smelt using materials from nearby containers.

### Features

**Crafting & Building:**
- Craft and build using items from containers within configurable range (default 15m)
- Visual material counter shows "required/total" format (e.g., "10/400 Wood")
- Works with workbenches, forges, stonecutters, and all vanilla crafting stations

**Automatic Station Management:**
- **Smelters, Kilns, Blast Furnaces** - Auto-pull ore and fuel from nearby chests
- **Cooking Stations** - Auto-pull food items and fuel from containers
- **Fireplaces** - Auto-refuel from designated chests
- **Fermenters** - Auto-pull ingredients from containers

**Container Type System:**
- Assign chest types (Smelter, Kiln, Fire, etc.) using Ctrl+E when interacting with containers
- Prevents accidental material mixing - dedicated chests for specific tasks
- Visual hover text shows current container type

**Configuration (Server-Synced):**
- Craft/build range distance (0-100m)
- Auto-fuel/ore range distance (0-100m)  
- Low fuel/ore thresholds for auto-refill
- Search interval for checking containers
- Warded area access control

### Compatible Mods

- Vapok's Adventure Backpacks
- Epic Loot (conditional compilation)
- Item Drawers (conditional compilation)

### Installation

**Requirements:**
- Valheim 0.221.12 (Unity 6)
- BepInEx 5.4.23+

**Client Install:**
1. Copy `CFC.dll` to `BepInEx/plugins/`
2. Copy `HighCapClient.dll` to `BepInEx/plugins/`

**Server Install:**
1. Copy `CFC.dll` to `BepInEx/plugins/`
2. Copy `ValheimHighCap.dll` to `BepInEx/plugins/`

### Configuration

Config file: `BepInEx/config/CFC.cfg`

**Key Settings:**
- `ChestDistance` - Range for craft/build material search (default: 15m)
- `FuelingDistance` - Range for auto-fuel/ore search (default: 15m)
- `LowFuelValue` - When to start auto-refueling fires (default: 1)
- `LowSmelterFuelValue` - When to start auto-fueling smelters (default: 1)
- `LowSmelterOreValue` - When to start auto-adding ore (default: 1)
- `SearchInterval` - How often to check for materials (default: 0.05s)
- `ShouldSearchWardedAreas` - Access warded containers (default: false)

### Usage

**Setting Container Types:**
1. Place a container near your station
2. Hold Left Ctrl + E (interact) on the container
3. Cycle through types: None → Fire → Smelter → Kiln → BlastFurnace → etc.
4. Hover over container to see current type

**Crafting:**
- Open crafting menu at any station
- Materials in nearby containers count automatically
- UI shows "required/available" (e.g., "10/400 Wood")

**Smelting/Cooking:**
- Place materials in designated chests (set correct type)
- Stations auto-pull when fuel/ore drops below threshold
- Finished items auto-deposit to first available chest

| `Version` | `Update Notes`                                                                |
|-----------|------------------------------------------------------------------------------|
| 1.1.6     | - Unity 6 / Valheim 0.221.12 upgrade                                         |
|           | - Fixed smelter depositing to multiple chests bug                            |
|           | - Fixed auto-fuel/auto-ore not working (inverted logic)                      |
|           | - Added "required/total" material display in crafting UI                     |
|           | - Fixed all IL transpilers for new Valheim version                           |
|           | - Updated ConfigSync system for BepInEx 5.4.21+                              |
| 1.0.4     | - Fighting TSIO Changelog.md not working how I thought                       |
| 1.0.3     | - Fix unlimited crafting bug                                                 |
| 1.0.2     | - Fix Issue when trying to make pieces from chests                           |
| 1.0.1     | - Fix discord report of NRE when placing chest                               |
| 1.0.0     | - Initial Release                                                            |
