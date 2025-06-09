using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newTool", menuName = "Tool Class")]
public class Tool : ScriptableObject
{
    public string toolName;
    public Sprite toolSprite;
    public ToolType toolType;
}
