using System.Collections;
using UnityEngine;
using BehaviorDesigner.Runtime;
using Combat.Interfaces;
using UI;
using AmbianceSystem;

namespace Combat
{
    /// <summary>
    /// 使用Behavior Designer的Boss控制器
    /// 提供基础功能，AI逻辑由行为树控制
    /// </summary>
    [RequireComponent(typeof(BehaviorTree), typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public class BossBehaviorDesignerController : MonoBehaviour, IDamageable
    {
        [Header("Boss基础配置")]
        [Tooltip("Boss名称")]
        public string bossName = "克苏鲁之眼";
        [Tooltip("最大血量")]
        public float maxHealth = 2800f;
        [Tooltip("当前血量")]
        public float currentHealth { get; private set; }
        [Tooltip("当前阶段")]
        public int currentPhase { get; set; } = 1;
        
        [Header("视觉效果")]
        [Tooltip("受伤闪烁持续时间")]
        public float hurtFlashDuration = 0.2f;
        [Tooltip("死亡动画持续时间")]
        public float deathAnimationDuration = 3f;
        
        [Header("Sprite动画配置")]
        [Tooltip("第一阶段Sprite动画帧")]
        public Sprite[] phase1AnimationFrames = new Sprite[3];
        [Tooltip("第二阶段Sprite动画帧")]
        public Sprite[] phase2AnimationFrames = new Sprite[3];
        [Tooltip("第一阶段动画播放速度（帧/秒）")]
        public float phase1AnimationSpeed = 8f;
        [Tooltip("第二阶段动画播放速度（帧/秒）")]
        public float phase2AnimationSpeed = 12f;
        [Tooltip("是否启用Sprite动画")]
        public bool enableSpriteAnimation = true;
        
        [Header("Sprite缩放配置")]
        [Tooltip("第一阶段的Sprite缩放")]
        public Vector3 phase1SpriteScale = Vector3.one;
        [Tooltip("第二阶段的Sprite缩放")]
        public Vector3 phase2SpriteScale = Vector3.one;
        
        [Header("音效")]
        [Tooltip("Boss出现音效")]
        public AudioClip spawnSound;
        [Tooltip("冲锋前吼叫音效")]
        public AudioClip roarSound;
        [Tooltip("受伤音效")]
        public AudioClip hurtSound;
        [Tooltip("死亡音效")]
        public AudioClip deathSound;
        
        [Header("Boss战音乐")]
        [Tooltip("Boss战背景音乐")]
        public AudioClip bossBattleMusic;
        [Tooltip("Boss音乐淡入时间")]
        public float bossMusicFadeInTime = 3f;
        [Tooltip("Boss音乐淡出时间")]
        public float bossMusicFadeOutTime = 2f;
        
        [Header("接触伤害")]
        [Tooltip("接触伤害值")]
        public float contactDamage = 15f;
        [Tooltip("接触伤害间隔（秒）")]
        public float contactDamageInterval = 1f;
        [Tooltip("接触击退力度")]
        public float contactKnockback = 8f;
        [Tooltip("玩家层级")]
        public LayerMask playerLayerMask = 1 << 6;
        [Tooltip("攻击触发器Layer")]
        public int attackTriggerLayer = 11; // EnemyAttack层
        
        [Header("行为树配置")]
        [Tooltip("第一阶段行为树")]
        public ExternalBehaviorTree phase1BehaviorTree;
        [Tooltip("第二阶段行为树")]
        public ExternalBehaviorTree phase2BehaviorTree;
        [Tooltip("是否启用调试信息")]
        public bool enableDebugInfo = true;
        
        // 组件引用
        private BehaviorTree behaviorTree;
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private AudioSource audioSource;
        
        // 状态
        private bool isDead = false;
        private Color originalColor = Color.white;
        
        // 动画系统
        private int currentAnimationFrame = 0;
        private float animationTimer = 0f;
        private Coroutine spriteAnimationCoroutine;
        private bool isPlayingHurtEffect = false;
        
        // 协程引用
        private Coroutine hurtEffectCoroutine;
        private Coroutine spawnCoroutine;
        
        // 接触伤害系统
        private float lastContactDamageTime = 0f;
        private GameObject attackTriggerObject;
        
        #region Unity生命周期
        void Awake()
        {
            // 获取组件
            behaviorTree = GetComponent<BehaviorTree>();
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();
            
            // 如果没有AudioSource，添加一个
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // 配置Rigidbody2D
            if (rb != null)
            {
                rb.gravityScale = 0f; // Boss飞行，不受重力
                rb.drag = 2f; // 适度阻力
            }
            
            // 设置攻击触发器
            SetupAttackTrigger();
            
            // 保存原始颜色
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
            
            // 初始化动画系统
            InitializeSpriteAnimation();
        }
        
        void Start()
        {
            // 初始化Boss
            InitializeBoss();
            
            // 开始生成流程
            StartSpawning();
        }
        
        void OnDestroy()
        {
            // 停止所有协程
            StopSpriteAnimation();
            
            // 确保血条UI被隐藏
            if (BossHealthBarUI.Instance != null && !isDead)
            {
                BossHealthBarUI.Instance.Hide();
            }
            
            // 确保Boss音乐被停止（保险措施）
            if (!isDead)
            {
                StopBossMusic();
            }
        }
        
        /// <summary>
        /// 设置攻击触发器（参考史莱姆的实现）
        /// </summary>
        private void SetupAttackTrigger()
        {
            // 创建攻击触发器子对象
            attackTriggerObject = new GameObject("BossAttackTrigger");
            attackTriggerObject.transform.SetParent(transform);
            attackTriggerObject.transform.localPosition = Vector3.zero;
            attackTriggerObject.transform.localScale = Vector3.one;
            attackTriggerObject.layer = attackTriggerLayer;
            
            // 添加触发器碰撞体
            CircleCollider2D triggerCollider = attackTriggerObject.AddComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 7f; // 比Boss sprite稍大
            
            // 添加Boss攻击触发器脚本
            var attackTrigger = attackTriggerObject.AddComponent<BossContactTrigger>();
            attackTrigger.bossController = this;
        }
        
        /// <summary>
        /// 处理接触伤害（由攻击触发器调用）
        /// </summary>
        public void OnPlayerContact(Collider2D playerCollider)
        {
            // 检查接触伤害冷却
            if (isDead || Time.time - lastContactDamageTime < contactDamageInterval)
                return;
            
            IDamageable damageable = playerCollider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // 计算伤害方向（从Boss指向玩家）
                Vector2 damageDirection = (playerCollider.transform.position - transform.position).normalized;
                
                // 创建接触伤害信息
                DamageInfo contactDamageInfo = new DamageInfo(
                    contactDamage,
                    DamageType.Physical,
                    false, // 接触伤害不是暴击
                    gameObject,
                    playerCollider.transform.position,
                    damageDirection,
                    contactKnockback
                );
                
                // 造成伤害
                damageable.TakeDamage(contactDamageInfo);
                lastContactDamageTime = Time.time;
            }
        }
        #endregion
        
        #region 初始化
        private void InitializeBoss()
        {
            // 设置血量
            currentHealth = maxHealth;
            currentPhase = 1;
            isDead = false;
            
            // 设置行为树共享变量
            SetupBehaviorTreeVariables();
            
            // 应用第一阶段的缩放
            ApplySpriteScale(1);
            
            // 初始化血条UI
            if (BossHealthBarUI.Instance != null)
            {
                BossHealthBarUI.Instance.Show(bossName, maxHealth);
            }
            else
            {
                Debug.LogWarning("[BossBehaviorDesignerController] BossHealthBarUI.Instance为空，血条可能无法显示");
            }
        }
        
        private void SetupBehaviorTreeVariables()
        {
            if (behaviorTree == null) return;
            
            // 设置第一阶段行为树
            if (phase1BehaviorTree != null)
            {
                behaviorTree.ExternalBehavior = phase1BehaviorTree;
            }
            
            // 设置共享变量（这些变量可以在行为树中使用）
            behaviorTree.SetVariableValue("MaxHealth", maxHealth);
            behaviorTree.SetVariableValue("CurrentHealth", currentHealth);
            behaviorTree.SetVariableValue("CurrentPhase", currentPhase);
            behaviorTree.SetVariableValue("BossName", bossName);
            
            // 禁用行为树直到生成完成
            behaviorTree.enabled = false;
        }
        
        private void StartSpawning()
        {
            if (spawnCoroutine != null)
                StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(SpawnSequence());
        }
        
        private IEnumerator SpawnSequence()
        {
            // 播放生成音效
            PlaySound(spawnSound);
            
            // 生成动画（透明度渐变）
            if (spriteRenderer != null)
            {
                Color spawnColor = originalColor;
                spawnColor.a = 0f;
                spriteRenderer.color = spawnColor;
                
                float elapsedTime = 0f;
                float spawnDuration = 2f;
                
                while (elapsedTime < spawnDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float alpha = Mathf.Lerp(0f, 1f, elapsedTime / spawnDuration);
                    spawnColor.a = alpha;
                    spriteRenderer.color = spawnColor;
                    yield return null;
                }
                
                spriteRenderer.color = originalColor;
            }
            
            // 启动行为树
            if (behaviorTree != null)
            {
                behaviorTree.enabled = true;
            }
            
            // 确保动画正在运行
            if (enableSpriteAnimation && spriteAnimationCoroutine == null)
            {
                StartSpriteAnimation();
            }
            
            // 启动Boss战音乐
            StartBossMusic();
            
            spawnCoroutine = null;
        }
        #endregion
        
        #region Sprite动画系统
        /// <summary>
        /// 初始化Sprite动画系统
        /// </summary>
        private void InitializeSpriteAnimation()
        {
            if (!enableSpriteAnimation || spriteRenderer == null) return;
            
            // 验证第一阶段动画帧
            if (phase1AnimationFrames == null || phase1AnimationFrames.Length == 0)
            {
                Debug.LogWarning("[BossBehaviorDesignerController] 第一阶段动画帧未设置，将禁用动画");
                enableSpriteAnimation = false;
                return;
            }
            
            // 设置初始帧
            currentAnimationFrame = 0;
            if (phase1AnimationFrames[0] != null)
            {
                spriteRenderer.sprite = phase1AnimationFrames[0];
            }
            
            // 启动动画协程
            StartSpriteAnimation();
        }
        
        /// <summary>
        /// 启动Sprite动画
        /// </summary>
        private void StartSpriteAnimation()
        {
            if (!enableSpriteAnimation) return;
            
            if (spriteAnimationCoroutine != null)
                StopCoroutine(spriteAnimationCoroutine);
                
            spriteAnimationCoroutine = StartCoroutine(SpriteAnimationLoop());
        }
        
        /// <summary>
        /// 停止Sprite动画
        /// </summary>
        private void StopSpriteAnimation()
        {
            if (spriteAnimationCoroutine != null)
            {
                StopCoroutine(spriteAnimationCoroutine);
                spriteAnimationCoroutine = null;
            }
        }
        
        /// <summary>
        /// Sprite动画循环协程
        /// </summary>
        private IEnumerator SpriteAnimationLoop()
        {
            while (enableSpriteAnimation && !isDead)
            {
                // 如果正在播放受伤效果，暂停动画
                if (isPlayingHurtEffect)
                {
                    yield return null;
                    continue;
                }
                
                // 获取当前阶段的动画帧和速度
                Sprite[] currentFrames = GetCurrentAnimationFrames();
                float currentSpeed = GetCurrentAnimationSpeed();
                
                if (currentFrames == null || currentFrames.Length == 0)
                {
                    yield return null;
                    continue;
                }
                
                // 更新动画计时器
                animationTimer += Time.deltaTime;
                
                // 检查是否需要切换到下一帧
                float frameInterval = 1f / currentSpeed;
                if (animationTimer >= frameInterval)
                {
                    animationTimer = 0f;
                    
                    // 切换到下一帧
                    currentAnimationFrame = (currentAnimationFrame + 1) % currentFrames.Length;
                    
                    // 更新Sprite
                    if (currentFrames[currentAnimationFrame] != null)
                    {
                        spriteRenderer.sprite = currentFrames[currentAnimationFrame];
                    }
                }
                
                yield return null;
            }
            
            spriteAnimationCoroutine = null;
        }
        
        /// <summary>
        /// 获取当前阶段的动画帧
        /// </summary>
        private Sprite[] GetCurrentAnimationFrames()
        {
            switch (currentPhase)
            {
                case 1:
                    return phase1AnimationFrames;
                case 2:
                    return phase2AnimationFrames;
                default:
                    return phase1AnimationFrames;
            }
        }
        
        /// <summary>
        /// 获取当前阶段的动画速度
        /// </summary>
        private float GetCurrentAnimationSpeed()
        {
            switch (currentPhase)
            {
                case 1:
                    return phase1AnimationSpeed;
                case 2:
                    return phase2AnimationSpeed;
                default:
                    return phase1AnimationSpeed;
            }
        }
        
        /// <summary>
        /// 重置动画到第一帧
        /// </summary>
        private void ResetAnimationToFirstFrame()
        {
            currentAnimationFrame = 0;
            animationTimer = 0f;
            
            Sprite[] currentFrames = GetCurrentAnimationFrames();
            if (currentFrames != null && currentFrames.Length > 0 && currentFrames[0] != null)
            {
                spriteRenderer.sprite = currentFrames[0];
            }
        }
        
        /// <summary>
        /// 应用阶段对应的Sprite缩放
        /// </summary>
        private void ApplySpriteScale(int phase)
        {
            Vector3 targetScale = Vector3.one;
            
            switch (phase)
            {
                case 1:
                    targetScale = phase1SpriteScale;
                    break;
                case 2:
                    targetScale = phase2SpriteScale;
                    break;
                default:
                    targetScale = phase1SpriteScale;
                    break;
            }

            transform.localScale = targetScale;
        }
        #endregion
        
        #region Boss音乐管理
        /// <summary>
        /// 启动Boss战音乐
        /// </summary>
        private void StartBossMusic()
        {
            if (bossBattleMusic == null)
            {
                Debug.LogWarning("[BossBehaviorDesignerController] Boss战音乐未设置，跳过音乐播放");
                return;
            }
            
            if (AmbianceManager.Instance == null)
            {
                Debug.LogWarning("[BossBehaviorDesignerController] AmbianceManager实例未找到，无法播放Boss音乐");
                return;
            }
            
            Debug.Log($"[BossBehaviorDesignerController] 开始播放Boss战音乐: {bossBattleMusic.name}");
            AmbianceManager.Instance.StartBossMusic(bossBattleMusic, bossMusicFadeInTime);
        }
        
        /// <summary>
        /// 停止Boss战音乐，恢复氛围音乐
        /// </summary>
        private void StopBossMusic()
        {
            if (AmbianceManager.Instance == null)
            {
                Debug.LogWarning("[BossBehaviorDesignerController] AmbianceManager实例未找到，无法停止Boss音乐");
                return;
            }
            
            Debug.Log("[BossBehaviorDesignerController] 停止Boss战音乐，恢复氛围音乐");
            AmbianceManager.Instance.StopBossMusic(bossMusicFadeOutTime);
        }
        #endregion
        
        #region 伤害系统
        public void TakeDamage(DamageInfo damageInfo)
        {
            if (isDead) return;
            
            float damage = damageInfo.GetDisplayDamage();
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            
            // 更新行为树变量
            if (behaviorTree != null)
            {
                behaviorTree.SetVariableValue("CurrentHealth", currentHealth);
            }
            
            
            // 更新血条
            if (BossHealthBarUI.Instance != null)
            {
                BossHealthBarUI.Instance.UpdateHealth(currentHealth, maxHealth);
            }
            
            // 显示伤害数字
            ShowDamageText(damage, damageInfo.isCritical, damageInfo.hitPoint);
            
            // 受伤效果
            StartHurtEffect();
            
            // 播放受伤音效
            PlaySound(hurtSound);
            
            // 检查阶段转换（血量低于50%且还在第一阶段）
            float healthPercentage = currentHealth / maxHealth;
            if (currentPhase == 1 && healthPercentage <= 0.5f)
            {
                SwitchToPhase(2);
            }
            
            // 检查死亡
            if (currentHealth <= 0f)
            {
                Die();
            }
        }
        
        private void ShowDamageText(float damage, bool isCritical, Vector2 hitPoint)
        {
            if (DamageTextManager.Instance != null)
            {
                // Boss头部上方显示伤害
                Vector2 textPosition = (Vector2)transform.position + Vector2.up * 1.5f;
                DamageTextManager.Instance.ShowDamage(textPosition, (int)damage, isCritical, DamageType.Physical);
            }
        }
        
        private void StartHurtEffect()
        {
            if (hurtEffectCoroutine != null)
                StopCoroutine(hurtEffectCoroutine);
            hurtEffectCoroutine = StartCoroutine(HurtEffect());
        }
        
        private IEnumerator HurtEffect()
        {
            if (spriteRenderer != null)
            {
                // 暂停动画，显示受伤效果
                isPlayingHurtEffect = true;
                
                // 变红效果
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(hurtFlashDuration);
                spriteRenderer.color = originalColor;
                
                // 恢复动画
                isPlayingHurtEffect = false;
            }
            
            hurtEffectCoroutine = null;
        }
        #endregion
        
        #region 死亡系统
        private void Die()
        {
            if (isDead) return;
            isDead = true;
            
            // 停止行为树
            if (behaviorTree != null)
            {
                behaviorTree.DisableBehavior();
            }
            
            // 停止Sprite动画
            StopSpriteAnimation();
            
            // 停止所有移动
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            
            // 停止Boss战音乐，恢复氛围音乐
            StopBossMusic();
            
            // 播放死亡音效
            PlaySound(deathSound);
            
            // 隐藏血条
            if (BossHealthBarUI.Instance != null)
            {
                BossHealthBarUI.Instance.Hide();
            }
            
            // 开始死亡动画
            StartCoroutine(DeathSequence());
        }
        
        private IEnumerator DeathSequence()
        {
            float elapsedTime = 0f;
            Vector3 originalScale = transform.localScale;
            
            while (elapsedTime < deathAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / deathAnimationDuration;
                
                // 缩放动画
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                
                // 透明度动画
                if (spriteRenderer != null)
                {
                    Color color = spriteRenderer.color;
                    color.a = Mathf.Lerp(1f, 0f, t);
                    spriteRenderer.color = color;
                }
                
                yield return null;
            }
            
            // 触发死亡事件
            OnBossDefeated();
            
            // 销毁Boss对象
            Destroy(gameObject);
        }
        
        private void OnBossDefeated()
        {
            Debug.Log($"[BossBehaviorDesignerController] {bossName} 被击败！");
            // 这里可以添加：
            // - 掉落物品
            // - 经验奖励
            // - 解锁成就
            // - 播放胜利音乐
        }
        #endregion
        
        #region 工具方法
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        /// <summary>
        /// 更新行为树的血量变量（供外部调用）
        /// </summary>
        public void UpdateBehaviorTreeHealth()
        {
            if (behaviorTree != null)
            {
                behaviorTree.SetVariableValue("CurrentHealth", currentHealth);
                float healthPercentage = currentHealth / maxHealth;
                behaviorTree.SetVariableValue("HealthPercentage", healthPercentage);
            }
        }
        
        /// <summary>
        /// 更新行为树的阶段变量（供外部调用）
        /// </summary>
        public void UpdateBehaviorTreePhase(int phase)
        {
            currentPhase = phase;
            if (behaviorTree != null)
            {
                behaviorTree.SetVariableValue("CurrentPhase", phase);
            }
        }
        
        /// <summary>
        /// 切换到指定阶段的行为树
        /// </summary>
        public void SwitchToPhase(int phase)
        {
            if (behaviorTree == null) return;
            
            currentPhase = phase;
            
            // 停止当前行为树
            behaviorTree.DisableBehavior();
            
            // 切换到对应阶段的行为树
            ExternalBehaviorTree targetBehaviorTree = null;
            
            switch (phase)
            {
                case 1:
                    targetBehaviorTree = phase1BehaviorTree;
                    break;
                case 2:
                    targetBehaviorTree = phase2BehaviorTree;
                    
                    // 第二阶段视觉效果（包含缩放处理）
                    StartCoroutine(Phase2TransitionEffect());
                    break;
                default:
                    Debug.LogWarning($"[BossBehaviorDesignerController] 未知阶段: {phase}");
                    return;
            }
            
            if (targetBehaviorTree != null)
            {
                // 设置新的行为树
                behaviorTree.ExternalBehavior = targetBehaviorTree;
                
                // 更新共享变量
                behaviorTree.SetVariableValue("CurrentPhase", phase);
                behaviorTree.SetVariableValue("CurrentHealth", currentHealth);
                float healthPercentage = currentHealth / maxHealth;
                behaviorTree.SetVariableValue("HealthPercentage", healthPercentage);
                
                // 重新启动行为树
                behaviorTree.EnableBehavior();
                
                // 更新血条UI阶段
                if (BossHealthBarUI.Instance != null)
                {
                    BossHealthBarUI.Instance.SetPhase(phase);
                }
                
                // 重置动画到新阶段的第一帧
                ResetAnimationToFirstFrame();
                
                // 应用阶段对应的缩放（第二阶段的缩放由转换特效处理）
                if (phase != 2)
                {
                    ApplySpriteScale(phase);
                }
            }
            else
            {
                Debug.LogError($"[BossBehaviorDesignerController] 第{phase}阶段的行为树未设置！");
            }
        }
        
        /// <summary>
        /// 第二阶段转换特效
        /// </summary>
        private IEnumerator Phase2TransitionEffect()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 enlargedScale = originalScale * 1.3f;
            
            float duration = 0.6f;
            float elapsedTime = 0f;
            
            // 放大效果
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                transform.localScale = Vector3.Lerp(originalScale, enlargedScale, t);
                yield return null;
            }
            
            // 缩回效果 - 但是要缩回到第二阶段的正确缩放
            Vector3 phase2Scale = phase2SpriteScale;
            elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                transform.localScale = Vector3.Lerp(enlargedScale, phase2Scale, t);
                yield return null;
            }
            
            // 确保最终缩放是第二阶段的缩放
            transform.localScale = phase2Scale;
        }
        #endregion
        
        /// <summary>
        /// Boss接触触发器内部类
        /// </summary>
        public class BossContactTrigger : MonoBehaviour
        {
            [HideInInspector]
            public BossBehaviorDesignerController bossController;
            
            void OnTriggerStay2D(Collider2D other)
            {
                if (bossController == null) return;
                
                // 检查是否是玩家层级
                if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    // 通知Boss控制器处理接触伤害
                    bossController.OnPlayerContact(other);
                }
            }
            
            void OnTriggerEnter2D(Collider2D other)
            {
                if (bossController == null) return;
                
                // 检查是否是玩家层级
                if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    // 进入时也触发接触伤害
                    bossController.OnPlayerContact(other);
                }
            }
        }
    }
} 