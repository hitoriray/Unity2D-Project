using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

/// <summary>
/// 高级光照系统 - 结合Unity 2D Lights和纹理光照的混合方案
/// </summary>
public class AdvancedLightingSystem : MonoBehaviour
{
    private static AdvancedLightingSystem instance;
    public static AdvancedLightingSystem Instance => instance;

    [Header("基础设置")]
    [SerializeField] private TerrainGeneration terrainGen;
    [SerializeField] private bool enableAdvancedLighting = true;
    
    [Header("环境光照设置")]
    [SerializeField] private Gradient dayNightGradient;
    [SerializeField] private AnimationCurve ambientIntensityCurve;
    [SerializeField] private float ambientUpdateInterval = 0.1f;
    
    [Header("光源预制体")]
    [SerializeField] private GameObject torchLightPrefab;
    [SerializeField] private GameObject playerLightPrefab;
    [SerializeField] private GameObject projectileLightPrefab;
    
    [Header("性能设置")]
    [SerializeField] private int maxDynamicLights = 50;
    [SerializeField] private float lightCullingDistance = 30f;
    
    // 光源池
    private Dictionary<string, Queue<GameObject>> lightPools = new();
    private List<DynamicLight> activeLights = new();
    private Camera mainCamera;
    
    // 环境光照
    private float currentTimeOfDay = 0.5f; // 0-1，0.5是正午
    private Color currentAmbientColor = Color.white;
    private float currentAmbientIntensity = 1f;
    
    // 纹理光照（用于静态光源和环境光）
    private Texture2D ambientLightMap;
    private RenderTexture dynamicLightRT;
    private Material lightBlendMaterial;
    
    private class DynamicLight
    {
        public GameObject lightObject;
        public Light2D light2D;
        public Transform transform;
        public float lifetime;
        public bool isPersistent;
        public string poolKey;
        public Vector3 followTarget;
        public Transform followTransform;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        InitializeLightingSystem();
    }

    private void InitializeLightingSystem()
    {
        mainCamera = Camera.main;
        
        // 创建环境光照贴图
        if (terrainGen != null)
        {
            ambientLightMap = new Texture2D(terrainGen.worldSize, terrainGen.worldSize, TextureFormat.RGBA32, false);
            ambientLightMap.filterMode = FilterMode.Bilinear;
            
            // 创建动态光照渲染纹理
            dynamicLightRT = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            dynamicLightRT.filterMode = FilterMode.Bilinear;
        }
        
        // 初始化光源池
        InitializeLightPools();
        
        // 开始环境光照更新
        InvokeRepeating(nameof(UpdateAmbientLighting), 0f, ambientUpdateInterval);
    }

    private void InitializeLightPools()
    {
        // 火把光源池
        if (torchLightPrefab != null)
            CreateLightPool("torch", torchLightPrefab, 20);
        
        // 玩家光源池
        if (playerLightPrefab != null)
            CreateLightPool("player", playerLightPrefab, 5);
        
        // 投射物光源池
        if (projectileLightPrefab != null)
            CreateLightPool("projectile", projectileLightPrefab, 30);
    }

    private void CreateLightPool(string key, GameObject prefab, int size)
    {
        var pool = new Queue<GameObject>();
        var poolParent = new GameObject($"LightPool_{key}");
        poolParent.transform.parent = transform;
        
        for (int i = 0; i < size; i++)
        {
            var lightObj = Instantiate(prefab, poolParent.transform);
            lightObj.SetActive(false);
            pool.Enqueue(lightObj);
        }
        
        lightPools[key] = pool;
    }

    #region 公共API

    /// <summary>
    /// 创建火把光源
    /// </summary>
    public GameObject CreateTorchLight(Vector3 position, float intensity = 1f, float radius = 5f)
    {
        var light = GetOrCreateLight("torch", position);
        if (light != null)
        {
            light.light2D.intensity = intensity;
            // 注意：Freeform Light 2D 使用不同的属性来控制半径
            // 这里需要根据实际的Light2D API调整
            SetLightRadius(light.light2D, radius);
            light.isPersistent = true;
            
            // 添加闪烁效果
            var flicker = light.lightObject.GetComponent<LightFlicker>();
            if (flicker == null)
                flicker = light.lightObject.AddComponent<LightFlicker>();
            flicker.baseIntensity = intensity;
        }
        return light?.lightObject;
    }

