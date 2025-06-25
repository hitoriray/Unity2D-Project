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

        [Header("交互设置")]
        [Tooltip("可交互的层级，默认为宝箱子对象的层级")]
        public LayerMask clickableLayer = 1 << 9; // Layer 9 (Clickable)

        [Header("音效")]
        [Tooltip("移动物品时的音效")]
        public AudioClip moveItemSound;

        private bool playerInRange = false;
        private Inventory playerInventory;
        private Camera playerCamera;

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

        private void Start()
        {
            // 获取主摄像机引用
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
        }

        private void Update()
        {
            // 只有在玩家范围内且按下右键时才检测点击
            if (playerInRange && Input.GetMouseButtonDown(1))
            {
                if (IsClickingOnBox())
                {
                    ToggleUI();
                }
            }
        }

        /// <summary>
        /// 检测鼠标是否点击在宝箱本体上
        /// </summary>
        private bool IsClickingOnBox()
        {
            if (playerCamera == null) return false;

            // 将鼠标屏幕坐标转换为世界坐标
            Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0; // 确保Z轴为0（2D游戏）

            // 使用射线检测点击的对象
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, 0f, clickableLayer);

            if (hit.collider != null)
            {
                // 检查点击的对象是否属于这个宝箱
                return hit.collider.transform.IsChildOf(this.transform) || hit.collider.transform == this.transform;
            }

            return false;
        }

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
                Debug.Log("玩家进入宝箱交互范围");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = false;
                Debug.Log("玩家离开宝箱交互范围");
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

            for (int y = playerContainer.height - 1; y >= 0; --y)
            {
                for (int x = 0; x < playerContainer.width; ++x)
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

            for (int y = boxContainer.height - 1; y >= 0; --y)
            {
                for (int x = 0; x < boxContainer.width; ++x)
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