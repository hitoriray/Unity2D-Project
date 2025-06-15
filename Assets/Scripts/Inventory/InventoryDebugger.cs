using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 库存拖拽系统调试工具
/// </summary>
public class InventoryDebugger : MonoBehaviour
{
    [Header("调试设置")]
    public bool enableDebugLogs = true;
    public bool showSlotStatesOnGUI = true;
    
    private Inventory inventory;
    private GraphicRaycaster mainCanvasRaycaster;
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;
    private string hoveredObjectName = "None";
    
    void Start()
    {
        inventory = GetComponentInParent<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("InventoryDebugger: 找不到Inventory组件！脚本已禁用。");
            enabled = false;
            return;
        }

        // Find the main canvas's GraphicRaycaster
        mainCanvasRaycaster = FindObjectOfType<GraphicRaycaster>();
        if(mainCanvasRaycaster == null)
        {
             Debug.LogError("InventoryDebugger: 找不到GraphicRaycaster！请确保场景中Canvas上挂载了此组件。");
             enabled = false;
             return;
        }
        
        eventSystem = EventSystem.current;
         if(eventSystem == null)
        {
             Debug.LogError("InventoryDebugger: 找不到EventSystem！请确保场景中有EventSystem。");
             enabled = false;
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            LogFullInventoryState();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ResetAllSlotStates();
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
            LogDragState();
        }

        // Real-time hover check
        if(showSlotStatesOnGUI)
        {
            CheckHoveredObject();
        }
    }

    private void CheckHoveredObject()
    {
        pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        mainCanvasRaycaster.Raycast(pointerEventData, results);

        if (results.Count > 0)
        {
            hoveredObjectName = results[0].gameObject.name;
        }
        else
        {
            hoveredObjectName = "None";
        }
    }
    
    public void LogFullInventoryState()
    {
        if (inventory == null || inventory.inventory == null) return;
        
        Debug.Log("=============== INVENTORY STATE (F1) ===============");
        
        // Display Main Inventory Data
        for (int y = 0; y < inventory.inventory.height; y++)
        {
            for (int x = 0; x < inventory.inventory.width; x++)
            {
                var slot = inventory.inventory.GetSlot(new Vector2Int(x, y));
                if (slot != null && slot.item != null)
                {
                    Debug.Log($"Main Inventory [{x},{y}]: {slot.item.itemName} x{slot.item.quantity}");
                }
            }
        }
        Debug.Log("======================================================");
    }
    
    public void ResetAllSlotStates()
    {
        Debug.LogWarning("=============== RESETTING ALL SLOTS (F2) ===============");
        
        // Reset main inventory UI slots
        if (inventory.inventoryUI != null && inventory.inventoryUI.uiSlots != null)
        {
            foreach (var slotGO in inventory.inventoryUI.uiSlots)
            {
                if(slotGO != null) ResetSlotUIState(slotGO);
            }
        }
        
        // Reset hotbar UI slots
        if (inventory.hotbarUI != null && inventory.hotbarUI.uiSlots != null)
        {
            foreach (var slotGO in inventory.hotbarUI.uiSlots)
            {
                 if(slotGO != null) ResetSlotUIState(slotGO);
            }
        }

        // Reset DragManager state
        if (DragManager.Instance != null && DragManager.Instance.IsDragging())
        {
            DragManager.Instance.EndDrag();
        }
        
        // Reset static slot reference
        InventorySlotUI.draggedSlot = null;
        
        Debug.Log("All slot states reset!");
    }
    
    void ResetSlotUIState(GameObject slotUI)
    {
        var canvasGroup = slotUI.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }
    
    public void LogDragState()
    {
        Debug.Log("=============== DRAG STATE (F3) ===============");
        if (DragManager.Instance != null)
        {
            Debug.Log($"- DragManager.IsDragging(): {DragManager.Instance.IsDragging()}");
        }
        else
        {
            Debug.LogWarning("- DragManager instance not found!");
        }
        Debug.Log($"- InventorySlotUI.draggedSlot: {(InventorySlotUI.draggedSlot != null ? InventorySlotUI.draggedSlot.name : "null")}");
        Debug.Log("===============================================");
    }
    
    void OnGUI()
    {
        if (!showSlotStatesOnGUI) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 250), "Inventory Debugger", GUI.skin.window);
        
        if (DragManager.Instance != null)
        {
            GUILayout.Label($"DragManager IsDragging: {DragManager.Instance.IsDragging()}");
        }
        GUILayout.Label($"Dragged Slot Object: {(InventorySlotUI.draggedSlot != null ? InventorySlotUI.draggedSlot.name : "None")}");
        
        // Display real-time hovered object name
        GUILayout.Space(10);
        GUI.color = Color.cyan;
        GUILayout.Label($"MOUSE IS OVER: {hoveredObjectName}");
        GUI.color = Color.white;
        GUILayout.Space(10);

        if (GUILayout.Button("Log Inventory State (F1)"))
        {
            LogFullInventoryState();
        }

        if (GUILayout.Button("Reset Slots (F2)"))
        {
            ResetAllSlotStates();
        }

        if (GUILayout.Button("Log Drag State (F3)"))
        {
            LogDragState();
        }

        GUILayout.EndArea();
    }
}
