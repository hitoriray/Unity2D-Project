using UnityEngine;
using UnityEngine.Rendering.Universal;
using AmbianceSystem;

/// <summary>
/// 增强版昼夜循环系统，支持高级光照系统集成
/// </summary>
public class EnhancedDayNightCycle : MonoBehaviour
{
    #region Singleton
    public static EnhancedDayNightCycle Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
    
    [Header("时间设置")]
    [SerializeField] private float dayDurationSeconds = 300f; // 5分钟白天
    [SerializeField] private float nightDurationSeconds = 180f; // 3分钟夜晚
    [SerializeField, Range(0f, 1f)] private float currentTimeNormalized = 0.5f; // 0-1的标准化时间
    
    [Header("光照设置")]
    [SerializeField] private bool useAdvancedLighting = true;
    [SerializeField] private Gradient dayNightColorGradient;
    [SerializeField] private AnimationCurve intensityCurve;
    
    [Header("视觉效果")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Gradient skyColorGradient;
    [SerializeField] private AnimationCurve fogDensityCurve;
    [SerializeField] private float maxFogDensity = 0.05f;
    
    [Header("太阳/月亮")]
    [SerializeField] private Transform sunMoonTransform;
    [SerializeField] private Light2D sunMoonLight;
    [SerializeField] private Gradient sunMoonColorGradient;
    
    // 私有变量
    private float currentCycleTimer = 0f;
    private bool isDay = true;
    private float totalCycleDuration;
    
    // 事件
    public delegate void TimeChangeHandler(bool isDay, float normalizedTime);
    public event TimeChangeHandler OnTimeChanged;
    
    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        totalCycleDuration = dayDurationSeconds + nightDurationSeconds;
        
        // 设置默认渐变
        SetupDefaultGradients();
        
        // 初始化时间
        UpdateTimeOfDay();
    }
    
    private void SetupDefaultGradients()
    {
        if (dayNightColorGradient == null)
        {
            dayNightColorGradient = new Gradient();
            var colorKeys = new GradientColorKey[7];
            colorKeys[0] = new GradientColorKey(new Color(0.05f, 0.05f, 0.2f), 0f); // 午夜 - 深蓝
            colorKeys[1] = new GradientColorKey(new Color(0.05f, 0.05f, 0.2f), 0.2f); // 凌晨 - 深蓝
            colorKeys[2] = new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.25f); // 黎明 - 橙色
            colorKeys[3] = new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0.35f); // 早晨 - 暖白
            colorKeys[4] = new GradientColorKey(Color.white, 0.5f); // 正午 - 纯白
            colorKeys[5] = new GradientColorKey(new Color(1f, 0.7f, 0.4f), 0.75f); // 黄昏 - 橙黄
            colorKeys[6] = new GradientColorKey(new Color(0.05f, 0.05f, 0.2f), 0.85f); // 夜晚 - 深蓝
            
