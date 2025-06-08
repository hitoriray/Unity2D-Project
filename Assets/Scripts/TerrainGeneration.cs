using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TerrainGeneration : MonoBehaviour
{
    [Header("Lighting")]
    public Texture2D worldTilesMap;
    public Material lightShader;
    public float lightThreshold;
    public float lightRadius;
    public HashSet<Vector2Int> unlitBlocks = new();

    [Header("Player")]
    public PlayerController player;
    public CameraController cameraController;
    public GameObject item_TileDrop;

    [Header("Tile Atlas")]
    public float seed;
    public Biome[] biomes;

    [Header("Biomes")]
    public float biomeFreq;
    public Gradient biomeGradient;
    public Texture2D biomeMap;

    [Header("Generation Settings")]
    public float terrainFreq = 0.05f;
    public float caveFreq = 0.05f;
    public int chunkSize = 16;
    public int worldSize = 100;
    public Texture2D caveNoiseTexture;

    [Header("Ores Settings")]
    public Ore[] ores;

    private readonly List<Vector2> worldTiles = new();
    private readonly List<GameObject> worldTileObjects = new();
    private readonly List<Vector2> worldWalls = new();
    private readonly List<GameObject> worldWallObjects = new();

    private Dictionary<Vector2, TileInfo> worldTileInfo = new();
    private float lastTreeX = -Mathf.Infinity;
    private TileType[,] terrainMap;
    private Dictionary<int, int> tileBitmaskMap;
    private GameObject[] worldChunks;
    private Biome curBiome;
    private Color[] biomeColors;
    private Dictionary<int, Biome> biomeHashMap;

    private void Start()
    {
        // initialize light
        worldTilesMap = new Texture2D(worldSize, worldSize);
        worldTilesMap.filterMode = FilterMode.Bilinear;
        lightShader.SetTexture("_ShadowTex", worldTilesMap);
        for (int x = 0; x < worldSize; ++x)
            for (int y = 0; y < worldSize; ++y)
                worldTilesMap.SetPixel(x, y, Color.white);
        worldTilesMap.Apply();

        // generate terrain stuff
        seed = Random.Range(-10000, 10000);

        for (int i = 0; i < ores.Length; ++i)
            ores[i].spreadTexture = new Texture2D(worldSize, worldSize);

        biomeColors = new Color[biomes.Length];
        for (int i = 0; i < biomes.Length; ++i)
            biomeColors[i] = biomes[i].biomeColor;


        InitializeTileBitmaskMap();
        InitializeBiomeHashMap();

        DrawBiomeMap();
        DrawCavesAndOres();

        CreateChunks();
        GenerateTerrain();

        for (int x = 0; x < worldSize; ++x)
            for (int y = 0; y < worldSize; ++y)
                if (worldTilesMap.GetPixel(x, y) == Color.white)
                    LightBlock(x, y, 1f, 0);
        worldTilesMap.Apply();

        cameraController.Spawn(new Vector3(player.spawnPos.x, player.spawnPos.y, cameraController.transform.position.z));
        cameraController.worldSize = worldSize;
        player.Spawn();
    }

    public void DrawBiomeMap()
    {
        biomeMap = new Texture2D(worldSize, worldSize);
        float b;
        Color col;
        for (int x = 0; x < biomeMap.width; ++x)
        {
            for (int y = 0; y < biomeMap.height; ++y)
            {
                b = Mathf.PerlinNoise((x + seed) * biomeFreq, (y + seed) * biomeFreq);
                col = biomeGradient.Evaluate(b);
                biomeMap.SetPixel(x, y, col);
            }
        }
        biomeMap.Apply();
    }

    public void DrawCavesAndOres()
    {
        caveNoiseTexture = new Texture2D(worldSize, worldSize);
        float v, o;
        for (int x = 0; x < worldSize; ++x)
            for (int y = 0; y < worldSize; ++y)
            {
                UpdateCurrentBiome(x, y);
                v = Mathf.PerlinNoise((x + seed) * caveFreq, (y + seed) * caveFreq);
                if (v > curBiome.surfaceValue)
                    caveNoiseTexture.SetPixel(x, y, Color.white);
                else
                    caveNoiseTexture.SetPixel(x, y, Color.black);

                for (int i = 0; i < ores.Length; ++i)
                {
                    ores[i].spreadTexture.SetPixel(x, y, Color.black);
                    if (curBiome.ores.Length > i)
                    {
                        o = Mathf.PerlinNoise((x + seed) * curBiome.ores[i].frequency, (y + seed) * curBiome.ores[i].frequency);
                        if (o > curBiome.ores[i].size)
                            ores[i].spreadTexture.SetPixel(x, y, Color.white);
                    }
                    ores[i].spreadTexture.Apply();
                }
            }
        caveNoiseTexture.Apply();
    }

    public void CreateChunks()
    {
        // 确保即使 worldSize 不能被 chunkSize 整除，也能创建足够的区块
        int numChunks = Mathf.CeilToInt((float)worldSize / chunkSize);
        worldChunks = new GameObject[numChunks];
        for (int i = 0; i < numChunks; ++i)
        {
            GameObject newChunk = new()
            {
                name = i.ToString()
            };
            newChunk.transform.parent = this.transform;
            worldChunks[i] = newChunk;
        }
    }

    private void Update()
    {
        UpdateChunks();
    }

    public void UpdateChunks()
    {
        if (Camera.main == null || player == null) return;
        Camera cam = Camera.main;
        float cameraHalfWidth = cam.orthographicSize * cam.aspect;
        Vector3 cameraPos = cam.transform.position;
        for (int i = 0; i < worldChunks.Length; ++i)
        {
            float chunkCenterX = i * chunkSize + chunkSize * 0.5f;
            float distanceX = Mathf.Abs(chunkCenterX - cameraPos.x);
            // 添加一些缓冲区域，避免频繁的开关
            float bufferDistance = chunkSize * 0.5f;
            // 如果chunk在相机视野范围内（加上缓冲区），则激活
            bool shouldBeActive = distanceX <= cameraHalfWidth + bufferDistance;
            if (worldChunks[i].activeSelf != shouldBeActive)
                worldChunks[i].SetActive(shouldBeActive);
        }
    }

    void InitializeTileBitmaskMap()
    {
        tileBitmaskMap = new Dictionary<int, int>
        {
            // 上方: 1
            // 下方: 2
            // 左方: 4
            // 右方: 8
            // 左上方 (内角): 16 (仅当 上和左 同时存在时激活)
            // 右上方 (内角): 32 (仅当 上和右 同时存在时激活)
            // 左下方 (内角): 64 (仅当 下和左 同时存在时激活)
            // 右下方 (内角): 128 (仅当 下和右 同时存在时激活)

            // **情况：孤立瓦片 (周围都没有相同类型)**
            // Bitmask: 0
            [0] = 0, // 假设 dirtTiles[0] 是孤立瓦片 Sprite

            // **情况：单边连接**
            [1] = 1, // 朝下
            [2] = 2, // 朝上
            [4] = 3, // 朝右
            [8] = 4, // 朝左

            // **情况：两边连接 (相对)**
            [3] = 5, // 垂直
            [12] = 6, // 水平

            // **情况：两边连接 (相邻 - 外角/凸角)**
            [5] = 7, // 朝右下
            [9] = 8, // 朝左下
            [6] = 9, // 朝右上
            [10] = 10, // 朝左上

            // **情况：三边连接 (U 型开口)**
            [13] = 11, // 朝下开口
            [14] = 12, // 朝上开口
            [7] = 13, // 朝左开口
            [11] = 14, // 朝右开口

            // **情况：四边连接 (中间瓦片 - 仅考虑主方向)**
            [15] = 15, // 假设 dirtTiles[15] 是四面连接的中间 Sprite

            // TODO: **情况：包含对角线的内角 (需要对应的 Sprite 支持)**
            // Bitmask: 21 (上、左、左上内角) (1 Up + 4 Left + 16 UpLeft)
            // [21] = 16, // 假设 dirtTiles[16] 是左上内角 Sprite
            // Bitmask: 41 (上、右、右上内角) (1 Up + 8 Right + 32 UpRight)
            // [41] = 17, // 假设 dirtTiles[17] 是右上内角 Sprite
            // Bitmask: 70 (下、左、左下内角) (2 Down + 4 Left + 64 DownLeft)
            // [70] = 18, // 假设 dirtTiles[18] 是左下内角 Sprite
            // Bitmask: 138 (下、右、右下内角) (2 Down + 8 Right + 128 DownRight)
            // [138] = 19, // 假设 dirtTiles[19] 是右下内角 Sprite

            // // **情况：全连接 (所有8个方向，包括所有内角填充)**
            // Bitmask: 255 (1+2+4+8+16+32+64+128)
            // [255] = 20 // 假设 dirtTiles[20] 是全连接的 Sprite (可能与 dirtTiles[15] 相同或更复杂)
        };
        // 目前index在15及以上的都是中间的tile
    }

    public void UpdateCurrentBiome(int x, int y)
    {
        Color32 pixelColor = biomeMap.GetPixel(x, y);
        int colorHash = (pixelColor.r << 24) | (pixelColor.g << 16) | (pixelColor.b << 8) | pixelColor.a;
        if (biomeHashMap.TryGetValue(colorHash, out Biome biome))
            curBiome = biome;
    }

    public TileType GetTileType(int x, int y)
    {
        if (x < 0 || x >= worldSize || y < 0 || y >= worldSize)
            return TileType.Air;
        return terrainMap[x, y];
    }

    public Biome GetCurrentBiome(int x, int y)
    {
        UpdateCurrentBiome(x, y);
        return curBiome;
    }

    private void InitializeBiomeHashMap()
    {
        biomeHashMap = new Dictionary<int, Biome>();
        foreach (Biome biome in biomes)
        {
            Color32 color32 = biome.biomeColor;
            int colorHash = (color32.r << 24) | (color32.g << 16) | (color32.b << 8) | color32.a;
            biomeHashMap[colorHash] = biome;
        }
    }

    private void GenerateTerrain()
    {
        terrainMap = new TileType[worldSize, worldSize]; // 初始化地形图
        for (int x = 0; x < worldSize; ++x)
        {
            float height;
            for (int y = 0; y < worldSize; ++y)
            {
                UpdateCurrentBiome(x, y);
                height = Mathf.PerlinNoise((x + seed) * terrainFreq, seed * terrainFreq) * curBiome.heightMultiplier + curBiome.heightAddition;
                if (player.spawnPos == Vector2.zero && x == worldSize / 2)
                {
                    player.spawnPos.Set(x, height + 5);
                }
                if (y >= height)
                    break;

                TileType tileType = TileType.Air; // 默认为空：Air
                if (y < height - curBiome.dirtLayerHeight)
                {
                    if (curBiome.biomeName == "forest") tileType = TileType.Dirt; // 森林没有石头
                    else tileType = TileType.Stone;

                    if (curBiome.ores[0].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > curBiome.ores[0].maxSpawnHeight)
                        tileType = TileType.Copper;
                    if (curBiome.ores[1].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > curBiome.ores[1].maxSpawnHeight)
                        tileType = TileType.Iron;
                    if (curBiome.ores[2].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > curBiome.ores[2].maxSpawnHeight)
                        tileType = TileType.Gold;
                    if (curBiome.ores[3].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > curBiome.ores[3].maxSpawnHeight)
                        tileType = TileType.Ruby;
                    if (curBiome.ores[4].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > curBiome.ores[4].maxSpawnHeight)
                        tileType = TileType.Emerald;
                    if (curBiome.ores[5].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > curBiome.ores[5].maxSpawnHeight)
                        tileType = TileType.Sapphire;
                }
                else if (y < height - curBiome.grassLayerHeight)
                    tileType = TileType.Dirt;
                else if (y < height)
                    tileType = TileType.Grass;

                if (curBiome.generateCave)
                {
                    if (caveNoiseTexture.GetPixel(x, y).r > curBiome.surfaceValue)
                        terrainMap[x, y] = tileType;
                    else
                        terrainMap[x, y] = TileType.Wall;
                }
                else
                    terrainMap[x, y] = tileType;
            }
        }

        // 根据地形图和邻居信息放置瓦片
        for (int x = 0; x < worldSize; ++x)
            for (int y = 0; y < worldSize; ++y)
            {
                UpdateCurrentBiome(x, y);
                if (terrainMap[x, y] != TileType.Air)
                {
                    TileType currentTileType = terrainMap[x, y];
                    Sprite finalTileSprite = GetCorrectTileSprite(x, y, currentTileType);
                    if (finalTileSprite != null)
                    {
                        if (currentTileType != TileType.Grass)
                            GenerateTile(GetCorrectTileSprite(x, y, TileType.Wall), x, y, true, "Wall", TileType.Wall, false);
                        if (currentTileType != TileType.Wall)
                            GenerateTile(finalTileSprite, x, y, false, "Ground", currentTileType, false);
                    }
                }

                // 只有在泥土和草地的表面才生成花草树木
                if ((terrainMap[x, y] == TileType.Grass/* || terrainMap[x, y] == TileType.Dirt*/) &&
                    (y + 1 < worldSize && terrainMap[x, y + 1] == TileType.Air)) // 确保 y+1 不越界
                {
                    float randomPick = Random.value;
                    float cumulativeProbability = 0f;

                    // 1. 小草
                    if (curBiome.biomeName == "grassland" || curBiome.biomeName == "forest")
                    {
                        cumulativeProbability += curBiome.smallGrassChance;
                        if (randomPick < cumulativeProbability)
                        {
                            if (curBiome.tileAtlas.smallGrass != null && curBiome.tileAtlas.smallGrass.tileSprites != null && curBiome.tileAtlas.smallGrass.tileSprites.Length > 0)
                            {
                                GenerateTile(curBiome.tileAtlas.smallGrass.tileSprites[Random.Range(0, curBiome.tileAtlas.smallGrass.tileSprites.Length)], x, y + 1, curBiome.tileAtlas.smallGrass.inBackground, "Plant", TileType.SmallGrass, false);
                                SetTerrainMap(x, y + 1, TileType.SmallGrass);
                            }
                            continue;
                        }
                    }

                    // 2. 花
                    cumulativeProbability += curBiome.flowerChance;
                    if (randomPick < cumulativeProbability)
                    {
                        if (curBiome.tileAtlas.flower != null && curBiome.tileAtlas.flower.tileSprites != null && curBiome.tileAtlas.flower.tileSprites.Length > 0)
                        {
                            GenerateTile(curBiome.tileAtlas.flower.tileSprites[Random.Range(0, curBiome.tileAtlas.flower.tileSprites.Length)], x, y + 1, curBiome.tileAtlas.flower.inBackground, "Plant");
                            SetTerrainMap(x, y + 1, TileType.SmallGrass);
                        }
                        continue;
                    }

                    // 3. 向日葵
                    if (curBiome.biomeName == "grassland")
                    {
                        cumulativeProbability += curBiome.sunflowerChance;
                        if (randomPick < cumulativeProbability)
                        {
                            if (curBiome.tileAtlas.sunflower != null && curBiome.tileAtlas.sunflower.tileSprites != null && curBiome.tileAtlas.sunflower.tileSprites.Length > 0)
                            {
                                GenerateTile(curBiome.tileAtlas.sunflower.tileSprites[Random.Range(0, curBiome.tileAtlas.sunflower.tileSprites.Length)], x, y + 2, curBiome.tileAtlas.sunflower.inBackground, "Plant");
                                SetTerrainMap(x, y + 2, TileType.Flower);
                            }
                            continue;
                        }
                    }

                    // 4. 小树
                    cumulativeProbability += curBiome.smallTreeChance;
                    if (randomPick < cumulativeProbability)
                    {
                        if (curBiome.tileAtlas.smallTree != null && curBiome.tileAtlas.smallTree.tileSprites != null && curBiome.tileAtlas.smallTree.tileSprites.Length > 0)
                        {
                            GenerateTile(curBiome.tileAtlas.smallTree.tileSprites[Random.Range(0, curBiome.tileAtlas.smallTree.tileSprites.Length)], x, y + 1, curBiome.tileAtlas.smallTree.inBackground, "Plant");
                            SetTerrainMap(x, y + 1, TileType.Tree);
                        }
                        continue;
                    }

                    // 5. 大树
                    cumulativeProbability += curBiome.treeChance;
                    if (randomPick < cumulativeProbability)
                    {
                        if (x - lastTreeX > curBiome.minTreeDistance)
                        {
                            if (curBiome.biomeName == "desert") GenerateCactus(x, y + 1);
                            else GenerateTree(x, y + 1);
                            lastTreeX = x;
                        }
                        continue;
                    }
                    // else: No decoration placed
                }
            }

        worldTilesMap.Apply();
    }


    public Sprite GetCorrectTileSprite(int x, int y, TileType tileType)
    {
        // 位掩码定义：
        // 1: 上方 (0, 1)
        // 2: 下方 (0, -1)
        // 4: 左方 (-1, 0)
        // 8: 右方 (1, 0)
        // 16: 左上方 (-1, 1)
        // 32: 右上方 (1, 1)
        // 64: 左下方 (-1, -1)
        // 128: 右下方 (1, -1)

        int bitmask = 0;

        if (IsTileOfType(x, y + 1, tileType)) bitmask |= 1; // 上
        if (IsTileOfType(x, y - 1, tileType)) bitmask |= 2; // 下
        if (IsTileOfType(x - 1, y, tileType)) bitmask |= 4; // 左
        if (IsTileOfType(x + 1, y, tileType)) bitmask |= 8; // 右

        if (tileType == TileType.Grass)
        {
            if (IsTileOfType(x, y + 1, TileType.Dirt)) bitmask |= 1; // 上
            if (IsTileOfType(x, y - 1, TileType.Dirt)) bitmask |= 2; // 下
            if (IsTileOfType(x - 1, y, TileType.Dirt)) bitmask |= 4; // 左
            if (IsTileOfType(x + 1, y, TileType.Dirt)) bitmask |= 8; // 右
        }
        if (tileType == TileType.Dirt)
        {
            if (IsTileOfType(x, y + 1, TileType.Grass)) bitmask |= 1; // 上
            if (IsTileOfType(x, y - 1, TileType.Grass)) bitmask |= 2; // 下
            if (IsTileOfType(x - 1, y, TileType.Grass)) bitmask |= 4; // 左
            if (IsTileOfType(x + 1, y, TileType.Grass)) bitmask |= 8; // 右
        }

        // TODO: 暂时不管对角线方向的瓦片
        // 检查对角线邻居 (只有当相邻的两个主方向瓦片存在时，对角线瓦片才影响位掩码)
        // if (IsTileOfType(x - 1, y + 1, tileType) && IsTileOfType(x - 1, y, tileType) && IsTileOfType(x, y + 1, tileType)) bitmask |= 16; // 左上
        // if (IsTileOfType(x + 1, y + 1, tileType) && IsTileOfType(x + 1, y, tileType) && IsTileOfType(x, y + 1, tileType)) bitmask |= 32; // 右上
        // if (IsTileOfType(x - 1, y - 1, tileType) && IsTileOfType(x - 1, y, tileType) && IsTileOfType(x, y - 1, tileType)) bitmask |= 64; // 左下
        // if (IsTileOfType(x + 1, y - 1, tileType) && IsTileOfType(x + 1, y, tileType) && IsTileOfType(x, y - 1, tileType)) bitmask |= 128; // 右下

        Sprite[] targetTiles;
        switch (tileType)
        {
            case TileType.Grass: targetTiles = curBiome.tileAtlas.grass.tileSprites; break;
            case TileType.Dirt: targetTiles = curBiome.tileAtlas.dirt.tileSprites; break;
            case TileType.Stone: targetTiles = curBiome.tileAtlas.stone.tileSprites; break;
            case TileType.Copper: targetTiles = curBiome.tileAtlas.copper.tileSprites; break;
            case TileType.Iron: targetTiles = curBiome.tileAtlas.iron.tileSprites; break;
            case TileType.Gold: targetTiles = curBiome.tileAtlas.gold.tileSprites; break;
            case TileType.Ruby: targetTiles = curBiome.tileAtlas.ruby.tileSprites; break;
            case TileType.Emerald: targetTiles = curBiome.tileAtlas.emerald.tileSprites; break;
            case TileType.Sapphire: targetTiles = curBiome.tileAtlas.sapphire.tileSprites; break;
            case TileType.Wall: targetTiles = curBiome.tileAtlas.wall.tileSprites; break;
            default: return null;
        }

        if (targetTiles == null || targetTiles.Length == 0) return null;

        if (tileBitmaskMap != null && tileBitmaskMap.TryGetValue(bitmask, out int dirtIdx))
        {
            if (dirtIdx >= 0 && dirtIdx < targetTiles.Length)
            {
                if (dirtIdx == 15) dirtIdx = Random.Range(dirtIdx, targetTiles.Length);
                return targetTiles[dirtIdx];
            }
        }
        return targetTiles.Length > 0 ? targetTiles[0] : null;
    }

    public Sprite GetCorrectTileSprite_Player(int x, int y, Tile tile, TileType tileType)
    {
        int bitmask = 0;
        if (IsTileOfType(x, y + 1, tileType)) bitmask |= 1; // 上
        if (IsTileOfType(x, y - 1, tileType)) bitmask |= 2; // 下
        if (IsTileOfType(x - 1, y, tileType)) bitmask |= 4; // 左
        if (IsTileOfType(x + 1, y, tileType)) bitmask |= 8; // 右

        if (tileType == TileType.Grass)
        {
            if (IsTileOfType(x, y + 1, TileType.Dirt)) bitmask |= 1;
            if (IsTileOfType(x, y - 1, TileType.Dirt)) bitmask |= 2;
            if (IsTileOfType(x - 1, y, TileType.Dirt)) bitmask |= 4;
            if (IsTileOfType(x + 1, y, TileType.Dirt)) bitmask |= 8;
        }
        if (tileType == TileType.Dirt)
        {
            if (IsTileOfType(x, y + 1, TileType.Grass)) bitmask |= 1;
            if (IsTileOfType(x, y - 1, TileType.Grass)) bitmask |= 2;
            if (IsTileOfType(x - 1, y, TileType.Grass)) bitmask |= 4;
            if (IsTileOfType(x + 1, y, TileType.Grass)) bitmask |= 8;
        }

        Sprite[] targetTiles;
        switch (tileType)
        {
            case TileType.Grass: targetTiles = tile.tileSprites; break;
            case TileType.Dirt: targetTiles = tile.tileSprites; break;
            case TileType.Stone: targetTiles = tile.tileSprites; break;
            case TileType.Copper: targetTiles = tile.tileSprites; break;
            case TileType.Iron: targetTiles = tile.tileSprites; break;
            case TileType.Gold: targetTiles = tile.tileSprites; break;
            case TileType.Ruby: targetTiles = tile.tileSprites; break;
            case TileType.Emerald: targetTiles = tile.tileSprites; break;
            case TileType.Sapphire: targetTiles = tile.tileSprites; break;
            case TileType.Wall: targetTiles = tile.tileSprites; break;
            default: return null;
        }

        if (targetTiles == null || targetTiles.Length == 0) return null;

        if (tileBitmaskMap != null && tileBitmaskMap.TryGetValue(bitmask, out int dirtIdx))
        {
            if (dirtIdx >= 0 && dirtIdx < targetTiles.Length)
            {
                if (dirtIdx == 15) dirtIdx = Random.Range(dirtIdx, targetTiles.Length);
                return targetTiles[dirtIdx];
            }
        }
        return targetTiles.Length > 0 ? targetTiles[0] : null;
    }

    private bool IsTileOfType(int x, int y, TileType type)
    {
        if (x < 0 || x >= worldSize || y < 0 || y >= worldSize)
            return false;
        return terrainMap[x, y] == type;
    }

    private void GenerateLeftBranch(int x, int y, int branchLength, bool isBottomSection = false, TileType tileType = TileType.Tree)
    {
        if (branchLength == 0)
        {
            GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[1], x - 1, y, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
            SetTerrainMap(x - 1, y, tileType);
        }
        else
        {
            GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[2], x - 1, y, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
            SetTerrainMap(x - 1, y, tileType);
            for (int j = 1; j < branchLength; ++j)
            {
                GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[3], x - 1, y + j, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
                SetTerrainMap(x - 1, y + j, tileType);
            }
            float yOffset = isBottomSection ? 0f : -0.5f;
            GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[4], x - 1, y + branchLength + yOffset, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
        }
    }

    private void GenerateRightBranch(int x, int y, int branchLength, bool isBottomSection = false, TileType tileType = TileType.Tree)
    {
        if (branchLength == 0)
        {
            GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[1], x + 1, y, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
            SetTerrainMap(x + 1, y, tileType);
        }
        else
        {
            GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[2], x + 1, y, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
            SetTerrainMap(x + 1, y, tileType);
            for (int j = 1; j < branchLength; ++j)
            {
                GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[3], x + 1, y + j, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
                SetTerrainMap(x + 1, y + j, tileType);
            }
            float yOffset = isBottomSection ? 0f : -0.5f;
            GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[4], x + 1, y + branchLength + yOffset, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
        }
    }

    public void GenerateCactus(int x, int y)
    {
        int treeHeight = Random.Range(curBiome.minTreeHeight, curBiome.maxTreeHeight);
        int lastBranchY_Left = -1;
        int lastBranchY_Right = -1;
        int currentLeftBranches = 0;
        int currentRightBranches = 0;

        // generate bottom
        int bottomIndex = Random.Range(0, 4);
        // 0: Left + Right
        if (bottomIndex == 0 && treeHeight > curBiome.minTreeBranchDistance && currentLeftBranches < curBiome.maxTreeBranches
            && treeHeight > curBiome.minTreeBranchDistance && currentRightBranches < curBiome.maxTreeBranches) // 顶部+两侧
        {
            GenerateTile(curBiome.tileAtlas.treeBottom.tileSprites[bottomIndex], x, y, curBiome.tileAtlas.treeBottom.inBackground, "Plant");
            SetTerrainMap(x, y, TileType.Cactus);
            // Left Branch
            int leftBranchLength = Random.Range(0, treeHeight - 1);
            GenerateLeftBranch(x, y, leftBranchLength, true, TileType.Cactus);
            currentLeftBranches++;
            lastBranchY_Left = leftBranchLength; // 更新上一次生成树枝的Y坐标
            // Right Branch
            int rightBranchLength = Random.Range(0, treeHeight - 1);
            GenerateRightBranch(x, y, rightBranchLength, true, TileType.Cactus);
            currentRightBranches++;
            lastBranchY_Right = rightBranchLength;
        }
        // 1: Left
        else if (bottomIndex == 1 && treeHeight > curBiome.minTreeBranchDistance && currentLeftBranches < curBiome.maxTreeBranches) // 顶部+左侧
        {
            GenerateTile(curBiome.tileAtlas.treeBottom.tileSprites[bottomIndex], x, y, curBiome.tileAtlas.treeBottom.inBackground, "Plant");
            SetTerrainMap(x, y, TileType.Cactus);
            int leftBranchLength = Random.Range(0, treeHeight - 1);
            GenerateLeftBranch(x, y, leftBranchLength, true, TileType.Cactus);
            currentLeftBranches++;
            lastBranchY_Left = leftBranchLength; // 更新上一次生成树枝的Y坐标
        }
        // 2: Right
        else if (bottomIndex == 2 && treeHeight > curBiome.minTreeBranchDistance && currentRightBranches < curBiome.maxTreeBranches) // 顶部+右侧
        {
            GenerateTile(curBiome.tileAtlas.treeBottom.tileSprites[bottomIndex], x, y, curBiome.tileAtlas.treeBottom.inBackground, "Plant");
            SetTerrainMap(x, y, TileType.Cactus);
            int rightBranchLength = Random.Range(0, treeHeight - 1);
            GenerateRightBranch(x, y, rightBranchLength, true, TileType.Cactus);
            currentRightBranches++;
            lastBranchY_Right = rightBranchLength;
        }
        // 3: None
        else
        {
            GenerateTile(curBiome.tileAtlas.treeBottom.tileSprites[bottomIndex], x, y, curBiome.tileAtlas.treeBottom.inBackground, "Plant");
            SetTerrainMap(x, y, TileType.Cactus);
        }

        // generate mid
        for (int i = 1; i < treeHeight; ++i)
        {
            if (Random.value < curBiome.treeBranchChance)
            {
                int midIndex = Random.Range(0, 4);
                // Left
                if (midIndex == 0 && i - lastBranchY_Left > curBiome.minTreeBranchDistance && currentLeftBranches < curBiome.maxTreeBranches)
                {
                    GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[0], x, y + i, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
                    SetTerrainMap(x, y + i, TileType.Cactus);
                    int leftBranchLength = Random.Range(0, treeHeight - i);
                    GenerateLeftBranch(x, y + i, leftBranchLength, false, TileType.Cactus);
                    currentLeftBranches++;
                    lastBranchY_Left = i + leftBranchLength;
                }
                // Right
                else if (midIndex == 1 && i - lastBranchY_Right > curBiome.minTreeBranchDistance && currentRightBranches < curBiome.maxTreeBranches)
                {
                    GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[0], x, y + i, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
                    SetTerrainMap(x, y + i, TileType.Cactus);
                    int rightBranchLength = Random.Range(0, treeHeight - i);
                    GenerateRightBranch(x, y + i, rightBranchLength, false, TileType.Cactus);
                    currentRightBranches++;
                    lastBranchY_Right = i + rightBranchLength;
                }
                // Left + Right
                else if (midIndex == 2 && i - lastBranchY_Left > curBiome.minTreeBranchDistance && currentLeftBranches < curBiome.maxTreeBranches
                    && i - lastBranchY_Right > curBiome.minTreeBranchDistance && currentRightBranches < curBiome.maxTreeBranches)
                {
                    GenerateTile(curBiome.tileAtlas.treeMid.tileSprites[1], x, y + i, curBiome.tileAtlas.treeMid.inBackground, "Plant");
                    SetTerrainMap(x, y + i, TileType.Cactus);
                    // Left Branch
                    int leftBranchLength = Random.Range(0, treeHeight - i);
                    GenerateLeftBranch(x, y + i, leftBranchLength, false, TileType.Cactus);
                    currentLeftBranches++;
                    lastBranchY_Left = i + leftBranchLength;
                    // Right Branch
                    int rightBranchLength = Random.Range(0, treeHeight - i);
                    GenerateRightBranch(x, y + i, rightBranchLength, false, TileType.Cactus);
                    currentRightBranches++;
                    lastBranchY_Right = i + rightBranchLength;
                }
                // None
                else
                {
                    GenerateTile(curBiome.tileAtlas.treeMid.tileSprites[0], x, y + i, curBiome.tileAtlas.treeMid.inBackground, "Plant");
                    SetTerrainMap(x, y + i, TileType.Cactus);
                }
            }
            // None
            else
            {
                GenerateTile(curBiome.tileAtlas.treeMid.tileSprites[0], x, y + i, curBiome.tileAtlas.treeMid.inBackground, "Plant");
                SetTerrainMap(x, y + i, TileType.Cactus);
            }
        }

        // generate tree top
        int topIndex = Random.Range(0, 4);
        // 0: Left + Right
        if (topIndex == 0 && treeHeight - lastBranchY_Left > curBiome.minTreeBranchDistance && currentLeftBranches < curBiome.maxTreeBranches
            && treeHeight - lastBranchY_Right > curBiome.minTreeBranchDistance && currentRightBranches < curBiome.maxTreeBranches) // 顶部+两侧
        {
            GenerateTile(curBiome.tileAtlas.treeTop.tileSprites[topIndex], x, y + treeHeight, curBiome.tileAtlas.treeTop.inBackground, "Plant");
            GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[1], x - 1, y + treeHeight, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
            GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[1], x + 1, y + treeHeight, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
            SetTerrainMap(x, y + treeHeight, TileType.Cactus);
            SetTerrainMap(x - 1, y + treeHeight, TileType.Cactus);
            SetTerrainMap(x + 1, y + treeHeight, TileType.Cactus);
        }
        // 1: Left
        else if (topIndex == 1 && treeHeight - lastBranchY_Left > curBiome.minTreeBranchDistance && currentLeftBranches < curBiome.maxTreeBranches) // 顶部+左侧
        {
            GenerateTile(curBiome.tileAtlas.treeTop.tileSprites[topIndex], x, y + treeHeight, curBiome.tileAtlas.treeTop.inBackground, "Plant");
            GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[1], x - 1, y + treeHeight, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
            SetTerrainMap(x, y + treeHeight, TileType.Cactus);
            SetTerrainMap(x - 1, y + treeHeight, TileType.Cactus);
        }
        // 2: Right
        else if (topIndex == 2 && treeHeight - lastBranchY_Right > curBiome.minTreeBranchDistance && currentRightBranches < curBiome.maxTreeBranches) // 顶部+右侧
        {
            GenerateTile(curBiome.tileAtlas.treeTop.tileSprites[topIndex], x, y + treeHeight, curBiome.tileAtlas.treeTop.inBackground, "Plant");
            GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[1], x + 1, y + treeHeight, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
            SetTerrainMap(x, y + treeHeight, TileType.Cactus);
            SetTerrainMap(x + 1, y + treeHeight, TileType.Cactus);
        }
        // 3: None
        else
        {
            GenerateTile(curBiome.tileAtlas.treeTop.tileSprites[3], x, y + treeHeight, curBiome.tileAtlas.treeTop.inBackground, "Plant");
            SetTerrainMap(x, y + treeHeight, TileType.Cactus);
        }
    }

    public void GenerateTree(int x, int y)
    {
        // generate tree bottom
        GenerateTile(curBiome.tileAtlas.treeBottom.tileSprites[Random.Range(0, curBiome.tileAtlas.treeBottom.tileSprites.Length)], x, y, curBiome.tileAtlas.treeBottom.inBackground, "Plant");
        SetTerrainMap(x, y, TileType.Tree);
        int treeHeight = Random.Range(curBiome.minTreeHeight, curBiome.maxTreeHeight);
        int lastBranchY_Left = -1; // 记录上一次生成树枝的Y坐标
        int lastBranchY_Right = -1;
        int currentLeftBranches = 0;
        int currentRightBranches = 0;
        // 判断这棵树是否秃顶
        bool baldness = Random.value > 0.5f;
        if (curBiome.biomeName == "snow") baldness = true;
        // generate tree mid
        for (int i = 1; i < treeHeight; ++i)
        {
            GenerateTile(curBiome.tileAtlas.treeMid.tileSprites[Random.Range(0, curBiome.tileAtlas.treeMid.tileSprites.Length)], x, y + i, curBiome.tileAtlas.treeMid.inBackground, "Plant");
            SetTerrainMap(x, y + i, TileType.Tree);
            // 随机在树干中间部分生成侧边树枝
            if (i > 1 && i < treeHeight - 2)
            {
                if (Random.value < curBiome.treeBranchChance)
                {
                    // 随机向左或向右生成
                    if (Random.value < 0.5f && i - lastBranchY_Left > curBiome.minTreeBranchDistance)
                    {
                        // 生成左侧树枝
                        if (currentLeftBranches < curBiome.maxTreeBranches)
                        {
                            int branchIndex = Random.Range(0, 3);
                            GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[branchIndex + (baldness ? 3 : 0)], x - 1 - (baldness ? 0.0f : 0.5f), y + i, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
                            currentLeftBranches++;
                            lastBranchY_Left = i; // 更新上一次生成树枝的Y坐标
                        }
                    }
                    else if (i - lastBranchY_Right > curBiome.minTreeBranchDistance)
                    {
                        // 生成右侧树枝
                        if (currentRightBranches < curBiome.maxTreeBranches)
                        {
                            int branchIndex = Random.Range(0, 3);
                            GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[branchIndex + (baldness ? 3 : 0)], x + 1 + (baldness ? 0.0f : 0.5f), y + i, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
                            currentRightBranches++;
                            lastBranchY_Right = i; // 更新上一次生成树枝的Y坐标
                        }
                    }
                }
            }
        }

        // generate tree top
        int topIndex = Random.Range(0, 3);
        GenerateTile(curBiome.tileAtlas.treeTop.tileSprites[topIndex + (baldness ? 3 : 0)], x, y + treeHeight + (baldness ? 0 : 2), curBiome.tileAtlas.treeTop.inBackground, "Plant");
        SetTerrainMap(x, y + treeHeight + (baldness ? 0 : 2), TileType.Tree);
    }

    public void RemoveTile(int x, int y)
    {
        if (x < 0 || x >= worldSize || y < 0 || y >= worldSize) return;
        Vector2 tilePos = new(x, y);
        if (worldTiles.Contains(tilePos))
        {
            int index = worldTiles.IndexOf(tilePos);
            GameObject tileObject = worldTileObjects[index];

            // 获取当前位置的tile类型，用于生成掉落物
            TileType currentTileType = GetTileType(x, y);

            bool isPlant = tileObject.CompareTag("Plant");
            bool isTree = false, isCactus = false;
            if (isPlant)
            {
                // 判断是否是树或仙人掌
                string spriteName = tileObject.GetComponent<SpriteRenderer>().sprite.name.ToLower();
                isTree = spriteName.Contains("tiles_5") || spriteName.Contains("tree_");
                isCactus = spriteName.Contains("tiles_80");
            }

            // 在销毁tile之前生成掉落物
            GenerateItemDrop(x, y, currentTileType);

            GameObject.Destroy(tileObject);
            worldTilesMap.SetPixel(x, y, Color.white);
            LightBlock(x, y, 1f, 0);

            worldTiles.RemoveAt(index);
            worldTileObjects.RemoveAt(index);

            // 清理tile信息记录
            if (worldTileInfo.ContainsKey(tilePos))
            {
                worldTileInfo.Remove(tilePos);
            }

            if (worldWalls.Contains(tilePos)) SetTerrainMap(x, y, TileType.Wall);
            else SetTerrainMap(x, y, TileType.Air);

            if (isCactus)
            {
                RemoveTileDFS(x, y, true);
            }
            else if (isTree)
            {
                RemoveTileDFS(x, y);
                tilePos.Set(x, y - 1);
                if (worldTiles.Contains(tilePos))
                {
                    index = worldTiles.IndexOf(tilePos);
                    tileObject = worldTileObjects[index];
                    string spriteName = tileObject.GetComponent<SpriteRenderer>().sprite.name.ToLower();
                    isTree = spriteName.Contains("tiles_5");
                    if (isTree)
                    {
                        GameObject.Destroy(tileObject);
                        worldTiles.RemoveAt(index);
                        worldTileObjects.RemoveAt(index);
                        UpdateCurrentBiome(x, y - 1);
                        GenerateTile(curBiome.tileAtlas.treeTop.tileSprites[Random.Range(2, curBiome.tileAtlas.treeTop.tileSprites.Length)], x, y - 1, curBiome.tileAtlas.treeTop.inBackground, "Plant");
                    }
                }
            }
            UpdateSurroundingTiles(x, y);

        }
        else if (worldWalls.Contains(tilePos))
        {
            int index = worldWalls.IndexOf(tilePos);
            GameObject wallObject = worldWallObjects[index];

            GenerateItemDrop(x, y, TileType.Wall);

            GameObject.Destroy(wallObject);
            worldTilesMap.SetPixel(x, y, Color.white);
            LightBlock(x, y, 1f, 0);

            worldWalls.RemoveAt(index);
            worldWallObjects.RemoveAt(index);
            SetTerrainMap(x, y, TileType.Air);
            UpdateSurroundingWalls(x, y);
        }
        worldTilesMap.Apply();
    }

    private void RemoveTileDFS(int centerX, int centerY, bool allRemoved = false)
    {
        for (float dx = -2.0f; dx <= 2.0f; dx += 0.5f)
            for (float dy = (allRemoved ? -1.0f : 0.0f); dy <= 3.0f; dy += 1.0f) // 只向上和同层搜索，不向下搜索
            {
                if (dx == 0 && dy == 0) continue;
                float newX = centerX + dx, newY = centerY + dy;
                if (newX < 0 || newX >= worldSize || newY < 0 || newY >= worldSize) continue;
                Vector2 tilePos = new(newX, newY);
                if (worldTiles.Contains(tilePos))
                {
                    int index = worldTiles.IndexOf(tilePos);
                    GameObject tileObject = worldTileObjects[index];
                    if (tileObject.CompareTag("Plant"))
                    {
                        string spriteName = tileObject.GetComponent<SpriteRenderer>().sprite.name.ToLower();
                        bool isTreePart = spriteName.Contains("tiles_5") || spriteName.Contains("tiles_80") || spriteName.Contains("tree_");
                        if (isTreePart)
                        {
                            GameObject.Destroy(tileObject);
                            worldTiles.RemoveAt(index);
                            worldTileObjects.RemoveAt(index);
                            SetTerrainMap((int)newX, (int)newY, TileType.Air);
                            RemoveTileDFS((int)newX, (int)newY, allRemoved);
                        }
                    }
                }
            }
    }

    public void UpdateSurroundingTiles(int centerX, int centerY)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int x = centerX + dx, y = centerY + dy;
                if (x < 0 || x >= worldSize || y < 0 || y >= worldSize) continue;

                Vector2 tilePos = new(x, y);

                // check problems gone
                if (GetTileType(x, y) == TileType.Air && worldTiles.Contains(tilePos))
                {
                    int index = worldTiles.IndexOf(tilePos);
                    worldTiles.RemoveAt(index);
                    worldTileObjects.RemoveAt(index);
                }

                if (worldTiles.Contains(tilePos))
                {
                    int index = worldTiles.IndexOf(tilePos);
                    GameObject tileObject = worldTileObjects[index];
                    UpdateCurrentBiome(x, y);
                    TileType tileType = GetTileType(x, y);
                    Sprite newSprite = GetCorrectTileSprite(x, y, tileType);

                    if (newSprite != null && tileObject != null)
                    {
                        SpriteRenderer spriteRenderer = tileObject.GetComponent<SpriteRenderer>();
                        if (spriteRenderer != null)
                        {
                            spriteRenderer.sprite = newSprite;
                            tileObject.name = newSprite.name;
                        }
                    }
                }
            }
    }

    public void UpdateSurroundingWalls(int centerX, int centerY)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int x = centerX + dx, y = centerY + dy;
                if (x < 0 || x >= worldSize || y < 0 || y >= worldSize) continue;

                Vector2 tilePos = new(x, y);

                // promise there has no problems here
                if (GetTileType(x, y) == TileType.Air && worldWalls.Contains(tilePos))
                {
                    int index = worldWalls.IndexOf(tilePos);
                    worldWalls.RemoveAt(index);
                    worldWallObjects.RemoveAt(index);
                }

                if (worldWalls.Contains(tilePos))
                {
                    int index = worldWalls.IndexOf(tilePos);
                    GameObject tileObject = worldWallObjects[index];
                    UpdateCurrentBiome(x, y);
                    TileType tileType = GetTileType(x, y);
                    Sprite newSprite = GetCorrectTileSprite(x, y, tileType);

                    if (newSprite != null && tileObject != null)
                    {
                        SpriteRenderer spriteRenderer = tileObject.GetComponent<SpriteRenderer>();
                        if (spriteRenderer != null)
                        {
                            spriteRenderer.sprite = newSprite;
                            tileObject.name = newSprite.name;
                        }
                    }
                }
            }
    }

    public void PlaceTile(int x, int y, Tile tile, TileType tileType, string tileTag)
    {
        if (x < 0 || x >= worldSize || y < 0 || y >= worldSize) return;
        if ((tileType != TileType.Wall && !worldTiles.Contains(new Vector2(x, y))) ||
            (tileType == TileType.Wall && !worldWalls.Contains(new Vector2(x, y))))
        {
            GameObject newTile = new();
            if (chunkSize <= 0)
            {
                Destroy(newTile);
                return;
            }
            int chunkIndex = Mathf.FloorToInt(x / chunkSize);
            if (worldChunks != null && chunkIndex >= 0 && chunkIndex < worldChunks.Length)
            {
                if (worldChunks[chunkIndex] != null) newTile.transform.parent = worldChunks[chunkIndex].transform;
                else newTile.transform.parent = this.transform;
            }
            else
            {
                if (chunkIndex < 0) chunkIndex = 0;
                else if (chunkIndex >= worldChunks.Length) chunkIndex = worldChunks.Length - 1;
                newTile.transform.parent = worldChunks[chunkIndex].transform;
            }

            newTile.AddComponent<SpriteRenderer>();
            if (!tile.inBackground)
            {
                newTile.AddComponent<BoxCollider2D>();
                newTile.GetComponent<BoxCollider2D>().size = Vector2.one;
            }
            UpdateCurrentBiome(x, y);
            Sprite tileSprite = GetCorrectTileSprite_Player(x, y, tile, tileType);
            newTile.GetComponent<SpriteRenderer>().sprite = tileSprite;
            newTile.GetComponent<SpriteRenderer>().sortingOrder = tile.inBackground ? -10 : -5;
            newTile.name = tileSprite.name;
            newTile.tag = tileTag;
            newTile.transform.position = new Vector2(x + 0.5f, y + 0.5f);
            if (tile.inBackground)
            {
                worldWalls.Add(new Vector2(x, y));
                worldWallObjects.Add(newTile);
            }
            else
            {
                worldTiles.Add(new Vector2(x, y));
                worldTileObjects.Add(newTile);
            }
            SetTerrainMap(x, y, tileType);

            // 记录玩家放置的tile信息
            Vector2 tilePos = new Vector2(x, y);
            string biomeName = curBiome != null ? curBiome.biomeName : "unknown";
            worldTileInfo[tilePos] = new TileInfo(tile, tileType, biomeName, true);

            if (tileType == TileType.Wall)
                UpdateSurroundingWalls(x, y);
            else
                UpdateSurroundingTiles(x, y);
        }
    }

    public void GenerateTile(Sprite tileSprite, float x, float y, bool backGroundElement, string tileTag = "Ground")
    {
        GenerateTile(tileSprite, x, y, backGroundElement, tileTag, GetTileType((int)x, (int)y), false);
    }

    public void GenerateTile(Sprite tileSprite, float x, float y, bool backGroundElement, string tileTag, TileType tileType, bool isPlayerPlaced)
    {
        if (x < 0 || x >= worldSize || y < 0 || y >= worldSize) return;
        if (!worldTiles.Contains(new Vector2(x, y)))
        {
            GameObject newTile = new();

            if (chunkSize <= 0)
            {
                Destroy(newTile);
                return;
            }

            int chunkIndex = Mathf.FloorToInt(x / chunkSize);
            if (worldChunks != null && chunkIndex >= 0 && chunkIndex < worldChunks.Length)
            {
                if (worldChunks[chunkIndex] != null) newTile.transform.parent = worldChunks[chunkIndex].transform;
                else newTile.transform.parent = this.transform;
            }
            else
            {
                if (chunkIndex < 0) chunkIndex = 0;
                else if (chunkIndex >= worldChunks.Length) chunkIndex = worldChunks.Length - 1;
                newTile.transform.parent = worldChunks[chunkIndex].transform;
            }

            newTile.AddComponent<SpriteRenderer>();
            if (!backGroundElement)
            {
                newTile.AddComponent<BoxCollider2D>();
                newTile.GetComponent<BoxCollider2D>().size = Vector2.one;
            }
            newTile.GetComponent<SpriteRenderer>().sprite = tileSprite;
            newTile.GetComponent<SpriteRenderer>().sortingOrder = backGroundElement ? -10 : -5;

            if (tileTag == "Wall") {
                newTile.GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f);
                worldTilesMap.SetPixel((int)x, (int)y, Color.black);
            } else if (!backGroundElement) {
                worldTilesMap.SetPixel((int)x, (int)y, Color.black);
            }

            newTile.name = tileSprite.name;
            newTile.tag = tileTag;
            newTile.transform.position = new Vector2(x + 0.5f, y + 0.5f);
            if (backGroundElement && tileTag == "Wall")
            {
                worldWalls.Add(new Vector2(x, y));
                worldWallObjects.Add(newTile);
            }
            else
            {
                worldTiles.Add(new Vector2(x, y));
                worldTileObjects.Add(newTile);
            }

            if (tileType != TileType.Air)
            {
                Vector2 tilePos = new Vector2(x, y);
                string biomeName = GetCurrentBiome((int)x, (int)y)?.biomeName;
                Tile sourceTile = GetTileFromType(tileType);
                if (sourceTile != null)
                {
                    worldTileInfo[tilePos] = new TileInfo(sourceTile, tileType, biomeName, isPlayerPlaced);
                }
            }
        }
    }

    public void SetTerrainMap(int x, int y, TileType tileType)
    {
        if (x < 0 || x >= worldSize || y < 0 || y >= worldSize) return;
        terrainMap[x, y] = tileType;
    }

    public Tile GetTileFromType(TileType tileType)
    {
        if (curBiome == null || curBiome.tileAtlas == null) return null;

        switch (tileType)
        {
            case TileType.Grass: return curBiome.tileAtlas.grass;
            case TileType.Dirt: return curBiome.tileAtlas.dirt;
            case TileType.Stone: return curBiome.tileAtlas.stone;
            case TileType.Copper: return curBiome.tileAtlas.copper;
            case TileType.Iron: return curBiome.tileAtlas.iron;
            case TileType.Gold: return curBiome.tileAtlas.gold;
            case TileType.Ruby: return curBiome.tileAtlas.ruby;
            case TileType.Emerald: return curBiome.tileAtlas.emerald;
            case TileType.Sapphire: return curBiome.tileAtlas.sapphire;
            case TileType.Wall: return curBiome.tileAtlas.wall;
            case TileType.Tree: return curBiome.tileAtlas.treeBottom;
            case TileType.Cactus: return curBiome.tileAtlas.treeBottom;
            case TileType.SmallGrass: return curBiome.tileAtlas.smallGrass;
            case TileType.Flower: return curBiome.tileAtlas.flower;
            default: return null;
        }
    }

    public void GenerateItemDrop(int x, int y, TileType tileType)
    {
        if (item_TileDrop == null) return;

        Vector2 tilePos = new Vector2(x, y);
        Tile sourceTile;
        string sourceBiome;

        if (worldTileInfo.ContainsKey(tilePos))
        {
            TileInfo info = worldTileInfo[tilePos];
            sourceTile = info.sourceTile;
            sourceBiome = info.sourceBiome;
        }
        else
        {
            // 如果没有记录，使用当前biome的tile（这种情况应该不会发生）
            UpdateCurrentBiome(x, y);
            sourceTile = GetTileFromType(tileType);
            sourceBiome = curBiome != null ? curBiome.biomeName : "unknown";
        }

        if (sourceTile == null || sourceTile.itemSprite == null) return;

        int quantity = 1;
        if (tileType == TileType.Tree || tileType == TileType.Cactus)
        {
            quantity = FindQuantityDFS(x, y, tileType);
        }

        // 创建Item对象
        Item item = new Item(sourceTile, tileType, sourceBiome, quantity);

        // 实例化掉落物
        GameObject itemDropObj = Instantiate(item_TileDrop, new Vector2(x, y), Quaternion.identity);

        // 添加ItemDrop组件并设置item信息
        ItemDrop itemDrop = itemDropObj.GetComponent<ItemDrop>();
        if (itemDrop == null)
        {
            itemDrop = itemDropObj.AddComponent<ItemDrop>();
        }
        itemDrop.SetItem(item);

        // 设置掉落物的位置（在tile中心稍微偏移）
        Vector2 dropPosition = new Vector2(x + 0.5f, y + 0.5f);
        // y轴添加一些随机偏移，让掉落物看起来更自然
        dropPosition.y += Random.Range(-0.3f, 0.3f);
        itemDropObj.transform.position = dropPosition;

        // 给掉落物一个初始的向上速度
        Rigidbody2D rb = itemDropObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = new Vector2(Random.Range(-2f, 2f), Random.Range(2f, 5f));
        }
    }

    int FindQuantityDFS(int x, int y, TileType tileType)
    {
        HashSet<Vector2> visited = new();
        int totalCount = FindQuantityDFSRecursive(x, y, tileType, visited);
        return totalCount;
    }

    int FindQuantityDFSRecursive(int x, int y, TileType tileType, HashSet<Vector2> visited)
    {
        Vector2 currentPos = new Vector2(x, y);
        if (visited.Contains(currentPos))
            return 0;
        if (x < 0 || x >= worldSize || y < 0 || y >= worldSize)
            return 0;
        if (!worldTiles.Contains(currentPos) || GetTileType(x, y) != tileType)
            return 0;

        visited.Add(currentPos);
        int count = 1;
        bool allRemoved = tileType == TileType.Cactus;
        int[] directions = GetSearchDirections(allRemoved);
        for (int i = 0; i < directions.Length; i += 2)
        {
            int newX = x + directions[i];
            int newY = y + directions[i + 1];
            count += FindQuantityDFSRecursive(newX, newY, tileType, visited);
        }

        return count;
    }

    int[] GetSearchDirections(bool allRemoved)
    {
        if (allRemoved) // Cactus类型：搜索所有方向
        {
            return new int[] {
                // 水平和垂直方向
                -1, 0,  1, 0,  0, -1,  0, 1,
                // 对角线方向
                -1, -1, -1, 1, 1, -1, 1, 1,
                // 扩展范围
                -2, 0,  2, 0,  0, -1,  0, 2,  0, 3
            };
        }
        else // 树类型：只向上和同层搜索
        {
            return new int[] {
                // 水平方向
                -1, 0,  1, 0,
                // 向上方向
                0, 1,  0, 2,  0, 3,
                // 对角线向上
                -1, 1,  1, 1,
                // 扩展水平范围
                -2, 0,  2, 0
            };
        }
    }

    public void LightBlock(int x, int y, float intensity, int iteration)
    {
        if (iteration < lightRadius)
        {
            worldTilesMap.SetPixel(x, y, Color.white * intensity);
            for (int dx = -1; dx <= 1; ++dx)
                for (int dy = -1; dy <= 1; ++dy)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx == x && ny == y) continue;
                    if (nx < 0 || nx >= worldSize || ny < 0 || ny >= worldSize) continue;
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nx, ny));
                    float targetIntensity = Mathf.Pow(0.9f, distance) * intensity;
                    if (worldTilesMap.GetPixel(nx, ny) != null && worldTilesMap.GetPixel(nx, ny).r < targetIntensity)
                        LightBlock(nx, ny, targetIntensity, iteration + 1);
                }
            worldTilesMap.Apply();
        }
    }

    public void RemoveLightSource(int x, int y)
    {
        unlitBlocks.Clear();
        UnlightBlock(x, y, x, y);

        List<Vector2Int> toRelight = new();
        foreach (Vector2Int block in unlitBlocks)
        {
            for (int dx = -1; dx <= 1; ++dx)
                for (int dy = -1; dy <= 1; ++dy)
                {
                    int nx = block.x + dx, ny = block.y + dy;
                    if (nx == block.x && ny == block.y) continue;
                    if (nx < 0 || nx >= worldSize || ny < 0 || ny >= worldSize) continue;
                    if (worldTilesMap.GetPixel(nx, ny) != null &&
                        worldTilesMap.GetPixel(nx, ny).r > worldTilesMap.GetPixel(block.x, block.y).r)
                    {
                        if (!toRelight.Contains(new Vector2Int(nx, ny)))
                            toRelight.Add(new Vector2Int(nx, ny));
                    }
                }
        }

        foreach (Vector2Int source in toRelight)
            LightBlock(source.x, source.y, worldTilesMap.GetPixel(source.x, source.y).r, 0);

        worldTilesMap.Apply();
    }

    public void UnlightBlock(int x, int y, int ix, int iy)
    {
        if (Mathf.Abs(x - ix) > lightRadius || Mathf.Abs(y - iy) > lightRadius || unlitBlocks.Contains(new Vector2Int(x, y))) 
            return;
        
        for (int dx = -1; dx <= 1; ++dx)
            for (int dy = -1; dy <= 1; ++dy)
            {
                int nx = x + dx, ny = y + dy;
                if (nx == x && ny == y) continue;
                if (nx < 0 || nx >= worldSize || ny < 0 || ny >= worldSize) continue;
                if (worldTilesMap.GetPixel(nx, ny) != null && 
                    worldTilesMap.GetPixel(nx, ny).r < worldTilesMap.GetPixel(x, y).r)
                    UnlightBlock(nx, ny, ix, iy);
            }
        worldTilesMap.SetPixel(x, y, Color.black);
        unlitBlocks.Add(new Vector2Int(x, y));
    }
}
