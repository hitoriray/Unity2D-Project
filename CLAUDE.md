# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 2021.3.45f1 project building a 2D sandbox game inspired by Terraria, featuring procedural world generation, block-based terrain, combat, crafting, and exploration systems.

## Development Commands

### Unity Editor
- Open with Unity Hub using Unity 2021.3.45f1
- Play mode: Ctrl+P (Windows) or Cmd+P (Mac)
- Build: File → Build Settings → Build

### Testing
```bash
# Unity Test Framework is included - run tests via:
# Window → General → Test Runner in Unity Editor
```

### Asset Bundle Management
- Asset bundles are managed through `ABManager.cs`
- Bundle browser available via custom Unity package

## Architecture Overview

### Core Systems

1. **Terrain System** (`Assets/Scripts/Terrain/`)
   - `TerrainGeneration.cs` - Procedural world generation with biomes
   - `Tile.cs` & `TileType.cs` - Tile-based world structure
   - `Biome.cs` - Biome definitions (Grassland, Forest, Desert, Snow)

2. **Combat System** (`Assets/Scripts/Combat/`)
   - `IDamageable` interface - Damage handling contract
   - `Weapon.cs` - ScriptableObject-based weapon system
   - `Projectile.cs` - Projectile behavior
   - AI system with Behavior Designer integration for bosses

3. **Inventory System** (`Assets/Scripts/Inventory/`)
   - Grid-based inventory with drag-and-drop
   - Crafting system with recipes
   - Warehouse/storage system
   - Item splitting mechanics

4. **Lighting System** (`Assets/Scripts/Terrain/Manager/`)
   - `LightingManager.cs` - Dynamic 2D lighting facade
   - `AdvancedLightingSystem.cs` - Advanced lighting calculations
   - Support for ambient and local light sources

5. **Time/Ambiance System**
   - `DayNightCycleManager.cs` - Day/night cycles
   - `AmbianceManager.cs` - Dynamic music/visuals based on biome and time

### Design Patterns

- **Object Pool Pattern**: Used for projectiles and UI elements (`ObjectPool.cs`)
- **Singleton Pattern**: Custom implementations in `Assets/Scripts/Singleton/`
- **ScriptableObject Configuration**: Weapons, tiles, biomes, items
- **Facade Pattern**: LightingManager simplifies complex lighting system
- **Observer Pattern**: IDamageable interface for damage events

### Third-Party Integrations

- **Behavior Designer**: AI behavior trees for enemies and bosses
- **XLua**: Lua scripting support for modding
- **SPUM**: 2D character customization system
- **Unity MCP Bridge**: Model Context Protocol integration

## Key Technical Decisions

1. **Tilemap-based World**: Using Unity's Tilemap system for efficient 2D world rendering
2. **Universal Render Pipeline (URP)**: Using URP 12.1.15 for optimized 2D rendering
3. **Physics2D**: All gameplay physics use Unity's 2D physics system
4. **Scriptable Objects**: Extensive use for data-driven design (items, weapons, biomes)

## Common Development Tasks

### Adding New Weapons
1. Create weapon ScriptableObject in `Assets/Config/Items/Weapons/`
2. Configure damage, type, and projectile settings
3. If projectile-based, create/assign projectile prefab

### Adding New Enemies
1. Create enemy prefab with required components (Rigidbody2D, Collider2D)
2. Add `EnemyHealth` component implementing `IDamageable`
3. Configure AI using Behavior Designer or custom controllers

### Modifying Terrain Generation
1. Edit `TerrainGeneration.cs` for generation logic
2. Modify biome definitions in `Assets/Config/Biomes/`
3. Add new tile types via ScriptableObjects

### Working with Lighting
1. Use `LightingManager.Instance.LightBlock()` for adding light
2. Use `LightingManager.Instance.UnlightBlock()` for removing light
3. Configure light sources on projectiles/items via prefab components

## Important Notes

- **Chinese Comments**: This project uses Simplified Chinese (简体中文) for comments and documentation
- **Memory Bank**: Project documentation maintained in `/memory-bank/` directory
- **Cursor Rules**: Additional development guidelines in `.cursorrules`
- **Boss System**: Eye of Cthulhu boss implemented with behavior trees
- **Audio**: Integrated audio mixer with SFX and music channels

## Performance Considerations

- Object pooling implemented for projectiles and UI elements
- Lighting system optimized with `LightingOptimizer.cs`
- Texture atlases used for tile rendering efficiency
- LOD considerations for distant terrain chunks