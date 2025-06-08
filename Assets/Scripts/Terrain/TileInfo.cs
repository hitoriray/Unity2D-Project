using UnityEngine;

[System.Serializable]
public class TileInfo
{
    public Tile sourceTile;      // 原始的Tile对象
    public TileType tileType;    // tile类型
    public string sourceBiome;   // 来源biome名称
    public bool isPlayerPlaced;  // 是否是玩家放置的
    
    public TileInfo(Tile tile, TileType type, string biomeName, bool playerPlaced = false)
    {
        sourceTile = tile;
        tileType = type;
        sourceBiome = biomeName;
        isPlayerPlaced = playerPlaced;
    }
}