using UnityEngine;

[System.Serializable]
public class Ore
{
    public string name;
    [Range(0, 1)]
    public float frequency;
    [Range(0, 1)]
    public float size;
    public int maxSpawnHeight; // 只能生成的这个高度下面
    public Texture2D spreadTexture;
}
