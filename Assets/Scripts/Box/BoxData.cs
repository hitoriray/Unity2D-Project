using UnityEngine;

namespace Box
{
    /// <summary>
    /// 定义宝箱的状态
    /// </summary>
    public enum BoxState
    {
        Closed = 0,  // 闭合
        Opening = 1, // 正在打开（半开）
        Open = 2     // 完全打开
    }

    /// <summary>
    /// 存储宝箱相关数据的ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewBoxData", menuName = "Box/Box Data")]
    public class BoxData : ScriptableObject
    {
        [Tooltip("宝箱的名称，用于识别")]
        public string BoxName;

        [Tooltip("打开宝箱的动画过渡时间（秒）")]
        public float openAnimationSpeed = 0.15f;

        [Header("宝箱底部Sprite (需按顺序)")]
        [Tooltip("宝箱左下部分的Sprite (0:闭合, 1:半开, 2:全开)")]
        public Sprite[] bottomLeftSprites = new Sprite[3];

        [Tooltip("宝箱右下部分的Sprite (0:闭合, 1:半开, 2:全开)")]
        public Sprite[] bottomRightSprites = new Sprite[3];

        [Header("宝箱顶部Sprite (需按顺序)")]
        [Tooltip("宝箱左上部分的Sprite数组 (0:闭合, 1:半开, 2:全开)")]
        public Sprite[] topLeftSprites = new Sprite[3];
        
        [Tooltip("宝箱右上部分的Sprite数组 (0:闭合, 1:半开, 2:全开)")]
        public Sprite[] topRightSprites = new Sprite[3];
    }
}