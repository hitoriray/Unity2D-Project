using UnityEngine;

/// <summary>
/// 光照系统优化器 - 提供性能监控和配置选项
/// </summary>
public class LightingOptimizer : MonoBehaviour
{
    [Header("光照性能配置")]
    [Tooltip("背景墙光照增强系数 (1.0-3.0)")]
    [Range(1.0f, 3.0f)]
    public float wallLightBoost = 1.5f;
    
    [Tooltip("光照更新队列最大延迟帧数")]
    [Range(1, 10)]
    public int maxQueueDelayFrames = 2;
    
    [Header("性能监控")]
    [Tooltip("显示光照更新性能统计")]
    public bool showPerformanceStats = true;
    
    [Tooltip("显示光照更新队列状态")]
    public bool showQueueStatus = true;
    
    // 性能统计
    private static int lightUpdatesThisFrame = 0;
    private static int totalLightUpdates = 0;
    private static float lastFrameTime = 0f;
    private static int queueSize = 0;
    
    private void Start()
    {
        // 应用初始配置
        LightingManager.SetWallLightBoost(wallLightBoost);
    }

    private void Update()
    {
        // 监控性能
        if (showPerformanceStats)
        {
            MonitorPerformance();
        }

        // 更新配置
        if (Mathf.Abs(LightingManager.GetWallLightBoost() - wallLightBoost) > 0.01f)
        {
            LightingManager.SetWallLightBoost(wallLightBoost);
        }
    }
    
    private void MonitorPerformance()
    {
        float currentTime = Time.time;
        if (currentTime - lastFrameTime >= 1.0f) // 每秒更新一次统计
        {
            if (lightUpdatesThisFrame > 0)
            {
                Debug.Log($"[光照性能] 光照更新: {lightUpdatesThisFrame}/秒, 总计: {totalLightUpdates}");
            }
            
            lightUpdatesThisFrame = 0;
            lastFrameTime = currentTime;
        }
    }
    
    private void OnGUI()
    {
        if (!showPerformanceStats && !showQueueStatus) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("光照系统优化状态");
        
        if (showPerformanceStats)
        {
            GUILayout.Label($"背景墙光照增强: {wallLightBoost:F1}x");
            GUILayout.Label($"本帧光照更新: {lightUpdatesThisFrame}");
            GUILayout.Label($"总光照更新: {totalLightUpdates}");
        }
        
        if (showQueueStatus)
        {
            GUILayout.Label($"光照队列大小: {queueSize}");
            GUILayout.Label($"队列处理状态: {(queueSize > 0 ? "处理中" : "空闲")}");
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// 记录光照更新统计
    /// </summary>
    public static void RecordLightUpdate()
    {
        lightUpdatesThisFrame++;
        totalLightUpdates++;
    }
    
    /// <summary>
    /// 更新队列大小统计
    /// </summary>
    public static void UpdateQueueSize(int size)
    {
        queueSize = size;
    }
    
    /// <summary>
    /// 重置性能统计
    /// </summary>
    [ContextMenu("重置性能统计")]
    public void ResetPerformanceStats()
    {
        lightUpdatesThisFrame = 0;
        totalLightUpdates = 0;
        lastFrameTime = Time.time;
        queueSize = 0;
        
        Debug.Log("[光照优化器] 性能统计已重置");
    }
    
    /// <summary>
    /// 测试光照性能
    /// </summary>
    [ContextMenu("测试光照性能")]
    public void TestLightingPerformance()
    {
        TerrainGeneration terrainGen = FindObjectOfType<TerrainGeneration>();
        if (terrainGen == null)
        {
            Debug.LogError("[光照优化器] 未找到 TerrainGeneration 组件");
            return;
        }
        
        Debug.Log("[光照优化器] 开始光照性能测试...");
        
        float startTime = Time.realtimeSinceStartup;
        
        // 模拟快速放置多个方块
        for (int i = 0; i < 10; i++)
        {
            int x = Random.Range(10, terrainGen.worldSize - 10);
            int y = Random.Range(10, terrainGen.worldSize - 10);
            
            LightingManager.QueueLightUpdate(terrainGen, x, y, 1f);
        }
        
        float endTime = Time.realtimeSinceStartup;
        float duration = (endTime - startTime) * 1000f; // 转换为毫秒
        
        Debug.Log($"[光照优化器] 性能测试完成 - 耗时: {duration:F2}ms");
    }
}
