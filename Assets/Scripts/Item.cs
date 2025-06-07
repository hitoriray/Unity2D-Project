using UnityEngine;

[System.Serializable]
public class Item
{
    [Header("基本信息")]
    public string itemName;
    public string description;
    public Sprite itemSprite;
    public int maxStackSize = 64;
    
    [Header("Tile信息")]
    public Tile sourceTile; // 来源的Tile对象
    public TileType tileType;
    public string sourceBiome; // 来源biome名称
    
    [Header("物品属性")]
    public ItemType itemType;
    public int quantity = 1;
    
    public Item()
    {
        quantity = 1;
    }
    
    public Item(Tile tile, TileType type, string biomeName, int qty = 1)
    {
        if (tile != null)
        {
            itemName = tile.itemName;
            itemSprite = tile.itemSprite;
            sourceTile = tile;
        }
        
        tileType = type;
        sourceBiome = biomeName;
        quantity = qty;
        itemType = ItemType.Tile;
        maxStackSize = 64;
    }
    
    // 复制构造函数
    public Item(Item other)
    {
        itemName = other.itemName;
        description = other.description;
        itemSprite = other.itemSprite;
        maxStackSize = other.maxStackSize;
        sourceTile = other.sourceTile;
        tileType = other.tileType;
        sourceBiome = other.sourceBiome;
        itemType = other.itemType;
        quantity = other.quantity;
    }
    
    // 检查是否可以与另一个物品堆叠
    public bool CanStackWith(Item other)
    {
        if (other == null) return false;
        
        return itemName == other.itemName &&
               tileType == other.tileType &&
               sourceBiome == other.sourceBiome &&
               itemType == other.itemType;
    }
    
    // 尝试堆叠物品，返回剩余数量
    public int TryStack(Item other)
    {
        if (!CanStackWith(other)) return other.quantity;
        
        int availableSpace = maxStackSize - quantity;
        int amountToAdd = Mathf.Min(availableSpace, other.quantity);
        
        quantity += amountToAdd;
        return other.quantity - amountToAdd;
    }
}

public enum ItemType
{
    Tile,       // 可放置的tile
    Tool,       // 工具
    Consumable, // 消耗品
    Material    // 材料
}