using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Combat.Interfaces;
using UI;
using TooltipAttribute = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;

namespace Combat.AI.BehaviorDesigner
{
    /// <summary>
    /// 克苏鲁之眼Boss的基础移动任务
    /// 围绕玩家飞行，保持一定距离
    /// </summary>
    [TaskCategory("Boss/EyeOfCthulhu")]
    [TaskDescription("让Boss围绕玩家飞行，保持指定距离")]
    public class EoCFlyAroundPlayer : Action
    {
        [Tooltip("飞行速度")]
        public SharedFloat flySpeed = 8f;
        [Tooltip("与玩家保持的距离")]
        public SharedFloat maintainDistance = 5f;
        [Tooltip("距离容忍范围")]
        public SharedFloat distanceTolerance = 1f;
        [Tooltip("转向速度")]
        public SharedFloat turnSpeed = 3f;
        [Tooltip("更新目标位置的间隔")]
        public SharedFloat updateInterval = 2f;
        [Tooltip("是否始终朝向玩家")]
        public SharedBool facePlayer = true;
        [Tooltip("飞行持续时间（秒，0为无限制）")]
        public SharedFloat flyDuration = 3f;
        
        private Transform player;
        private Rigidbody2D rb;
        private Vector2 targetPosition;
        private float updateTimer;
        private float flyTimer;
        
        public override void OnStart()
        {
            // 通过PlayerController组件查找玩家
            PlayerController playerController = GameObject.FindObjectOfType<PlayerController>();
            player = playerController?.transform;
            rb = GetComponent<Rigidbody2D>();
            
            if (player == null)
            {
                Debug.LogError("[EoCFlyAroundPlayer] 未找到PlayerController组件的玩家对象!");
                return;
            }
            
            if (rb == null)
            {
                Debug.LogError("[EoCFlyAroundPlayer] 未找到Rigidbody2D组件!");
                return;
            }
            
            updateTimer = 0f;
            flyTimer = 0f;
            SetNewTargetPosition();
        }
        
        public override TaskStatus OnUpdate()
        {
            if (player == null || rb == null)
                return TaskStatus.Failure;
            
            // 检查飞行时间限制
            if (flyDuration.Value > 0)
            {
                flyTimer += Time.deltaTime;
                if (flyTimer >= flyDuration.Value)
                {
                    return TaskStatus.Success; // 任务完成，让行为树重新评估
                }
            }
            
            // 更新目标位置
            updateTimer -= Time.deltaTime;
            if (updateTimer <= 0)
            {
                SetNewTargetPosition();
                updateTimer = updateInterval.Value;
            }
            
            // 向目标位置飞行
            FlyTowardsTarget();
            
            // 持续运行
            return TaskStatus.Running;
        }
        
        private void SetNewTargetPosition()
        {
            // 在玩家周围生成随机位置
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = maintainDistance.Value + Random.Range(-distanceTolerance.Value, distanceTolerance.Value);
            
            Vector2 playerPos = player.position;
            targetPosition = playerPos + new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance
            );
            
            // 确保目标位置在玩家上方
            targetPosition.y = Mathf.Max(targetPosition.y, playerPos.y + 2f);
        }
        
        private void FlyTowardsTarget()
        {
            Vector2 currentPos = transform.position;
            Vector2 direction = (targetPosition - currentPos).normalized;
            
            // 平滑移动
            Vector2 targetVelocity = direction * flySpeed.Value;
            rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, Time.deltaTime * turnSpeed.Value);
            
