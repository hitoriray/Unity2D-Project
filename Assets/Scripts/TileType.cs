using UnityEngine;

public enum TileType
{
    // Basic
    Air = 0,
    Grass = 1,
    Dirt = 2,
    Stone = 3,

    // Ores
    Copper,
    Iron,
    Gold,
    Ruby,
    Emerald,
    Sapphire,
    
    // Plants
    Tree,
    Cactus,
    SmallGrass, // 小的植物都用SmallGrass代指
    Flower,

    // Wall
    Wall
}