using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 静态管理器，负责计算和追踪天空光。
/// 天空光是指从世界顶部垂直向下传播的光，直到遇到第一个不透明方块为止。
/// </summary>
public static class SkyLightManager
{
    // 使用一个布尔数组来存储每个瓦片是否暴露在天空光下。
    private static bool[,] isSkyLit;
    private static int worldSize;
    private static bool isInitialized = false;

    /// <summary>
    /// 初始化天空光系统。应在世界生成后调用一次。
    /// </summary>
    /// <param name="terrainGen">地形生成器的引用</param>
    public static void Initialize(TerrainGeneration terrainGen)
    {
        if (terrainGen == null)
        {
            Debug.LogError("[SkyLightManager] TerrainGeneration instance is null. Initialization failed.");
            return;
        }

        worldSize = terrainGen.worldSize;
        isSkyLit = new bool[worldSize, worldSize];

        // 从上到下扫描每一列，以确定天空光。
        for (int x = 0; x < worldSize; x++)
        {
            UpdateSkylightForColumn(x, terrainGen, worldSize - 1);
        }

        // 新增：初始化后，立即触发一次全局天空光传播
        PropagateInitialSkylight(terrainGen);

        isInitialized = true;
        // Debug.Log("[SkyLightManager] Sky light system initialized and initial light propagated.");
    }

    /// <summary>
    /// 当一个方块发生变化时，更新该列的天空光信息。
    /// </summary>
    /// <param name="x">变化的方块所在的X坐标</param>
    /// <param name="y">变化的方块所在的Y坐标</param>
    /// <param name="terrainGen">地形生成器的引用</param>
    public static void OnBlockChanged(int x, TerrainGeneration terrainGen)
    {
        if (!isInitialized) return;
        // 从该列的顶部开始重新计算
        UpdateSkylightForColumn(x, terrainGen, worldSize - 1);
    }

    /// <summary>
    /// 检查指定位置是否被天空光照亮。
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>如果位置暴露在天空光下，则为true</returns>
    public static bool IsPositionSkyLit(int x, int y)
    {
        if (!isInitialized || x < 0 || x >= worldSize || y < 0 || y >= worldSize)
        {
            return false;
        }
        return isSkyLit[x, y];
    }

    /// <summary>
    /// 重新计算单个列的天空光。
    /// </summary>
    /// <param name="x">要更新的列的X坐标</param>
    /// <param name="terrainGen">地形生成器的引用</param>
    /// <param name="startY">开始扫描的Y坐标</param>
    private static void UpdateSkylightForColumn(int x, TerrainGeneration terrainGen, int startY)
    {
        bool skyVisible = true;
        for (int y = startY; y >= 0; y--)
        {
            // 如果天空可见，当前块就是天空光照亮的
            isSkyLit[x, y] = skyVisible;

            // 检查当前块是否阻挡光线。
            // 注意：这里的 IsTileSolid 需要你在 TerrainGeneration 中实现，
            // 或者用你现有的逻辑替换。
            // 例如：TileType tileType = terrainGen.GetTileType(x, y);
            // bool isSolid = tileType != TileType.Air;
            if (skyVisible && IsTileSolid(terrainGen, x, y))
            {
                // 一旦遇到实体方块，它下面的所有方块都将不再被天空直接照射。
                skyVisible = false;
            }
        }
    }

    /// <summary>
    /// 辅助方法，用于确定一个瓦片是否为固体，从而阻挡天空光。
    /// 你需要根据你的项目来调整此逻辑。
    /// </summary>
    private static bool IsTileSolid(TerrainGeneration terrainGen, int x, int y)
    {
        TileType tileType = terrainGen.GetTileType(x, y);
        return tileType != TileType.Air && tileType != TileType.Tree &&
            tileType != TileType.Cactus && tileType != TileType.SmallGrass &&
            tileType != TileType.Flower && tileType != TileType.Wall;
    }

    /// <summary>
    /// 在初始化时，将所有天空光照亮的点作为光源，进行一次全局光照传播。
    /// </summary>
    private static void PropagateInitialSkylight(TerrainGeneration terrainGen)
    {
        // Debug.Log("[SkyLightManager] Starting initial skylight propagation...");
        // 遍历所有列
        for (int x = 0; x < worldSize; x++)
        {
            // 从上到下扫描
            for (int y = worldSize - 1; y >= 0; y--)
            {
                // 我们只需要从每列最上方的、暴露在天空下的非实体方块开始传播光即可。
                if (isSkyLit[x, y] && !IsTileSolid(terrainGen, x, y))
                {
                    // 一旦找到这个"表面"方块，就把它作为一个最高强度的光源，
                    // 让 LightingManager 的队列去处理光照传播。
                    LightingManager.QueueLightUpdate(terrainGen, x, y, 1.0f);
                    // 然后就可以跳到下一列了，因为光会从这个点向下传播。
                    break;
                }
            }
        }
        // Debug.Log("[SkyLightManager] Initial skylight propagation queued for processing.");
    }
} 