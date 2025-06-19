using UnityEngine;

public class WarehouseController : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private WarehouseUI warehouseUI; // 引用UI控制器

    [Header("设置")]
    [SerializeField] private KeyCode interactionKey = KeyCode.B;


    void Start()
    {
        warehouseUI.gameObject.SetActive(false);
        warehouseUI.closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void OnCloseButtonClicked()
    {
        warehouseUI.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(interactionKey) && !warehouseUI.gameObject.activeSelf)
        {
            warehouseUI.gameObject.SetActive(true);
        }
    }

    public bool IsWarehouseUIShowing()
    {
        return warehouseUI.gameObject.activeInHierarchy;
    }
} 