    /// <summary>
    /// 创建玩家光源
    /// </summary>
    public GameObject CreatePlayerLight(Transform player, float intensity = 0.8f, float radius = 8f)
    {
        var light = GetOrCreateLight("player", player.position);
        if (light != null)
        {
            light.light2D.intensity = intensity;
            SetLightRadius(light.light2D, radius);
            light.isPersistent = true;
            light.followTransform = player;
        }
        return light?.lightObject;
    }

    /// <summary>
    /// 创建临时光源（如爆炸、技能效果等）
    /// </summary>
    public GameObject CreateTemporaryLight(Vector3 position, Color color, float intensity, float radius, float duration)
    {
        var light = GetOrCreateLight("projectile", position);
        if (light != null)
        {
            light.light2D.color = color;
            light.light2D.intensity = intensity;
            SetLightRadius(light.light2D, radius);
            light.lifetime = duration;
            light.isPersistent = false;
        }
        return light?.lightObject;
    }

    /// <summary>
    /// 移除光源
    /// </summary>
    public void RemoveLight(GameObject lightObject)
    {
        var light = activeLights.Find(l => l.lightObject == lightObject);
        if (light != null)
        {
            ReturnLightToPool(light);
        }
    }

    /// <summary>
    /// 设置时间（用于昼夜循环）
    /// </summary>
    public void SetTimeOfDay(float time)
    {
        currentTimeOfDay = Mathf.Clamp01(time);
        UpdateAmbientLighting();
    }

    #endregion

    #region 内部方法

    /// <summary>
    /// 设置Light2D的半径属性
    /// 注意：这个方法需要根据实际使用的Light Type调整属性名称
    /// </summary>
    private void SetLightRadius(Light2D light2D, float radius)
    {
        // 对于Freeform Light 2D，可能需要调整shapeLightFalloffSize或其他属性
        // 这里提供一个通用的解决方案，但可能需要根据实际情况调整
        
        if (light2D.lightType == Light2D.LightType.Freeform)
        {
            // Freeform Light 2D 使用 shapeLightFalloffSize 来控制光照范围
            light2D.shapeLightFalloffSize = radius;
        }
        else if (light2D.lightType == Light2D.LightType.Sprite)
        {
            // Sprite Light 2D 也使用类似属性
            light2D.shapeLightFalloffSize = radius;
        }
        // 如果有其他需要处理的Light Type，在这里添加
        
        // 注意：如果发现属性不存在，请查看Unity文档或Inspector面板
        // 找到正确的属性名称并替换上述代码
    }

    private DynamicLight GetOrCreateLight(string poolKey, Vector3 position)
    {
        if (activeLights.Count >= maxDynamicLights)
        {
            // 移除最远的非持久光源
            RemoveFarthestLight();
        }
        
        GameObject lightObj = null;
        
        if (lightPools.ContainsKey(poolKey) && lightPools[poolKey].Count > 0)
        {
            lightObj = lightPools[poolKey].Dequeue();
        }
        else
        {
            // 如果池中没有，创建新的
            var prefab = GetPrefabForPool(poolKey);
            if (prefab != null)
            {
                lightObj = Instantiate(prefab, transform);
            }
        }
        
        if (lightObj != null)
        {
            lightObj.SetActive(true);
            lightObj.transform.position = position;
            
            var dynamicLight = new DynamicLight
            {
                lightObject = lightObj,
                light2D = lightObj.GetComponent<Light2D>(),
                transform = lightObj.transform,
                poolKey = poolKey,
                lifetime = -1f,
                isPersistent = false
            };
            
            activeLights.Add(dynamicLight);
            return dynamicLight;
        }
        
        return null;
    }

    private GameObject GetPrefabForPool(string poolKey)
    {
        return poolKey switch
        {
            "torch" => torchLightPrefab,
            "player" => playerLightPrefab,
            "projectile" => projectileLightPrefab,
            _ => null
        };
    }

    private void ReturnLightToPool(DynamicLight light)
    {
        light.lightObject.SetActive(false);
        activeLights.Remove(light);
        
        if (lightPools.ContainsKey(light.poolKey))
        {
            lightPools[light.poolKey].Enqueue(light.lightObject);
        }
        else
        {
            Destroy(light.lightObject);
        }
    }

