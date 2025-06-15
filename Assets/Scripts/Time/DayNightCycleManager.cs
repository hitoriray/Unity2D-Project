using UnityEngine;
using AmbianceSystem; // 引用以使用 AmbianceSystem.TimeOfDay

// DayNightCycleManager 现在位于全局命名空间

/// <summary>
/// 管理游戏中的昼夜循环。
/// 实现基于计时器的自动昼夜切换。
/// </summary>
public class DayNightCycleManager : MonoBehaviour
{
    #region Singleton
    public static DayNightCycleManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 可选：如果希望跨场景存在

            // 初始化昼夜循环状态
            _currentAmbianceTime = AmbianceSystem.TimeOfDay.Day; // 默认从白天开始
            currentCycleTimer = 0f;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    // [Header("Debug Settings")] // 移除调试相关的 Header
    // [Tooltip("（调试用）手动设置当前的游戏时间段 (白天/夜晚)")] // 移除调试相关的 Tooltip
    // public AmbianceSystem.TimeOfDay debugCurrentTime = AmbianceSystem.TimeOfDay.Day; // 移除或注释掉调试变量

    [Header("Cycle Durations")]
    [Tooltip("白天持续时间（秒）")]
    public float dayDurationSeconds = 60f; // 白天持续时间，可在检视面板配置

    [Tooltip("夜晚持续时间（秒）")]
    public float nightDurationSeconds = 40f; // 夜晚持续时间，可在检视面板配置

    private float currentCycleTimer; // 当前时间段已过的时间
    private AmbianceSystem.TimeOfDay _currentAmbianceTime; // 内部追踪当前时间段

    /// <summary>
    /// 获取当前的氛围系统时间段 (例如白天或夜晚)。
    /// </summary>
    public AmbianceSystem.TimeOfDay CurrentAmbianceTime
    {
        get { return _currentAmbianceTime; }
        // 此处移除了原先被注释的 private set
    }

    void Update()
    {
        currentCycleTimer += Time.deltaTime; // 累加计时器

        // 检查是否需要切换时间段
        if (_currentAmbianceTime == AmbianceSystem.TimeOfDay.Day)
        {
            if (currentCycleTimer >= dayDurationSeconds)
            {
                _currentAmbianceTime = AmbianceSystem.TimeOfDay.Night; // 切换到夜晚
                currentCycleTimer = 0f; // 重置计时器
                Debug.Log("[DayNightCycleManager] 时间切换到: Night");
            }
        }
        else // 当前是夜晚
        {
            if (currentCycleTimer >= nightDurationSeconds)
            {
                _currentAmbianceTime = AmbianceSystem.TimeOfDay.Day; // 切换到白天
                currentCycleTimer = 0f; // 重置计时器
                Debug.Log("[DayNightCycleManager] 时间切换到: Day");
            }
        }
    }
}