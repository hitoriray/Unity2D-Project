using UnityEngine;

/// <summary>
/// 库存拖拽系统调试工具
/// </summary>
public class InventoryDebugger : MonoBehaviour
{
    [Header("调试设置")]
    public bool enableDebugLogs = true;
    public bool showSlotStates = false;
    
    private Inventory inventory;
    
    void Start()
    {
        inventory = GetComponent<Inventory>();
        if (inventory == null)
        {
            inventory = FindObjectOfType<Inventory>();
        }
        
        if (inventory == null)
        {
            Debug.LogError("找不到Inventory组件！");
        }
    }
    
    void Update()
    {
        // 按F1显示库存状态
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ShowInventoryState();
        }
        
        // 按F2重置所有槽位状态
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ResetAllSlotStates();
        }
        
        // 按F3检查拖拽状态
        if (Input.GetKeyDown(KeyCode.F3))
        {
            CheckDragState();
        }
    }
    
    void ShowInventoryState()
    {
        if (inventory == null) return;
        
        Debug.Log("=== 库存状态 ===");
        
        // 显示主库存
        for (int y = 0; y < inventory.inventoryHeight; y++)
        {
            for (int x = 0; x < inventory.inventoryWidth; x++)
            {
                var slot = inventory.inventorySlots[x, y];
                if (slot != null && slot.item != null)
                {
                    Debug.Log($"库存[{x},{y}]: {slot.item.itemName} x{slot.item.quantity}");
                }
            }
        }
        
        // 显示热键栏
        for (int x = 0; x < inventory.hotbarSlots.Length; x++)
        {
            var slot = inventory.hotbarSlots[x];
            if (slot != null && slot.item != null)
            {
                Debug.Log($"热键栏[{x}]: {slot.item.itemName} x{slot.item.quantity}");
            }
        }
    }
    
    void ResetAllSlotStates()
    {
        Debug.Log("重置所有槽位状态...");
        
        // 重置主库存槽位UI状态
        for (int y = 0; y < inventory.inventoryHeight; y++)
        {
            for (int x = 0; x < inventory.inventoryWidth; x++)
            {
                GameObject slotUI = inventory.inventoryUISlots[x, y];
                if (slotUI != null)
                {
                    ResetSlotUIState(slotUI);
                }
            }
        }
        
        // 重置热键栏槽位UI状态
        for (int x = 0; x < inventory.hotbarUISlots.Length; x++)
        {
            GameObject slotUI = inventory.hotbarUISlots[x];
            if (slotUI != null)
            {
                ResetSlotUIState(slotUI);
            }
        }
        
        // 重置拖拽状态
        InventorySlotUI.isDragging = false;
        InventorySlotUI.draggedSlot = null;
        
        Debug.Log("所有槽位状态已重置！");
    }
    
    void ResetSlotUIState(GameObject slotUI)
    {
        var canvasGroup = slotUI.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        
        var slotUIComponent = slotUI.GetComponent<InventorySlotUI>();
        if (slotUIComponent != null)
        {
            // 通过反射重置私有字段（如果需要）
            // 这里可以添加更多重置逻辑
        }
    }
    
    void CheckDragState()
    {
        Debug.Log($"拖拽状态检查:");
        Debug.Log($"- isDragging: {InventorySlotUI.isDragging}");
        Debug.Log($"- draggedSlot: {(InventorySlotUI.draggedSlot != null ? "有" : "无")}");
        
        if (DragManager.Instance != null)
        {
            Debug.Log($"- DragManager正在拖拽: {DragManager.Instance.IsDragging()}");
        }
        else
        {
            Debug.LogWarning("- DragManager实例不存在！");
        }
    }
    
    void OnGUI()
    {
        if (!showSlotStates) return;

        // 移到右上角
        GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 300, 200));
        GUILayout.Label("库存调试信息:");
        GUILayout.Label($"拖拽状态: {InventorySlotUI.isDragging}");
        GUILayout.Label($"拖拽槽位: {(InventorySlotUI.draggedSlot != null ? "有" : "无")}");

        if (GUILayout.Button("显示库存状态 (F1)"))
        {
            ShowInventoryState();
        }

        if (GUILayout.Button("重置槽位状态 (F2)"))
        {
            ResetAllSlotStates();
        }

        if (GUILayout.Button("检查拖拽状态 (F3)"))
        {
            CheckDragState();
        }

        GUILayout.EndArea();
    }
}
