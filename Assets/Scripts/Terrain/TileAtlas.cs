using UnityEngine;

[CreateAssetMenu(fileName = "newTileAtlas", menuName = "Tile Atlas")]
public class TileAtlas : ScriptableObject
{
    [Header("Environment")]
    public Tile grass;
    public Tile dirt;
    public Tile stone;
    public Tile treeBottom;
    public Tile treeMid;
    public Tile treeBranches_Left;
    public Tile treeBranches_Right;
    public Tile treeTop;

    [Header("Ores")]
    public Tile copper;
    public Tile iron;
    public Tile gold;
    public Tile ruby;
    public Tile emerald;
    public Tile sapphire;

    [Header("Flowers")]
    public Tile smallGrass;
    public Tile smallTree;
    public Tile flower;
    public Tile sunflower;
    
    [Header("Wall")]
    public Tile wall;
}
