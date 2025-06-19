using UnityEngine;
using UnityEngine.UI;
using Box;

public class BoxUI : MonoBehaviour
{
    [SerializeField] private BoxInteraction boxInteraction; // 引用BoxInteraction脚本
    [SerializeField] private GameObject boxWindow;
    [Tooltip("全部取出")]
    [SerializeField] private Button allOutButton;
    [Tooltip("全部存入")]
    [SerializeField] private Button allInButton;   

    private void Start()
    {
        // 确保在Start时，能拿到BoxInteraction的引用
        if (boxInteraction == null)
        {
            // 如果没有在Inspector中指定，尝试从父对象或当前对象获取
            boxInteraction = GetComponentInParent<BoxInteraction>();
            if (boxInteraction == null)
            {
                Debug.LogError("BoxUI无法找到BoxInteraction组件!", this);
                return;
            }
        }
        
        allOutButton.onClick.AddListener(OnAllOutButtonClicked);
        allInButton.onClick.AddListener(OnAllInButtonClicked);
    }

    private void OnAllOutButtonClicked()
    {
        boxInteraction.TakeAll();
    }

    private void OnAllInButtonClicked()
    {
        boxInteraction.StoreAll();
    }
} 