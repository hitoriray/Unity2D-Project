using System.Collections.Generic;
using UnityEngine;

public class TerrainGeneration : MonoBehaviour
{

    #region 公有变量

    [Header("Lighting")]
    public Texture2D worldTilesMap;
    public Material lightShader;
    public float groundLightThreshold = 0.7f;
    public float airLightThreshold = 0.85f;
    public float lightRadius = 5f;
    public HashSet<Vector2Int> unlitBlocks = new();

    [Header("Player")]
    public PlayerController player;
    public CameraController cameraController;
    public GameObject itemTileDrop;

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

    #endregion


    #region 私有变量

    private readonly List<Vector2> worldTilePosition = new();
    private readonly List<GameObject> worldTileObjects = new();
    private readonly List<Vector2> worldWallPosition = new();
    private readonly List<GameObject> worldWallObjects = new();
    private Dictionary<Vector2, TileInfo> worldTileInfo = new();
    private TileType[,] terrainMap;
    private bool[,] wallMap;
    private Dictionary<int, int> tileBitmaskMap;
    private GameObject[] worldChunks;
    private Biome curBiome;
    private Color[] biomeColors;
    private Dictionary<int, Biome> biomeHashMap;
    
    #endregion


    #region 生命周期函数
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

        TextureDrawer.DrawBiomeMap(this);
        TextureDrawer.DrawCavesAndOres(this);

        CreateChunks();
        GenerateTerrain();

        SkyLightManager.Initialize(this);

        for (int x = 0; x < worldSize; ++x)
            for (int y = 0; y < worldSize; ++y)
                if (worldTilesMap.GetPixel(x, y) == Color.white)
                    LightingManager.LightBlock(this, x, y, 1f, 0);
        worldTilesMap.Apply();

