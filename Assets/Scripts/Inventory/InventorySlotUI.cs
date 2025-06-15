using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IDropHandler
{
    public Image icon;
    public TextMeshProUGUI quantityText;
    private CanvasGroup canvasGroup;

    private ItemContainer itemContainer;
    private Vector2Int positionInContainer;

    // Static variables to track drag state across all slots
    public static InventorySlotUI draggedSlot;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void Initialize(ItemContainer container, Vector2Int position)
    {
        itemContainer = container;
        positionInContainer = position;
    }

    public void AssignContainer(ItemContainer container, Vector2Int position)
    {
        itemContainer = container;
        positionInContainer = position;
        // Optionally, force an immediate UI update for this slot after re-assignment
        UpdateSlotDisplay(itemContainer.GetSlot(positionInContainer));
    }

    public void UpdateSlotDisplay(InventorySlot slot)
    {
        if (slot == null || slot.item == null)
        {
            if(icon != null) icon.sprite = null;
            if(icon != null) icon.enabled = false;
            if(quantityText != null) quantityText.text = "";
            return;
        }

        if(icon != null) icon.enabled = true;
        if(icon != null) icon.sprite = slot.item.itemSprite;
        if(quantityText != null) quantityText.text = slot.item.quantity > 1 ? slot.item.quantity.ToString() : "";
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        InventorySlot slot = itemContainer.GetSlot(positionInContainer);
        if (itemContainer == null || slot == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        draggedSlot = this;
        DragManager.Instance.StartDrag(slot.item, transform.position);

        canvasGroup.alpha = 0.5f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // The DragManager handles the icon's position update.
        // This method is required by the interface but can be empty.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Get the slot where the drag started from the event data. This is the most reliable source.
        var startingSlot = eventData.pointerDrag.GetComponent<InventorySlotUI>();
        
        // If the starting slot can't be found, something is wrong. Exit to prevent errors.
        if (startingSlot == null)
        {
            DragManager.Instance.EndDrag();
            draggedSlot = null;
            return;
        }

        // Check if the drop was successfully handled by an OnDrop call.
        // The signal for this is our static draggedSlot being set to null.
        bool dropWasHandled = (draggedSlot == null);

        // If the drop was NOT handled (i.e., we dropped on an invalid location)
        if (!dropWasHandled)
        {
            // This is the "Drop into World" case.
            Inventory playerInventory = FindObjectOfType<Inventory>();
            if (playerInventory != null)
            {
                var originalPosition = startingSlot.positionInContainer;
                Item itemToDrop = startingSlot.itemContainer.GetSlot(originalPosition).item;
                playerInventory.DropItem(itemToDrop, playerInventory.transform.position);
                
                // Use the proper container method to remove the item and trigger UI updates
                startingSlot.itemContainer.RemoveItem(originalPosition.x, originalPosition.y);
            }
            else
            {
                Debug.LogError("Could not find Player's Inventory to drop item!");
            }
        }

        // This must always be called at the very end of the entire drag-drop operation.
        DragManager.Instance.EndDrag();
        
        // This resets the visual state (e.g., alpha) of the original slot.
        startingSlot.ResetSlotVisual();
        
        // And finally, clear the static reference.
        draggedSlot = null; 
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggedSlot != null && draggedSlot != this)
        {
            var fromSlotUI = draggedSlot;
            var toSlotUI = this;

            var fromContainer = fromSlotUI.GetAssignedContainer();
            var toContainer = toSlotUI.GetAssignedContainer();
            var fromPosition = fromSlotUI.GetAssignedPosition();
            var toPosition = toSlotUI.GetAssignedPosition();

            if (fromContainer != null && fromContainer == toContainer)
            {
                fromContainer.Swap(fromPosition, toPosition);
            }
            else
            {
                Debug.Log("Cross-container drop detected but not implemented.");
            }
            
            // Mark the drag as handled by setting draggedSlot to null.
            // This is the "signal" to OnEndDrag that it doesn't need to do anything for the data.
            draggedSlot = null;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemContainer == null) return;
        
        InventorySlot slot = itemContainer.GetSlot(positionInContainer);
        if (slot == null || slot.item == null) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (slot.item.quantity > 1)
            {
                ItemSplitUI.Instance.ShowSplitUI(slot.item, this, Input.mousePosition, (splitAmount) => {
                    // This is the callback action when split is confirmed
                    Item newItem = new Item(slot.item);
                    newItem.quantity = splitAmount;
                    
                    slot.item.quantity -= splitAmount;
                    
                    // Manually start dragging the new split stack
                    DragManager.Instance.StartDrag(newItem, transform.position);
                    
                    // We need a way to handle this dragged item. For now, we just log it.
                    // A proper implementation would require a temporary "hand" slot.
                    Debug.Log($"Started dragging a new stack of {newItem.itemName} with quantity {newItem.quantity}");

                    // Manually update the source slot display
                    UpdateSlotDisplay(slot);
                });
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Future implementation: Show item tooltip
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Future implementation: Hide item tooltip
    }
    
    public void ResetSlotVisual()
    {
        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = true;
    }

    public ItemContainer GetAssignedContainer()
    {
        return itemContainer;
    }

    public Vector2Int GetAssignedPosition()
    {
        return positionInContainer;
    }
} 