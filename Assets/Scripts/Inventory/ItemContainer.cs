using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemContainer
{
    public InventorySlot[,] slots;
    public int width;
    public int height;

    public event Action OnItemsChanged;

    public ItemContainer(int width, int height)
    {
        this.width = width;
        this.height = height;
        slots = new InventorySlot[width, height];
    }

    public bool AddItem(Item item)
    {
        Vector2Int? firstEmptySlot = null;

        for (int y = height - 1; y >= 0; --y)
        {
            for (int x = 0; x < width; ++x)
            {
                if (slots[x, y] != null && slots[x, y].item.CanStackWith(item))
                {
                    int remain = slots[x, y].TryStack(item);
                    item.quantity = remain;
                    if (remain == 0)
                    {
                        OnItemsChanged?.Invoke();
                        return true;
                    }
                }

                if (slots[x, y] == null && !firstEmptySlot.HasValue)
                {
                    firstEmptySlot = new Vector2Int(x, y);
                }
            }
        }

        if (firstEmptySlot.HasValue)
        {
            Vector2Int pos = firstEmptySlot.Value;
            slots[pos.x, pos.y] = new InventorySlot
            {
                position = pos,
                item = item
            };
            item.quantity = 0;
            OnItemsChanged?.Invoke();
            return true;
        }

        OnItemsChanged?.Invoke();
        return false;
    }

    public Vector2Int Contains(Item item)
    {
        for (int y = height - 1; y >= 0; --y)
        {
            for (int x = 0; x < width; ++x)
            {
                if (slots[x, y] != null &&
                    slots[x, y].item.itemName == item.itemName &&
                    slots[x, y].item.quantity == item.quantity)
                    return new Vector2Int(x, y);
            }
        }
        return Vector2Int.one * -1;
    }

    public bool IsFull(Item item)
    {
        for (int y = height - 1; y >= 0; --y)
        {
            for (int x = 0; x < width; ++x)
            {
                if (slots[x, y] == null) return false;
                if (slots[x, y].item.itemName == item.itemName &&
                    slots[x, y].item.quantity < slots[x, y].item.maxStackSize)
                    return false;
            }
        }
        return true;
    }

    public void Swap(Vector2Int posA, Vector2Int posB)
    {
        if (!IsValid(posA) || !IsValid(posB))
        {
            Debug.LogError("Invalid position for swap");
            return;
        }

        var slotA = GetSlot(posA);
        var slotB = GetSlot(posB);

        SetSlot(posA, slotB);
        SetSlot(posB, slotA);

        OnItemsChanged?.Invoke();
    }

    public Item DropItem(Vector2Int pos)
    {
        InventorySlot slot = GetSlot(pos);
        if (slot != null)
        {
            Item itemToDrop = new Item(slot.item);
            SetSlot(pos, null);
            OnItemsChanged?.Invoke();
            return itemToDrop;
        }
        return null;
    }

    public void SplitItem(Vector2Int pos, int splitAmount)
    {
        InventorySlot originalSlot = GetSlot(pos);
        if (originalSlot != null && originalSlot.item.quantity > splitAmount)
        {
            Vector2Int? emptySlotPos = FindEmptySlot();
            if (emptySlotPos.HasValue)
            {
                Item newItem = new Item(originalSlot.item);
                newItem.quantity = splitAmount;
                originalSlot.item.quantity -= splitAmount;

                SetSlot(emptySlotPos.Value, new InventorySlot { position = emptySlotPos.Value, item = newItem });
                OnItemsChanged?.Invoke();
            }
        }
    }

    public InventorySlot GetSlot(Vector2Int pos)
    {
        if (!IsValid(pos))
            return null;
        return slots[pos.x, pos.y];
    }

    public void SetSlot(Vector2Int pos, InventorySlot slot)
    {
        if (!IsValid(pos)) return;
        
        if (slot != null)
        {
            slot.position = pos;
        }
        slots[pos.x, pos.y] = slot;
    }

    public void RemoveItem(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height || slots[x, y] == null)
        {
            return; 
        }
        slots[x, y] = null;
        OnItemsChanged?.Invoke(); // 使用你更正后的小写 'o'
    }

    public Vector2Int? FindEmptySlot()
    {
        for (int y = height - 1; y >= 0; --y)
        {
            for (int x = 0; x < width; ++x)
            {
                if (slots[x, y] == null)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return null;
    }

    public Vector2Int? FindStackableSlot(Item item)
    {
        if (item.maxStackSize <= 1) return null;

        for (int y = height - 1; y >= 0; --y)
        {
            for (int x = 0; x < width; ++x)
            {
                InventorySlot slot = slots[x, y];
                if (slot != null && slot.item != null && slot.item.itemName == item.itemName && slot.item.quantity < item.maxStackSize)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return null;
    }

    public bool CanAddItem(Item item)
    {
        // 1. 检查是否可以堆叠
        Vector2Int? stackableSlot = FindStackableSlot(item);
        if (stackableSlot.HasValue)
        {
            return true;
        }

        // 2. 检查是否有空格子
        Vector2Int? emptySlot = FindEmptySlot();
        if (emptySlot.HasValue)
        {
            return true;
        }

        // 3. 背包已满
        return false;
    }

    /// <summary>
    /// 获取容器中指定名称物品的总数
    /// </summary>
    public int GetTotalItemCount(string itemName)
    {
        int totalCount = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (slots[x, y] != null && slots[x, y].item.itemName == itemName)
                {
                    if (slots[x, y].item.quantity != 0) totalCount += slots[x, y].item.quantity;
                    else totalCount += 1;
                }
            }
        }
        return totalCount;
    }
    
    /// <summary>
    /// 从容器中移除指定数量的物品
    /// </summary>
    public void RemoveItems(string itemName, int amountToRemove)
    {
        int amountRemoved = 0;
        // 从后往前遍历，这样可以先消耗后面的物品
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = width - 1; x >= 0; x--)
            {
                if (amountRemoved >= amountToRemove)
                {
                    OnItemsChanged?.Invoke();
                    return;
                }

                InventorySlot slot = slots[x, y];
                if (slot != null && slot.item.itemName == itemName)
                {
                    int quantityInSlot = slot.item.quantity;
                    int canRemoveFromSlot = Mathf.Min(quantityInSlot, amountToRemove - amountRemoved);

                    slot.item.quantity -= canRemoveFromSlot;
                    amountRemoved += canRemoveFromSlot;

                    if (slot.item.quantity <= 0)
                    {
                        slots[x, y] = null;
                    }
                }
            }
        }

        if (amountRemoved > 0)
        {
            OnItemsChanged?.Invoke();
        }
    }

    public void DecreaseItemQuantity(Vector2Int pos, int amount)
    {
        InventorySlot slot = GetSlot(pos);
        if (slot != null && slot.item != null)
        {
            slot.item.quantity -= amount;
            if (slot.item.quantity <= 0)
            {
                SetSlot(pos, null);
            }
            OnItemsChanged?.Invoke();
        }
    }

    public void Swap(int i, int j)
    {
        // ... (rest of the script)
    }

    public void AddItemAt(Item item, int x, int y)
    {
        slots[x, y] = new InventorySlot { item = item }; // Simplified for now
        OnItemsChanged?.Invoke();
    }

    public void Swap(int x1, int y1, int x2, int y2)
    {
        if (x1 < 0 || x1 >= width || y1 < 0 || y1 >= height ||
            x2 < 0 || x2 >= width || y2 < 0 || y2 >= height)
        {
            Debug.LogError("Index out of range in Swap.");
            return;
        }

        InventorySlot temp = slots[x1, y1];
        slots[x1, y1] = slots[x2, y2];
        slots[x2, y2] = temp;
        OnItemsChanged?.Invoke();
    }

    public bool IsValid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }
} 