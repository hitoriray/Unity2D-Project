using UnityEngine;

/// <summary>
/// 光照系统测试脚本
/// 将此脚本添加到玩家身上，用于快速测试光照功能
/// </summary>
public class LightingTester : MonoBehaviour
{
    [Header("测试控制")]
    [SerializeField] private bool enableTesting = true;
    [SerializeField] private KeyCode torchKey = KeyCode.T;
    [SerializeField] private KeyCode explosionKey = KeyCode.E;
    [SerializeField] private KeyCode dayKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode nightKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode togglePlayerLightKey = KeyCode.L;
    
    [Header("测试参数")]
    [SerializeField] private float torchDistance = 2f;
    [SerializeField] private float explosionIntensity = 3f;
    [SerializeField] private float explosionRadius = 10f;
    [SerializeField] private float explosionDuration = 0.8f;
    
    private GameObject playerLight;
    private bool playerLightActive = true;
    
    private void Update()
    {
        if (!enableTesting) return;
        
        HandleInput();
        ShowUI();
    }
    
    private void HandleInput()
    {
        // 放置火把
        if (Input.GetKeyDown(torchKey))
        {
            PlaceTorch();
        }
        
        // 创建爆炸光效
        if (Input.GetKeyDown(explosionKey))
        {
            CreateExplosion();
        }
        
        // 切换到白天
        if (Input.GetKeyDown(dayKey))
        {
            SetDay();
        }
        
        // 切换到夜晚
        if (Input.GetKeyDown(nightKey))
        {
            SetNight();
        }
        
        // 切换玩家光源
        if (Input.GetKeyDown(togglePlayerLightKey))
        {
            TogglePlayerLight();
        }
    }
    
    private void PlaceTorch()
    {
        if (AdvancedLightingSystem.Instance == null)
        {
            Debug.LogWarning("[LightingTester] AdvancedLightingSystem未找到！");
            return;
        }
        
        Vector3 torchPosition = transform.position + Vector3.right * torchDistance;
        GameObject torch = AdvancedLightingSystem.Instance.CreateTorchLight(
            torchPosition, 
            1.2f, 
            6f
        );
        
        if (torch != null)
        {
            Debug.Log($"[LightingTester] 在位置 {torchPosition} 放置了火把");
        }
        else
        {
            Debug.LogWarning("[LightingTester] 火把创建失败！");
        }
    }
    
    private void CreateExplosion()
    {
        if (AdvancedLightingSystem.Instance == null)
        {
            Debug.LogWarning("[LightingTester] AdvancedLightingSystem未找到！");
            return;
        }
        
        // 随机爆炸颜色
        Color[] explosionColors = { 
            Color.yellow, 
            Color.red, 
            new Color(1.0f, 0.64f, 0.0f), 
            new Color(1.0f, 0.5f, 0.0f), // 橙红色
            Color.white 
        };
        
        Color explosionColor = explosionColors[Random.Range(0, explosionColors.Length)];
        
        GameObject explosion = AdvancedLightingSystem.Instance.CreateTemporaryLight(
            transform.position,
            explosionColor,
            explosionIntensity,
            explosionRadius,
            explosionDuration
        );
        
        if (explosion != null)
        {
            Debug.Log($"[LightingTester] 创建了{explosionColor}颜色的爆炸光效");
        }
        else
        {
            Debug.LogWarning("[LightingTester] 爆炸光效创建失败！");
        }
    }
    
    private void SetDay()
    {
        if (EnhancedDayNightCycle.Instance != null)
        {
            EnhancedDayNightCycle.Instance.SetToDay();
            Debug.Log("[LightingTester] 切换到白天");
        }
        else
        {
            Debug.LogWarning("[LightingTester] EnhancedDayNightCycle未找到！");
        }
    }
    
    private void SetNight()
    {
        if (EnhancedDayNightCycle.Instance != null)
        {
            EnhancedDayNightCycle.Instance.SetToNight();
            Debug.Log("[LightingTester] 切换到夜晚");
        }
        else
        {
            Debug.LogWarning("[LightingTester] EnhancedDayNightCycle未找到！");
        }
    }
    