        cameraController.Spawn(new Vector3(player.spawnPos.x, player.spawnPos.y, cameraController.transform.position.z));
        cameraController.worldSize = worldSize;
        player.Spawn();
    }

    private void Update()
    {
        UpdateChunks();
    }

    #endregion


    #region 初始化相关

    private void InitializeTileBitmaskMap()
    {
        tileBitmaskMap = new Dictionary<int, int>
        {
            [0] = 0, // 孤立瓦片
            [1] = 1, // 朝下
            [2] = 2, // 朝上
            [4] = 3, // 朝右
            [8] = 4, // 朝左
            [3] = 5, // 垂直
            [12] = 6, // 水平
            [5] = 7, // 朝右下
            [9] = 8, // 朝左下
            [6] = 9, // 朝右上
            [10] = 10, // 朝左上
            [13] = 11, // 朝下开口
            [14] = 12, // 朝上开口
            [7] = 13, // 朝左开口
            [11] = 14, // 朝右开口
            [15] = 15, // 四周都有
        };
        // 目前index在15及以上的都是中间的tile
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

    #endregion


    #region 创建区块

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

    #endregion

    
    #region 地形生成
    
    private void GenerateTerrain()
    {
        terrainMap = new TileType[worldSize, worldSize];
        wallMap = new bool[worldSize, worldSize];
        float height;
        for (int x = 0; x < worldSize; ++x)
        {
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

                    if (ores[0].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > curBiome.ores[0].maxSpawnHeight)
                        tileType = TileType.Copper;
                    if (ores[1].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > curBiome.ores[1].maxSpawnHeight)
                        tileType = TileType.Iron;
                    if (ores[2].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > curBiome.ores[2].maxSpawnHeight)
                        tileType = TileType.Gold;
                    if (ores[3].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > curBiome.ores[3].maxSpawnHeight)
                        tileType = TileType.Ruby;
                    if (ores[4].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > curBiome.ores[4].maxSpawnHeight)
                        tileType = TileType.Emerald;
                    if (ores[5].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > curBiome.ores[5].maxSpawnHeight)
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
                    else {
                        terrainMap[x, y] = TileType.Wall; // 洞穴背景有Wall
                        wallMap[x, y] = true;
                    }
                }
                else
                    terrainMap[x, y] = tileType;

                wallMap[x, y] = terrainMap[x, y] != TileType.Grass;
            }
        }

        // 根据地形图和邻居信息放置瓦片
        float lastTreeX = -Mathf.Infinity;
        for (int x = 0; x < worldSize; ++x)
            for (int y = 0; y < worldSize; ++y)
            {
                UpdateCurrentBiome(x, y);
                if (terrainMap[x, y] != TileType.Air)
                {
                    TileType currentTileType = terrainMap[x, y];
                    Sprite finalTileSprite = GetCorrectTileSprite(x, y, GetCurrentBiomeTileFromType(currentTileType), currentTileType);
                    if (finalTileSprite != null)
                    {
                        if (currentTileType != TileType.Grass) {
                            GenerateTile(GetCorrectTileSprite(x, y, GetCurrentBiomeTileFromType(TileType.Wall), TileType.Wall), x, y, true, "Wall", TileType.Wall, false);
                        }
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
                            GenerateTile(curBiome.tileAtlas.flower.tileSprites[Random.Range(0, curBiome.tileAtlas.flower.tileSprites.Length)], x, y + 1, curBiome.tileAtlas.flower.inBackground, "Plant", TileType.SmallGrass, false);
                            SetTerrainMap(x, y + 1, TileType.Flower);
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
                                GenerateTile(curBiome.tileAtlas.sunflower.tileSprites[Random.Range(0, curBiome.tileAtlas.sunflower.tileSprites.Length)], x, y + 2, curBiome.tileAtlas.sunflower.inBackground, "Plant", TileType.Flower, false);
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
                            GenerateTile(curBiome.tileAtlas.smallTree.tileSprites[Random.Range(0, curBiome.tileAtlas.smallTree.tileSprites.Length)], x, y + 1, curBiome.tileAtlas.smallTree.inBackground, "Plant", TileType.Tree, false);
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
                            if (curBiome.biomeName == "desert")
                                FeatureGenerator.GenerateCactus(this, x, y + 1);
                            else
                                FeatureGenerator.GenerateTree(this, x, y + 1);
                            lastTreeX = x;
                        }
                        continue;
                    }
                }
            }

        worldTilesMap.Apply();
    }

    #endregion


    #region 放置方块

    public bool PlaceTile(int x, int y, Tile tile, TileType tileType, string tileTag, string biomeName)
    {
        if (x < 0 || x >= worldSize || y < 0 || y >= worldSize) return false;
        if ((tileType != TileType.Wall && !worldTilePosition.Contains(new Vector2(x, y))) ||
            (tileType == TileType.Wall && !worldWallPosition.Contains(new Vector2(x, y))))
        {
            GameObject newTile = new();
            if (chunkSize <= 0)
            {
                Destroy(newTile);
                return false;
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
            Sprite tileSprite = GetCorrectTileSprite(x, y, tile, tileType);
            newTile.GetComponent<SpriteRenderer>().sprite = tileSprite;
            newTile.GetComponent<SpriteRenderer>().sortingOrder = tile.inBackground ? -10 : -5;
            newTile.name = tileSprite.name;
            newTile.tag = tileTag;
            newTile.transform.position = new Vector2(x + 0.5f, y + 0.5f);
            if (tile.inBackground)
            {
                worldWallPosition.Add(new Vector2(x, y));
                worldWallObjects.Add(newTile);
            }
            else
            {
                worldTilePosition.Add(new Vector2(x, y));
                worldTileObjects.Add(newTile);
            }
            SetTerrainMap(x, y, tileType);

            Vector2 tilePos = new Vector2(x, y);
            worldTileInfo[tilePos] = new TileInfo(tile, tileType, biomeName, true);

            // 放置方块后移除该位置的光照（阻挡光线）
            worldTilesMap.SetPixel(x, y, Color.black);

            if (tileType == TileType.Wall)
                UpdateSurroundingWalls(x, y);
            else
                UpdateSurroundingTiles(x, y);

            return true;
        }
        return false;
    }

    public void GenerateTile(Sprite tileSprite, float x, float y, bool backGroundElement, string tileTag = "Ground")
    {
        GenerateTile(tileSprite, x, y, backGroundElement, tileTag, GetTileType((int)x, (int)y), false);
    }

    public void GenerateTile(Sprite tileSprite, float x, float y, bool backGroundElement, string tileTag, TileType tileType, bool isPlayerPlaced)
    {
        if (x < 0 || x >= worldSize || y < 0 || y >= worldSize) return;
        if (!worldTilePosition.Contains(new Vector2(x, y)))
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

            if (tileTag == "Wall")
            {
                newTile.GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f);
                worldTilesMap.SetPixel((int)x, (int)y, Color.black);
            }
            else if (!backGroundElement)
            {
                worldTilesMap.SetPixel((int)x, (int)y, Color.black);
            }

            newTile.name = tileSprite.name;
            newTile.tag = tileTag;
            newTile.transform.position = new Vector2(x + 0.5f, y + 0.5f);
            if (backGroundElement && tileTag == "Wall")
            {
                worldWallPosition.Add(new Vector2(x, y));
                worldWallObjects.Add(newTile);
            }
            else
            {
                worldTilePosition.Add(new Vector2(x, y));
                worldTileObjects.Add(newTile);
            }

            if (tileType != TileType.Air)
            {
                
                Vector2 tilePos = new Vector2(x, y);
                string biomeName = GetCurrentBiome((int)x, (int)y)?.biomeName;
                Tile sourceTile = GetCurrentBiomeTileFromType(tileType);
                worldTileInfo[tilePos] = new TileInfo(sourceTile, tileType, biomeName, isPlayerPlaced);
            }
        }
    }

    #endregion


    #region 移除方块

    public bool RemoveTile(int x, int y, TileType tileType)
    {
        if (x < 0 || x >= worldSize || y < 0 || y >= worldSize) return false;
        Vector2 tilePos = new(x, y);
        if (worldTilePosition.Contains(tilePos) && tileType != TileType.Wall)
        {
            int index = worldTilePosition.IndexOf(tilePos);
            GameObject tileObject = worldTileObjects[index];
            TileType currentTileType = GetTileType(x, y);
            bool isPlant = tileObject.CompareTag("Plant");
            bool isTree = false, isCactus = false;
            if (isPlant)
            {
                string spriteName = tileObject.GetComponent<SpriteRenderer>().sprite.name.ToLower();
                isTree = spriteName.Contains("tiles_5") || spriteName.Contains("tree_");
                isCactus = spriteName.Contains("tiles_80");
            }

            if (((isTree || isCactus) && tileType == TileType.Tree) ||
                (!isPlant && !isTree && !isCactus && tileType != TileType.Tree))
            {
                GenerateItemDrop(x, y, currentTileType);

                GameObject.Destroy(tileObject);
                worldTilesMap.SetPixel(x, y, Color.white);
                // 使用队列机制更新光照，提升性能
                // LightingManager.QueueLightUpdate(this, x, y, 1f);
                SkyLightManager.OnBlockChanged(x, this);
                LightingManager.UpdateBlockLighting(this, x, y);

                worldTilePosition.RemoveAt(index);
                worldTileObjects.RemoveAt(index);

                if (worldTileInfo.ContainsKey(tilePos))
                {
                    worldTileInfo.Remove(tilePos);
                }

                if (worldWallPosition.Contains(tilePos))
                {
                    wallMap[x, y] = true;
                    SetTerrainMap(x, y, TileType.Wall);
                    worldTileInfo[tilePos] = new TileInfo(GetCurrentBiomeTileFromType(TileType.Wall), TileType.Wall, curBiome.biomeName, false);
                }
                else SetTerrainMap(x, y, TileType.Air);

                if (isCactus && tileType == TileType.Tree)
                {
                    RemoveTileDFS(x, y, true);
                }
                else if (isTree && tileType == TileType.Tree)
                {
                    RemoveTileDFS(x, y);
                    tilePos.Set(x, y - 1);
                    if (worldTilePosition.Contains(tilePos))
                    {
                        index = worldTilePosition.IndexOf(tilePos);
                        tileObject = worldTileObjects[index];
                        string spriteName = tileObject.GetComponent<SpriteRenderer>().sprite.name.ToLower();
                        isTree = spriteName.Contains("tiles_5");
                        if (isTree)
                        {
                            GameObject.Destroy(tileObject);
                            worldTilePosition.RemoveAt(index);
                            worldTileObjects.RemoveAt(index);
                            UpdateCurrentBiome(x, y - 1);
                            GenerateTile(curBiome.tileAtlas.treeTop.tileSprites[Random.Range(2, curBiome.tileAtlas.treeTop.tileSprites.Length)], x, y - 1, curBiome.tileAtlas.treeTop.inBackground, "Plant", TileType.Tree, false);
                        }
                    }
                }
                UpdateSurroundingTiles(x, y);
                worldTilesMap.Apply();
                return true;
            }
        }
        else if (!worldTilePosition.Contains(tilePos) && worldWallPosition.Contains(tilePos) && tileType == TileType.Wall)
        {
            int index = worldWallPosition.IndexOf(tilePos);
            GameObject wallObject = worldWallObjects[index];

            GenerateItemDrop(x, y, TileType.Wall);

            GameObject.Destroy(wallObject);
            worldTilesMap.SetPixel(x, y, Color.white);
            // 使用队列机制更新光照，提升性能
            // LightingManager.QueueLightUpdate(this, x, y, 1f);
            SkyLightManager.OnBlockChanged(x, this);
            LightingManager.UpdateBlockLighting(this, x, y);

            worldWallPosition.RemoveAt(index);
            worldWallObjects.RemoveAt(index);
            wallMap[x, y] = false;
            SetTerrainMap(x, y, TileType.Air);
            UpdateSurroundingWalls(x, y);
            worldTilesMap.Apply();
            return true;
        }
        return false;
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
                if (worldTilePosition.Contains(tilePos))
                {
                    int index = worldTilePosition.IndexOf(tilePos);
                    GameObject tileObject = worldTileObjects[index];
                    if (tileObject.CompareTag("Plant"))
                    {
                        string spriteName = tileObject.GetComponent<SpriteRenderer>().sprite.name.ToLower();
                        bool isTreePart = spriteName.Contains("tiles_5") || spriteName.Contains("tiles_80") || spriteName.Contains("tree_");
                        if (isTreePart)
                        {
                            GameObject.Destroy(tileObject);
                            worldTilePosition.RemoveAt(index);
                            worldTileObjects.RemoveAt(index);
                            SetTerrainMap((int)newX, (int)newY, TileType.Air);
                            RemoveTileDFS((int)newX, (int)newY, allRemoved);
                        }
                    }
                }
            }
    }

    #endregion


    #region 更新周围方块
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
                if (GetTileType(x, y) == TileType.Air && worldTilePosition.Contains(tilePos))
                {
                    int index = worldTilePosition.IndexOf(tilePos);
                    worldTilePosition.RemoveAt(index);
                    worldTileObjects.RemoveAt(index);
                }

                if (worldTilePosition.Contains(tilePos) && worldTileInfo.ContainsKey(tilePos))
                {
                    int index = worldTilePosition.IndexOf(tilePos);
                    GameObject tileObject = worldTileObjects[index];
                    UpdateCurrentBiome(x, y);
                    TileType tileType = GetTileType(x, y);
                    Sprite newSprite = GetCorrectTileSprite(x, y, worldTileInfo[tilePos].sourceTile, tileType);

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
                if (GetTileType(x, y) == TileType.Air && worldWallPosition.Contains(tilePos))
                {
                    int index = worldWallPosition.IndexOf(tilePos);
                    worldWallPosition.RemoveAt(index);
                    worldWallObjects.RemoveAt(index);
                }

                if (worldWallPosition.Contains(tilePos) && worldTileInfo.ContainsKey(tilePos))
                {
                    int index = worldWallPosition.IndexOf(tilePos);
                    GameObject tileObject = worldWallObjects[index];
                    TileType tileType = GetTileType(x, y);
                    Sprite newSprite = GetCorrectTileSprite(x, y, worldTileInfo[tilePos].sourceTile, tileType);

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

    #endregion


    #region 工具方法

    public Biome GetCurrentBiome(int x, int y)
    {
        UpdateCurrentBiome(x, y);
        return curBiome;
    }

    public void UpdateCurrentBiome(int x, int y)
    {
        Color32 pixelColor = biomeMap.GetPixel(x, y);
        int colorHash = (pixelColor.r << 24) | (pixelColor.g << 16) | (pixelColor.b << 8) | pixelColor.a;
        if (biomeHashMap.TryGetValue(colorHash, out Biome biome))
            curBiome = biome;
    }

    public Sprite GetCorrectTileSprite(int x, int y, Tile tile, TileType tileType)
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
        if (type == TileType.Wall) return terrainMap[x, y] == type || wallMap[x, y];
        return terrainMap[x, y] == type;
    }

    public void SetTerrainMap(int x, int y, TileType tileType)
    {
        if (x < 0 || x >= worldSize || y < 0 || y >= worldSize) return;
        terrainMap[x, y] = tileType;
    }

    public TileType GetTileType(int x, int y)
    {
        if (x < 0 || x >= worldSize || y < 0 || y >= worldSize)
            return TileType.Air;
        return terrainMap[x, y];
    }

    public Tile GetCurrentBiomeTileFromType(TileType tileType)
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
            case TileType.Tree: return curBiome.tileAtlas.treeBottom;
            case TileType.Cactus: return curBiome.tileAtlas.treeBottom;
            case TileType.SmallGrass: return curBiome.tileAtlas.smallGrass;
            case TileType.Flower: return curBiome.tileAtlas.flower;
            case TileType.Wall: return curBiome.tileAtlas.wall;
            default: return null;
        }
    }

    public Biome GetBiomeByName(string biomeName)
    {
        foreach (Biome biome in biomes)
        {
            if (biome.biomeName == biomeName)
                return biome;
        }
        return null;
    }

    int[] GetSearchDirections(bool allRemoved)
    {
        if (allRemoved) // Cactus类型：搜索所有方向
        {
            return new int[] {
                -1, 0,  1, 0,  0, -1,  0, 1,
                -1, -1, -1, 1, 1, -1, 1, 1,
                -2, 0,  2, 0,  0, -1,  0, 2,  0, 3
            };
        }
        else // Tree类型：只向上和同层搜索
        {
            return new int[] {
                -1, 0,  1, 0,
                0, 1,  0, 2,  0, 3,
                -1, 1,  1, 1,
                -2, 0,  2, 0
            };
        }
    }

    public bool IsWallAt(int x, int y)
    {
        return wallMap[x, y] || terrainMap[x, y] == TileType.Wall;
    }
    
    /// <summary>
    /// 获取指定世界坐标对应的氛围系统生物群系类型。
    /// </summary>
    /// <param name="worldPosition">世界坐标。</param>
    /// <returns>对应的 AmbianceSystem.BiomeType。</returns>
    public global::AmbianceSystem.BiomeType GetAmbianceBiomeTypeAtWorldPosition(Vector3 worldPosition)
    {
        // 将世界坐标转换为图块坐标
        int tileX = Mathf.RoundToInt(worldPosition.x - 0.5f);
        int tileY = Mathf.RoundToInt(worldPosition.y - 0.5f);

        // 边界检查
        if (tileX < 0 || tileX >= worldSize || tileY < 0 || tileY >= worldSize)
        {
            return global::AmbianceSystem.BiomeType.None;
        }

        Biome macroBiome = GetCurrentBiome(tileX, tileY);
        if (macroBiome == null)
        {
            return global::AmbianceSystem.BiomeType.None;
        }

        TileType actualTileAtPlayer = GetTileType(tileX, tileY);

        // 洞穴判断：如果玩家脚下是空气，但其位置在地图上被标记为背景墙 (wallMap[tileX, tileY] is true)
        // 或者宏观生物群系明确是洞穴类型
        if (IsWallAt(tileX, tileY) && actualTileAtPlayer == TileType.Air) // 玩家在背景墙前的空气中，很可能是在洞穴里
        {
            return global::AmbianceSystem.BiomeType.Generic; // 之前是 Cave
        }
        if (actualTileAtPlayer == TileType.Wall && !worldTilePosition.Contains(new Vector2(tileX, tileY))) // 玩家所在格是墙（无前景方块）
        {
            return global::AmbianceSystem.BiomeType.Generic; // 之前是 Cave
        }
        if (macroBiome.biomeName != null && macroBiome.biomeName.ToLower().Contains("cave"))
        {
            return global::AmbianceSystem.BiomeType.Generic; // 之前是 Cave
        }

        if (macroBiome.biomeName == null) return global::AmbianceSystem.BiomeType.Generic; // 如果宏观生物群系名为空，默认为通用 (之前是 Surface)

        switch (macroBiome.biomeName.ToLower())
        {
            case "forest":
                return global::AmbianceSystem.BiomeType.Forest;
            case "desert":
                return global::AmbianceSystem.BiomeType.Desert;
            case "snow": // 新增雪原生物群系判断
                return global::AmbianceSystem.BiomeType.Snow;
            case "grassland":
                return global::AmbianceSystem.BiomeType.Generic; // 之前是 Surface
            default:
                return global::AmbianceSystem.BiomeType.Generic; // 之前是 Surface
        }
    }

    #endregion


    #region 掉落物生成
    public void GenerateItemDrop(int x, int y, TileType tileType)
    {
        if (itemTileDrop == null)
        {
            return;
        }

        Vector2 tilePos = new(x, y);
        Tile sourceTile;
        string sourceBiome;
        if (worldTileInfo.ContainsKey(tilePos))
        {
            TileInfo info = worldTileInfo[tilePos];
            // TODO:如果墙壁在方块后面，则会爆方块的itemDrop
            sourceTile = info.sourceTile;
            sourceBiome = info.sourceBiome;
        }
        else
        {
            UpdateCurrentBiome(x, y);
            sourceTile = GetCurrentBiomeTileFromType(tileType);
            sourceBiome = curBiome != null ? curBiome.biomeName : "unknown";
        }

        int quantity = 1;
        if (tileType == TileType.Tree || tileType == TileType.Cactus)
            quantity = FindQuantityDFS(x, y, tileType);

        Item item = new Item(sourceTile, tileType, sourceBiome, quantity);
        GameObject itemDropObj = Instantiate(itemTileDrop, new Vector2(x, y), Quaternion.identity);
        ItemDrop itemDrop = itemDropObj.GetComponent<ItemDrop>();
        if (itemDrop == null)
        {
            itemDrop = itemDropObj.AddComponent<ItemDrop>();
        }
        itemDrop.SetItem(item);
        Vector2 dropPosition = new Vector2(x + 0.5f, y + 0.5f);
        // y轴添加一些随机偏移，让掉落物看起来更自然
        dropPosition.y += Random.Range(-0.3f, 0.3f);
        itemDropObj.transform.position = dropPosition;

        Rigidbody2D rb = itemDropObj.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = new Vector2(Random.Range(-2f, 2f), Random.Range(2f, 5f));
    }

    int FindQuantityDFS(int x, int y, TileType tileType)
    {
        HashSet<Vector2> visited = new();
        int totalCount = FindQuantityDFSRecursive(x, y, tileType, visited);
        return totalCount;
    }

    int FindQuantityDFSRecursive(int x, int y, TileType tileType, HashSet<Vector2> visited)
    {
        Vector2 pos = new Vector2(x, y);
        if (visited.Contains(pos))
            return 0;
        if (x < 0 || x >= worldSize || y < 0 || y >= worldSize)
            return 0;
        if (!worldTilePosition.Contains(pos) || GetTileType(x, y) != tileType)
            return 0;

        visited.Add(pos);
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
    #endregion

}