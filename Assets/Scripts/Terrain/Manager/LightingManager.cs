using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public static class LightingManager
{
    // 光照更新队列，用于批量处理
    private static Queue<Vector3> lightUpdateQueue = new Queue<Vector3>(); // x, y, intensity
    private static bool isProcessingQueue = false;

    // 背景墙光照增强系数
    private static float wallLightBoost = 1.5f;

    /// <summary>
    /// 添加光照更新到队列中，支持批量处理
    /// </summary>
    public static void QueueLightUpdate(TerrainGeneration terrainGen, int x, int y, float intensity)
    {
        lightUpdateQueue.Enqueue(new Vector3(x, y, intensity));

        // 如果没有在处理队列，开始处理
        if (!isProcessingQueue)
        {
            terrainGen.StartCoroutine(ProcessLightQueue(terrainGen));
        }
    }

    /// <summary>
    /// 批量处理光照更新队列
    /// </summary>
    private static IEnumerator ProcessLightQueue(TerrainGeneration terrainGen)
    {
        isProcessingQueue = true;

        // 等待一帧，让更多的光照更新加入队列
        yield return null;

        int processedCount = 0;

        // 处理队列中的所有光照更新
        while (lightUpdateQueue.Count > 0)
        {
            Vector3 lightData = lightUpdateQueue.Dequeue();
            LightBlockInternal(terrainGen, (int)lightData.x, (int)lightData.y, lightData.z, 0);
            processedCount++;

            // 队列大小监控
            // if (lightUpdateQueue.Count % 10 == 0 && lightUpdateQueue.Count > 0)
            // {
            //     Debug.Log($"[光照系统] 队列处理中，剩余: {lightUpdateQueue.Count}");
            // }
        }

        // 批量应用纹理更新
        terrainGen.worldTilesMap.Apply();

        // 性能统计日志
        // if (processedCount > 0)
        // {
        //     Debug.Log($"[光照系统] 批量处理完成，更新了 {processedCount} 个光照点");
        // }

        isProcessingQueue = false;
    }

    /// <summary>
    /// 立即更新光照（用于初始化等场景）
    /// </summary>
    public static void LightBlock(TerrainGeneration terrainGen, int x, int y, float intensity, int iteration)
    {
        LightBlockInternal(terrainGen, x, y, intensity, iteration);
        terrainGen.worldTilesMap.Apply();
    }

    /// <summary>
    /// 内部光照计算方法，不调用Apply()
    /// </summary>
    private static void LightBlockInternal(TerrainGeneration terrainGen, int x, int y, float intensity, int iteration)
    {
        if (iteration < terrainGen.lightRadius)
        {
            // 获取当前位置的瓦片信息
            TileType tileType = terrainGen.GetTileType(x, y);
            Tile tile = terrainGen.GetCurrentBiomeTileFromType(tileType);

            // 计算最终光照强度
            float finalIntensity = intensity;

            // 如果是背景墙，应用光照增强
            if (tile != null && tile.inBackground)
            {
                finalIntensity *= wallLightBoost;
            }

            terrainGen.worldTilesMap.SetPixel(x, y, Color.white * finalIntensity);

            // 计算光照衰减阈值
            float thresh = terrainGen.groundLightThreshold;
            if (tile != null && tile.inBackground)
                thresh = terrainGen.airLightThreshold;

            // 向周围传播光照
            for (int dx = -1; dx <= 1; ++dx)
                for (int dy = -1; dy <= 1; ++dy)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx == x && ny == y) continue;
                    if (nx < 0 || nx >= terrainGen.worldSize || ny < 0 || ny >= terrainGen.worldSize) continue;

                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nx, ny));
                    float targetIntensity = Mathf.Pow(thresh, distance) * intensity;

                    if (terrainGen.worldTilesMap.GetPixel(nx, ny) != null &&
                        terrainGen.worldTilesMap.GetPixel(nx, ny).r < targetIntensity)
                    {
                        LightBlockInternal(terrainGen, nx, ny, targetIntensity, iteration + 1);
                    }
                }
        }
    }

    /// <summary>
    /// 移除光源并重新计算周围光照
    /// </summary>
    public static void RemoveLightSource(TerrainGeneration terrainGen, int x, int y)
    {
        terrainGen.unlitBlocks.Clear();
        UnlightBlockInternal(terrainGen, x, y, x, y);

        List<Vector2Int> toRelight = new List<Vector2Int>();
        foreach (Vector2Int block in terrainGen.unlitBlocks)
        {
            for (int dx = -1; dx <= 1; ++dx)
                for (int dy = -1; dy <= 1; ++dy)
                {
                    int nx = block.x + dx, ny = block.y + dy;
                    if (nx == block.x && ny == block.y) continue;
                    if (nx < 0 || nx >= terrainGen.worldSize || ny < 0 || ny >= terrainGen.worldSize) continue;
                    if (terrainGen.worldTilesMap.GetPixel(nx, ny) != null &&
                        terrainGen.worldTilesMap.GetPixel(nx, ny).r > terrainGen.worldTilesMap.GetPixel(block.x, block.y).r)
                    {
                        Vector2Int relightPos = new Vector2Int(nx, ny);
                        if (!toRelight.Contains(relightPos))
                            toRelight.Add(relightPos);
                    }
                }
        }

        // 批量重新照亮区域
        foreach (Vector2Int source in toRelight)
        {
            LightBlockInternal(terrainGen, source.x, source.y, terrainGen.worldTilesMap.GetPixel(source.x, source.y).r, 0);
        }

        // 批量应用纹理更新
        terrainGen.worldTilesMap.Apply();
    }

    /// <summary>
    /// 内部方法：移除指定位置的光照
    /// </summary>
    private static void UnlightBlockInternal(TerrainGeneration terrainGen, int x, int y, int ix, int iy)
    {
        Vector2Int currentPos = new Vector2Int(x, y);

        if (Mathf.Abs(x - ix) > terrainGen.lightRadius ||
            Mathf.Abs(y - iy) > terrainGen.lightRadius ||
            terrainGen.unlitBlocks.Contains(currentPos))
            return;

        for (int dx = -1; dx <= 1; ++dx)
            for (int dy = -1; dy <= 1; ++dy)
            {
                int nx = x + dx, ny = y + dy;
                if (nx == x && ny == y) continue;
                if (nx < 0 || nx >= terrainGen.worldSize || ny < 0 || ny >= terrainGen.worldSize) continue;
                if (terrainGen.worldTilesMap.GetPixel(nx, ny) != null &&
                    terrainGen.worldTilesMap.GetPixel(nx, ny).r < terrainGen.worldTilesMap.GetPixel(x, y).r)
                    UnlightBlockInternal(terrainGen, nx, ny, ix, iy);
            }
        terrainGen.worldTilesMap.SetPixel(x, y, Color.black);
        terrainGen.unlitBlocks.Add(currentPos);
    }

    /// <summary>
    /// 设置背景墙光照增强系数
    /// </summary>
    public static void SetWallLightBoost(float boost)
    {
        wallLightBoost = Mathf.Clamp(boost, 1.0f, 3.0f); // 限制在合理范围内
    }

    /// <summary>
    /// 获取当前背景墙光照增强系数
    /// </summary>
    public static float GetWallLightBoost()
    {
        return wallLightBoost;
    }

}