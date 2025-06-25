using UnityEngine;
using System.Collections.Generic;

[SerializeField]
public class WarehouseData
{
    [SerializeField] public List<Item> warehouseItems = new();
    [SerializeField] public int size = 0;
    [SerializeField] public int capacity = 1000;

    public bool AddItem(Item item)
    {
        if (item == null) return false;
        
        if (warehouseItems.Count == 0)
        {
            warehouseItems.Add(item);
            return true;
        }
        
        for (int i = 0; i < warehouseItems.Count; ++i)
        {
            if (item.itemName == warehouseItems[i].itemName)
            {
                int remain = Mathf.Max(item.quantity + warehouseItems[i].quantity - warehouseItems[i].maxStackSize, 0);
                if (remain == 0) // 直接添加
                {
                    warehouseItems[i].quantity += item.quantity;
                }
                else
                {
                    warehouseItems[i].quantity = warehouseItems[i].maxStackSize;
                    if (warehouseItems.Count + 1 > capacity) return false;
                    
                    // 创建新物品存储剩余数量
                    Item remainItem = new Item(item);
                    remainItem.quantity = remain;
                    warehouseItems.Add(remainItem);
                }
                return true;
            }
        }
        
        // 没有找到相同物品，作为新物品添加
        if (warehouseItems.Count + 1 > capacity) return false;
        warehouseItems.Add(item);
        return true;
    }

    /// <summary>
    /// 从仓库中移除指定物品
    /// </summary>
    /// <param name="item">要移除的物品</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveItem(Item item)
    {
        if (item == null) return false;

        for (int i = 0; i < warehouseItems.Count; ++i)
        {
            if (warehouseItems[i] != null && warehouseItems[i].itemName == item.itemName)
            {
                // 找到了匹配的物品，直接移除整个物品条目
                warehouseItems.RemoveAt(i);
                Debug.Log($"从仓库移除物品: {item.itemName} x{item.quantity}");
                return true;
            }
        }

        Debug.LogWarning($"仓库中未找到物品: {item.itemName}");
        return false;
    }
}