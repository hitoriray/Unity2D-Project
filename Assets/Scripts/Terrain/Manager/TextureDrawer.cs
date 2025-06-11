using UnityEngine;

public static class TextureDrawer
{
    public static void DrawBiomeMap(TerrainGeneration terrainGen)
    {
        terrainGen.biomeMap = new Texture2D(terrainGen.worldSize, terrainGen.worldSize);
        float b;
        Color col;
        for (int x = 0; x < terrainGen.biomeMap.width; ++x)
        {
            for (int y = 0; y < terrainGen.biomeMap.height; ++y)
            {
                b = Mathf.PerlinNoise((x + terrainGen.seed) * terrainGen.biomeFreq, (y + terrainGen.seed) * terrainGen.biomeFreq);
                col = terrainGen.biomeGradient.Evaluate(b);
                terrainGen.biomeMap.SetPixel(x, y, col);
            }
        }
        terrainGen.biomeMap.Apply();
    }

    public static void DrawCavesAndOres(TerrainGeneration terrainGen)
    {
        terrainGen.caveNoiseTexture = new Texture2D(terrainGen.worldSize, terrainGen.worldSize);
        float v, o;
        for (int x = 0; x < terrainGen.worldSize; ++x)
            for (int y = 0; y < terrainGen.worldSize; ++y)
            {
                v = Mathf.PerlinNoise((x + terrainGen.seed) * terrainGen.caveFreq, (y + terrainGen.seed) * terrainGen.caveFreq);
                if (v > terrainGen.GetCurrentBiome(x, y).surfaceValue)
                    terrainGen.caveNoiseTexture.SetPixel(x, y, Color.white);
                else
                    terrainGen.caveNoiseTexture.SetPixel(x, y, Color.black);

                for (int i = 0; i < terrainGen.ores.Length; ++i)
                {
                    terrainGen.ores[i].spreadTexture.SetPixel(x, y, Color.black);
                    Biome curBiome = terrainGen.GetCurrentBiome(x, y);
                    if (curBiome.ores.Length > i)
                    {
                        o = Mathf.PerlinNoise((x + terrainGen.seed) * curBiome.ores[i].frequency, (y + terrainGen.seed) * curBiome.ores[i].frequency);
                        if (o > curBiome.ores[i].size)
                            terrainGen.ores[i].spreadTexture.SetPixel(x, y, Color.white);
                    }
                    terrainGen.ores[i].spreadTexture.Apply();
                }
            }
        terrainGen.caveNoiseTexture.Apply();
    }
}