            // 朝向玩家
            if (facePlayer.Value && player != null)
            {
                FaceTowardsPlayer();
            }
        }
        
        /// <summary>
        /// 让Boss朝向玩家
        /// </summary>
        private void FaceTowardsPlayer()
        {
            Vector2 directionToPlayer = (player.position - transform.position).normalized;
            
            // 计算朝向玩家的角度 (Boss正面朝下，所以需要减去90度)
            float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg + 90f;
            
            // 平滑旋转朝向玩家
            Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed.Value);
        }
        
        public override void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            if (player != null)
            {
                // 绘制维持距离
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(player.position, maintainDistance.Value);
                
                // 绘制目标位置
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(targetPosition, Vector3.one * 0.5f);
            }
            #endif
        }
    }
    
    /// <summary>
    /// 克苏鲁之眼Boss的冲撞攻击任务
    /// </summary>
    [TaskCategory("Boss/EyeOfCthulhu")]
    [TaskDescription("向玩家发起冲撞攻击")]
    public class EoCChargeAttack : Action
    {
        [Tooltip("冲撞速度")]
        public SharedFloat chargeSpeed = 20f;
        [Tooltip("冲撞持续时间")]
        public SharedFloat chargeDuration = 1.5f;
        [Tooltip("冲撞伤害")]
        public SharedFloat chargeDamage = 25f;
        [Tooltip("击退力度")]
        public SharedFloat knockbackForce = 10f;
        [Tooltip("攻击层级")]
        public LayerMask playerLayer = 1 << 6;
        [Tooltip("冲锋前吼叫延迟时间（第一阶段）")]
        public SharedFloat roarDelay = 0.5f;
        [Tooltip("第二阶段吼叫延迟时间")]
        public SharedFloat roarDelayPhase2 = 0.3f;
        [Tooltip("冲刺后的冷却时间")]
        public SharedFloat cooldownTime = 3f;
        
        private Transform player;
        private Rigidbody2D rb;
        private Vector2 chargeDirection;
        private float chargeTimer;
        private bool hasHitPlayer;
        private AudioSource audioSource;
        private bool isRoaring = false;
        private float roarTimer = 0f;
        
        // 静态冷却管理（所有Boss实例共享）
        private static float lastChargeTime = -999f;
        private static bool isOnCooldown = false;
        
        public override void OnStart()
        {
            // 检查是否在冷却中
            if (Time.time - lastChargeTime < cooldownTime.Value)
            {
                return; // 在冷却中，不执行攻击
            }
            
            // 通过PlayerController组件查找玩家
            PlayerController playerController = GameObject.FindObjectOfType<PlayerController>();
            player = playerController?.transform;
            rb = GetComponent<Rigidbody2D>();
            audioSource = GetComponent<AudioSource>();
            
            if (player == null)
            {
                Debug.LogError("[EoCChargeAttack] 未找到PlayerController组件的玩家对象!");
                return;
            }
            
            if (rb == null)
            {
                Debug.LogError("[EoCChargeAttack] 未找到Rigidbody2D组件!");
                return;
            }
            
            // 计算冲撞方向
            chargeDirection = (player.position - transform.position).normalized;
            chargeTimer = chargeDuration.Value;
            hasHitPlayer = false;
            isRoaring = true;
            
            // 根据当前阶段决定吼叫时间
            // 简化处理，默认使用第一阶段延迟
                roarTimer = roarDelay.Value;
            
            // 播放冲锋前吼叫音效
            PlayRoarSound();
        }
        
        public override TaskStatus OnUpdate()
        {
            if (rb == null)
                return TaskStatus.Failure;
            
            // 如果在冷却中，直接返回失败
            if (Time.time - lastChargeTime < cooldownTime.Value)
            {
                return TaskStatus.Failure;
            }
            
            // 处理吼叫阶段
            if (isRoaring)
            {
                roarTimer -= Time.deltaTime;
                
                if (roarTimer <= 0f)
                {
                    isRoaring = false;
                }
                else
                {
                    // 吼叫期间保持朝向玩家
                    if (player != null)
                    {
                        FaceTowardsPlayer();
                    }
                    return TaskStatus.Running;
                }
            }
            
            chargeTimer -= Time.deltaTime;
            
            if (chargeTimer > 0)
            {
                // 执行冲撞
                rb.velocity = chargeDirection * chargeSpeed.Value;
                
                // 冲撞时也保持朝向玩家
                if (player != null)
                {
                    FaceTowardsPlayer();
                }
                
                // 检测碰撞
                CheckPlayerCollision();
                
                return TaskStatus.Running;
            }
            else
            {
                // 冲撞结束，记录冷却时间
                lastChargeTime = Time.time;
                isOnCooldown = true;
                return TaskStatus.Success;
            }
        }
        
        private void CheckPlayerCollision()
        {
            if (hasHitPlayer) return;
            
            // 检测与玩家的碰撞
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f, playerLayer);
            
            foreach (var hit in hits)
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // 创建伤害信息
                    Vector2 hitPoint = hit.transform.position;
                    Vector2 hitDirection = (hitPoint - (Vector2)transform.position).normalized;
                    
                    DamageInfo damageInfo = new DamageInfo(
                        chargeDamage.Value,
                        DamageType.Physical,
                        false,
                        gameObject,
                        hitPoint,
                        hitDirection,
                        knockbackForce.Value
                    );
                    
                    // 造成伤害
                    damageable.TakeDamage(damageInfo);
                    hasHitPlayer = true;
                    
                    break;
                }
            }
        }
        
        public override void OnEnd()
        {
            // 恢复正常阻力
            if (rb != null)
            {
                rb.velocity = rb.velocity * 0.5f; // 减速
            }
        }
        
        /// <summary>
        /// 播放吼叫音效
        /// </summary>
        private void PlayRoarSound()
        {
            // 尝试使用BossBehaviorDesignerController的公共方法
            var bossController = GetComponent<BossBehaviorDesignerController>();
            if (bossController != null)
            {
                bossController.PlayRoarSound();
                // Debug.Log("[EoCChargeAttack] 通过Boss控制器播放吼叫音效");
            }
            else if (audioSource != null)
            {
                // 回退方案：直接通过AudioSource播放
                if (bossController != null && bossController.roarSound != null)
                {
                    audioSource.PlayOneShot(bossController.roarSound);
                    // Debug.Log("[EoCChargeAttack] 直接播放吼叫音效: " + bossController.roarSound.name);
                }
                else
                {
                    Debug.LogWarning("[EoCChargeAttack] 未找到BossBehaviorDesignerController或roarSound音效");
                }
            }
            else
            {
                Debug.LogWarning("[EoCChargeAttack] 未找到BossBehaviorDesignerController和AudioSource组件");
            }
        }
        
        /// <summary>
        /// 让Boss朝向玩家（冲撞攻击版本）
        /// </summary>
        private void FaceTowardsPlayer()
        {
            if (player == null) return;
            
            Vector2 directionToPlayer = (player.position - transform.position).normalized;
            
            // 计算朝向玩家的角度 (Boss正面朝下，所以需要减去90度)
            float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg + 90f;
            
            // 快速旋转朝向玩家（冲撞时更快的转向）
            Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
        
        public override void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            // 绘制攻击范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
            
            // 绘制冲撞方向
            if (chargeTimer > 0 && !isRoaring)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, chargeDirection * 3f);
            }
            
            // 绘制到玩家的朝向线
            if (player != null)
            {
                Gizmos.color = isRoaring ? Color.yellow : Color.cyan;
                Gizmos.DrawLine(transform.position, player.position);
            }
            #endif
        }
    }
    
    /// <summary>
    /// 检查Boss血量阶段的条件任务
    /// </summary>
    [TaskCategory("Boss/EyeOfCthulhu")]
    [TaskDescription("检查Boss是否进入第二阶段")]
    public class EoCCheckPhase : Conditional
    {
        [Tooltip("触发第二阶段的血量百分比")]
        public SharedFloat phase2Threshold = 0.5f;
        [Tooltip("检查的阶段（1或2）")]
        public int checkPhase = 2;
        
        private BossController bossController;
        
        public override void OnStart()
        {
            bossController = GetComponent<BossController>();
        }
        
        public override TaskStatus OnUpdate()
        {
            if (bossController == null)
                return TaskStatus.Failure;
            
            float healthPercentage = bossController.currentHealth / bossController.maxHealth;
            
            if (checkPhase == 2)
            {
                // 检查是否应该进入第二阶段
                return healthPercentage <= phase2Threshold.Value ? TaskStatus.Success : TaskStatus.Failure;
            }
            else
            {
                // 检查是否还在第一阶段
                return healthPercentage > phase2Threshold.Value ? TaskStatus.Success : TaskStatus.Failure;
            }
        }
    }
    
    /// <summary>
    /// 切换Boss阶段的动作任务
    /// </summary>
    [TaskCategory("Boss/EyeOfCthulhu")]
    [TaskDescription("切换Boss到指定阶段")]
    public class EoCSwitchPhase : Action
    {
        [Tooltip("要切换到的阶段")]
        public int targetPhase = 2;
        [Tooltip("第二阶段的Sprite")]
        public Sprite phase2Sprite;
        [Tooltip("阶段切换音效")]
        public AudioClip phaseChangeSound;
        
        private BossController bossController;
        private SpriteRenderer spriteRenderer;
        private AudioSource audioSource;
        
        public override void OnStart()
        {
            bossController = GetComponent<BossController>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();
        }
        
        public override TaskStatus OnUpdate()
        {
            // 简化阶段切换逻辑
            if (bossController == null)
                return TaskStatus.Failure;
            
            // 更新Boss控制器的阶段
            bossController.currentPhase = targetPhase;
            
            // 更换Sprite
            if (targetPhase == 2 && phase2Sprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = phase2Sprite;
            }
            
            // 播放音效
            if (phaseChangeSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(phaseChangeSound);
            }
            
            // 更新血条UI
            if (BossHealthBarUI.Instance != null)
            {
                BossHealthBarUI.Instance.SetPhase(targetPhase);
            }
            
            // 阶段转换特效
            StartPhaseTransitionEffect();
            
            return TaskStatus.Success;
        }
        
        private void StartPhaseTransitionEffect()
        {
            // 简单的缩放效果
            transform.localScale = transform.localScale * 1.2f;
            // 实际游戏中应该用协程逐渐恢复
        }
    }
    
    /// <summary>
    /// Boss待机任务
    /// </summary>
    [TaskCategory("Boss/EyeOfCthulhu")]
    [TaskDescription("Boss在原地悬浮待机")]
    public class EoCIdle : Action
    {
        [Tooltip("待机持续时间")]
        public SharedFloat idleDuration = 1f;
        [Tooltip("悬浮幅度")]
        public SharedFloat hoverAmplitude = 0.5f;
        [Tooltip("悬浮速度")]
        public SharedFloat hoverSpeed = 2f;
        
        private float idleTimer;
        private Vector3 startPosition;
        
        public override void OnStart()
        {
            idleTimer = idleDuration.Value;
            startPosition = transform.position;
        }
        
        public override TaskStatus OnUpdate()
        {
            idleTimer -= Time.deltaTime;
            
            // 悬浮动画
            float yOffset = Mathf.Sin(Time.time * hoverSpeed.Value) * hoverAmplitude.Value;
            transform.position = startPosition + Vector3.up * yOffset;
            
            // 检查是否完成
            if (idleTimer <= 0)
            {
                return TaskStatus.Success;
            }
            
            return TaskStatus.Running;
        }
    }
    
    /// <summary>
    /// 检查与玩家的距离
    /// </summary>
    [TaskCategory("Boss/EyeOfCthulhu")]
    [TaskDescription("检查Boss与玩家的距离是否在指定范围内")]
    public class EoCCheckDistance : Conditional
    {
        [Tooltip("最小距离")]
        public SharedFloat minDistance = 3f;
        [Tooltip("最大距离")]
        public SharedFloat maxDistance = 8f;
        
        private Transform player;
        
        public override void OnStart()
        {
            // 通过PlayerController组件查找玩家
            PlayerController playerController = GameObject.FindObjectOfType<PlayerController>();
            player = playerController?.transform;
            
            if (player == null)
            {
                Debug.LogError("[EoCCheckDistance] 未找到PlayerController组件的玩家对象!");
            }
        }
        
        public override TaskStatus OnUpdate()
        {
            if (player == null)
                return TaskStatus.Failure;
            
            float distance = Vector2.Distance(transform.position, player.position);
            
            if (distance >= minDistance.Value && distance <= maxDistance.Value)
            {
                return TaskStatus.Success;
            }
            
            return TaskStatus.Failure;
        }
    }
    
    /// <summary>
    /// 检查Boss冲刺冷却状态
    /// </summary>
    [TaskCategory("Boss/EyeOfCthulhu")]
    [TaskDescription("检查Boss是否可以进行冲刺攻击（不在冷却中）")]
    public class EoCCheckChargeCooldown : Conditional
    {
        [Tooltip("冷却时间")]
        public SharedFloat cooldownTime = 3f;
        
        // 获取EoCChargeAttack的静态冷却时间
        private static float GetLastChargeTime()
        {
            // 通过反射访问EoCChargeAttack的lastChargeTime
            var field = typeof(EoCChargeAttack).GetField("lastChargeTime", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (field != null)
            {
                return (float)field.GetValue(null);
            }
            return -999f;
        }
        
        public override TaskStatus OnUpdate()
        {
            float lastChargeTime = GetLastChargeTime();
            float timeSinceLastCharge = Time.time - lastChargeTime;
            
            if (timeSinceLastCharge >= cooldownTime.Value)
            {
                return TaskStatus.Success;
            }
            else
            {
                return TaskStatus.Failure;
            }
        }
    }
    
    /// <summary>
    /// 第二阶段五连冲刺攻击
    /// </summary>
    [TaskCategory("Boss/EyeOfCthulhu")]
    [TaskDescription("执行五连冲刺攻击（第二阶段专用）")]
    public class EoCMultiChargeAttack : Action
    {
        [Tooltip("冲刺速度")]
        public SharedFloat chargeSpeed = 25f;
        [Tooltip("每次冲刺持续时间")]
        public SharedFloat chargeDuration = 1f;
        [Tooltip("连击间隔时间")]
        public SharedFloat chargeInterval = 0.3f;
        [Tooltip("冲刺伤害")]
        public SharedFloat chargeDamage = 30f;
        [Tooltip("击退力度")]
        public SharedFloat knockbackForce = 12f;
        [Tooltip("攻击层级")]
        public LayerMask playerLayer = 1 << 6;
        [Tooltip("吼叫延迟时间")]
        public SharedFloat roarDelay = 0.3f;
        [Tooltip("五连击后的冷却时间")]
        public SharedFloat cooldownTime = 4f;
        [Tooltip("连击次数")]
        public int maxCharges = 5;
        
        private Transform player;
        private Rigidbody2D rb;
        private AudioSource audioSource;
        
        // 连击状态
        private int currentChargeCount = 0;
        private float chargeTimer = 0f;
        private float intervalTimer = 0f;
        private Vector2 chargeDirection;
        private bool hasHitPlayer = false;
        private bool isRoaring = false;
        private float roarTimer = 0f;
        private bool isInInterval = false;
        
        // 静态冷却管理（与单次冲刺共享）
        private static float lastMultiChargeTime = -999f;
        
        public override void OnStart()
        {
            // 检查是否在冷却中
            if (Time.time - lastMultiChargeTime < cooldownTime.Value)
            {
                return;
            }
            
            // 通过PlayerController组件查找玩家
            PlayerController playerController = GameObject.FindObjectOfType<PlayerController>();
            player = playerController?.transform;
            rb = GetComponent<Rigidbody2D>();
            audioSource = GetComponent<AudioSource>();
            
            if (player == null || rb == null)
            {
                Debug.LogError("[EoCMultiChargeAttack] 未找到必要组件!");
                return;
            }
            
            // 初始化连击
            currentChargeCount = 0;
            StartNextCharge();
        }
        
        public override TaskStatus OnUpdate()
        {
            if (player == null || rb == null)
                return TaskStatus.Failure;
            
            // 检查是否在冷却中
            if (Time.time - lastMultiChargeTime < cooldownTime.Value)
            {
                return TaskStatus.Failure;
            }
            
            // 处理吼叫阶段
            if (isRoaring)
            {
                roarTimer -= Time.deltaTime;
                if (roarTimer <= 0f)
                {
                    isRoaring = false;
                    chargeTimer = chargeDuration.Value;
                }
                else
                {
                    // 吼叫期间朝向玩家
                    FaceTowardsPlayer();
                    return TaskStatus.Running;
                }
            }
            
            // 处理连击间隔
            if (isInInterval)
            {
                intervalTimer -= Time.deltaTime;
                if (intervalTimer <= 0f)
                {
                    isInInterval = false;
                    StartNextCharge();
                }
                return TaskStatus.Running;
            }
            
            // 执行冲刺
            chargeTimer -= Time.deltaTime;
            if (chargeTimer > 0)
            {
                rb.velocity = chargeDirection * chargeSpeed.Value;
                FaceTowardsPlayer();
                CheckPlayerCollision();
                return TaskStatus.Running;
            }
            else
            {
                // 当前冲刺结束
                currentChargeCount++;
                
                if (currentChargeCount >= maxCharges)
                {
                    // 五连击完成
                    lastMultiChargeTime = Time.time;
                    return TaskStatus.Success;
                }
                else
                {
                    // 开始间隔时间
                    isInInterval = true;
                    intervalTimer = chargeInterval.Value;
                    hasHitPlayer = false; // 重置击中状态
                    return TaskStatus.Running;
                }
            }
        }
        
        private void StartNextCharge()
        {
            // 重新计算冲刺方向（瞄准玩家当前位置）
            chargeDirection = (player.position - transform.position).normalized;
            
            // 开始吼叫
            isRoaring = true;
            roarTimer = roarDelay.Value;
            
            // 播放吼叫音效
            PlayRoarSound();
        }
        
        private void CheckPlayerCollision()
        {
            if (hasHitPlayer) return;
            
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f, playerLayer);
            
            foreach (var hit in hits)
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    Vector2 hitPoint = hit.transform.position;
                    Vector2 hitDirection = (hitPoint - (Vector2)transform.position).normalized;
                    
                    DamageInfo damageInfo = new DamageInfo(
                        chargeDamage.Value,
                        DamageType.Physical,
                        false,
                        gameObject,
                        hitPoint,
                        hitDirection,
                        knockbackForce.Value
                    );
                    
                    damageable.TakeDamage(damageInfo);
                    hasHitPlayer = true;
                    
                    break;
                }
            }
        }
        
        private void PlayRoarSound()
        {
            // 尝试使用BossBehaviorDesignerController的公共方法
            var bossController = GetComponent<BossBehaviorDesignerController>();
            if (bossController != null)
            {
                bossController.PlayRoarSound();
                Debug.Log("[EoCMultiChargeAttack] 通过Boss控制器播放吼叫音效");
            }
            else if (audioSource != null)
            {
                // 回退方案：直接通过AudioSource播放
                if (bossController != null && bossController.roarSound != null)
                {
                    audioSource.PlayOneShot(bossController.roarSound);
                    Debug.Log("[EoCMultiChargeAttack] 直接播放吼叫音效: " + bossController.roarSound.name);
                }
                else
                {
                    Debug.LogWarning("[EoCMultiChargeAttack] 未找到BossBehaviorDesignerController或roarSound音效");
                }
            }
            else
            {
                Debug.LogWarning("[EoCMultiChargeAttack] 未找到BossBehaviorDesignerController和AudioSource组件");
            }
        }
        
        private void FaceTowardsPlayer()
        {
            if (player == null) return;
            
            Vector2 directionToPlayer = (player.position - transform.position).normalized;
            float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg + 90f;
            
            Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }
        
        public override void OnEnd()
        {
            if (rb != null)
            {
                rb.velocity = rb.velocity * 0.3f; // 减速
            }
        }
        
        public override void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            // 绘制攻击范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
            
            // 绘制连击进度
            if (Application.isPlaying && currentChargeCount > 0)
            {
                Vector3 textPos = transform.position + Vector3.up * 2f;
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(textPos, $"连击: {currentChargeCount}/{maxCharges}");
                #endif
            }
            
            // 绘制冲刺方向
            if (!isRoaring && !isInInterval)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, chargeDirection * 3f);
            }
            #endif
        }
    }
    
    /// <summary>
    /// 克苏鲁之眼Boss召唤仆从的任务
    /// </summary>
    [TaskCategory("Boss/EyeOfCthulhu")]
    [TaskDescription("召唤克苏鲁之眼仆从（Servants of Cthulhu）")]
    public class EoCSpawnServants : Action
    {
        [Tooltip("仆从预制体")]
        public SharedGameObject servantPrefab;
        [Tooltip("召唤数量（第一阶段）")]
        public SharedInt phase1ServantCount = 3;
        [Tooltip("召唤数量（第二阶段）")]
        public SharedInt phase2ServantCount = 5;
        [Tooltip("生成半径")]
        public SharedFloat spawnRadius = 3f;
        [Tooltip("生成高度偏移")]
        public SharedFloat spawnHeightOffset = 1f;
        [Tooltip("召唤间隔时间")]
        public SharedFloat spawnInterval = 0.2f;
        [Tooltip("召唤音效")]
        public AudioClip spawnSound;
        [Tooltip("召唤特效预制体")]
        public GameObject spawnEffectPrefab;
        [Tooltip("召唤后的等待时间")]
        public SharedFloat postSpawnWait = 1.5f;
        
        private BossController bossController;
        private AudioSource audioSource;
        private int servantsToSpawn;
        private int servantsSpawned;
        private float spawnTimer;
        private bool isSpawning;
        
        public override void OnStart()
        {
            bossController = GetComponent<BossController>();
            audioSource = GetComponent<AudioSource>();
            
            if (servantPrefab.Value == null)
            {
                Debug.LogError("[EoCSpawnServants] 仆从预制体未设置!");
                return;
            }
            
            // 根据当前阶段决定召唤数量
            if (bossController != null)
            {
                servantsToSpawn = bossController.currentPhase == 2 ? phase2ServantCount.Value : phase1ServantCount.Value;
            }
            else
            {
                servantsToSpawn = phase1ServantCount.Value;
            }
            
            servantsSpawned = 0;
            spawnTimer = 0f;
            isSpawning = true;
            
            // 播放召唤音效
            PlaySpawnSound();
            
            // Boss停止移动，准备召唤
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }
        
        public override TaskStatus OnUpdate()
        {
            if (servantPrefab.Value == null)
                return TaskStatus.Failure;
            
            if (isSpawning)
            {
                spawnTimer += Time.deltaTime;
                
                if (spawnTimer >= spawnInterval.Value)
                {
                    SpawnServant();
                    servantsSpawned++;
                    spawnTimer = 0f;
                    
                    if (servantsSpawned >= servantsToSpawn)
                    {
                        isSpawning = false;
                        spawnTimer = 0f;
                    }
                }
                
                return TaskStatus.Running;
            }
            else
            {
                // 召唤完成后的等待时间
                spawnTimer += Time.deltaTime;
                if (spawnTimer >= postSpawnWait.Value)
                {
                    return TaskStatus.Success;
                }
                
                return TaskStatus.Running;
            }
        }
        
        private void SpawnServant()
        {
            // 计算生成位置
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 spawnOffset = new Vector2(
                Mathf.Cos(angle) * spawnRadius.Value,
                Mathf.Sin(angle) * spawnRadius.Value + spawnHeightOffset.Value
            );
            Vector3 spawnPosition = transform.position + (Vector3)spawnOffset;
            
            // 生成仆从
            GameObject servant = Object.Instantiate(servantPrefab.Value, spawnPosition, Quaternion.identity);
            
            // 配置仆从
            ConfigureServant(servant);
            
            // 播放生成特效
            if (spawnEffectPrefab != null)
            {
                GameObject effect = Object.Instantiate(spawnEffectPrefab, spawnPosition, Quaternion.identity);
                Object.Destroy(effect, 2f); // 2秒后销毁特效
            }
            
            // 添加生成动画（缩放效果）
            StartServantSpawnAnimation(servant);
        }
        
        private void ConfigureServant(GameObject servant)
        {
            // 获取仆从控制器
            ServantController servantController = servant.GetComponent<ServantController>();
            if (servantController != null)
            {
                // 可以在这里设置仆从的额外属性
                // 例如：根据Boss阶段调整仆从的速度或伤害
                if (bossController != null && bossController.currentPhase == 2)
                {
                    servantController.speed *= 1.5f; // 第二阶段仆从速度提升50%
                }
            }
            
            // 设置仆从的层级（确保与Boss不会碰撞）
            servant.layer = gameObject.layer;
        }
        
        private void StartServantSpawnAnimation(GameObject servant)
        {
            // 从小到大的生成动画
            Vector3 originalScale = servant.transform.localScale;
            servant.transform.localScale = Vector3.zero;
            
            // 使用协程实现缩放动画
            StartCoroutine(ScaleServant(servant.transform, originalScale, 0.3f));
        }
        
        private System.Collections.IEnumerator ScaleServant(Transform servantTransform, Vector3 targetScale, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration && servantTransform != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 使用缓动函数使动画更平滑
                t = Mathf.SmoothStep(0f, 1f, t);
                
                servantTransform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
                yield return null;
            }
            
            if (servantTransform != null)
            {
                servantTransform.localScale = targetScale;
            }
        }
        
        private void PlaySpawnSound()
        {
            if (spawnSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(spawnSound);
            }
            else
            {
                // 如果没有专门的召唤音效，简化处理
                if (audioSource != null)
                {
                    Debug.Log("[EoCSpawnServants] 播放召唤音效");
                }
            }
        }
        
        public override void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            // 绘制生成范围
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, spawnRadius.Value);
            
            // 绘制生成高度
            Vector3 heightOffset = Vector3.up * spawnHeightOffset.Value;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + heightOffset);
            
            // 显示将要召唤的数量
            if (Application.isPlaying && bossController != null)
            {
                int count = bossController.currentPhase == 2 ? phase2ServantCount.Value : phase1ServantCount.Value;
                Vector3 textPos = transform.position + Vector3.up * (spawnRadius.Value + 1f);
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(textPos, $"召唤数量: {count}");
                #endif
            }
            #endif
        }
    }
} 