    private void TogglePlayerLight()
    {
        if (AdvancedLightingSystem.Instance == null)
        {
            Debug.LogWarning("[LightingTester] AdvancedLightingSystem未找到！");
            return;
        }
        
        if (playerLightActive)
        {
            // 移除玩家光源
            if (playerLight != null)
            {
                AdvancedLightingSystem.Instance.RemoveLight(playerLight);
                playerLight = null;
            }
            playerLightActive = false;
            Debug.Log("[LightingTester] 关闭玩家光源");
        }
        else
        {
            // 创建玩家光源
            playerLight = AdvancedLightingSystem.Instance.CreatePlayerLight(transform, 0.8f, 8f);
            playerLightActive = true;
            Debug.Log("[LightingTester] 开启玩家光源");
        }
    }
    
    private void ShowUI()
    {
        // 在屏幕上显示控制说明
        if (enableTesting)
        {
            string instructions = $"光照测试控制:\n" +
                                 $"{torchKey}: 放置火把\n" +
                                 $"{explosionKey}: 创建爆炸光效\n" +
                                 $"{dayKey}: 切换到白天\n" +
                                 $"{nightKey}: 切换到夜晚\n" +
                                 $"{togglePlayerLightKey}: 切换玩家光源\n" +
                                 $"\n状态:\n" +
                                 $"高级光照系统: {(AdvancedLightingSystem.Instance != null ? "✓" : "✗")}\n" +
                                 $"昼夜循环系统: {(EnhancedDayNightCycle.Instance != null ? "✓" : "✗")}\n" +
                                 $"玩家光源: {(playerLightActive ? "开启" : "关闭")}";
            
            GUI.Box(new Rect(10, 10, 300, 200), instructions);
        }
    }
    
    private void OnGUI()
    {
        ShowUI();
    }
    
    /// <summary>
    /// 自动测试序列
    /// </summary>
    [ContextMenu("运行自动测试")]
    public void RunAutoTest()
    {
        StartCoroutine(AutoTestSequence());
    }
    
    private System.Collections.IEnumerator AutoTestSequence()
    {
        Debug.Log("[LightingTester] 开始自动测试序列...");
        
        // 测试1：切换到夜晚
        SetNight();
        yield return new WaitForSeconds(2f);
        
        // 测试2：放置几个火把
        for (int i = 0; i < 3; i++)
        {
            Vector3 torchPos = transform.position + new Vector3(i * 3f - 3f, 0, 0);
            AdvancedLightingSystem.Instance?.CreateTorchLight(torchPos, 1.2f, 6f);
            yield return new WaitForSeconds(0.5f);
        }
        
        // 测试3：创建爆炸光效
        for (int i = 0; i < 3; i++)
        {
            CreateExplosion();
            yield return new WaitForSeconds(1f);
        }
        
        // 测试4：切换到白天
        SetDay();
        yield return new WaitForSeconds(2f);
        
        Debug.Log("[LightingTester] 自动测试序列完成！");
    }
    
    /// <summary>
    /// 压力测试
    /// </summary>
    [ContextMenu("运行压力测试")]
    public void RunStressTest()
    {
        StartCoroutine(StressTestSequence());
    }
    
    private System.Collections.IEnumerator StressTestSequence()
    {
        Debug.Log("[LightingTester] 开始压力测试...");
        
        if (AdvancedLightingSystem.Instance == null)
        {
            Debug.LogError("[LightingTester] 无法进行压力测试：AdvancedLightingSystem未找到");
            yield break;
        }
        
        // 创建大量临时光源
        for (int i = 0; i < 20; i++)
        {
            Vector3 randomPos = transform.position + new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-5f, 5f),
                0
            );
            
            Color randomColor = new Color(
                Random.Range(0.5f, 1f),
                Random.Range(0.5f, 1f),
                Random.Range(0.5f, 1f)
            );
            
            AdvancedLightingSystem.Instance.CreateTemporaryLight(
                randomPos,
                randomColor,
                Random.Range(0.5f, 2f),
                Random.Range(3f, 8f),
                Random.Range(2f, 5f)
            );
            
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("[LightingTester] 压力测试完成！");
    }
} 