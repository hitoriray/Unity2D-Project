using UnityEngine;

public class Box : MonoBehaviour
{
    [Header("箱子设置")]
    public int boxWidth = 6;
    public int boxHeight = 4;

    [Header("UI引用")]
    public ItemContainerUI boxUI;

    private ItemContainer boxContainer;
    private bool playerInRange = false;

    private void Awake()
    {
        // 创建箱子数据容器
        boxContainer = new ItemContainer(boxWidth, boxHeight);

        // 确保UI初始时是隐藏的
        if (boxUI != null)
        {
            boxUI.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 当玩家在范围内并按下'E'键
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            ToggleUI();
        }
    }

    public void ToggleUI()
    {
        bool isActive = !boxUI.gameObject.activeSelf;
        boxUI.gameObject.SetActive(isActive);

        // 当UI激活时，初始化它以显示箱子的内容
        if (isActive)
        {
            boxUI.Initialize(boxContainer);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 假设玩家有一个 "Player" 标签
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            // TODO: 可以考虑在这里显示一个交互提示，比如 "按E打开"
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            // 如果玩家离开范围，关闭箱子UI
            if (boxUI.gameObject.activeSelf)
            {
                ToggleUI();
            }
        }
    }
} 