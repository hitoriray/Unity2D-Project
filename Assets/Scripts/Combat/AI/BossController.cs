using System.Collections;
using Combat.Interfaces;
using UI;
using UnityEngine;
using AmbianceSystem;

namespace Combat
{
    /// <summary>
    /// 泰拉瑞亚克苏鲁之眼Boss控制器
    /// 实现两阶段Boss战斗系统，包括飞行AI、冲撞攻击和血条管理
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Collider2D))]
    public class BossController : MonoBehaviour, IDamageable
    {
        [Header("Boss基础配置")]
        [Tooltip("Boss名称")]
        public string bossName = "克苏鲁之眼";
        [Tooltip("最大血量")]
        public float maxHealth = 2800f;
        [Tooltip("第二阶段触发血量百分比")]
        [Range(0.1f, 0.8f)]
        public float phase2HealthThreshold = 0.5f;

        [Header("移动配置")]
        [Tooltip("飞行速度")]
        public float flySpeed = 8f;
        [Tooltip("与玩家保持的距离")]
        public float maintainDistance = 5f;
        [Tooltip("距离容忍范围")]
        public float distanceTolerance = 1f;
        [Tooltip("转向速度")]
        public float turnSpeed = 3f;

        [Header("攻击配置")]
        [Tooltip("冲撞攻击伤害")]
        public float chargeAttackDamage = 25f;
        [Tooltip("冲撞攻击力度")]
        public float chargeForce = 20f;
        [Tooltip("冲撞攻击冷却时间")]
        public float chargeAttackCooldown = 3f;
        [Tooltip("冲撞攻击持续时间")]
        public float chargeDuration = 1.5f;
        [Tooltip("攻击触发范围")]
        public float attackRange = 2f;
        [Tooltip("玩家层级")]
        public LayerMask playerLayer = 1 << 6;

        [Header("阶段1配置")]
        [Tooltip("第一阶段移动间隔")]
        public float phase1MoveInterval = 2f;
        [Tooltip("第一阶段攻击间隔")]
        public float phase1AttackInterval = 4f;

        [Header("阶段2配置")]
        [Tooltip("第二阶段移动速度倍率")]
        public float phase2SpeedMultiplier = 1.5f;
        [Tooltip("第二阶段移动间隔")]
        public float phase2MoveInterval = 1.2f;
        [Tooltip("第二阶段攻击间隔")]
        public float phase2AttackInterval = 2.5f;

        [Header("视觉效果")]
        [Tooltip("第一阶段Sprite")]
        public Sprite phase1Sprite;
        [Tooltip("第二阶段Sprite")]
        public Sprite phase2Sprite;
        [Tooltip("受伤闪烁持续时间")]
        public float hurtFlashDuration = 0.2f;

        [Header("音效")]
        [Tooltip("Boss出现音效")]
        public AudioClip spawnSound;
        [Tooltip("第一阶段背景音乐")]
        public AudioClip phase1Music;
        [Tooltip("第二阶段背景音乐")]
        public AudioClip phase2Music;
        [Tooltip("攻击音效")]
        public AudioClip attackSound;
        [Tooltip("受伤音效")]
        public AudioClip hurtSound;
        [Tooltip("死亡音效")]
        public AudioClip deathSound;
        
        [Header("Boss战音乐")]
        [Tooltip("Boss战背景音乐（覆盖氛围音乐）")]
        public AudioClip bossBattleMusic;
        [Tooltip("Boss音乐淡入时间")]
        public float bossMusicFadeInTime = 3f;
        [Tooltip("Boss音乐淡出时间")]
        public float bossMusicFadeOutTime = 2f;

        [Header("调试选项")]
        [Tooltip("启用调试信息")]
        public bool enableDebugInfo = true;
        [Tooltip("调试信息间隔")]
        public float debugInfoInterval = 3f;

        // Boss状态枚举
        public enum BossState { Spawning, Phase1_Flying, Phase1_Charging, Phase2_Flying, Phase2_Charging, Dying, Dead }
        
        // 私有变量
        private BossState currentState = BossState.Spawning;
        public float currentHealth;
        public int currentPhase = 1;
        private Transform player;
        private Vector2 targetPosition;
        private bool isCharging = false;
        private Vector2 chargeDirection;
        
        // 组件引用
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Collider2D bossCollider;
        private AudioSource audioSource;
        
        // 计时器
        private float moveTimer;
        private float attackTimer;
        private float chargeTimer;
        private float debugTimer;
        
        // 协程引用
        private Coroutine currentMovementCoroutine;
        private Coroutine hurtEffectCoroutine;
        private Coroutine spawnCoroutine;
        
        // 视觉效果
        private Color originalColor = Color.white;
        private Vector3 baseScale = Vector3.one;

        #region Unity生命周期
        void Awake()
        {
            // 获取组件
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            bossCollider = GetComponent<Collider2D>();
            audioSource = GetComponent<AudioSource>();
            
            // 基础设置
            baseScale = transform.localScale;
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        void Start()
        {
            // 初始化Boss
            InitializeBoss();
            
            // 开始生成流程
            StartSpawning();
        }

        void FixedUpdate()
        {
            // 更新Boss逻辑
            UpdateBossLogic();
            
            // 更新计时器
            UpdateTimers();
            
            // 调试信息
            if (enableDebugInfo && debugTimer <= 0)
            {
                PrintDebugInfo();
                debugTimer = debugInfoInterval;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // 攻击检测
            if (isCharging && IsPlayerCollider(other))
            {
                PerformAttackOnPlayer(other);
            }
        }
        #endregion

        #region Boss初始化
        private void InitializeBoss()
        {
            // 设置血量
            currentHealth = maxHealth;
            
            // 寻找玩家
            FindPlayer();
            
            // 配置Rigidbody2D
            if (rb != null)
            {
                rb.gravityScale = 0f; // Boss飞行，不受重力影响
                rb.drag = 2f; // 适度阻力，让移动更平滑
            }
            
            // 设置初始Sprite
            if (spriteRenderer != null && phase1Sprite != null)
            {
                spriteRenderer.sprite = phase1Sprite;
            }
            
            // 初始化血条UI
            if (BossHealthBarUI.Instance != null)
            {
                BossHealthBarUI.Instance.Show(bossName, maxHealth);
            }
            else
            {
                Debug.LogWarning("[BossController] BossHealthBarUI.Instance为空，血条可能无法显示");
            }
            
            Debug.Log($"[BossController] {bossName} 初始化完成 - 血量: {maxHealth}");
        }

        private void StartSpawning()
        {
            if (spawnCoroutine != null)
                StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(SpawnSequence());
        }

        private IEnumerator SpawnSequence()
        {
            currentState = BossState.Spawning;
            
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
            
            // 播放第一阶段音乐
            PlayMusic(phase1Music);
            
            // 进入第一阶段
            EnterPhase1();
            
            spawnCoroutine = null;
        }
        #endregion

        #region 状态机管理
        private void UpdateBossLogic()
        {
            switch (currentState)
            {
                case BossState.Spawning:
                    // 生成阶段不需要更新逻辑
                    break;
                    
                case BossState.Phase1_Flying:
                    UpdatePhase1Flying();
                    break;
                    
                case BossState.Phase1_Charging:
                    UpdateCharging();
                    break;
                    
                case BossState.Phase2_Flying:
                    UpdatePhase2Flying();
                    break;
                    
                case BossState.Phase2_Charging:
                    UpdateCharging();
                    break;
                    
                case BossState.Dying:
                case BossState.Dead:
                    // 死亡状态不需要更新
                    break;
            }
        }

        private void EnterPhase1()
        {
            currentPhase = 1;
            currentState = BossState.Phase1_Flying;
            moveTimer = phase1MoveInterval;
            attackTimer = phase1AttackInterval;
            
            Debug.Log($"[BossController] 进入第一阶段");
            
            // 启动Boss战音乐
            StartBossMusic();
            
            // 更新血条阶段
            if (BossHealthBarUI.Instance != null)
            {
                BossHealthBarUI.Instance.SetPhase(1);
            }
        }

        private void EnterPhase2()
        {
            currentPhase = 2;
            currentState = BossState.Phase2_Flying;
            moveTimer = phase2MoveInterval;
            attackTimer = phase2AttackInterval;
            
            Debug.Log($"[BossController] 进入第二阶段！Boss变得愤怒了！");
            
            // 切换到第二阶段Sprite
            if (spriteRenderer != null && phase2Sprite != null)
            {
                spriteRenderer.sprite = phase2Sprite;
            }
            
            // 播放第二阶段音乐
            PlayMusic(phase2Music);
            
            // 更新血条阶段
            if (BossHealthBarUI.Instance != null)
            {
                BossHealthBarUI.Instance.SetPhase(2);
            }
            
            // 第二阶段特效（可选：屏幕震动、粒子效果等）
            StartCoroutine(Phase2TransitionEffect());
        }

        private IEnumerator Phase2TransitionEffect()
        {
            // 简单的缩放效果表示愤怒
            Vector3 originalScale = transform.localScale;
            Vector3 enlargedScale = originalScale * 1.2f;
            
            float duration = 0.5f;
            float elapsedTime = 0f;
            
            // 放大
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                transform.localScale = Vector3.Lerp(originalScale, enlargedScale, t);
                yield return null;
            }
            
            // 缩回
            elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                transform.localScale = Vector3.Lerp(enlargedScale, originalScale, t);
                yield return null;
            }
            
            transform.localScale = originalScale;
        }
        #endregion

        #region 第一阶段AI
        private void UpdatePhase1Flying()
        {
            if (player == null) return;
            
            // 围绕玩家飞行
            if (moveTimer <= 0)
            {
                SetNewTargetPosition();
                moveTimer = phase1MoveInterval;
            }
            
            // 向目标位置移动
            FlyTowardsTarget(flySpeed);
            
            // 检查是否可以攻击
            if (attackTimer <= 0 && CanAttackPlayer())
            {
                StartChargeAttack();
                attackTimer = phase1AttackInterval;
            }
        }

        private void SetNewTargetPosition()
        {
            if (player == null) return;
            
            // 在玩家周围生成随机位置
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = maintainDistance + Random.Range(-distanceTolerance, distanceTolerance);
            
            Vector2 playerPos = player.position;
            targetPosition = playerPos + new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance
            );
            
            // 确保目标位置在合理范围内
            targetPosition.y = Mathf.Max(targetPosition.y, playerPos.y + 2f);
        }
        #endregion

        #region 第二阶段AI
        private void UpdatePhase2Flying()
        {
            if (player == null) return;
            
            // 更激进的移动模式
            if (moveTimer <= 0)
            {
                SetAggressiveTargetPosition();
                moveTimer = phase2MoveInterval;
            }
            
            // 更快的移动速度
            FlyTowardsTarget(flySpeed * phase2SpeedMultiplier);
            
            // 更频繁的攻击
            if (attackTimer <= 0 && CanAttackPlayer())
            {
                StartChargeAttack();
                attackTimer = phase2AttackInterval;
            }
        }

        private void SetAggressiveTargetPosition()
        {
            if (player == null) return;
            
            Vector2 playerPos = player.position;
            
            // 第二阶段更靠近玩家
            float aggressiveDistance = maintainDistance * 0.7f;
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            
            targetPosition = playerPos + new Vector2(
                Mathf.Cos(angle) * aggressiveDistance,
                Mathf.Sin(angle) * aggressiveDistance
            );
            
            // 在玩家上方保持最小高度
            targetPosition.y = Mathf.Max(targetPosition.y, playerPos.y + 1.5f);
        }
        #endregion

        #region 移动系统
        private void FlyTowardsTarget(float speed)
        {
            Vector2 currentPos = transform.position;
            Vector2 direction = (targetPosition - currentPos).normalized;
            
            // 平滑移动
            Vector2 targetVelocity = direction * speed;
            rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, Time.fixedDeltaTime * turnSpeed);
            
            // 面向移动方向
            if (direction.x != 0)
            {
                transform.localScale = new Vector3(
                    direction.x > 0 ? baseScale.x : -baseScale.x,
                    baseScale.y,
                    baseScale.z
                );
            }
        }
        #endregion

        #region 攻击系统
        private bool CanAttackPlayer()
        {
            if (player == null) return false;
            
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            return distanceToPlayer <= attackRange;
        }

        private void StartChargeAttack()
        {
            if (player == null) return;
            
            // 计算冲撞方向
            chargeDirection = (player.position - transform.position).normalized;
            
            // 切换到攻击状态
            if (currentPhase == 1)
                currentState = BossState.Phase1_Charging;
            else
                currentState = BossState.Phase2_Charging;
            
            isCharging = true;
            chargeTimer = chargeDuration;
            
            // 播放攻击音效
            PlaySound(attackSound);
            
            Debug.Log($"[BossController] 开始冲撞攻击，方向: {chargeDirection}");
        }

        private void UpdateCharging()
        {
            if (chargeTimer > 0)
            {
                // 冲撞移动
                float currentChargeForce = chargeForce;
                if (currentPhase == 2)
                    currentChargeForce *= phase2SpeedMultiplier;
                
                rb.velocity = chargeDirection * currentChargeForce;
            }
            else
            {
                // 冲撞结束，回到飞行状态
                EndChargeAttack();
            }
        }

        private void EndChargeAttack()
        {
            isCharging = false;
            
            // 恢复飞行状态
            if (currentPhase == 1)
                currentState = BossState.Phase1_Flying;
            else
                currentState = BossState.Phase2_Flying;
            
            // 设置新的目标位置
            if (currentPhase == 1)
                SetNewTargetPosition();
            else
                SetAggressiveTargetPosition();
            
            Debug.Log("[BossController] 冲撞攻击结束，回到飞行状态");
        }

        private void PerformAttackOnPlayer(Collider2D playerCollider)
        {
            IDamageable playerDamageable = playerCollider.GetComponent<IDamageable>();
            if (playerDamageable != null)
            {
                // 创建伤害信息
                Vector2 hitPoint = playerCollider.transform.position;
                Vector2 hitDirection = (hitPoint - (Vector2)transform.position).normalized;
                
                DamageInfo damageInfo = new DamageInfo(
                    chargeAttackDamage,
                    DamageType.Physical,
                    false, // 非暴击
                    gameObject,
                    hitPoint,
                    hitDirection,
                    chargeForce * 0.5f // 击退力
                );
                
                // 施加伤害
                playerDamageable.TakeDamage(damageInfo);
                
                Debug.Log($"[BossController] 对玩家造成 {chargeAttackDamage} 点伤害");
                
                // 结束冲撞攻击
                EndChargeAttack();
            }
        }

        private bool IsPlayerCollider(Collider2D other)
        {
            // 优先检查PlayerController组件
            if (other.GetComponent<PlayerController>() != null)
                return true;
            
            // 备用检查：Layer匹配
            return ((1 << other.gameObject.layer) & playerLayer) != 0;
        }
        #endregion

        #region 伤害系统
        public void TakeDamage(DamageInfo damageInfo)
        {
            if (currentState == BossState.Dead) return;
            
            float damage = damageInfo.GetDisplayDamage();
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            
            Debug.Log($"[BossController] {bossName} 受到 {damage} 点伤害，剩余血量: {currentHealth}/{maxHealth}");
            
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
            
            // 检查阶段转换
            float healthPercentage = currentHealth / maxHealth;
            if (currentPhase == 1 && healthPercentage <= phase2HealthThreshold)
            {
                EnterPhase2();
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
                // 变红效果
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(hurtFlashDuration);
                spriteRenderer.color = originalColor;
            }
            
            hurtEffectCoroutine = null;
        }
        #endregion

        #region 死亡系统
        private void Die()
        {
            currentState = BossState.Dying;
            
            Debug.Log($"[BossController] {bossName} 死亡");
            
            // 停止所有移动
            rb.velocity = Vector2.zero;
            
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
            // 死亡动画（可以添加更复杂的效果）
            float deathDuration = 3f;
            float elapsedTime = 0f;
            
            Vector3 originalScale = transform.localScale;
            
            while (elapsedTime < deathDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / deathDuration;
                
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
            
            currentState = BossState.Dead;
            
            // 可以在这里触发掉落物品、经验等
            OnBossDefeated();
            
            // 销毁Boss对象
            Destroy(gameObject);
        }

        private void OnBossDefeated()
        {
            Debug.Log($"[BossController] {bossName} 被击败！");
            // 这里可以添加：
            // - 掉落物品
            // - 经验奖励
            // - 解锁成就
            // - 播放胜利音乐
        }
        #endregion

        #region 工具方法
        private void UpdateTimers()
        {
            moveTimer -= Time.fixedDeltaTime;
            attackTimer -= Time.fixedDeltaTime;
            chargeTimer -= Time.fixedDeltaTime;
            debugTimer -= Time.fixedDeltaTime;
        }

        private void FindPlayer()
        {
            // 查找玩家对象
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log($"[BossController] 找到玩家: {player.name}");
            }
            else
            {
                Debug.LogError("[BossController] 未找到带有Player标签的对象！");
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void PlayMusic(AudioClip music)
        {
            if (audioSource != null && music != null)
            {
                audioSource.clip = music;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        
        /// <summary>
        /// 启动Boss战音乐
        /// </summary>
        private void StartBossMusic()
        {
            if (bossBattleMusic == null)
            {
                Debug.LogWarning("[BossController] Boss战音乐未设置，跳过音乐播放");
                return;
            }
            
            if (AmbianceManager.Instance == null)
            {
                Debug.LogWarning("[BossController] AmbianceManager实例未找到，无法播放Boss音乐");
                return;
            }
            
            Debug.Log($"[BossController] 开始播放Boss战音乐: {bossBattleMusic.name}");
            AmbianceManager.Instance.StartBossMusic(bossBattleMusic, bossMusicFadeInTime);
        }
        
        /// <summary>
        /// 停止Boss战音乐，恢复氛围音乐
        /// </summary>
        private void StopBossMusic()
        {
            if (AmbianceManager.Instance == null)
            {
                Debug.LogWarning("[BossController] AmbianceManager实例未找到，无法停止Boss音乐");
                return;
            }
            
            Debug.Log("[BossController] 停止Boss战音乐，恢复氛围音乐");
            AmbianceManager.Instance.StopBossMusic(bossMusicFadeOutTime);
        }

        private void PrintDebugInfo()
        {
            if (player == null) return;
            
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            float healthPercentage = (currentHealth / maxHealth) * 100f;
            
            Debug.Log($"[BossController] {bossName} 状态报告:" +
                     $"\n  当前状态: {currentState}" +
                     $"\n  当前阶段: {currentPhase}" +
                     $"\n  血量: {currentHealth:F0}/{maxHealth:F0} ({healthPercentage:F1}%)" +
                     $"\n  与玩家距离: {distanceToPlayer:F2}" +
                     $"\n  目标位置: {targetPosition}" +
                     $"\n  是否冲撞中: {isCharging}" +
                     $"\n  移动计时器: {moveTimer:F1}" +
                     $"\n  攻击计时器: {attackTimer:F1}");
        }
        #endregion

        #region Gizmos调试
        void OnDrawGizmosSelected()
        {
            // 绘制攻击范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 绘制维持距离范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, maintainDistance);
            
            // 绘制目标位置
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(targetPosition, Vector3.one * 0.5f);
            
            // 绘制到玩家的连线
            if (player != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, player.position);
            }
            
            // 绘制冲撞方向
            if (isCharging)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, chargeDirection * 3f);
            }
        }
        #endregion

        #region 调试方法
        [ContextMenu("测试受伤")]
        private void TestTakeDamage()
        {
            if (Application.isPlaying)
            {
                DamageInfo testDamage = new DamageInfo(100f, DamageType.Physical, gameObject, transform.position);
                TakeDamage(testDamage);
            }
        }

        [ContextMenu("强制进入第二阶段")]
        private void ForcePhase2()
        {
            if (Application.isPlaying && currentPhase == 1)
            {
                EnterPhase2();
            }
        }

        [ContextMenu("立即死亡")]
        private void InstantDeath()
        {
            if (Application.isPlaying)
            {
                currentHealth = 0f;
                Die();
            }
        }
        #endregion
    }
} 