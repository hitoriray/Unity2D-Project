using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newTool", menuName = "Tool Class")]
public class Tool : ScriptableObject
{
    public string toolName;
    public Sprite toolSprite;
    public string description;
    public string specificDescription;
    public ToolType toolType;
}