    private void RemoveFarthestLight()
    {
        if (mainCamera == null) return;
        
        DynamicLight farthestLight = null;
        float maxDistance = 0f;
        
        foreach (var light in activeLights)
        {
            if (!light.isPersistent)
            {
                float distance = Vector3.Distance(light.transform.position, mainCamera.transform.position);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthestLight = light;
                }
            }
        }
        
        if (farthestLight != null)
        {
            ReturnLightToPool(farthestLight);
        }
    }

    private void UpdateAmbientLighting()
    {
        if (dayNightGradient != null)
        {
            currentAmbientColor = dayNightGradient.Evaluate(currentTimeOfDay);
        }
        
        if (ambientIntensityCurve != null)
        {
            currentAmbientIntensity = ambientIntensityCurve.Evaluate(currentTimeOfDay);
        }
        
        // 更新所有2D光源的环境光
        RenderSettings.ambientLight = currentAmbientColor * currentAmbientIntensity;
        
        // 更新纹理环境光（如果仍在使用）
        if (enableAdvancedLighting && ambientLightMap != null)
        {
            UpdateAmbientLightMap();
        }
    }

    private void UpdateAmbientLightMap()
    {
        // 这里可以保留原有的纹理光照系统用于环境光
        // 但主要的动态光源使用Unity的2D Light
        if (terrainGen != null)
        {
            for (int x = 0; x < terrainGen.worldSize; x++)
            {
                for (int y = 0; y < terrainGen.worldSize; y++)
                {
                    if (SkyLightManager.IsPositionSkyLit(x, y))
                    {
                        ambientLightMap.SetPixel(x, y, currentAmbientColor * currentAmbientIntensity);
                    }
                    else
                    {
                        // 地下区域使用较暗的环境光
                        ambientLightMap.SetPixel(x, y, currentAmbientColor * currentAmbientIntensity * 0.1f);
                    }
                }
            }
            ambientLightMap.Apply();
        }
    }

    #endregion

    private void Update()
    {
        if (!enableAdvancedLighting) return;
        
        // 更新动态光源
        UpdateDynamicLights();
        
        // 视锥体剔除
        PerformLightCulling();
    }

    private void UpdateDynamicLights()
    {
        for (int i = activeLights.Count - 1; i >= 0; i--)
        {
            var light = activeLights[i];
            
            // 更新跟随目标
            if (light.followTransform != null)
            {
                light.transform.position = light.followTransform.position;
            }
            
            // 更新生命周期
            if (!light.isPersistent && light.lifetime > 0)
            {
                light.lifetime -= Time.deltaTime;
                
                // 淡出效果
                if (light.lifetime < 0.5f)
                {
                    light.light2D.intensity = Mathf.Lerp(0, light.light2D.intensity, light.lifetime / 0.5f);
                }
                
                if (light.lifetime <= 0)
                {
                    ReturnLightToPool(light);
                }
            }
        }
    }

    private void PerformLightCulling()
    {
        if (mainCamera == null) return;
        
        Vector3 cameraPos = mainCamera.transform.position;
        
        foreach (var light in activeLights)
        {
            float distance = Vector3.Distance(light.transform.position, cameraPos);
            light.lightObject.SetActive(distance <= lightCullingDistance);
        }
    }

    private void OnDestroy()
    {
        if (ambientLightMap != null)
            Destroy(ambientLightMap);
        
        if (dynamicLightRT != null)
            dynamicLightRT.Release();
    }
}

/// <summary>
/// 光源闪烁组件
/// </summary>
public class LightFlicker : MonoBehaviour
{
    public float baseIntensity = 1f;
    public float flickerAmount = 0.1f;
    public float flickerSpeed = 10f;
    
    private Light2D light2D;
    private float time;
    
    private void Start()
    {
        light2D = GetComponent<Light2D>();
    }
    
    private void Update()
    {
        if (light2D != null)
        {
            time += Time.deltaTime * flickerSpeed;
            float flicker = Mathf.PerlinNoise(time, 0) * flickerAmount;
            light2D.intensity = baseIntensity + flicker;
        }
    }
} 