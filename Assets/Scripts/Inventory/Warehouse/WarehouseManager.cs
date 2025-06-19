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
        GetAllItemsFromInventory();

        Debug.Log("==========warehouseData=============");
        for (int i = 0; i < warehouseData.warehouseItems.Count; ++i)
            Debug.Log(warehouseData.warehouseItems[i].itemName);
        Debug.Log("==========warehouseData=============");
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
                    Debug.Log("Added!");
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