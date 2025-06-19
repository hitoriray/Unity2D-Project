using Box;
using UnityEngine;

namespace Box
{
    [RequireComponent(typeof(BoxController))]
    public class BoxInteraction : MonoBehaviour
    {
        [Header("核心数据")]
        public ItemContainer boxContainer;
        
        [Header("UI引用")]
        public BoxUI boxUI;
        public ItemContainerUI boxContainerUI;

        [Header("宝箱物品容器设置")]
        public int boxWidth = 8;
        public int boxHeight = 4;

        [Header("音效")]
        [Tooltip("移动物品时的音效")]
        public AudioClip moveItemSound;

        private bool playerInRange = false;
        private Inventory playerInventory;

        private BoxController BoxController;

        private void Awake()
        {
            BoxController = GetComponent<BoxController>();
            boxContainer = new ItemContainer(boxWidth, boxHeight);

            if (boxUI != null)
            {
                boxUI.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (playerInRange && Input.GetMouseButtonDown(1))
            {
                ToggleUI();
            }
        }

        // ToggleUI, OnTriggerEnter2D, OnTriggerExit2D 方法都保持原样，无需改动
        public void ToggleUI()
        {
            if (BoxController.IsOpen)
            {
                boxUI.gameObject.SetActive(false);
                boxContainerUI.gameObject.SetActive(false);
                BoxController.CloseBox();
            }
            else
            {
                BoxController.OpenBox(() =>
                {
                    boxUI.gameObject.SetActive(true);
                    boxContainerUI.gameObject.SetActive(true);
                    boxContainerUI.Initialize(boxContainer);
                });
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = true;
                playerInventory = other.GetComponent<Inventory>();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = false;
                if (BoxController.IsOpen)
                {
                    boxUI.gameObject.SetActive(false);
                    boxContainerUI.gameObject.SetActive(false);
                    BoxController.CloseBox();
                }
            }
        }
        
        /// <summary>
        /// 将玩家背包的所有物品存入宝箱
        /// </summary>
        public void StoreAll()
        {
            if (playerInventory == null) return;

            var playerContainer = playerInventory.items;
            bool itemsMoved = false;

            // 从后往前遍历以安全地移除物品
            for (int y = playerContainer.height - 1; y >= 0; y--)
            {
                for (int x = playerContainer.width - 1; x >= 0; x--)
                {
                    InventorySlot slot = playerContainer.GetSlot(new Vector2Int(x, y));
                    if (slot != null && slot.item != null)
                    {
                        if (boxContainer.AddItem(slot.item))
                            itemsMoved = true;
                        if (slot.item.quantity == 0)
                            playerContainer.RemoveItem(x, y);
                    }
                }
            }
            
            if (itemsMoved && moveItemSound != null)
            {
                AudioSource.PlayClipAtPoint(moveItemSound, Camera.main.transform.position);
            }
        }

        /// <summary>
        /// 从宝箱中取出所有物品到玩家背包
        /// </summary>
        public void TakeAll()
        {
            if (playerInventory == null) return;

            var playerContainer = playerInventory.items;
            bool itemsMoved = false;

            // 从后往前遍历以安全地移除物品
            for (int y = boxContainer.height - 1; y >= 0; y--)
            {
                for (int x = boxContainer.width - 1; x >= 0; x--)
                {
                    InventorySlot slot = boxContainer.GetSlot(new Vector2Int(x, y));
                    if (slot != null && slot.item != null)
                    {
                        if (playerContainer.AddItem(slot.item))
                            itemsMoved = true;
                        if (slot.item.quantity == 0)
                            boxContainer.RemoveItem(x, y);
                    }
                }
            }
            
            if (itemsMoved && moveItemSound != null)
            {
                AudioSource.PlayClipAtPoint(moveItemSound, Camera.main.transform.position);
            }
        }
    }
}