            var alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            dayNightColorGradient.SetKeys(colorKeys, alphaKeys);
        }
        
        if (skyColorGradient == null)
        {
            skyColorGradient = new Gradient();
            var colorKeys = new GradientColorKey[7];
            colorKeys[0] = new GradientColorKey(new Color(0.02f, 0.02f, 0.1f), 0f); // 午夜
            colorKeys[1] = new GradientColorKey(new Color(0.02f, 0.02f, 0.1f), 0.2f); // 凌晨
            colorKeys[2] = new GradientColorKey(new Color(0.9f, 0.4f, 0.2f), 0.25f); // 黎明
            colorKeys[3] = new GradientColorKey(new Color(0.5f, 0.7f, 0.9f), 0.35f); // 早晨
            colorKeys[4] = new GradientColorKey(new Color(0.53f, 0.81f, 0.92f), 0.5f); // 正午
            colorKeys[5] = new GradientColorKey(new Color(0.9f, 0.5f, 0.3f), 0.75f); // 黄昏
            colorKeys[6] = new GradientColorKey(new Color(0.02f, 0.02f, 0.1f), 0.85f); // 夜晚
            
            var alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            skyColorGradient.SetKeys(colorKeys, alphaKeys);
        }
        
        if (intensityCurve == null)
        {
            intensityCurve = new AnimationCurve();
            intensityCurve.AddKey(0f, 0.1f); // 午夜最暗
            intensityCurve.AddKey(0.25f, 0.3f); // 黎明
            intensityCurve.AddKey(0.5f, 1f); // 正午最亮
            intensityCurve.AddKey(0.75f, 0.4f); // 黄昏
            intensityCurve.AddKey(1f, 0.1f); // 回到夜晚
        }
        
        if (fogDensityCurve == null)
        {
            fogDensityCurve = new AnimationCurve();
            fogDensityCurve.AddKey(0f, 0.8f); // 夜晚有雾
            fogDensityCurve.AddKey(0.25f, 1f); // 黎明最浓
            fogDensityCurve.AddKey(0.5f, 0f); // 正午无雾
            fogDensityCurve.AddKey(0.75f, 0.6f); // 黄昏有些雾
            fogDensityCurve.AddKey(1f, 0.8f); // 夜晚有雾
        }
    }
    
    private void Update()
    {
        // 更新计时器
        currentCycleTimer += Time.deltaTime;
        
        // 计算标准化时间（0-1）
        if (currentCycleTimer >= totalCycleDuration)
        {
            currentCycleTimer = 0f;
        }
        
        // 计算当前在循环中的位置
        if (currentCycleTimer < dayDurationSeconds)
        {
            // 白天：0.25 到 0.75
            currentTimeNormalized = 0.25f + (currentCycleTimer / dayDurationSeconds) * 0.5f;
            isDay = true;
        }
        else
        {
            // 夜晚：0.75 到 1.0，然后 0.0 到 0.25
            float nightProgress = (currentCycleTimer - dayDurationSeconds) / nightDurationSeconds;
            if (nightProgress < 0.5f)
            {
                currentTimeNormalized = 0.75f + nightProgress * 0.5f;
            }
            else
            {
                currentTimeNormalized = (nightProgress - 0.5f) * 0.5f;
            }
            isDay = false;
        }
        
        UpdateTimeOfDay();
    }
    
    private void UpdateTimeOfDay()
    {
        // 更新高级光照系统
        if (useAdvancedLighting && AdvancedLightingSystem.Instance != null)
        {
            AdvancedLightingSystem.Instance.SetTimeOfDay(currentTimeNormalized);
        }
        
        // 更新天空颜色
        if (mainCamera != null && skyColorGradient != null)
        {
            mainCamera.backgroundColor = skyColorGradient.Evaluate(currentTimeNormalized);
        }
        
        // 更新雾效
        if (fogDensityCurve != null)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = fogDensityCurve.Evaluate(currentTimeNormalized) * maxFogDensity;
            RenderSettings.fogColor = skyColorGradient.Evaluate(currentTimeNormalized);
        }
        
        // 更新太阳/月亮
        UpdateSunMoon();
        
        // 更新环境光（如果没有使用高级光照系统）
        if (!useAdvancedLighting || AdvancedLightingSystem.Instance == null)
        {
            Color ambientColor = dayNightColorGradient.Evaluate(currentTimeNormalized);
            float intensity = intensityCurve.Evaluate(currentTimeNormalized);
            RenderSettings.ambientLight = ambientColor * intensity;
        }
        
        // 触发事件
        OnTimeChanged?.Invoke(isDay, currentTimeNormalized);
    }
    
    private void UpdateSunMoon()
    {
        if (sunMoonTransform != null)
        {
            // 旋转太阳/月亮（东升西落）
            float angle = currentTimeNormalized * 360f - 90f;
            sunMoonTransform.rotation = Quaternion.Euler(angle, 170f, 0f);
        }
        
        if (sunMoonLight != null)
        {
            // 更新光照颜色和强度
            if (sunMoonColorGradient != null)
            {
                sunMoonLight.color = sunMoonColorGradient.Evaluate(currentTimeNormalized);
            }
            
            if (intensityCurve != null)
            {
                float baseIntensity = intensityCurve.Evaluate(currentTimeNormalized);
                
                // 夜晚时减弱光照并可能改变颜色（模拟月光）
                if (!isDay)
                {
                    sunMoonLight.intensity = baseIntensity * 0.3f;
                    // 夜晚时给光照添加蓝色调（月光效果）
                    sunMoonLight.color = Color.Lerp(sunMoonLight.color, new Color(0.7f, 0.8f, 1f), 0.5f);
                }
                else
                {
                    sunMoonLight.intensity = baseIntensity;
                }
            }
        }
    }
    
    #region 公共方法
    
    /// <summary>
    /// 设置时间（0-1）
    /// </summary>
    public void SetTime(float normalizedTime)
    {
        currentTimeNormalized = Mathf.Clamp01(normalizedTime);
        
        // 根据时间计算计时器位置
        if (normalizedTime >= 0.25f && normalizedTime <= 0.75f)
        {
            // 白天
            float dayProgress = (normalizedTime - 0.25f) / 0.5f;
            currentCycleTimer = dayProgress * dayDurationSeconds;
        }
        else
        {
            // 夜晚
            float nightProgress;
            if (normalizedTime > 0.75f)
            {
                nightProgress = (normalizedTime - 0.75f) / 0.25f * 0.5f;
            }
            else
            {
                nightProgress = 0.5f + (normalizedTime / 0.25f * 0.5f);
            }
            currentCycleTimer = dayDurationSeconds + nightProgress * nightDurationSeconds;
        }
        
        UpdateTimeOfDay();
    }
    
    /// <summary>
    /// 获取当前是否为白天
    /// </summary>
    public bool IsDay()
    {
        return isDay;
    }
    
    /// <summary>
    /// 获取当前时间段
    /// </summary>
    public AmbianceSystem.TimeOfDay GetTimeOfDay()
    {
        return isDay ? AmbianceSystem.TimeOfDay.Day : AmbianceSystem.TimeOfDay.Night;
    }
    
    /// <summary>
    /// 获取标准化时间（0-1）
    /// </summary>
    public float GetNormalizedTime()
    {
        return currentTimeNormalized;
    }
    
    /// <summary>
    /// 快速切换到白天
    /// </summary>
    [ContextMenu("切换到白天")]
    public void SetToDay()
    {
        SetTime(0.5f); // 正午
    }
    
    /// <summary>
    /// 快速切换到夜晚
    /// </summary>
    [ContextMenu("切换到夜晚")]
    public void SetToNight()
    {
        SetTime(0f); // 午夜
    }
    
    #endregion
} 