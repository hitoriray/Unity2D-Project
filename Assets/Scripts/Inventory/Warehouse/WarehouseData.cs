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
                    warehouseItems.Add(item);
                }
                return true;
            }
        }
        if (warehouseItems.Count + 1 > capacity) return false;
        warehouseItems.Add(item);
        return false;
    }
}