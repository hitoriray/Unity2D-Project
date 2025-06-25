using UnityEngine;

[CreateAssetMenu(fileName = "newTileClass", menuName = "Tile Class")]
public class Tile : ScriptableObject
{
    public string tileName;
    public string itemName;
    public string description = "Hello";
    public string specificDescription = "World";

    // 图集名
    public string atlasName = "Tiles_";
    // 需要加载的Sprite索引
    public int[] spriteIndices = { 57, 55, 7, 28, 25, 21, 70, 66, 65, 51, 50, 34, 2, 36, 32, 10, 11, 17, 18, 19, 22, 23, 24, 26, 27, 38, 39, 40 };

    public Sprite[] tileSprites;
    public Sprite itemSprite;

    public bool inBackground = false;

    public static bool IsOre(TileType tileType)
    {
        return tileType == TileType.Copper || tileType == TileType.Iron || tileType == TileType.Gold ||
               tileType == TileType.Ruby || tileType == TileType.Emerald || tileType == TileType.Sapphire;
    }
}
