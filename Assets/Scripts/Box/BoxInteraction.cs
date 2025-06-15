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
        public ItemContainerUI boxUI;

        [Header("宝箱物品容器设置")]
        public int boxWidth = 8;
        public int boxHeight = 4;

        private bool playerInRange = false;

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
                BoxController.CloseBox();
            }
            else
            {
                BoxController.OpenBox(() =>
                {
                    boxUI.gameObject.SetActive(true);
                    boxUI.Initialize(boxContainer);
                });
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = true;
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
                    BoxController.CloseBox();
                }
            }
        }
    }
}