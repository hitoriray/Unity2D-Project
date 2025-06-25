using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AmbianceSystem
{
    /// <summary>
    /// 管理动态背景图片和音乐，根据玩家所处的地形和游戏时间进行切换。
    /// 现在使用 SpriteRenderer 控制背景图片，并使其跟随相机铺满屏幕。
    /// 音乐在相同时会无缝播放，仅调整音量。
    /// 支持Boss战音乐覆盖功能。
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

        [Header("Boss Music Settings")]
        [Tooltip("Boss音乐淡入淡出持续时间")]
        public float bossMusicFadeDuration = 2f;
        [Tooltip("Boss音乐音量")]
        [Range(0f, 1f)]
        public float bossMusicVolume = 0.8f;

        private AmbianceProfile currentActiveProfile; // 当前实际生效的Profile
        private Coroutine activeImageFadeCoroutine;
        private Coroutine activeMusicFadeCoroutine;

        // Boss音乐覆盖系统
        private bool isBossMusicActive = false; // 是否正在播放Boss音乐
        private AudioClip currentBossMusic; // 当前Boss音乐
        private AmbianceProfile savedProfile; // Boss战前保存的氛围配置
        private AudioClip savedMusic; // Boss战前的音乐
        private float savedMusicVolume; // Boss战前的音乐音量
        private float savedMusicTime; // Boss战前的音乐播放时间

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
                
                // 如果正在播放Boss音乐，跳过正常的氛围检查
                if (!isBossMusicActive)
                {
                    UpdateAmbiance();
                }
            }
        }

        void UpdateAmbiance()
        {
            // 如果正在播放Boss音乐，不更新氛围
            if (isBossMusicActive) return;

            BiomeType currentBiome = GetCurrentBiome();
            TimeOfDay currentTimeOfDay = GetCurrentTimeOfDay();
            AmbianceProfile targetProfile = FindMatchingProfile(currentBiome, currentTimeOfDay);

            if (targetProfile != currentActiveProfile) 
            {
                // Debug.Log($"[AmbianceManager] Profile Transition: From '{currentActiveProfile?.profileName ?? "None"}' to '{targetProfile?.profileName ?? "None"}'");
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
                        // Debug.Log($"[AmbianceManager] Image change: '{currentDisplayedSprite?.name ?? "None"}' -> '{newTargetSprite.name}'. Starting image fade.");
                        activeImageFadeCoroutine = StartCoroutine(FadeSpriteCoroutine(newTargetSprite, newProfile.imageFadeDuration));
                    }
                }
                else 
                {
                    // Debug.Log($"[AmbianceManager] New Profile '{newProfile?.profileName ?? "None"}' has no image or is null. Hiding current image.");
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
                //  Debug.Log($"[AmbianceManager] Image '{newTargetSprite.name}' is same as current. Ensuring visibility and adjustment.");
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
                    // Debug.Log($"[AmbianceManager] New Profile has no music or is null. Fading out current music: {currentPlayingClip.name}");
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
                    // Debug.Log($"[AmbianceManager] Music '{newTargetMusic.name}' is same. Current Vol: {musicAudioSource1.volume}, Target Vol: {newTargetVolume}");
                    if (Mathf.Abs(musicAudioSource1.volume - newTargetVolume) > 0.01f)
                    {
                        // TODO: Smooth volume transition here if desired
                        musicAudioSource1.volume = newTargetVolume;
                        // Debug.Log($"[AmbianceManager] Adjusted volume for '{newTargetMusic.name}' to {newTargetVolume}.");
                    }
                    if (musicAudioSource2 != null && musicAudioSource2.isPlaying && musicAudioSource2.clip == newTargetMusic)
                    {
                        musicAudioSource2.Stop(); 
                    }
                }
                else
                {
                    // Debug.Log($"[AmbianceManager] Music change: '{(currentPlayingClip?.name ?? "None")}' -> '{newTargetMusic.name}'. Starting crossfade.");
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

        #region Boss Music System
        
        /// <summary>
        /// 开始播放Boss音乐，覆盖当前氛围音乐
        /// </summary>
        /// <param name="bossMusic">Boss音乐AudioClip</param>
        /// <param name="fadeInDuration">淡入时间，如果为0则使用默认值</param>
        public void StartBossMusic(AudioClip bossMusic, float fadeInDuration = 0f)
        {
            if (bossMusic == null)
            {
                Debug.LogWarning("[AmbianceManager] Boss音乐为空，无法播放！");
                return;
            }

            if (isBossMusicActive && currentBossMusic == bossMusic)
            {
                Debug.Log("[AmbianceManager] 相同的Boss音乐已在播放中，跳过");
                return;
            }

            float finalFadeDuration = fadeInDuration > 0 ? fadeInDuration : bossMusicFadeDuration;
            
            Debug.Log($"[AmbianceManager] 开始播放Boss音乐: {bossMusic.name}");
            
            // 保存当前状态（仅在第一次进入Boss战时保存）
            if (!isBossMusicActive)
            {
                SaveCurrentMusicState();
            }
            
            // 设置Boss音乐状态
            isBossMusicActive = true;
            currentBossMusic = bossMusic;
            
            // 停止当前的音乐过渡
            if (activeMusicFadeCoroutine != null)
            {
                StopCoroutine(activeMusicFadeCoroutine);
                activeMusicFadeCoroutine = null;
            }
            
            // 开始播放Boss音乐
            activeMusicFadeCoroutine = StartCoroutine(FadeToBossMusicCoroutine(bossMusic, finalFadeDuration));
        }
        
        /// <summary>
        /// 停止Boss音乐，恢复之前的氛围音乐
        /// </summary>
        /// <param name="fadeOutDuration">淡出时间，如果为0则使用默认值</param>
        public void StopBossMusic(float fadeOutDuration = 0f)
        {
            if (!isBossMusicActive)
            {
                Debug.Log("[AmbianceManager] 当前没有播放Boss音乐");
                return;
            }
            
            float finalFadeDuration = fadeOutDuration > 0 ? fadeOutDuration : bossMusicFadeDuration;
            
            Debug.Log("[AmbianceManager] 停止Boss音乐，恢复氛围音乐");
            
            // 停止当前的音乐过渡
            if (activeMusicFadeCoroutine != null)
            {
                StopCoroutine(activeMusicFadeCoroutine);
                activeMusicFadeCoroutine = null;
            }
            
            // 恢复氛围音乐
            activeMusicFadeCoroutine = StartCoroutine(RestoreAmbianceMusicCoroutine(finalFadeDuration));
        }
        
        /// <summary>
        /// 检查是否正在播放Boss音乐
        /// </summary>
        public bool IsBossMusicPlaying()
        {
            return isBossMusicActive;
        }
        
        /// <summary>
        /// 获取当前Boss音乐
        /// </summary>
        public AudioClip GetCurrentBossMusic()
        {
            return isBossMusicActive ? currentBossMusic : null;
        }
        
        private void SaveCurrentMusicState()
        {
            savedProfile = currentActiveProfile;
            
            // 保存当前播放的音乐状态
            if (musicAudioSource1 != null && musicAudioSource1.isPlaying)
            {
                savedMusic = musicAudioSource1.clip;
                savedMusicVolume = musicAudioSource1.volume;
                savedMusicTime = musicAudioSource1.time;
            }
            else if (musicAudioSource2 != null && musicAudioSource2.isPlaying)
            {
                savedMusic = musicAudioSource2.clip;
                savedMusicVolume = musicAudioSource2.volume;
                savedMusicTime = musicAudioSource2.time;
            }
            else
            {
                savedMusic = null;
                savedMusicVolume = 1f;
                savedMusicTime = 0f;
            }
            
            Debug.Log($"[AmbianceManager] 保存音乐状态: {savedMusic?.name ?? "None"}, Volume: {savedMusicVolume}, Time: {savedMusicTime}");
        }
        
        private IEnumerator FadeToBossMusicCoroutine(AudioClip bossMusic, float fadeDuration)
        {
            if (musicAudioSource1 == null || musicAudioSource2 == null)
            {
                activeMusicFadeCoroutine = null;
                yield break;
            }

            // 使用source2播放Boss音乐
            musicAudioSource2.clip = bossMusic;
            musicAudioSource2.volume = 0f;
            musicAudioSource2.loop = true;
            musicAudioSource2.Play();

            float elapsedTime = 0f;
            float startVolume1 = musicAudioSource1.isPlaying ? musicAudioSource1.volume : 0f;

            if (fadeDuration <= 0) fadeDuration = 0.01f;

            // 交叉淡入淡出
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / fadeDuration);
                
                // 淡出当前音乐
                if (musicAudioSource1.isPlaying)
                {
                    musicAudioSource1.volume = Mathf.Lerp(startVolume1, 0f, t);
                }
                
                // 淡入Boss音乐
                musicAudioSource2.volume = Mathf.Lerp(0f, bossMusicVolume, t);
                yield return null;
            }

            // 停止原音乐
            if (musicAudioSource1.isPlaying)
            {
                musicAudioSource1.Stop();
            }
            musicAudioSource1.volume = 0f;
            musicAudioSource1.clip = null;

            // 完成Boss音乐设置
            musicAudioSource2.volume = bossMusicVolume;

            // 交换音频源引用，保持source1为主音源的约定
            AudioSource temp = musicAudioSource1;
            musicAudioSource1 = musicAudioSource2;
            musicAudioSource2 = temp;
            
            activeMusicFadeCoroutine = null;
        }
        
        private IEnumerator RestoreAmbianceMusicCoroutine(float fadeDuration)
        {
            if (musicAudioSource1 == null || musicAudioSource2 == null)
            {
                activeMusicFadeCoroutine = null;
                yield break;
            }

            // 重置Boss音乐状态
            isBossMusicActive = false;
            currentBossMusic = null;
            
            // 如果有保存的音乐，恢复播放
            if (savedMusic != null)
            {
                musicAudioSource2.clip = savedMusic;
                musicAudioSource2.volume = 0f;
                musicAudioSource2.loop = true;
                musicAudioSource2.Play();
                
                // 尝试恢复播放位置（如果音乐长度允许）
                if (savedMusicTime < savedMusic.length)
                {
                    musicAudioSource2.time = savedMusicTime;
                }

                float elapsedTime = 0f;
                float startVolume1 = musicAudioSource1.isPlaying ? musicAudioSource1.volume : 0f;

                if (fadeDuration <= 0) fadeDuration = 0.01f;

                // 交叉淡入淡出
                while (elapsedTime < fadeDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsedTime / fadeDuration);
                    
                    // 淡出Boss音乐
                    if (musicAudioSource1.isPlaying)
                    {
                        musicAudioSource1.volume = Mathf.Lerp(startVolume1, 0f, t);
                    }
                    
                    // 淡入氛围音乐
                    musicAudioSource2.volume = Mathf.Lerp(0f, savedMusicVolume, t);
                    yield return null;
                }

                // 停止Boss音乐
                if (musicAudioSource1.isPlaying)
                {
                    musicAudioSource1.Stop();
                }
                musicAudioSource1.volume = 0f;
                musicAudioSource1.clip = null;

                // 完成氛围音乐恢复
                musicAudioSource2.volume = savedMusicVolume;

                // 交换音频源引用
                AudioSource temp = musicAudioSource1;
                musicAudioSource1 = musicAudioSource2;
                musicAudioSource2 = temp;
            }
            else
            {
                // 没有保存的音乐，直接淡出Boss音乐
                yield return StartCoroutine(FadeOutMusicOnlyCoroutine(fadeDuration, musicAudioSource1));
            }
            
            // 恢复正常的氛围系统
            currentActiveProfile = savedProfile;
            
            // 立即检查并更新氛围（可能环境已经改变）
            UpdateAmbiance();
            
            activeMusicFadeCoroutine = null;
        }
        
        #endregion
    }
}