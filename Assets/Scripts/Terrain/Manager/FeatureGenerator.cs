using UnityEngine;

public static class FeatureGenerator
{
    public static void GenerateTree(TerrainGeneration terrainGen, int x, int y)
    {
        Biome curBiome = terrainGen.GetCurrentBiome(x, y);

        // generate tree bottom
        terrainGen.GenerateTile(curBiome.tileAtlas.treeBottom.tileSprites[Random.Range(0, curBiome.tileAtlas.treeBottom.tileSprites.Length)], x, y, curBiome.tileAtlas.treeBottom.inBackground, "Plant");
        terrainGen.SetTerrainMap(x, y, TileType.Tree);
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
            terrainGen.GenerateTile(curBiome.tileAtlas.treeMid.tileSprites[Random.Range(0, curBiome.tileAtlas.treeMid.tileSprites.Length)], x, y + i, curBiome.tileAtlas.treeMid.inBackground, "Plant");
            terrainGen.SetTerrainMap(x, y + i, TileType.Tree);
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
                            terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[branchIndex + (baldness ? 3 : 0)], x - 1 - (baldness ? 0.0f : 0.5f), y + i, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
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
                            terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[branchIndex + (baldness ? 3 : 0)], x + 1 + (baldness ? 0.0f : 0.5f), y + i, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
                            currentRightBranches++;
                            lastBranchY_Right = i; // 更新上一次生成树枝的Y坐标
                        }
                    }
                }
            }
        }

        // generate tree top
        int topIndex = Random.Range(0, 3);
        if (topIndex + (baldness ? 3 : 0) < curBiome.tileAtlas.treeTop.tileSprites.Length)
        {
            terrainGen.GenerateTile(curBiome.tileAtlas.treeTop.tileSprites[topIndex + (baldness ? 3 : 0)], x, y + treeHeight + (baldness ? 0 : 2), curBiome.tileAtlas.treeTop.inBackground, "Plant");
            terrainGen.SetTerrainMap(x, y + treeHeight + (baldness ? 0 : 2), TileType.Tree);
        }
        else
        {
            Debug.LogError("top index out of bound: " + topIndex + (baldness ? 3 : 0));
        }
    }

    
    public static void GenerateCactus(TerrainGeneration terrainGen, int x, int y)
    {
        Biome curBiome = terrainGen.GetCurrentBiome(x, y);
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
            terrainGen.GenerateTile(curBiome.tileAtlas.treeBottom.tileSprites[bottomIndex], x, y, curBiome.tileAtlas.treeBottom.inBackground, "Plant");
            terrainGen.SetTerrainMap(x, y, TileType.Cactus);
            // Left Branch
            int leftBranchLength = Random.Range(0, treeHeight - 1);
            GenerateLeftBranch(terrainGen, x, y, leftBranchLength, true, TileType.Cactus);
            currentLeftBranches++;
            lastBranchY_Left = leftBranchLength; // 更新上一次生成树枝的Y坐标
            // Right Branch
            int rightBranchLength = Random.Range(0, treeHeight - 1);
            GenerateRightBranch(terrainGen, x, y, rightBranchLength, true, TileType.Cactus);
            currentRightBranches++;
            lastBranchY_Right = rightBranchLength;
        }
        // 1: Left
        else if (bottomIndex == 1 && treeHeight > curBiome.minTreeBranchDistance && currentLeftBranches < curBiome.maxTreeBranches) // 顶部+左侧
        {
            terrainGen.GenerateTile(curBiome.tileAtlas.treeBottom.tileSprites[bottomIndex], x, y, curBiome.tileAtlas.treeBottom.inBackground, "Plant");
            terrainGen.SetTerrainMap(x, y, TileType.Cactus);
            int leftBranchLength = Random.Range(0, treeHeight - 1);
            GenerateLeftBranch(terrainGen, x, y, leftBranchLength, true, TileType.Cactus);
            currentLeftBranches++;
            lastBranchY_Left = leftBranchLength; // 更新上一次生成树枝的Y坐标
        }
        // 2: Right
        else if (bottomIndex == 2 && treeHeight > curBiome.minTreeBranchDistance && currentRightBranches < curBiome.maxTreeBranches) // 顶部+右侧
        {
            terrainGen.GenerateTile(curBiome.tileAtlas.treeBottom.tileSprites[bottomIndex], x, y, curBiome.tileAtlas.treeBottom.inBackground, "Plant");
            terrainGen.SetTerrainMap(x, y, TileType.Cactus);
            int rightBranchLength = Random.Range(0, treeHeight - 1);
            GenerateRightBranch(terrainGen, x, y, rightBranchLength, true, TileType.Cactus);
            currentRightBranches++;
            lastBranchY_Right = rightBranchLength;
        }
        // 3: None
        else
        {
            terrainGen.GenerateTile(curBiome.tileAtlas.treeBottom.tileSprites[bottomIndex], x, y, curBiome.tileAtlas.treeBottom.inBackground, "Plant");
            terrainGen.SetTerrainMap(x, y, TileType.Cactus);
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
                    terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[0], x, y + i, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
                    terrainGen.SetTerrainMap(x, y + i, TileType.Cactus);
                    int leftBranchLength = Random.Range(0, treeHeight - i);
                    GenerateLeftBranch(terrainGen, x, y + i, leftBranchLength, false, TileType.Cactus);
                    currentLeftBranches++;
                    lastBranchY_Left = i + leftBranchLength;
                }
                // Right
                else if (midIndex == 1 && i - lastBranchY_Right > curBiome.minTreeBranchDistance && currentRightBranches < curBiome.maxTreeBranches)
                {
                    terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[0], x, y + i, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
                    terrainGen.SetTerrainMap(x, y + i, TileType.Cactus);
                    int rightBranchLength = Random.Range(0, treeHeight - i);
                    GenerateRightBranch(terrainGen, x, y + i, rightBranchLength, false, TileType.Cactus);
                    currentRightBranches++;
                    lastBranchY_Right = i + rightBranchLength;
                }
                // Left + Right
                else if (midIndex == 2 && i - lastBranchY_Left > curBiome.minTreeBranchDistance && currentLeftBranches < curBiome.maxTreeBranches
                    && i - lastBranchY_Right > curBiome.minTreeBranchDistance && currentRightBranches < curBiome.maxTreeBranches)
                {
                    terrainGen.GenerateTile(curBiome.tileAtlas.treeMid.tileSprites[1], x, y + i, curBiome.tileAtlas.treeMid.inBackground, "Plant");
                    terrainGen.SetTerrainMap(x, y + i, TileType.Cactus);
                    // Left Branch
                    int leftBranchLength = Random.Range(0, treeHeight - i);
                    GenerateLeftBranch(terrainGen, x, y + i, leftBranchLength, false, TileType.Cactus);
                    currentLeftBranches++;
                    lastBranchY_Left = i + leftBranchLength;
                    // Right Branch
                    int rightBranchLength = Random.Range(0, treeHeight - i);
                    GenerateRightBranch(terrainGen, x, y + i, rightBranchLength, false, TileType.Cactus);
                    currentRightBranches++;
                    lastBranchY_Right = i + rightBranchLength;
                }
                // None
                else
                {
                    terrainGen.GenerateTile(curBiome.tileAtlas.treeMid.tileSprites[0], x, y + i, curBiome.tileAtlas.treeMid.inBackground, "Plant");
                    terrainGen.SetTerrainMap(x, y + i, TileType.Cactus);
                }
            }
            // None
            else
            {
                terrainGen.GenerateTile(curBiome.tileAtlas.treeMid.tileSprites[0], x, y + i, curBiome.tileAtlas.treeMid.inBackground, "Plant");
                terrainGen.SetTerrainMap(x, y + i, TileType.Cactus);
            }
        }

        // generate tree top
        int topIndex = Random.Range(0, 4);
        // 0: Left + Right
        if (topIndex == 0 && treeHeight - lastBranchY_Left > curBiome.minTreeBranchDistance && currentLeftBranches < curBiome.maxTreeBranches
            && treeHeight - lastBranchY_Right > curBiome.minTreeBranchDistance && currentRightBranches < curBiome.maxTreeBranches) // 顶部+两侧
        {
            terrainGen.GenerateTile(curBiome.tileAtlas.treeTop.tileSprites[topIndex], x, y + treeHeight, curBiome.tileAtlas.treeTop.inBackground, "Plant");
            terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[1], x - 1, y + treeHeight, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
            terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[1], x + 1, y + treeHeight, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
            terrainGen.SetTerrainMap(x, y + treeHeight, TileType.Cactus);
            terrainGen.SetTerrainMap(x - 1, y + treeHeight, TileType.Cactus);
            terrainGen.SetTerrainMap(x + 1, y + treeHeight, TileType.Cactus);
        }
        // 1: Left
        else if (topIndex == 1 && treeHeight - lastBranchY_Left > curBiome.minTreeBranchDistance && currentLeftBranches < curBiome.maxTreeBranches) // 顶部+左侧
        {
            terrainGen.GenerateTile(curBiome.tileAtlas.treeTop.tileSprites[topIndex], x, y + treeHeight, curBiome.tileAtlas.treeTop.inBackground, "Plant");
            terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[1], x - 1, y + treeHeight, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
            terrainGen.SetTerrainMap(x, y + treeHeight, TileType.Cactus);
            terrainGen.SetTerrainMap(x - 1, y + treeHeight, TileType.Cactus);
        }
        // 2: Right
        else if (topIndex == 2 && treeHeight - lastBranchY_Right > curBiome.minTreeBranchDistance && currentRightBranches < curBiome.maxTreeBranches) // 顶部+右侧
        {
            terrainGen.GenerateTile(curBiome.tileAtlas.treeTop.tileSprites[topIndex], x, y + treeHeight, curBiome.tileAtlas.treeTop.inBackground, "Plant");
            terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[1], x + 1, y + treeHeight, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
            terrainGen.SetTerrainMap(x, y + treeHeight, TileType.Cactus);
            terrainGen.SetTerrainMap(x + 1, y + treeHeight, TileType.Cactus);
        }
        // 3: None
        else
        {
            terrainGen.GenerateTile(curBiome.tileAtlas.treeTop.tileSprites[3], x, y + treeHeight, curBiome.tileAtlas.treeTop.inBackground, "Plant");
            terrainGen.SetTerrainMap(x, y + treeHeight, TileType.Cactus);
        }
    }

    

    private static void GenerateLeftBranch(TerrainGeneration terrainGen, int x, int y, int branchLength, bool isBottomSection = false, TileType tileType = TileType.Tree)
    {
        Biome curBiome = terrainGen.GetCurrentBiome(x, y);
        if (branchLength == 0)
        {
            terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[1], x - 1, y, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
            terrainGen.SetTerrainMap(x - 1, y, tileType);
        }
        else
        {
            terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[2], x - 1, y, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
            terrainGen.SetTerrainMap(x - 1, y, tileType);
            for (int j = 1; j < branchLength; ++j)
            {
                terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[3], x - 1, y + j, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
                terrainGen.SetTerrainMap(x - 1, y + j, tileType);
            }
            float yOffset = isBottomSection ? 0f : -0.5f;
            terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Left.tileSprites[4], x - 1, y + branchLength + yOffset, curBiome.tileAtlas.treeBranches_Left.inBackground, "Plant");
        }
    }

    private static void GenerateRightBranch(TerrainGeneration terrainGen, int x, int y, int branchLength, bool isBottomSection = false, TileType tileType = TileType.Tree)
    {
        Biome curBiome = terrainGen.GetCurrentBiome(x, y);
        if (branchLength == 0)
        {
            terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[1], x + 1, y, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
            terrainGen.SetTerrainMap(x + 1, y, tileType);
        }
        else
        {
            terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[2], x + 1, y, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
            terrainGen.SetTerrainMap(x + 1, y, tileType);
            for (int j = 1; j < branchLength; ++j)
            {
                terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[3], x + 1, y + j, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
                terrainGen.SetTerrainMap(x + 1, y + j, tileType);
            }
            float yOffset = isBottomSection ? 0f : -0.5f;
            terrainGen.GenerateTile(curBiome.tileAtlas.treeBranches_Right.tileSprites[4], x + 1, y + branchLength + yOffset, curBiome.tileAtlas.treeBranches_Right.inBackground, "Plant");
        }
    }
} 