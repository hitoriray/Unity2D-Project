using System.Collections.Generic;
using UnityEngine;

// 一个轻量级的数据结构，用来代表一个动态光点
public class LightPoint
{
    public Vector2 position;
    public float radius;
    public float lifetime;
    public float initialIntensity;
    public float age;

    public LightPoint(Vector2 pos, float rad, float life, float intensity)
    {
        position = pos;
        radius = rad;
        lifetime = life;
        initialIntensity = intensity;
        age = 0f;
    }

    public float GetCurrentIntensity()
    {
        return Mathf.Lerp(initialIntensity, 0f, age / lifetime);
    }
}

public class DynamicLightManager : MonoBehaviour
{
    public static DynamicLightManager Instance { get; private set; }

    [Tooltip("关联您场景中的TerrainGeneration脚本")]
    public TerrainGeneration terrainGenerator;

    private List<LightPoint> activeLights = new List<LightPoint>();
    
    // “脏区” - 记录所有需要被重新计算光照的像素
    private HashSet<Vector2Int> dirtyPixels = new HashSet<Vector2Int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// 公开接口，用于从外部（如幻影剑）添加一个新的动态光点
    /// </summary>
    public void AddLight(Vector2 position, float radius, float lifetime, float initialIntensity)
    {
        activeLights.Add(new LightPoint(position, radius, lifetime, initialIntensity));
    }

    private void LateUpdate()
    {
        if (terrainGenerator == null || terrainGenerator.worldTilesMap == null) return;
        
        // --- 阶段一：更新光源，并标记“脏区” ---
        
        // 1. 清理上一帧的过期光源，并将它们影响过的区域加入脏区，以便重绘
        for (int i = activeLights.Count - 1; i >= 0; i--)
        {
            var light = activeLights[i];
            light.age += Time.deltaTime;
            if (light.age >= light.lifetime)
            {
                MarkAreaAsDirty(light);
                activeLights.RemoveAt(i);
            }
        }

        // 2. 将当前所有激活光源的区域也加入脏区
        foreach (var light in activeLights)
        {
            MarkAreaAsDirty(light);
        }

        // --- 阶段二：对所有脏区的像素，进行一次最终光照计算 ---
        if (dirtyPixels.Count > 0)
        {
            foreach (var pos in dirtyPixels)
            {
                // a. 计算基础光照 (天空光)
                float baseLight = SkyLightManager.IsPositionSkyLit(pos.x, pos.y) ? 1.0f : 0.0f;

                // b. 计算所有动态光对该点的最大光照贡献
                float maxDynamicLight = 0f;
                foreach (var light in activeLights)
                {
                    float dist = Vector2.Distance(light.position, pos + new Vector2(0.5f, 0.5f));
                    if (dist < light.radius)
                    {
                        float falloff = 1 - (dist / light.radius);
                        float intensity = light.GetCurrentIntensity() * falloff;
                        if (intensity > maxDynamicLight)
                        {
                            maxDynamicLight = intensity;
                        }
                    }
                }

                // c. 取最大值作为最终亮度
                float finalIntensity = Mathf.Max(baseLight, maxDynamicLight);
                terrainGenerator.worldTilesMap.SetPixel(pos.x, pos.y, Color.white * finalIntensity);
            }

            // --- 阶段三：统一应用 ---
            terrainGenerator.worldTilesMap.Apply();
        }

        dirtyPixels.Clear();
    }

    /// <summary>
    /// 将一个光源的圆形影响区域内的所有像素，标记为“脏”
    /// </summary>
    private void MarkAreaAsDirty(LightPoint light)
    {
        int radius = Mathf.CeilToInt(light.radius);
        int centerX = Mathf.RoundToInt(light.position.x - 0.5f);
        int centerY = Mathf.RoundToInt(light.position.y - 0.5f);

        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (x < 0 || x >= terrainGenerator.worldSize || y < 0 || y >= terrainGenerator.worldSize) continue;
                if (Vector2.Distance(new Vector2(centerX, centerY), new Vector2(x, y)) <= radius)
                {
                    dirtyPixels.Add(new Vector2Int(x, y));
                }
            }
        }
    }
} 