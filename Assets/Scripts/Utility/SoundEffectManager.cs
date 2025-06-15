using UnityEngine;
using UnityEngine.Audio; // 需要引入以使用 AudioMixerGroup
using System.Collections.Generic;
using System.Collections;

// 移除了 namespace GameAudio

/// <summary>
/// 全局音效管理器，使用对象池播放一次性音效，并允许路由到指定的AudioMixerGroup。
/// </summary>
public class SoundEffectManager : MonoBehaviour
{
    #region Singleton
    public static SoundEffectManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 使管理器跨场景存在
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePool();
    }
    #endregion

    [Header("Audio Configuration")]
    [Tooltip("用于播放音效的 AudioSource 预制体。预制体上应挂载 AudioSource 组件。")]
    public GameObject audioSourcePrefab;

    [Tooltip("初始化的对象池大小。")]
    public int poolSize = 10;

    [Tooltip("音效输出到的 Audio Mixer Group (例如 SFX 组)。")]
    public AudioMixerGroup sfxMixerGroup; // 用于指定SFX输出组

    private List<AudioSource> pooledAudioSources;
    private Queue<AudioSource> availableAudioSources;

    /// <summary>
    /// 初始化对象池。
    /// </summary>
    private void InitializePool()
    {
        pooledAudioSources = new List<AudioSource>(poolSize);
        availableAudioSources = new Queue<AudioSource>(poolSize);

        if (audioSourcePrefab == null)
        {
            Debug.LogError("[SoundEffectManager] AudioSource Prefab 未分配！对象池无法初始化。");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(audioSourcePrefab, transform); // 作为子对象创建，便于管理
            AudioSource source = obj.GetComponent<AudioSource>();
            if (source != null)
            {
                obj.SetActive(false); // 初始时禁用
                source.playOnAwake = false;
                if (sfxMixerGroup != null)
                {
                    source.outputAudioMixerGroup = sfxMixerGroup; // 设置输出到指定的Mixer Group
                }
                else
                {
                    Debug.LogWarning("[SoundEffectManager] SFX Mixer Group 未分配。音效将输出到 Master。");
                }
                pooledAudioSources.Add(source);
                availableAudioSources.Enqueue(source);
            }
            else
            {
                Debug.LogError("[SoundEffectManager] AudioSource Prefab 上没有找到 AudioSource 组件！");
                Destroy(obj); // 销毁无效对象
            }
        }
        Debug.Log($"[SoundEffectManager] 对象池初始化完成，大小: {availableAudioSources.Count}");
    }

    /// <summary>
    /// 从对象池获取一个可用的 AudioSource。
    /// </summary>
    /// <returns>可用的 AudioSource，如果没有则返回 null。</returns>
    private AudioSource GetPooledAudioSource()
    {
        if (availableAudioSources.Count > 0)
        {
            AudioSource source = availableAudioSources.Dequeue();
            source.gameObject.SetActive(true);
            return source;
        }
        else
        {
            // 可选：如果池已空，动态扩展池或返回null/打印警告
            Debug.LogWarning("[SoundEffectManager] 对象池已空！考虑增加 Pool Size。");
            // 动态扩展示例 (简单版本):
            if (audioSourcePrefab != null)
            {
                GameObject obj = Instantiate(audioSourcePrefab, transform);
                AudioSource newSource = obj.GetComponent<AudioSource>();
                if (newSource != null)
                {
                    newSource.playOnAwake = false;
                    if (sfxMixerGroup != null) newSource.outputAudioMixerGroup = sfxMixerGroup;
                    pooledAudioSources.Add(newSource); // 添加到总列表，但不立即加入可用队列
                    obj.SetActive(true); // 直接激活使用
                    Debug.Log("[SoundEffectManager] 动态扩展了一个AudioSource。");
                    return newSource;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// 将 AudioSource 归还到对象池。
    /// </summary>
    /// <param name="source">要归还的 AudioSource。</param>
    private void ReturnAudioSourceToPool(AudioSource source)
    {
        if (source != null)
        {
            source.Stop(); // 确保停止播放
            source.clip = null; // 清除 clip 引用
            source.gameObject.SetActive(false);
            if (!availableAudioSources.Contains(source)) // 防止重复添加
            {
                availableAudioSources.Enqueue(source);
            }
        }
    }

    /// <summary>
    /// 在指定位置播放音效。
    /// </summary>
    /// <param name="clip">要播放的音频片段。</param>
    /// <param name="position">播放位置。</param>
    /// <param name="volume">音量 (0.0 到 1.0)。</param>
    /// <param name="pitch">音高 (例如 1.0 为正常音高)。</param>
    public void PlaySoundAtPoint(AudioClip clip, Vector3 position, float volume = 1.0f, float pitch = 1.0f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[SoundEffectManager] 尝试播放空的 AudioClip。");
            return;
        }

        AudioSource source = GetPooledAudioSource();
        if (source != null)
        {
            source.transform.position = position; // 设置播放位置
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 1.0f; // 强制3D音效，因为 PlayClipAtPoint 是3D的

            // 确保再次设置Mixer Group，以防动态扩展的实例未设置
            if (sfxMixerGroup != null && source.outputAudioMixerGroup != sfxMixerGroup)
            {
                 source.outputAudioMixerGroup = sfxMixerGroup;
            }

            source.Play();
            StartCoroutine(ReturnToPoolAfterPlay(source, clip.length / pitch)); // 根据音高调整时长
        }
    }
    
    /// <summary>
    /// 播放2D音效，忽略位置。
    /// </summary>
    public void Play2DSound(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[SoundEffectManager] 尝试播放空的 AudioClip (2D)。");
            return;
        }

        AudioSource source = GetPooledAudioSource();
        if (source != null)
        {
            // 对于2D音效，位置通常不重要，可以固定在Listener附近或(0,0,0)
            source.transform.localPosition = Vector3.zero; 
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 0.0f; // 强制2D音效

            if (sfxMixerGroup != null && source.outputAudioMixerGroup != sfxMixerGroup)
            {
                 source.outputAudioMixerGroup = sfxMixerGroup;
            }

            source.Play();
            StartCoroutine(ReturnToPoolAfterPlay(source, clip.length / pitch));
        }
    }


    /// <summary>
    /// 音频播放完毕后将其归还到对象池的协程。
    /// </summary>
    private IEnumerator ReturnToPoolAfterPlay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnAudioSourceToPool(source);
    }

    // 可选：在销毁时清理对象池
    private void OnDestroy()
    {
        if (pooledAudioSources != null)
        {
            foreach (AudioSource source in pooledAudioSources)
            {
                if (source != null && source.gameObject != null)
                {
                    Destroy(source.gameObject);
                }
            }
            pooledAudioSources.Clear();
            availableAudioSources.Clear();
        }
    }
}