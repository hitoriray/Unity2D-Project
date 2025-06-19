using UnityEngine;

[System.Serializable]
public class Item
{
    [Header("基本信息")]
    public string itemName;
    public string description;
    public string specificDescription;
    public Sprite itemSprite;
    public int maxStackSize;
    
    [Header("Tile信息")]
    public Tile tile;
    public TileType tileType;
    public string sourceBiome;
    
    [Header("物品属性")]
    public Tool tool;
    public Weapon weapon;
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
        description = _tool.description;
        specificDescription = _tool.specificDescription;
        itemType = ItemType.Tool;
        toolType = _tool.toolType;
        maxStackSize = 1;
        quantity = 1;
    }

    public Item(Weapon _weapon)
    {
        weapon = _weapon;
        itemName = _weapon.weaponName;
        itemSprite = _weapon.weaponSprite;
        description = _weapon.description;
        specificDescription = _weapon.specificDescription;
        itemType = ItemType.Weapon;
        maxStackSize = 1;  // 武器不可堆叠
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
        weapon = other.weapon;  // 添加武器字段复制
        itemType = other.itemType;
        toolType = other.toolType;
        quantity = other.quantity;
    }
    
    // 检查是否可以与另一个物品堆叠
    public bool CanStackWith(Item other)
    {
        if (other == null) return false;

        // 武器和工具不可堆叠
        if (itemType == ItemType.Weapon || itemType == ItemType.Tool) return false;
        if (other.itemType == ItemType.Weapon || other.itemType == ItemType.Tool) return false;

        // 其他物品按名称判断是否可堆叠
        return itemName == other.itemName && itemType == other.itemType;
    }
}

public enum ItemType
{
    Block = 3,      // 可放置的块
    Wall = 4,       // 可放置的墙
    Tool = 1,       // 工具
    Weapon = 0,     // 武器
    Consumable = 2, // 消耗品
    Material = 5    // 材料
}

public enum ToolType
{
    Axe,        // 斧
    PickAxe,    // 镐
    Hammer      // 锤
}