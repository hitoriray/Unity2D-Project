using System.Collections.Generic;
using UnityEngine;

public class WarehouseManager : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private Inventory inventory; // 引用玩家的背包
    [Header("数据")]
    [SerializeField] private WarehouseData warehouseData;

    public Inventory GetInventory()
    {
        return inventory;
    }

    void Start()
    {
        warehouseData = new WarehouseData();
        // GetAllItemsFromInventory();
    }

    public int GetWarehouseSizeForCategory(ItemType itemType)
    {
        int count = 0;
        foreach (var item in warehouseData.warehouseItems)
        {
            if (item.itemType == itemType)
            {
                ++count;
            }
        }
        return count;
    }

    public int GetWarehouseSize()
    {
        return warehouseData.size;
    }

    public int GetWarehouseCapacity()
    {
        return warehouseData.capacity;
    }

    private void GetAllItemsFromInventory()
    {
        int height = inventory.items.height;
        int width = inventory.items.width;
        for (int y = height - 1; y >= 0; --y)
        {
            for (int x = 0; x < width; ++x)
            {
                if (inventory.items.slots[x, y] != null)
                {
                    warehouseData.AddItem(inventory.items.slots[x, y].item);
                }
            }
        }
    }

    /// <summary>
    /// 显示更多详情
    /// </summary>
    public void ShowMoreDetail(Item item)
    {
        Debug.Log("显示更多详情..." + item.itemName);
    }

    /// <summary>
    /// 添加物品到仓库
    /// </summary>
    /// <param name="item">要添加的物品</param>
    /// <returns>是否成功添加</returns>
    public bool AddItemToWarehouse(Item item)
    {
        if (item == null)
        {
            Debug.LogWarning("尝试添加空物品到仓库");
            return false;
        }

        if (warehouseData == null)
        {
            Debug.LogError("仓库数据未初始化！");
            return false;
        }

        // 创建物品副本，避免引用问题
        Item itemCopy = new Item(item);
        bool success = warehouseData.AddItem(itemCopy);
        
        if (success)
        {
            Debug.Log($"物品成功添加到仓库: {item.itemName} x{item.quantity}");
        }
        else
        {
            Debug.LogWarning($"仓库已满，无法添加物品: {item.itemName}");
        }
        
        return success;
    }

    /// <summary>
    /// 从仓库中移除物品
    /// </summary>
    /// <param name="item">要移除的物品</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveItemFromWarehouse(Item item)
    {
        if (item == null)
        {
            Debug.LogWarning("尝试移除空物品");
            return false;
        }

        if (warehouseData == null)
        {
            Debug.LogError("仓库数据未初始化！");
            return false;
        }

        bool success = warehouseData.RemoveItem(item);
        
        if (success)
        {
            Debug.Log($"物品成功从仓库移除: {item.itemName} x{item.quantity}");
        }
        else
        {
            Debug.LogWarning($"从仓库移除物品失败: {item.itemName}");
        }
        
        return success;
    }

    /// <summary>
    /// 获取指定分类下的所有配方
    /// </summary>
    public List<Item> GetAllItemsForCategory(ItemType itemType)
    {
        List<Item> items = new List<Item>();
        foreach (var item in warehouseData.warehouseItems)
        {
            if (item.itemType == itemType)
            {
                items.Add(item);
            }
        }
        return items;
    }
} 