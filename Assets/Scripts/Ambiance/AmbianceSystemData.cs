using UnityEngine;

namespace AmbianceSystem
{
    /// <summary>
    /// 定义不同的地形/生物群系类型。
    /// </summary>
    public enum BiomeType
    {
        None,     // 用于未定义或特殊情况
        Generic,  // 通用区域 (例如，通用地表草地、通用地下洞穴)
        Forest,
        Desert,
        Snow
        // 未来可以添加更多，例如: Corruption, Jungle, Hallow, etc.
    }

    /// <summary>
    /// 定义垂直分层（地表/地下）。
    /// </summary>
    public enum VerticalLayer
    {
        Surface,    // 地表
        Underground // 地下
    }

    /// <summary>
    /// 定义一天中的不同时间段。
    /// </summary>
    public enum TimeOfDay
    {
        Any,   // 用于不区分时间的配置 (例如，洞穴音乐可能不分昼夜)
        Day,
        Night
        // 未来可以添加更多，例如: Dawn, Dusk
    }

    /// <summary>
    /// ScriptableObject 用于定义特定情境下的氛围设置（背景图片和音乐）。
    /// </summary>
    [CreateAssetMenu(fileName = "AmbianceProfile", menuName = "Ambiance/Ambiance Profile", order = 1)]
    public class AmbianceProfile : ScriptableObject
    {
        [Header("Context Definition")]
        [Tooltip("用于在编辑器中识别此配置的名称")]
        public string profileName = "New Ambiance Profile";

        [Tooltip("此配置适用的地形类型")]
        public BiomeType biome = BiomeType.Generic;

        [Tooltip("此配置适用的垂直分层（地表/地下）")]
        public VerticalLayer layer = VerticalLayer.Surface;

        [Tooltip("此配置适用的时间段")]
        public TimeOfDay timeOfDay = TimeOfDay.Day;

        [Header("Assets")]
        [Tooltip("用于此情境的背景图片")]
        public Sprite backgroundImage;

        [Tooltip("用于此情境的背景音乐")]
        public AudioClip backgroundMusic;

        [Header("Transition Settings")]
        [Tooltip("背景图片淡入淡出效果的持续时间（秒）")]
        [Range(0f, 10f)]
        public float imageFadeDuration = 1.0f;

        [Tooltip("背景音乐交叉淡化效果的持续时间（秒）")]
        [Range(0f, 10f)]
        public float musicFadeDuration = 2.0f;

        [Tooltip("此氛围配置下的背景音乐音量 (0 到 1)")]
        [Range(0f, 1f)]
        public float musicVolume = 1.0f; // 新增音乐音量字段
    }
}