using UnityEngine;
using UI;

/// <summary>
/// 血量UI调试器 - 帮助诊断和修复血量UI系统的问题
/// </summary>
public class HealthUIDebugger : MonoBehaviour
{
    [Header("调试控制")]
    [Tooltip("是否自动创建血量UI")]
    public bool autoCreateHealthUI = true;
    [Tooltip("是否显示详细日志")]
    public bool enableVerboseLogging = true;
    [Tooltip("测试用的血量值")]
    public float testCurrentHealth = 100f;
    [Tooltip("测试用的最大血量")]
    public float testMaxHealth = 100f;

    void Start()
    {
        if (enableVerboseLogging)
        {
            Debug.Log("[HealthUIDebugger] 开始调试血量UI系统");
        }

        // 延迟执行，确保其他系统已初始化
        Invoke(nameof(CheckAndCreateHealthUI), 0.5f);
    }

    void CheckAndCreateHealthUI()
    {
        // 检查是否存在PlayerHealthUI
        if (PlayerHealthUI.Instance == null)
        {
            Debug.LogWarning("[HealthUIDebugger] 未找到PlayerHealthUI实例");
            
            if (autoCreateHealthUI)
            {
                CreateHealthUI();
            }
        }
        else
        {
            Debug.Log("[HealthUIDebugger] 找到PlayerHealthUI实例");
            TestHealthUI();
        }
    }

    void CreateHealthUI()
    {
        Debug.Log("[HealthUIDebugger] 自动创建PlayerHealthUI");

        // 寻找主Canvas
        Canvas mainCanvas = FindMainCanvas();
        if (mainCanvas == null)
        {
            Debug.LogError("[HealthUIDebugger] 无法找到主Canvas，无法创建血量UI");
            return;
        }

        // 创建PlayerHealthUI对象
        GameObject healthUIObj = new GameObject("PlayerHealthUI");
        healthUIObj.transform.SetParent(mainCanvas.transform, false);
        
        // 设置RectTransform
        RectTransform rectTransform = healthUIObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        // 添加PlayerHealthUI组件
        PlayerHealthUI healthUI = healthUIObj.AddComponent<PlayerHealthUI>();
        
        Debug.Log("[HealthUIDebugger] PlayerHealthUI创建完成");
        
        // 等一帧后测试
        Invoke(nameof(TestHealthUI), 0.1f);
    }

    void TestHealthUI()
    {
        if (PlayerHealthUI.Instance == null)
        {
            Debug.LogError("[HealthUIDebugger] PlayerHealthUI实例仍然为空");
            return;
        }

        Debug.Log("[HealthUIDebugger] 测试血量UI功能");
        
        try
        {
            PlayerHealthUI.Instance.UpdateHealth(testCurrentHealth, testMaxHealth, false);
            Debug.Log($"[HealthUIDebugger] 血量UI更新成功: {testCurrentHealth}/{testMaxHealth}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HealthUIDebugger] 血量UI更新失败: {e.Message}");
        }
    }

    Canvas FindMainCanvas()
    {
        // 首先尝试通过标签查找
        GameObject canvasObj = GameObject.FindGameObjectWithTag("MainCanvas");
        if (canvasObj != null)
        {
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            if (canvas != null)
            {
                Debug.Log("[HealthUIDebugger] 通过MainCanvas标签找到Canvas");
                return canvas;
            }
        }

        // 如果标签查找失败，查找第一个Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        if (canvases.Length > 0)
        {
            Debug.Log($"[HealthUIDebugger] 找到{canvases.Length}个Canvas，使用第一个");
            return canvases[0];
        }

        Debug.LogError("[HealthUIDebugger] 场景中没有找到任何Canvas");
        return null;
    }

    // 编辑器测试方法
    #if UNITY_EDITOR
    [ContextMenu("强制创建血量UI")]
    void ForceCreateHealthUI()
    {
        CreateHealthUI();
    }

    [ContextMenu("测试血量更新")]
    void ForceTestHealthUI()
    {
        TestHealthUI();
    }

    [ContextMenu("重置血量UI")]
    void ResetHealthUI()
    {
        if (PlayerHealthUI.Instance != null)
        {
            DestroyImmediate(PlayerHealthUI.Instance.gameObject);
        }
        
        Invoke(nameof(CheckAndCreateHealthUI), 0.1f);
    }
    #endif

    void Update()
    {
        // 键盘快捷键测试
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (PlayerHealthUI.Instance != null)
            {
                testCurrentHealth = Mathf.Max(0f, testCurrentHealth - 10f);
                PlayerHealthUI.Instance.UpdateHealth(testCurrentHealth, testMaxHealth);
                Debug.Log($"[HealthUIDebugger] 键盘测试 - 减少血量: {testCurrentHealth}");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (PlayerHealthUI.Instance != null)
            {
                testCurrentHealth = Mathf.Min(testMaxHealth, testCurrentHealth + 10f);
                PlayerHealthUI.Instance.UpdateHealth(testCurrentHealth, testMaxHealth);
                Debug.Log($"[HealthUIDebugger] 键盘测试 - 增加血量: {testCurrentHealth}");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (PlayerHealthUI.Instance != null)
            {
                testMaxHealth += 20f;
                testCurrentHealth += 20f;
                PlayerHealthUI.Instance.UpdateHealth(testCurrentHealth, testMaxHealth);
                Debug.Log($"[HealthUIDebugger] 键盘测试 - 增加血量上限: {testMaxHealth}");
            }
        }
    }

    void OnGUI()
    {
        if (!enableVerboseLogging) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("血量UI调试器");
        GUILayout.Label($"PlayerHealthUI Instance: {(PlayerHealthUI.Instance != null ? "存在" : "不存在")}");
        GUILayout.Label($"当前血量: {testCurrentHealth}/{testMaxHealth}");
        GUILayout.Label("快捷键: H(减血) J(加血) K(增加上限)");
        
        if (GUILayout.Button("测试血量UI"))
        {
            TestHealthUI();
        }
        
        if (GUILayout.Button("重新创建血量UI"))
        {
            if (PlayerHealthUI.Instance != null)
            {
                Destroy(PlayerHealthUI.Instance.gameObject);
            }
            CreateHealthUI();
        }
        
        GUILayout.EndArea();
    }
} 