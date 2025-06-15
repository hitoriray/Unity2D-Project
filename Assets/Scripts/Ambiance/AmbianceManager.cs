using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AmbianceSystem
{
    /// <summary>
    /// 管理动态背景图片和音乐，根据玩家所处的地形和游戏时间进行切换。
    /// 现在使用 SpriteRenderer 控制背景图片，并使其跟随相机铺满屏幕。
    /// 音乐在相同时会无缝播放，仅调整音量。
    /// </summary>
    public class AmbianceManager : MonoBehaviour
    {
        #region Singleton
        public static AmbianceManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        [Header("Configuration")]
        [Tooltip("所有可用的氛围配置列表")]
        public List<AmbianceProfile> ambianceProfiles = new List<AmbianceProfile>();

        [Tooltip("检查环境（地形/时间）变化的频率（秒）")]
        public float ambianceCheckInterval = 1.0f;

        [Header("Sprite Renderer Components")]
        [Tooltip("用于显示当前背景图片 (SpriteRenderer)")]
        public SpriteRenderer backgroundSpriteRenderer1;
        [Tooltip("用于淡入新背景图片 (SpriteRenderer)")]
        public SpriteRenderer backgroundSpriteRenderer2;

        [Header("Audio Components")]
        [Tooltip("用于播放当前/淡出背景音乐")]
        public AudioSource musicAudioSource1;
        [Tooltip("用于淡入新背景音乐")]
        public AudioSource musicAudioSource2;

        private AmbianceProfile currentActiveProfile; // 当前实际生效的Profile
        private Coroutine activeImageFadeCoroutine;
        private Coroutine activeMusicFadeCoroutine;

        // 外部依赖
        private PlayerController playerController;
        private TerrainGeneration terrainGenerator;
        private DayNightCycleManager dayNightCycleManager;
        private Camera mainCamera;

        void Start()
        {
            playerController = FindObjectOfType<PlayerController>();
            terrainGenerator = FindObjectOfType<TerrainGeneration>();
            dayNightCycleManager = DayNightCycleManager.Instance;
            mainCamera = Camera.main;

            if (playerController == null) Debug.LogError("[AmbianceManager] PlayerController 未找到！");
            if (terrainGenerator == null) Debug.LogError("[AmbianceManager] TerrainGeneration 未找到！");
            if (dayNightCycleManager == null) Debug.LogError("[AmbianceManager] DayNightCycleManager 未找到！");
            if (mainCamera == null) Debug.LogError("[AmbianceManager] Main Camera 未找到!");

            if (backgroundSpriteRenderer1 == null || backgroundSpriteRenderer2 == null)
            {
                Debug.LogError("[AmbianceManager] 背景 SpriteRenderer 组件未分配!");
                return;
            }
            if (musicAudioSource1 == null || musicAudioSource2 == null)
            {
                Debug.LogError("[AmbianceManager] 背景音乐AudioSource组件未分配!");
                return;
            }

            if (backgroundSpriteRenderer2 != null)
            {
                Color tempColor = backgroundSpriteRenderer2.color;
                tempColor.a = 0f;
                backgroundSpriteRenderer2.color = tempColor;
                backgroundSpriteRenderer2.gameObject.SetActive(false);
            }
            
            if (backgroundSpriteRenderer1 != null && backgroundSpriteRenderer1.sprite != null)
            {
                Color tempColor = backgroundSpriteRenderer1.color;
                tempColor.a = 1f;
                backgroundSpriteRenderer1.color = tempColor;
                backgroundSpriteRenderer1.gameObject.SetActive(true);
                AdjustSpriteRendererToCamera(backgroundSpriteRenderer1);
            }

            if(musicAudioSource1 != null) musicAudioSource1.playOnAwake = false;
            if(musicAudioSource2 != null) musicAudioSource2.playOnAwake = false;

            UpdateAmbiance();
            StartCoroutine(CheckAmbianceLoop());
        }

        void LateUpdate()
        {
            if (mainCamera == null) return;

            if (backgroundSpriteRenderer1 != null && backgroundSpriteRenderer1.gameObject.activeInHierarchy && backgroundSpriteRenderer1.sprite != null)
            {
                AdjustSpriteRendererToCamera(backgroundSpriteRenderer1);
            }
        }

        void AdjustSpriteRendererToCamera(SpriteRenderer spriteRenderer)
        {
            if (mainCamera == null || spriteRenderer == null || spriteRenderer.sprite == null) return;
            if (!mainCamera.orthographic)
            {
                // Debug.LogWarning("[AmbianceManager] AdjustSpriteRendererToCamera: 主相机不是正交相机。"); 
                return;
            }
            
            spriteRenderer.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, spriteRenderer.transform.position.z);
            float camHeight = mainCamera.orthographicSize * 2f;
            float camWidth = camHeight * mainCamera.aspect;
            Vector2 spriteOriginalSize = spriteRenderer.sprite.bounds.size;
            if (spriteOriginalSize.x == 0 || spriteOriginalSize.y == 0) return;
            float scaleX = camWidth / spriteOriginalSize.x;
            float scaleY = camHeight / spriteOriginalSize.y;
            spriteRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        IEnumerator CheckAmbianceLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(ambianceCheckInterval);
                UpdateAmbiance();
            }
        }

        void UpdateAmbiance()
        {
            BiomeType currentBiome = GetCurrentBiome();
            TimeOfDay currentTimeOfDay = GetCurrentTimeOfDay();
            AmbianceProfile targetProfile = FindMatchingProfile(currentBiome, currentTimeOfDay);

            if (targetProfile != currentActiveProfile) 
            {
                Debug.Log($"[AmbianceManager] Profile Transition: From '{currentActiveProfile?.profileName ?? "None"}' to '{targetProfile?.profileName ?? "None"}'");
                TransitionToProfile(targetProfile);
            }
        }

        BiomeType GetCurrentBiome()
        {
            if (playerController != null && terrainGenerator != null)
            {
                return terrainGenerator.GetAmbianceBiomeTypeAtWorldPosition(playerController.transform.position);
            }
            return BiomeType.None;
        }

        TimeOfDay GetCurrentTimeOfDay()
        {
            if (dayNightCycleManager != null)
            {
                return dayNightCycleManager.CurrentAmbianceTime;
            }
            return TimeOfDay.Day;
        }

        AmbianceProfile FindMatchingProfile(BiomeType biome, TimeOfDay time)
        {
            foreach (var profile in ambianceProfiles)
            {
                if (profile.biome == biome && profile.timeOfDay == time) return profile;
            }
            foreach (var profile in ambianceProfiles)
            {
                if (profile.biome == biome && profile.timeOfDay == TimeOfDay.Any) return profile;
            }
            return null;
        }

        void TransitionToProfile(AmbianceProfile newProfile)
        {
            if (activeImageFadeCoroutine != null) { StopCoroutine(activeImageFadeCoroutine); activeImageFadeCoroutine = null; }
            if (activeMusicFadeCoroutine != null) { StopCoroutine(activeMusicFadeCoroutine); activeMusicFadeCoroutine = null; }

            // --- 图片过渡逻辑 ---
            Sprite currentDisplayedSprite = (backgroundSpriteRenderer1 != null && backgroundSpriteRenderer1.gameObject.activeInHierarchy) ? backgroundSpriteRenderer1.sprite : null;
            Sprite newTargetSprite = newProfile?.backgroundImage;

            if (newTargetSprite != currentDisplayedSprite)
            {
                if (newTargetSprite != null)
                {
                    if (backgroundSpriteRenderer1 != null && backgroundSpriteRenderer2 != null)
                    {
                        Debug.Log($"[AmbianceManager] Image change: '{currentDisplayedSprite?.name ?? "None"}' -> '{newTargetSprite.name}'. Starting image fade.");
                        activeImageFadeCoroutine = StartCoroutine(FadeSpriteCoroutine(newTargetSprite, newProfile.imageFadeDuration));
                    }
                }
                else 
                {
                    Debug.Log($"[AmbianceManager] New Profile '{newProfile?.profileName ?? "None"}' has no image or is null. Hiding current image.");
                    if (backgroundSpriteRenderer1 != null && backgroundSpriteRenderer1.sprite != null && backgroundSpriteRenderer1.gameObject.activeInHierarchy)
                    {
                        Color c = backgroundSpriteRenderer1.color; c.a = 0; backgroundSpriteRenderer1.color = c;
                    }
                    if (backgroundSpriteRenderer2 != null && backgroundSpriteRenderer2.gameObject.activeInHierarchy) // 确保备用也隐藏
                    {
                        Color c = backgroundSpriteRenderer2.color; c.a = 0; backgroundSpriteRenderer2.color = c;
                        backgroundSpriteRenderer2.gameObject.SetActive(false);
                    }
                }
            }
            else if (newTargetSprite != null) 
            {
                 Debug.Log($"[AmbianceManager] Image '{newTargetSprite.name}' is same as current. Ensuring visibility and adjustment.");
                 if(backgroundSpriteRenderer1 != null) {
                    Color c = backgroundSpriteRenderer1.color; c.a = 1f; backgroundSpriteRenderer1.color = c;
                    backgroundSpriteRenderer1.gameObject.SetActive(true);
                    AdjustSpriteRendererToCamera(backgroundSpriteRenderer1);
                 }
            }

            // --- 音乐过渡逻辑 ---
            AudioClip currentPlayingClip = (musicAudioSource1 != null && musicAudioSource1.isPlaying && musicAudioSource1.clip != null) ? musicAudioSource1.clip : null;
            AudioClip newTargetMusic = newProfile?.backgroundMusic;
            float newTargetVolume = newProfile?.musicVolume ?? 1.0f;
            float musicFadeOutDuration = currentActiveProfile?.musicFadeDuration ?? 1.0f; 
            float musicFadeInDuration = newProfile?.musicFadeDuration ?? 1.0f;


            if (newTargetMusic == null) 
            {
                if (currentPlayingClip != null) 
                {
                    Debug.Log($"[AmbianceManager] New Profile has no music or is null. Fading out current music: {currentPlayingClip.name}");
                    activeMusicFadeCoroutine = StartCoroutine(FadeOutMusicOnlyCoroutine(musicFadeOutDuration, musicAudioSource1));
                }
                if (musicAudioSource2 != null && musicAudioSource2.isPlaying) 
                {
                     StartCoroutine(FadeOutMusicOnlyCoroutine(musicFadeOutDuration, musicAudioSource2));
                }
            }
            else 
            {
                if (musicAudioSource1.isPlaying && currentPlayingClip == newTargetMusic)
                {
                    Debug.Log($"[AmbianceManager] Music '{newTargetMusic.name}' is same. Current Vol: {musicAudioSource1.volume}, Target Vol: {newTargetVolume}");
                    if (Mathf.Abs(musicAudioSource1.volume - newTargetVolume) > 0.01f)
                    {
                        // TODO: Smooth volume transition here if desired
                        musicAudioSource1.volume = newTargetVolume;
                        Debug.Log($"[AmbianceManager] Adjusted volume for '{newTargetMusic.name}' to {newTargetVolume}.");
                    }
                    if (musicAudioSource2 != null && musicAudioSource2.isPlaying && musicAudioSource2.clip == newTargetMusic)
                    {
                        musicAudioSource2.Stop(); 
                    }
                }
                else
                {
                    Debug.Log($"[AmbianceManager] Music change: '{(currentPlayingClip?.name ?? "None")}' -> '{newTargetMusic.name}'. Starting crossfade.");
                    if (musicAudioSource1 != null && musicAudioSource2 != null)
                    {
                        activeMusicFadeCoroutine = StartCoroutine(FadeMusicCoroutine(newTargetMusic, musicFadeInDuration)); // Use new profile's fade duration
                    }
                }
            }
            currentActiveProfile = newProfile; 
        }

        IEnumerator FadeSpriteCoroutine(Sprite newSprite, float duration)
        {
            if (backgroundSpriteRenderer1 == null || backgroundSpriteRenderer2 == null || newSprite == null)
            {
                activeImageFadeCoroutine = null; yield break;
            }
            
            backgroundSpriteRenderer2.sprite = newSprite;
            AdjustSpriteRendererToCamera(backgroundSpriteRenderer2);
            
            Color color2 = backgroundSpriteRenderer2.color;
            color2.a = 0f;
            backgroundSpriteRenderer2.color = color2;
            backgroundSpriteRenderer2.gameObject.SetActive(true);

            Color color1 = backgroundSpriteRenderer1.color;
            float startAlpha1 = (backgroundSpriteRenderer1.sprite != null && backgroundSpriteRenderer1.gameObject.activeInHierarchy) ? color1.a : 0f;

            float elapsedTime = 0f;
            if (duration <= 0) duration = 0.01f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);

                if (backgroundSpriteRenderer1.sprite != null && backgroundSpriteRenderer1.gameObject.activeInHierarchy)
                {
                    color1.a = Mathf.Lerp(startAlpha1, 0f, t);
                    backgroundSpriteRenderer1.color = color1;
                }

                color2.a = Mathf.Lerp(0f, 1f, t);
                backgroundSpriteRenderer2.color = color2;
                
                yield return null;
            }

            backgroundSpriteRenderer1.sprite = newSprite;
            AdjustSpriteRendererToCamera(backgroundSpriteRenderer1);
            color1 = backgroundSpriteRenderer1.color;
            color1.a = 1f;
            backgroundSpriteRenderer1.color = color1;
            backgroundSpriteRenderer1.gameObject.SetActive(true);

            color2 = backgroundSpriteRenderer2.color;
            color2.a = 0f;
            backgroundSpriteRenderer2.color = color2;
            backgroundSpriteRenderer2.gameObject.SetActive(false);

            activeImageFadeCoroutine = null;
        }

        IEnumerator FadeMusicCoroutine(AudioClip newClip, float duration)
        {
            if (musicAudioSource1 == null || musicAudioSource2 == null || newClip == null)
            {
                activeMusicFadeCoroutine = null; yield break;
            }

            musicAudioSource2.clip = newClip;
            // Target volume for the new clip should come from the newProfile (which is currentActiveProfile at this point)
            float targetVolume2 = currentActiveProfile?.musicVolume ?? 1.0f; 
            musicAudioSource2.volume = 0f; 
            musicAudioSource2.loop = true;
            musicAudioSource2.Play();

            float elapsedTime = 0f;
            float startVolume1 = musicAudioSource1.isPlaying ? musicAudioSource1.volume : 0f;

            if (duration <= 0) duration = 0.01f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                if (musicAudioSource1.isPlaying)
                {
                    musicAudioSource1.volume = Mathf.Lerp(startVolume1, 0f, t);
                }
                musicAudioSource2.volume = Mathf.Lerp(0f, targetVolume2, t);
                yield return null;
            }

            if (musicAudioSource1.isPlaying) musicAudioSource1.Stop();
            musicAudioSource1.volume = 0f;
            musicAudioSource1.clip = null;

            musicAudioSource2.volume = targetVolume2;

            AudioSource temp = musicAudioSource1;
            musicAudioSource1 = musicAudioSource2;
            musicAudioSource2 = temp;
            
            activeMusicFadeCoroutine = null;
        }
        
        IEnumerator FadeOutMusicOnlyCoroutine(float duration, AudioSource sourceToFade = null)
        {
            if (sourceToFade == null) 
            {
                if (musicAudioSource1 != null && musicAudioSource1.isPlaying) sourceToFade = musicAudioSource1;
                else if (musicAudioSource2 != null && musicAudioSource2.isPlaying) sourceToFade = musicAudioSource2;
            }

            if (sourceToFade == null || !sourceToFade.isPlaying)
            {
                activeMusicFadeCoroutine = null; yield break;
            }

            float elapsedTime = 0f;
            float startVolume = sourceToFade.volume;
            if (duration <= 0) duration = 0.01f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                sourceToFade.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsedTime / duration));
                yield return null;
            }

            sourceToFade.Stop();
            sourceToFade.clip = null;
            sourceToFade.volume = 1f; 
            activeMusicFadeCoroutine = null;
        }
    }
}