using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [Header("UI组件")]
    public Image itemIcon;
    public TextMeshProUGUI quantityText;
    
    [Header("拖拽设置")]
    public GameObject dragPreviewPrefab; // 拖拽时显示的预览物体
    
    // 私有变量
    private Vector2Int slotPosition;
    private Inventory inventory;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private bool isHotbarSlot;
    
    // 静态变量用于管理拖拽状态
    public static InventorySlotUI draggedSlot;
    public static bool isDragging = false;
    
    void Awake()
    {
        // 获取组件
        if (itemIcon == null)
            itemIcon = transform.GetChild(0).GetComponent<Image>();
        if (quantityText == null)
            quantityText = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        canvas = GetComponentInParent<Canvas>();
    }
    
    public void Initialize(Vector2Int position, Inventory inv, bool isHotbar = false)
    {
        slotPosition = position;
        inventory = inv;
        isHotbarSlot = isHotbar;
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 检查槽位是否有物品
        InventorySlot slot = GetInventorySlot();
        if (slot == null || slot.item == null) return;

        // 设置拖拽状态
        isDragging = true;
        draggedSlot = this;

        // 使用DragManager创建拖拽预览
        if (DragManager.Instance != null)
        {
            DragManager.Instance.StartDrag(slot.item, transform.position);
        }
        else
        {
            Debug.LogError("DragManager.Instance 为 null！请确保场景中有DragManager组件。");
        }

        // 设置原槽位透明度
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // DragManager现在在Update中自动更新位置，这里不需要手动更新
        // 但保留这个方法以满足IDragHandler接口要求
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 使用DragManager结束拖拽
        if (DragManager.Instance != null)
        {
            DragManager.Instance.EndDrag();
        }

        // 检查库存是否还在显示，如果不在显示则取消拖拽
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null && !playerController.IsInventoryShowing)
        {
            // 库存已关闭，取消拖拽，不执行任何操作
        }
        else
        {
            // 库存还在显示，检查是否拖拽到库存界面外
            if (!IsPointerOverInventory(eventData))
            {
                DropItemOutsideInventory();
            }
        }

        // 重置拖拽状态 - 必须在最后执行
        isDragging = false;
        draggedSlot = null;

        // 恢复原槽位状态 - 确保总是恢复
        ResetSlotVisual();
    }

    // 新增方法：重置槽位视觉状态
    public void ResetSlotVisual()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        if (draggedSlot != null && draggedSlot != this)
        {
            // 执行物品交换或移动
            SwapItems(draggedSlot, this);
        }

        // 确保拖拽源槽位状态重置
        if (draggedSlot != null)
        {
            draggedSlot.ResetSlotVisual();
        }
    }
    

    
    private bool IsPointerOverInventory(PointerEventData eventData)
    {
        // 检查鼠标是否在库存界面上
        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        var results = new System.Collections.Generic.List<RaycastResult>();
        raycaster.Raycast(eventData, results);
        
        foreach (var result in results)
        {
            if (result.gameObject.GetComponent<InventorySlotUI>() != null ||
                result.gameObject.transform.IsChildOf(inventory.inventoryUI.transform) ||
                result.gameObject.transform.IsChildOf(inventory.hotbarUI.transform))
            {
                return true;
            }
        }
        return false;
    }
    
    private void DropItemOutsideInventory()
    {
        InventorySlot slot = GetInventorySlot();
        if (slot != null && slot.item != null)
        {
            inventory.DropItem(slotPosition, isHotbarSlot);
        }
        else
        {
            Debug.LogWarning("尝试丢弃空槽位的物品！");
        }
    }
    
    private void SwapItems(InventorySlotUI fromSlot, InventorySlotUI toSlot)
    {
        inventory.SwapItems(fromSlot.slotPosition, fromSlot.isHotbarSlot,
                           toSlot.slotPosition, toSlot.isHotbarSlot);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 右键点击分割物品
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            InventorySlot slot = GetInventorySlot();
            if (slot != null && slot.item != null && slot.item.quantity > 1)
            {
                // 显示分割界面
                if (ItemSplitUI.Instance != null)
                {
                    ItemSplitUI.Instance.ShowSplitUI(slot.item, this, eventData.position, OnSplitConfirmed);
                }
            }
        }
    }

    /// <summary>
    /// 分割确认回调
    /// </summary>
    private void OnSplitConfirmed(int splitAmount)
    {
        InventorySlot slot = GetInventorySlot();
        if (slot != null && slot.item != null && splitAmount > 0 && splitAmount < slot.item.quantity)
        {
            // 调用库存的分割方法
            inventory.SplitItem(slotPosition, isHotbarSlot, splitAmount);
        }
    }

    private InventorySlot GetInventorySlot()
    {
        if (isHotbarSlot)
        {
            return inventory.hotbarSlots[slotPosition.x];
        }
        else
        {
            return inventory.inventorySlots[slotPosition.x, slotPosition.y];
        }
    }
}
