using System.Collections;
using UnityEngine;

public class InventorySlot
{
    public Vector2Int position;
    public Item item;

    public int TryStack(Item other)
    {
        if (item == null || !item.CanStackWith(other)) return other.quantity;
        
        int availableSize = item.maxStackSize - item.quantity;
        int amountToAdd = Mathf.Min(availableSize, other.quantity);
        
        item.quantity += amountToAdd;
        return other.quantity - amountToAdd;
    }
}
