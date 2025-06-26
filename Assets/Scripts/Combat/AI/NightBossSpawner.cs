using System.Collections;
using UnityEngine;
using UI;
using AmbianceSystem;

namespace Combat
{
    /// <summary>
    /// 夜间Boss生成管理器
    /// 负责在夜晚时检测玩家并生成Boss，同时管理Boss血量UI的显示
    /// </summary>
    public class NightBossSpawner : MonoBehaviour
    {
        [Header("Boss生成配置")]
        [Tooltip("Boss预制体")]
        public GameObject bossPrefab;
        [Tooltip("最小生成距离（距离玩家）")]
        public float minSpawnDistance = 15f;
        [Tooltip("最大生成距离（距离玩家）")]
        public float maxSpawnDistance = 25f;
        [Tooltip("生成高度偏移（相对于玩家）")]
        public float spawnHeightOffset = 8f;
        [Tooltip("每夜Boss生成概率 (0-1)")]
        [Range(0f, 1f)]
        public float spawnChance = 0.7f;
        
        [Header("生成条件")]
        [Tooltip("夜晚开始后的延迟时间（秒）")]
        public float nightStartDelay = 5f;
        [Tooltip("是否只在夜晚第一次生成")]
        public bool oncePerNight = true;
        [Tooltip("Boss消失的最大距离")]
        public float despawnDistance = 50f;
        
        [Header("生成特效")]
        [Tooltip("Boss生成时的音效")]
        public AudioClip bossSpawnSound;
        [Tooltip("Boss警告音效")]
        public AudioClip bossWarningSound;
        [Tooltip("生成特效预制体")]
        public GameObject spawnEffectPrefab;
        
        [Header("警告系统")]
        [Tooltip("生成前警告时间（秒）")]
        public float warningDuration = 3f;
        [Tooltip("警告文本UI")]
        public GameObject warningTextUI;
        
        [Header("调试")]
        [Tooltip("启用调试信息")]
        public bool enableDebugInfo = true;
        [Tooltip("强制生成Boss（调试用）")]
        public bool forceSpawnBoss = false;
        
        // 私有变量
        private PlayerController player;
        private DayNightCycleManager dayNightManager;
        private AudioSource audioSource;
        private GameObject currentBoss;
        
        // 状态跟踪
        private bool hasSpawnedThisNight = false;
        private bool isNightStartProcessing = false;
        private AmbianceSystem.TimeOfDay lastTimeOfDay;
        
        // 协程引用
        private Coroutine nightSpawnCoroutine;
        private Coroutine warningCoroutine;
        private Coroutine bossDistanceCheckCoroutine;

