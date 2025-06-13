using UnityEngine;

[System.Serializable]
public class Item
{
    [Header("基本信息")]
    public string itemName;
    public string description;
    public Sprite itemSprite;
    public int maxStackSize;
    
    [Header("Tile信息")]
    public Tile tile;
    public TileType tileType;
    public string sourceBiome;
    
    [Header("物品属性")]
    public Tool tool;
    public ItemType itemType;
    public ToolType toolType;
    public int quantity = 1;
    
    public Item()
    {
        quantity = 1;
    }

    public Item(Tile _tile)
    {
        itemName = tile.itemName;
        itemSprite = tile.itemSprite;
        tile = _tile;
        itemType = ItemType.Block;
        maxStackSize = 64;
        quantity = 1;
    }

    public Item(Tool _tool)
    {
        tool = _tool;
        itemName = _tool.toolName;
        itemSprite = _tool.toolSprite;
        itemType = ItemType.Tool;
        toolType = _tool.toolType;
        maxStackSize = 1;
        quantity = 1;
    }
    
    public Item(Tile tile, TileType type, string biomeName, int qty = 1)
    {
        if (tile != null)
        {
            itemName = tile.itemName;
            itemSprite = tile.itemSprite;
            this.tile = tile;
        }

        tileType = type;
        sourceBiome = biomeName;
        quantity = qty;
        itemType = ItemType.Block;
        maxStackSize = 64;
    }
    
    // 复制构造函数
    public Item(Item other)
    {
        itemName = other.itemName;
        description = other.description;
        itemSprite = other.itemSprite;
        maxStackSize = other.maxStackSize;
        tile = other.tile;
        tileType = other.tileType;
        sourceBiome = other.sourceBiome;
        tool = other.tool;
        itemType = other.itemType;
        toolType = other.toolType;
        quantity = other.quantity;
    }
    
    // 检查是否可以与另一个物品堆叠
    public bool CanStackWith(Item other)
    {
        if (other == null) return false;
        return itemName == other.itemName;
    }
}

public enum ItemType
{
    Block,      // 可放置的块
    Wall,       // 可放置的墙
    Tool,       // 工具
    Consumable, // 消耗品
    Material    // 材料
}

public enum ToolType
{
    Axe,        // 斧
    PickAxe,    // 镐
    Hammer      // 锤
}