using UnityEngine;

public class Workbench : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private CraftingUI craftingUI; // 引用UI控制器

    [Header("设置")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("Sprite")]
    public Sprite leftSprite;
    public Sprite rightSprite;
    [SerializeField] private SpriteRenderer rendererLeft;
    [SerializeField] private SpriteRenderer rendererRight;


    private bool playerInRange = false;

    void Start()
    {
        rendererLeft.sprite = leftSprite;
        rendererRight.sprite = rightSprite;
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            // 这里我们直接开关UI，而不是在CraftingUI里再用一个按键控制
            craftingUI.gameObject.SetActive(!craftingUI.gameObject.activeSelf);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 假设玩家的GameObject上有一个"Player"标签
        if (other.CompareTag("Player"))
        {
            Debug.Log("trigger player");
            playerInRange = true;
            // 可以在这里显示一个 "按E打开" 的提示
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            // 如果玩家离开范围时，制作界面是打开的，就把它关掉
            if (craftingUI.gameObject.activeSelf)
            {
                craftingUI.gameObject.SetActive(false);
            }
            // 可以在这里隐藏提示
        }
    }
} 