        #region Unity生命周期
        void Awake()
        {
            // 获取或添加AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        void Start()
        {
            // 寻找必要的组件
            player = FindObjectOfType<PlayerController>();
            dayNightManager = DayNightCycleManager.Instance;
            
            // 验证组件
            if (player == null)
            {
                Debug.LogError("[NightBossSpawner] 未找到PlayerController！");
                enabled = false;
                return;
            }
            
            if (dayNightManager == null)
            {
                Debug.LogError("[NightBossSpawner] 未找到DayNightCycleManager！");
                enabled = false;
                return;
            }
            
            if (bossPrefab == null)
            {
                Debug.LogError("[NightBossSpawner] Boss预制体未设置！");
                enabled = false;
                return;
            }
            
            // 初始化状态
            lastTimeOfDay = dayNightManager.CurrentAmbianceTime;
            
            if (enableDebugInfo)
            {
                Debug.Log("[NightBossSpawner] 夜间Boss生成器初始化完成");
            }
        }

        void Update()
        {
            // 检查时间变化
            CheckTimeChange();
            
            // 处理强制生成（调试用）
            if (forceSpawnBoss)
            {
                forceSpawnBoss = false;
                ForceSpawnBoss();
            }
            
            // 检查Boss距离（如果存在）
            CheckBossDistance();
        }
        #endregion

        #region 时间检测与Boss生成
        /// <summary>
        /// 检查时间变化
        /// </summary>
        private void CheckTimeChange()
        {
            AmbianceSystem.TimeOfDay currentTime = dayNightManager.CurrentAmbianceTime;
            
            // 检测夜晚开始
            if (lastTimeOfDay != currentTime)
            {
                if (currentTime == AmbianceSystem.TimeOfDay.Night && !isNightStartProcessing)
                {
                    OnNightStart();
                }
                else if (currentTime == AmbianceSystem.TimeOfDay.Day)
                {
                    OnDayStart();
                }
                
                lastTimeOfDay = currentTime;
            }
        }

        /// <summary>
        /// 夜晚开始时的处理
        /// </summary>
        private void OnNightStart()
        {
            if (enableDebugInfo)
            {
                Debug.Log("[NightBossSpawner] 夜晚开始，准备生成Boss");
            }
            
            isNightStartProcessing = true;
            hasSpawnedThisNight = false;
            
            // 开始夜间生成流程
            if (nightSpawnCoroutine != null)
                StopCoroutine(nightSpawnCoroutine);
            nightSpawnCoroutine = StartCoroutine(NightSpawnSequence());
        }

        /// <summary>
        /// 白天开始时的处理
        /// </summary>
        private void OnDayStart()
        {
            if (enableDebugInfo)
            {
                Debug.Log("[NightBossSpawner] 白天开始，重置夜间状态");
            }
            
            isNightStartProcessing = false;
            hasSpawnedThisNight = false;
            
            // 停止夜间生成协程
            if (nightSpawnCoroutine != null)
            {
                StopCoroutine(nightSpawnCoroutine);
                nightSpawnCoroutine = null;
            }
            
            // 如果Boss仍然存在，让它消失（可选）
            if (currentBoss != null)
            {
                DespawnBoss("白天开始");
            }
        }

        /// <summary>
        /// 夜间生成序列
        /// </summary>
        private IEnumerator NightSpawnSequence()
        {
            // 等待夜晚开始延迟
            yield return new WaitForSeconds(nightStartDelay);
            
            // 检查是否应该生成Boss
            if (ShouldSpawnBoss())
            {
                yield return StartCoroutine(SpawnBossWithWarning());
            }
            
            isNightStartProcessing = false;
            nightSpawnCoroutine = null;
        }

        /// <summary>
        /// 检查是否应该生成Boss
        /// </summary>
        private bool ShouldSpawnBoss()
        {
            // 检查是否已经生成过
            if (oncePerNight && hasSpawnedThisNight)
            {
                if (enableDebugInfo)
                    Debug.Log("[NightBossSpawner] 本夜已生成过Boss，跳过");
                return false;
            }
            
            // 检查是否已有Boss存在
            if (currentBoss != null)
            {
                if (enableDebugInfo)
                    Debug.Log("[NightBossSpawner] Boss已存在，跳过生成");
                return false;
            }
            
            // 检查生成概率
            float randomValue = Random.Range(0f, 1f);
            if (randomValue > spawnChance)
            {
                if (enableDebugInfo)
                    Debug.Log($"[NightBossSpawner] 生成概率检查失败 ({randomValue:F2} > {spawnChance:F2})");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// 带警告的Boss生成
        /// </summary>
        private IEnumerator SpawnBossWithWarning()
        {
            // 播放警告音效
            PlaySound(bossWarningSound);
            
            // 显示警告UI
            if (warningCoroutine != null)
                StopCoroutine(warningCoroutine);
            warningCoroutine = StartCoroutine(ShowWarningSequence());
            
            // 等待警告时间
            yield return new WaitForSeconds(warningDuration);
            
            // 生成Boss
            SpawnBoss();
        }

        /// <summary>
        /// 显示警告序列
        /// </summary>
        private IEnumerator ShowWarningSequence()
        {
            if (warningTextUI != null)
            {
                warningTextUI.SetActive(true);
                
                // 可以在这里添加闪烁效果
                float elapsedTime = 0f;
                while (elapsedTime < warningDuration)
                {
                    // 简单的闪烁效果
                    bool shouldShow = (Time.time % 0.5f) < 0.25f;
                    warningTextUI.SetActive(shouldShow);
                    
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                
                warningTextUI.SetActive(false);
            }
            
            warningCoroutine = null;
        }
        #endregion

        #region Boss生成与管理
        /// <summary>
        /// 生成Boss
        /// </summary>
        private void SpawnBoss()
        {
            Vector3 spawnPosition = CalculateSpawnPosition();
            
            if (enableDebugInfo)
            {
                Debug.Log($"[NightBossSpawner] 在位置 {spawnPosition} 生成Boss");
            }
            
            // 实例化Boss
            currentBoss = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
            
            // 确保Boss是激活状态
            if (currentBoss != null)
            {
                currentBoss.SetActive(true);
                
                if (enableDebugInfo)
                {
                    Debug.Log($"[NightBossSpawner] Boss已激活 - Active状态: {currentBoss.activeInHierarchy}");
                }
            }
            
            // 播放生成音效
            PlaySound(bossSpawnSound);
            
            // 生成特效
            if (spawnEffectPrefab != null)
            {
                GameObject effect = Instantiate(spawnEffectPrefab, spawnPosition, Quaternion.identity);
                Destroy(effect, 3f); // 3秒后销毁特效
            }
            
            // 确保Boss血量UI显示
            EnsureBossHealthUI();
            
            // 标记已生成
            hasSpawnedThisNight = true;
            
            // 开始距离检查
            if (bossDistanceCheckCoroutine != null)
                StopCoroutine(bossDistanceCheckCoroutine);
            bossDistanceCheckCoroutine = StartCoroutine(BossDistanceCheckRoutine());
        }

        /// <summary>
        /// 计算Boss生成位置
        /// </summary>
        private Vector3 CalculateSpawnPosition()
        {
            Vector3 playerPos = player.transform.position;
            
            // 计算随机角度和距离
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            
            // 计算生成位置
            Vector3 spawnPos = playerPos + new Vector3(
                Mathf.Cos(angle) * distance,
                spawnHeightOffset,
                0f
            );
            
            return spawnPos;
        }

        /// <summary>
        /// 确保Boss血量UI正确显示
        /// </summary>
        private void EnsureBossHealthUI()
        {
            // 等待一帧，让Boss完全初始化
            StartCoroutine(EnsureBossHealthUIDelayed());
        }

        private IEnumerator EnsureBossHealthUIDelayed()
        {
            // 等待几帧，确保所有组件都完全初始化
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            
            if (currentBoss != null)
            {
                if (enableDebugInfo)
                {
                    Debug.Log($"[NightBossSpawner] 开始确保Boss血量UI显示 - Boss Active: {currentBoss.activeInHierarchy}");
                }
                
                // 尝试获取Boss的血量信息
                var bossController = currentBoss.GetComponent<BossBehaviorDesignerController>();
                
                if (bossController != null)
                {
                    if (enableDebugInfo)
                    {
                        Debug.Log($"[NightBossSpawner] 找到BossBehaviorDesignerController - Boss名称: {bossController.bossName}, 血量: {bossController.maxHealth}");
                    }
                    
                    // 检查BossHealthBarUI.Instance是否存在
                    if (BossHealthBarUI.Instance == null)
                    {
                        Debug.LogWarning("[NightBossSpawner] BossHealthBarUI.Instance为null，尝试手动查找...");
                        
                        // 手动查找BossHealthBarUI
                        var healthBarUI = FindObjectOfType<BossHealthBarUI>();
                        if (healthBarUI != null)
                        {
                            Debug.Log("[NightBossSpawner] 手动找到BossHealthBarUI，尝试激活...");
                            if (!healthBarUI.gameObject.activeInHierarchy)
                            {
                                healthBarUI.gameObject.SetActive(true);
                            }
                            
                            // 等待一帧让Awake执行
                            yield return null;
                        }
                    }
                    
                    // 再次检查Instance
                    if (BossHealthBarUI.Instance != null)
                    {
                        // 手动显示血条UI
                        BossHealthBarUI.Instance.Show(bossController.bossName, bossController.maxHealth);
                        
                        if (enableDebugInfo)
                        {
                            Debug.Log("[NightBossSpawner] Boss血量UI已成功显示");
                        }
                    }
                    else
                    {
                        Debug.LogError("[NightBossSpawner] 仍然无法获取BossHealthBarUI.Instance！请检查场景中是否存在BossHealthBarUI组件");
                    }
                }
                else
                {
                    Debug.LogWarning($"[NightBossSpawner] Boss预制体上没有找到BossBehaviorDesignerController组件！Boss对象: {currentBoss.name}");
                    
                    // 尝试查找所有可能的Boss控制器组件
                    var allComponents = currentBoss.GetComponents<MonoBehaviour>();
                    if (enableDebugInfo)
                    {
                        Debug.Log($"[NightBossSpawner] Boss上的所有组件: {string.Join(", ", System.Array.ConvertAll(allComponents, c => c.GetType().Name))}");
                    }
                }
            }
            else
            {
                Debug.LogError("[NightBossSpawner] currentBoss为null，无法设置血量UI");
            }
        }

        /// <summary>
        /// Boss距离检查协程
        /// </summary>
        private IEnumerator BossDistanceCheckRoutine()
        {
            while (currentBoss != null && player != null)
            {
                float distance = Vector3.Distance(currentBoss.transform.position, player.transform.position);
                
                if (distance > despawnDistance)
                {
                    if (enableDebugInfo)
                    {
                        Debug.Log($"[NightBossSpawner] Boss距离过远 ({distance:F1} > {despawnDistance})，移除Boss");
                    }
                    
                    DespawnBoss("距离过远");
                    break;
                }
                
                yield return new WaitForSeconds(2f); // 每2秒检查一次
            }
            
            bossDistanceCheckCoroutine = null;
        }

        /// <summary>
        /// 检查Boss距离（在Update中调用）
        /// </summary>
        private void CheckBossDistance()
        {
            // 这个方法主要用于清理无效的Boss引用
            if (currentBoss == null && bossDistanceCheckCoroutine != null)
            {
                StopCoroutine(bossDistanceCheckCoroutine);
                bossDistanceCheckCoroutine = null;
            }
        }

        /// <summary>
        /// 移除Boss
        /// </summary>
        private void DespawnBoss(string reason)
        {
            if (currentBoss != null)
            {
                if (enableDebugInfo)
                {
                    Debug.Log($"[NightBossSpawner] 移除Boss - 原因: {reason}");
                }
                
                // 隐藏血量UI
                if (BossHealthBarUI.Instance != null)
                {
                    BossHealthBarUI.Instance.Hide();
                }
                
                // 销毁Boss对象
                Destroy(currentBoss);
                currentBoss = null;
            }
            
            // 停止距离检查协程
            if (bossDistanceCheckCoroutine != null)
            {
                StopCoroutine(bossDistanceCheckCoroutine);
                bossDistanceCheckCoroutine = null;
            }
        }

        /// <summary>
        /// 强制生成Boss（调试用）
        /// </summary>
        private void ForceSpawnBoss()
        {
            if (currentBoss != null)
            {
                DespawnBoss("强制重生");
            }
            
            SpawnBoss();
            Debug.Log("[NightBossSpawner] 强制生成Boss完成");
        }
        #endregion

        #region 工具方法
        /// <summary>
        /// 播放音效
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// 获取当前Boss实例
        /// </summary>
        public GameObject GetCurrentBoss()
        {
            return currentBoss;
        }

        /// <summary>
        /// 检查是否有Boss存在
        /// </summary>
        public bool HasActiveBoss()
        {
            return currentBoss != null;
        }
        #endregion

        #region 调试方法
        void OnDrawGizmosSelected()
        {
            if (player == null) return;
            
            Vector3 playerPos = player.transform.position;
            
            // 绘制生成范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerPos, minSpawnDistance);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(playerPos, maxSpawnDistance);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerPos, despawnDistance);
            
            // 绘制生成高度
            Gizmos.color = Color.cyan;
            Vector3 spawnHeight = playerPos + Vector3.up * spawnHeightOffset;
            Gizmos.DrawWireCube(spawnHeight, Vector3.one * 2f);
        }

        [ContextMenu("强制生成Boss")]
        private void DebugForceSpawn()
        {
            if (Application.isPlaying)
            {
                forceSpawnBoss = true;
            }
        }

        [ContextMenu("移除当前Boss")]
        private void DebugRemoveBoss()
        {
            if (Application.isPlaying && currentBoss != null)
            {
                DespawnBoss("调试移除");
            }
        }
        #endregion
    }
} 