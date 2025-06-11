using UnityEngine;
using System.Collections.Generic;

public static class LightingManager
{
    public static void LightBlock(TerrainGeneration terrainGen, int x, int y, float intensity, int iteration)
    {
        if (iteration < terrainGen.lightRadius)
        {
            terrainGen.worldTilesMap.SetPixel(x, y, Color.white * intensity);
            float thresh = terrainGen.groundLightThreshold;
            Tile tile = terrainGen.GetCurrentBiomeTileFromType(terrainGen.GetTileType(x, y));
            if (tile != null && tile.inBackground)
                thresh = terrainGen.airLightThreshold;
            for (int dx = -1; dx <= 1; ++dx)
                for (int dy = -1; dy <= 1; ++dy)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx == x && ny == y) continue;
                    if (nx < 0 || nx >= terrainGen.worldSize || ny < 0 || ny >= terrainGen.worldSize) continue;
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nx, ny));
                    float targetIntensity = Mathf.Pow(thresh, distance) * intensity;
                    if (terrainGen.worldTilesMap.GetPixel(nx, ny) != null && terrainGen.worldTilesMap.GetPixel(nx, ny).r < targetIntensity)
                        LightBlock(terrainGen, nx, ny, targetIntensity, iteration + 1);
                }
            terrainGen.worldTilesMap.Apply();
        }
    }

    public static void RemoveLightSource(TerrainGeneration terrainGen, int x, int y)
    {
        terrainGen.unlitBlocks.Clear();
        UnlightBlock(terrainGen, x, y, x, y);

        List<Vector2Int> toRelight = new();
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
                        if (!toRelight.Contains(new Vector2Int(nx, ny)))
                            toRelight.Add(new Vector2Int(nx, ny));
                    }
                }
        }

        foreach (Vector2Int source in toRelight)
            LightBlock(terrainGen, source.x, source.y, terrainGen.worldTilesMap.GetPixel(source.x, source.y).r, 0);

        terrainGen.worldTilesMap.Apply();
    }

    public static void UnlightBlock(TerrainGeneration terrainGen, int x, int y, int ix, int iy)
    {
        if (Mathf.Abs(x - ix) > terrainGen.lightRadius || 
            Mathf.Abs(y - iy) > terrainGen.lightRadius || terrainGen.unlitBlocks.Contains(new Vector2Int(x, y)))
            return;

        for (int dx = -1; dx <= 1; ++dx)
            for (int dy = -1; dy <= 1; ++dy)
            {
                int nx = x + dx, ny = y + dy;
                if (nx == x && ny == y) continue;
                if (nx < 0 || nx >= terrainGen.worldSize || ny < 0 || ny >= terrainGen.worldSize) continue;
                if (terrainGen.worldTilesMap.GetPixel(nx, ny) != null &&
                    terrainGen.worldTilesMap.GetPixel(nx, ny).r < terrainGen.worldTilesMap.GetPixel(x, y).r)
                    UnlightBlock(terrainGen, nx, ny, ix, iy);
            }
        terrainGen.worldTilesMap.SetPixel(x, y, Color.black);
        terrainGen.unlitBlocks.Add(new Vector2Int(x, y));
    }
}