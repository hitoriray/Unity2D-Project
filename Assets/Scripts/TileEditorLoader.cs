using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(Tile))]
public class TileEditorLoader : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Tile tile = (Tile)target;

        if (GUILayout.Button("自动加载Sprites"))
        {
            LoadSprites(tile);
        }
    }

    private void LoadSprites(Tile tile)
    {
        // 加载整张图集中的所有Sprite
        Sprite[] allSprites = Resources.LoadAll<Sprite>($"TerrairaAssets/{tile.atlasName}");

        // 构造名字 -> Sprite 映射表，加快查找速度
        Dictionary<string, Sprite> spriteDict = new Dictionary<string, Sprite>();
        foreach (var sprite in allSprites)
        {
            spriteDict[sprite.name] = sprite;
        }

        // 按指定顺序加载
        List<Sprite> result = new List<Sprite>();
        foreach (int index in tile.spriteIndices)
        {
            string spriteName = $"{tile.atlasName}_{index}";
            if (spriteDict.TryGetValue(spriteName, out Sprite s))
            {
                result.Add(s);
            }
            else
            {
                Debug.LogWarning($"未找到Sprite: {spriteName}");
            }
        }

        tile.tileSprites = result.ToArray();
        EditorUtility.SetDirty(tile); // 标记为已修改
        Debug.Log($"已按顺序加载 {result.Count} 个Sprite到 {tile.tileName}");
    }

}
