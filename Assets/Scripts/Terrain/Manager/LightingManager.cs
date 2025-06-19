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
            // 队列处理的光照通常使用地形的默认光照半径
            LightBlockInternal(terrainGen, (int)lightData.x, (int)lightData.y, lightData.z, 0, (int)terrainGen.lightRadius, true); // 显式传递 applyWallBoost = true
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
    /// 当一个方块被放置或破坏时，更新其光照。
    /// 这是处理方块变化时光照的主要入口点。
    /// </summary>
    public static void UpdateBlockLighting(TerrainGeneration terrainGen, int x, int y)
    {
        // 首先，根据天空光设置方块的基础亮度
        if (SkyLightManager.IsPositionSkyLit(x, y))
        {
            terrainGen.worldTilesMap.SetPixel(x, y, Color.white);
        }
        else
        {
            terrainGen.worldTilesMap.SetPixel(x, y, Color.black);
        }

        // 然后，从邻近的更亮的方块传播光线到这个方块
        for (int dx = -1; dx <= 1; ++dx)
        {
            for (int dy = -1; dy <= 1; ++dy)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx < 0 || nx >= terrainGen.worldSize || ny < 0 || ny >= terrainGen.worldSize) continue;

                // 如果邻居比当前方块亮，就从邻居那里开始重新传播光
                if (terrainGen.worldTilesMap.GetPixel(nx, ny).r > terrainGen.worldTilesMap.GetPixel(x, y).r)
                {
                    float neighborIntensity = terrainGen.worldTilesMap.GetPixel(nx, ny).r;
                    LightBlockInternal(terrainGen, nx, ny, neighborIntensity, 0, (int)terrainGen.lightRadius, true);
                }
            }
        }

        // 最后，移除当前位置可能存在的旧光照，并重新计算周围
        RemoveLightSource(terrainGen, x, y);
        // terrainGen.worldTilesMap.Apply(); // 确保这行被注释或删除
    }

    /// <summary>
    /// UpdateBlockLighting的无Apply版本，用于批量处理。
    /// </summary>
    public static void UpdateBlockLighting_NoApply(TerrainGeneration terrainGen, int x, int y)
    {
        // 首先，根据天空光设置方块的基础亮度
        if (SkyLightManager.IsPositionSkyLit(x, y))
        {
            terrainGen.worldTilesMap.SetPixel(x, y, Color.white);
        }
        else
        {
            terrainGen.worldTilesMap.SetPixel(x, y, Color.black);
        }

        // 然后，从邻近的更亮的方块传播光线到这个方块
        for (int dx = -1; dx <= 1; ++dx)
        {
            for (int dy = -1; dy <= 1; ++dy)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx < 0 || nx >= terrainGen.worldSize || ny < 0 || ny >= terrainGen.worldSize) continue;

                // 如果邻居比当前方块亮，就从邻居那里开始重新传播光
                if (terrainGen.worldTilesMap.GetPixel(nx, ny).r > terrainGen.worldTilesMap.GetPixel(x, y).r)
                {
                    float neighborIntensity = terrainGen.worldTilesMap.GetPixel(nx, ny).r;
                    LightBlockInternal(terrainGen, nx, ny, neighborIntensity, 0, (int)terrainGen.lightRadius, true);
                }
            }
        }

        // 最后，移除当前位置可能存在的旧光照，并重新计算周围
        RemoveLightSource(terrainGen, x, y);
        // terrainGen.worldTilesMap.Apply(); // 确保这行被注释或删除
    }

    /// <summary>
    /// 立即更新光照（用于初始化等场景），使用地形的默认光照半径。
    /// </summary>
    public static void LightBlock(TerrainGeneration terrainGen, int x, int y, float intensity, int iteration)
    {
        LightBlockInternal(terrainGen, x, y, intensity, iteration, (int)terrainGen.lightRadius, true); // 显式传递 applyWallBoost = true
        terrainGen.worldTilesMap.Apply();
    }

    /// <summary>
    /// 内部光照计算方法，不调用Apply()
    /// </summary>
    /// <param name="maxIterationDepth">光照传播的最大迭代深度 (实际半径效果)</param>
    /// <param name="applyWallBoost">是否应用墙壁光照增强</param>
    private static void LightBlockInternal(TerrainGeneration terrainGen, int x, int y, float intensity, int iteration, int maxIterationDepth, bool applyWallBoost = true)
    {
        if (terrainGen == null) return; // 安全检查
        if (iteration < maxIterationDepth) // 使用传入的最大迭代深度
        {
            // 获取当前位置的瓦片信息
            TileType tileType = terrainGen.GetTileType(x, y);
            Tile tile = terrainGen.GetCurrentBiomeTileFromType(tileType);

            // 计算最终光照强度
            float finalIntensity = intensity;

            // 如果是背景墙，并且允许应用增强，则应用光照增强
            if (applyWallBoost && tile != null && tile.inBackground)
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
                        LightBlockInternal(terrainGen, nx, ny, targetIntensity, iteration + 1, maxIterationDepth, applyWallBoost); // 传递 applyWallBoost
                    }
                }
        }
    }

    /// <summary>
    /// 移除光源并重新计算周围光照
    /// </summary>
    /// <param name="specificRadiusToClear">如果提供，则使用此半径进行反照，否则使用terrainGen.lightRadius</param>
    public static void RemoveLightSource(TerrainGeneration terrainGen, int x, int y, int? specificRadiusToClear = null)
    {
        int radiusToUse = specificRadiusToClear ?? (int)terrainGen.lightRadius;
        int effectiveClearRadius = radiusToUse + 1; // 稍微扩大清除半径以确保覆盖
        // Debug.Log($"[LightingManager] RemoveLightSource: Called for ({x},{y}). Nominal radius: {radiusToUse}, Effective clear radius: {effectiveClearRadius}.");

        if (terrainGen == null)
        {
            Debug.LogError($"[LightingManager] RemoveLightSource: terrainGen is null for ({x},{y}). Cannot remove light.");
            return;
        }
        terrainGen.unlitBlocks.Clear();
        // 直接迭代受影响的区域并将其设置为基础未照亮状态，使用 effectiveClearRadius
        for (int ix = x - effectiveClearRadius; ix <= x + effectiveClearRadius; ix++)
        {
            for (int iy = y - effectiveClearRadius; iy <= y + effectiveClearRadius; iy++)
            {
                if (ix < 0 || ix >= terrainGen.worldSize || iy < 0 || iy >= terrainGen.worldSize) continue;

                if (Vector2.Distance(new Vector2(x, y), new Vector2(ix, iy)) <= effectiveClearRadius)
                {
                    Vector2Int currentPos = new Vector2Int(ix, iy);

                    // 核心修改：在决定方块的"自然"光照状态时，优先考虑天空光
                    if (SkyLightManager.IsPositionSkyLit(ix, iy))
                    {
                        // 如果方块暴露在天空下，无论它是什么类型（空气、实体、背景墙），
                        // 在移除人造光源后，它都应该恢复到最亮的白色。
                        terrainGen.worldTilesMap.SetPixel(ix, iy, Color.white);
                    }
                    else
                    {
                        // 对于地下（不被天空光照射）的方块，使用旧的逻辑
                        // 注意：即使是地下的纯空气，也应该先变黑，然后由周围的光源重新照亮
                        terrainGen.worldTilesMap.SetPixel(ix, iy, Color.black);
                    }

                    // 确保添加到 unlitBlocks 列表中，以便后续的 relight 逻辑可以处理边界
                    if (!terrainGen.unlitBlocks.Contains(currentPos))
                    {
                        terrainGen.unlitBlocks.Add(currentPos);
                    }
                }
            }
        }

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
            float sourceIntensity = terrainGen.worldTilesMap.GetPixel(source.x, source.y).r;
            // 仅当邻近的源光照强度足够高时（表明它可能是一个持久光源），才进行重新光照。
            if (sourceIntensity > 0.25f)
            {
                // 【修正】使用地形的默认光照半径进行重新传播，而不是硬编码的 1。
                // 这允许光线从侧面完全填充被遮挡的列。
                LightBlockInternal(terrainGen, source.x, source.y, sourceIntensity, 0, (int)terrainGen.lightRadius, false);
            }
        }

        // 批量应用纹理更新
        terrainGen.worldTilesMap.Apply();
    }

    /// <summary>
    /// 内部方法：移除指定位置的光照
    /// </summary>
    /// <param name="maxUnlightRadius">从此光源原点开始取消光照的最大迭代深度/半径</param>
    private static void UnlightBlockInternal(TerrainGeneration terrainGen, int x, int y, int originX, int originY, int maxUnlightRadius)
    {
        if (terrainGen == null) return; // 安全检查
        Vector2Int currentPos = new Vector2Int(x, y);

        // 使用 maxUnlightRadius 进行边界检查
        if (Mathf.Abs(x - originX) > maxUnlightRadius ||
            Mathf.Abs(y - originY) > maxUnlightRadius ||
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
                    UnlightBlockInternal(terrainGen, nx, ny, originX, originY, maxUnlightRadius); // 传递 maxUnlightRadius
            }
        
        // 核心修改：同样，在这里应用天空光逻辑
        if (SkyLightManager.IsPositionSkyLit(x, y))
        {
            // 天空光下的方块总是亮的
            terrainGen.worldTilesMap.SetPixel(x, y, Color.white);
        }
        else
        {
            // 地下逻辑保持不变
            TileType currentTileType = terrainGen.GetTileType(x,y);
            if (currentTileType == TileType.Air && !terrainGen.IsWallAt(x, y))
            {
                terrainGen.worldTilesMap.SetPixel(x, y, Color.white);
            }
            else 
            {
                terrainGen.worldTilesMap.SetPixel(x, y, Color.black);
            }
        }
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

    /// <summary>
    /// 创建一个临时的脉冲光照效果。
    /// </summary>
    /// <param name="coroutineRunner">用于启动协程的 MonoBehaviour 实例。</param>
    /// <param name="terrainGen">TerrainGeneration 实例的引用。</param>
    /// <param name="x">光源中心的X坐标。</param>
    /// <param name="y">光源中心的Y坐标。</param>
    /// <param name="intensity">光照强度。</param>
    /// <param name="radius">光照半径（注意：这里的radius可能需要适配LightBlockInternal的迭代次数逻辑）。</param>
    /// <param name="duration">光照持续时间（秒）。</param>
    public static void PulseTemporaryLight(MonoBehaviour coroutineRunner, TerrainGeneration terrainGen, int x, int y, float intensity, int radius, float duration)
    {
        if (terrainGen == null || coroutineRunner == null)
        {
            Debug.LogError($"[LightingManager] PulseTemporaryLight: terrainGen ({(terrainGen == null ? "null" : "valid")}) or coroutineRunner ({(coroutineRunner == null ? "null" : "valid")}) is null. Cannot pulse light at ({x},{y}).");
            return;
        }

        // Debug.Log($"[LightingManager] PulseTemporaryLight: Pulsing light at ({x},{y}) with Intensity: {intensity}, Radius: {radius}, Duration: {duration}. Runner: {coroutineRunner.gameObject.name}");

        // 立即应用光照 - 注意：LightBlockInternal的iteration参数与radius的直接关系需要明确。
        // LightBlockInternal的第三个参数是intensity，第四个是iteration。
        // 我们需要一种方式将 'radius' (格子数) 转换为 'iteration' (递归深度)。
        // 简单处理：直接使用传入的radius作为迭代次数上限，或者修改LightBlockInternal。
        // 为简化，我们假设LightBlockInternal的迭代次数与我们期望的半径有一定关联，
        // 或者我们可以在LightBlockInternal内部根据intensity和衰减阈值来控制实际传播范围。
        // 这里我们暂时不直接使用传入的radius去限制迭代，而是依赖LightBlockInternal的现有逻辑。
        // 真正的半径控制应该在LightBlockInternal中基于衰减实现。
        // 或者，我们可以修改LightBlockInternal使其接受一个明确的半径参数。
        // 为了不大幅修改现有光照逻辑，我们先用现有方式照亮，然后定时移除。
        // 立即照亮该区域
        // 现在 LightBlockInternal 的最后一个参数是 maxIterationDepth，我们传入从武器配置中得到的 radius
        LightBlockInternal(terrainGen, x, y, intensity, 0, radius, true); // 显式传递 applyWallBoost = true
        terrainGen.worldTilesMap.Apply(); // 确保立即应用
        // Debug.Log($"[LightingManager] PulseTemporaryLight: Initial light applied at ({x},{y}). Starting RemoveTemporaryLightCoroutine with duration {duration}s on TerrainGeneration object, pulseRadius: {radius}.");


        // 启动协程以在持续时间结束后移除光照
        terrainGen.StartCoroutine(RemoveTemporaryLightCoroutine(terrainGen, x, y, duration, radius)); // 传递原始脉冲半径
    }

    private static IEnumerator RemoveTemporaryLightCoroutine(TerrainGeneration terrainGen, int x, int y, float duration, int originalPulseRadius)
    {
        // Debug.Log($"[LightingManager] RemoveTemporaryLightCoroutine: Started for ({x},{y}) with originalPulseRadius: {originalPulseRadius}. Waiting for {duration}s.");
        yield return new WaitForSeconds(duration);
        // Debug.Log($"[LightingManager] RemoveTemporaryLightCoroutine: Wait finished for ({x},{y}). Calling RemoveLightSource with specific radius {originalPulseRadius}.");
        RemoveLightSource(terrainGen, x, y, originalPulseRadius); // 将原始脉冲半径传递给RemoveLightSource
    }
}