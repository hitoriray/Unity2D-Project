using UnityEngine;

[System.Serializable]
public class Biome
{
    public string biomeName;
    public Color biomeColor;
    public TileAtlas tileAtlas;

    // [Header("Noise Settings")]
    // public float terrainFreq = 0.05f;
    // public float caveFreq = 0.05f;
    // public Texture2D caveNoiseTexture;

    [Header("Generation Settings")]
    public int grassLayerHeight = 1;
    public bool generateCave = true;

    public int dirtLayerHeight = 10;
    public float surfaceValue = 0.2f;
    public float heightMultiplier = 15f;
    public int heightAddition = 25;

    [Header("Trees Config")]
    public int minTreeHeight = 5;
    public int maxTreeHeight = 20;
    public float minTreeDistance = 5f; // 最小树木间距
    public int minTreeBranchDistance = 3; // 树枝之间的最小距离
    public int maxTreeBranches = 3; // 每侧最大树枝数量

    [Header("Generation Chance")]
    [Range(0, 1)]
    public float treeChance = 0.1f;
    [Range(0, 1)]
    public float treeBranchChance = 0.4f;

    [Range(0, 1)]
    public float smallGrassChance = 0.2f;
    [Range(0, 1)]
    public float flowerChance = 0.1f;
    [Range(0, 1)]
    public float sunflowerChance = 0.1f;
    [Range(0, 1)]
    public float smallTreeChance = 0.08f;

    [Header("Ore Settings")]
    public Ore[] ores;
}
