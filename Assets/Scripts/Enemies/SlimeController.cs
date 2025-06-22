using System.Collections;
using Combat.Interfaces;
using UI; // 新增：为了使用DamageTextManager
using UnityEngine;

/// <summary>
/// 泰拉瑞亚风格史莱姆控制器
/// 实现了流畅的动画系统、智能AI和攻击机制
/// 包含：缩放动画、弹跳效果、攻击系统、状态机
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Collider2D))]
public class SlimeController : MonoBehaviour, IDamageable
{
    [Header("Configuration")]
    [Tooltip("史莱姆的属性数据")]
    [SerializeField] private AIStats stats;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Attack System")]
    [Tooltip("攻击伤害")]
    public float attackDamage = 15f;
    [Tooltip("攻击击退力")]
    public float knockbackForce = 5f;
    [Tooltip("攻击冷却时间")]
    public float attackCooldown = 2f;
    [Tooltip("攻击距离")]
    public float attackRange = 1.2f;
    [Tooltip("玩家层级")]
    public LayerMask playerLayer = 1 << 6;
    [Header("攻击触发器设置")]
    [Tooltip("攻击触发器的Layer（建议使用EnemyAttack层）")]
    public int attackTriggerLayer = 11;

    [Header("Animation Sprites")]
    [Tooltip("落地/站立时的Sprite")]
    public Sprite idleSprite;
    [Tooltip("准备跳跃时的Sprite")]
    public Sprite preparingJumpSprite;
    [Tooltip("在空中时的Sprite")]
    public Sprite inAirSprite;
    [Tooltip("即将落地时的Sprite")]
    public Sprite landingSprite;

    [Header("Animation Settings")]
    [Tooltip("准备跳跃动画持续时间")]
    public float prepareJumpDuration = 0.3f;
    [Tooltip("落地时的缩放效果强度")]
    public float landingSquashIntensity = 0.8f;
    [Tooltip("跳跃时的拉伸效果强度")]
    public float jumpStretchIntensity = 1.3f;
    [Tooltip("缩放动画恢复时间")]
    public float scaleRecoveryTime = 0.2f;
    [Tooltip("闲置时的呼吸效果强度")]
    public float breathingIntensity = 0.05f;
    [Tooltip("呼吸效果速度")]
    public float breathingSpeed = 2f;

    [Header("AI Behavior")]
    [Tooltip("闲逛时的跳跃力度")]
    public float wanderHopForce = 12f;
    [Tooltip("追击时的跳跃力度")]
    public float chaseHopForce = 12f;
    [Tooltip("攻击时的跳跃力度")]
    public float attackHopForce = 15f;
    [Tooltip("闲逛时跳跃的最小间隔时间")]
    public float minWanderInterval = 2f;
    [Tooltip("闲逛时跳跃的最大间隔时间")]
    public float maxWanderInterval = 4f;
    [Tooltip("追击时跳跃的间隔时间")]
    public float chaseHopInterval = 1.2f;

    [Header("Audio")]
    [Tooltip("跳跃音效")]
    public AudioClip hopSound;
    [Tooltip("攻击音效")]
    public AudioClip attackSound;
    [Tooltip("受伤音效")]
    public AudioClip hurtSound;
    [Tooltip("死亡音效")]
    public AudioClip deathSound;

    // AI状态枚举
    private enum AIState { Wandering, Chasing, Attacking, Stunned }
    private AIState currentState = AIState.Wandering;

    // 动画状态枚举
    private enum AnimationState { Idle, PreparingJump, InAir, Landing }
    private AnimationState currentAnimState = AnimationState.Idle;

    // 核心组件
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform target;

    // 状态变量
    private float currentHealth;
    private bool isGrounded;
    private bool hasBeenAttacked = false;
    private float hopCooldownTimer;
    private float attackCooldownTimer;
    private Vector3 baseScale = Vector3.one;
    private Color originalSpriteColor = Color.white; // 保存史莱姆的原始颜色
    private float lastActionTime;
    private const float MAX_IDLE_TIME = 15f; // 增加到15秒，给闲逛状态更多时间
    
    // 动画协程
    private Coroutine currentAnimationCoroutine;
    private Coroutine breathingCoroutine;
    private Coroutine scaleAnimationCoroutine;
    private Coroutine hurtEffectCoroutine;

    void Start()
    {
        // 初始化组件
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 初始化数据
        currentHealth = stats.maxHealth;
        baseScale = transform.localScale;
        hopCooldownTimer = Random.Range(minWanderInterval, maxWanderInterval);
        lastActionTime = Time.time;
        
        // 寻找玩家
        FindPlayerTarget();
        
        // 配置检查
        // PerformConfigurationCheck(); // 调试完成，注释掉
        
        // 设置攻击触发器Layer
        SetupAttackTrigger();
        
        // 开始呼吸动画
        StartBreathingAnimation();
        
        // 设置初始sprite
        if (spriteRenderer != null && idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
            // 保存原始颜色
            originalSpriteColor = spriteRenderer.color;
        }
    }

    void FixedUpdate()
    {
        // 改进的地面检测
        CheckGroundedStatus();
        
        // 调试信息（每3秒打印一次）
        // if (Time.fixedTime % 3f < Time.fixedDeltaTime)
        // {
        //     PrintDebugInfo();
        // }
        
        // 更新AI逻辑
        HandleAIStateMachine();
        
        // 更新动画
        UpdateAnimation();
        
        // 更新计时器
        UpdateTimers();
        
        // 安全检查
        CheckForceRestart();
    }

    #region AI状态机
    private void HandleAIStateMachine()
    {
        CheckStateTransitions();
        
            switch (currentState)
            {
                case AIState.Wandering:
                // 在闲逛状态下，每5秒更新一次活动时间，防止误触发重启
                if (Time.time - lastActionTime > 5f)
                {
                    lastActionTime = Time.time;
                }
                
                if (hopCooldownTimer <= 0)
                {
                    PerformWanderHop();
                    hopCooldownTimer = Random.Range(minWanderInterval, maxWanderInterval);
                }
                    break;
                case AIState.Chasing:
                if (hopCooldownTimer <= 0)
                {
                    PerformChaseHop();
                    hopCooldownTimer = chaseHopInterval;
                }
                    break;
            case AIState.Attacking:
                // 攻击状态下，即使不在地面也要尝试攻击（修复卡住问题）
                if (hopCooldownTimer <= 0)
                {
                    // 如果长时间不在地面，强制认为在地面
                    if (!isGrounded && Time.time - lastActionTime > 2f)
                    {
                        Debug.LogWarning($"[SlimeController] {gameObject.name} 攻击状态下长时间不在地面，强制执行攻击");
                        isGrounded = true;
                    }
                    
                    if (isGrounded)
                    {
                        PerformAttackHop();
                        hopCooldownTimer = chaseHopInterval;
                    }
                }
                break;
        }
    }

    private void CheckStateTransitions()
    {
        bool playerInSight = IsPlayerInSight();
        bool playerInAttackRange = IsPlayerInAttackRange();

        switch (currentState)
        {
            case AIState.Wandering:
                if (hasBeenAttacked && playerInSight)
                    currentState = AIState.Chasing;
                break;
            case AIState.Chasing:
                if (playerInAttackRange && attackCooldownTimer <= 0)
                    currentState = AIState.Attacking;
                else if (!playerInSight)
                    currentState = AIState.Wandering;
                break;
            case AIState.Attacking:
                if (!playerInAttackRange)
                    currentState = AIState.Chasing;
                break;
        }
    }
    #endregion

    #region 跳跃行为
    private void PerformWanderHop()
    {
        if (!isGrounded) return;
        
        float randomDirection = Random.Range(0, 2) == 0 ? -1f : 1f;
        Vector2 hopDirection = new Vector2(randomDirection, 1f).normalized;
        PerformHop(hopDirection, wanderHopForce);
    }

    private void PerformChaseHop()
    {
        if (!isGrounded || target == null) return;
        
        Vector2 directionToTarget = (target.position - transform.position).normalized;
        Vector2 hopDirection = new Vector2(directionToTarget.x, 1f).normalized;
        PerformHop(hopDirection, chaseHopForce);
    }

    private void PerformAttackHop()
    {
        if (!isGrounded || target == null) return;
        
        Vector2 directionToTarget = (target.position - transform.position).normalized;
        Vector2 hopDirection = new Vector2(directionToTarget.x, 1.2f).normalized;
        PerformHop(hopDirection, attackHopForce);
    }

    private void PerformHop(Vector2 direction, float force)
    {
        lastActionTime = Time.time;
        StartHopAnimation();
        StartCoroutine(DelayedHop(direction, force, prepareJumpDuration * 0.7f));
        PlaySound(hopSound);
    }

    private IEnumerator DelayedHop(Vector2 direction, float force, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (rb != null)
        {
            rb.AddForce(direction * force, ForceMode2D.Impulse);
            
            if (scaleAnimationCoroutine != null)
                StopCoroutine(scaleAnimationCoroutine);
            scaleAnimationCoroutine = StartCoroutine(ScaleAnimation(
                new Vector3(baseScale.x * 0.9f, baseScale.y * jumpStretchIntensity, baseScale.z),
                scaleRecoveryTime * 0.3f
            ));
        }
    }
    #endregion

    #region 动画系统
    private void StartHopAnimation()
    {
        if (currentAnimationCoroutine != null)
            StopCoroutine(currentAnimationCoroutine);
        currentAnimationCoroutine = StartCoroutine(HopAnimationSequence());
    }

    private IEnumerator HopAnimationSequence()
    {
        float timeoutTimer = 0f;
        const float MAX_ANIMATION_TIME = 5f;
        
        // 准备跳跃阶段
        currentAnimState = AnimationState.PreparingJump;
        if (spriteRenderer != null && preparingJumpSprite != null)
            spriteRenderer.sprite = preparingJumpSprite;
        
        if (scaleAnimationCoroutine != null)
            StopCoroutine(scaleAnimationCoroutine);
        scaleAnimationCoroutine = StartCoroutine(ScaleAnimation(
            new Vector3(baseScale.x * 1.1f, baseScale.y * 0.8f, baseScale.z),
            prepareJumpDuration * 0.5f
        ));
        
        yield return new WaitForSeconds(prepareJumpDuration);
        
        // 在空中阶段
        currentAnimState = AnimationState.InAir;
        if (spriteRenderer != null && inAirSprite != null)
            spriteRenderer.sprite = inAirSprite;
        
        // 等待开始下降
        timeoutTimer = 0f;
        while (rb != null && rb.velocity.y > 0 && timeoutTimer < MAX_ANIMATION_TIME)
        {
            timeoutTimer += Time.deltaTime;
            yield return null;
        }
        
        // 落地准备阶段
        currentAnimState = AnimationState.Landing;
        if (spriteRenderer != null && landingSprite != null)
            spriteRenderer.sprite = landingSprite;
        
        // 等待着地
        timeoutTimer = 0f;
        while (!isGrounded && timeoutTimer < MAX_ANIMATION_TIME)
        {
            timeoutTimer += Time.deltaTime;
            yield return null;
        }
        
        // 落地挤压效果
        if (scaleAnimationCoroutine != null)
            StopCoroutine(scaleAnimationCoroutine);
        scaleAnimationCoroutine = StartCoroutine(ScaleAnimation(
            new Vector3(baseScale.x * 1.2f, baseScale.y * landingSquashIntensity, baseScale.z),
            scaleRecoveryTime
        ));
        
        // 回到闲置状态
        yield return new WaitForSeconds(0.1f);
        currentAnimState = AnimationState.Idle;
        if (spriteRenderer != null && idleSprite != null)
            spriteRenderer.sprite = idleSprite;
        
        currentAnimationCoroutine = null;
    }

    private void UpdateAnimation()
    {
        if (currentAnimationCoroutine != null) return;

        if (isGrounded)
        {
            if (currentAnimState != AnimationState.Idle)
            {
                currentAnimState = AnimationState.Idle;
                if (spriteRenderer != null && idleSprite != null)
                spriteRenderer.sprite = idleSprite;
            }
        }
        else
        {
            if (rb.velocity.y > 0.1f && currentAnimState != AnimationState.InAir)
            {
                currentAnimState = AnimationState.InAir;
                if (spriteRenderer != null && inAirSprite != null)
                    spriteRenderer.sprite = inAirSprite;
            }
            else if (rb.velocity.y < -0.1f && currentAnimState != AnimationState.Landing)
            {
                currentAnimState = AnimationState.Landing;
                if (spriteRenderer != null && landingSprite != null)
                    spriteRenderer.sprite = landingSprite;
            }
        }
    }

    private void StartBreathingAnimation()
    {
        if (breathingCoroutine != null)
            StopCoroutine(breathingCoroutine);
        breathingCoroutine = StartCoroutine(BreathingAnimation());
    }

    private IEnumerator BreathingAnimation()
    {
        while (true)
        {
            if (currentAnimState == AnimationState.Idle && isGrounded && scaleAnimationCoroutine == null)
            {
                float breathingScale = 1f + Mathf.Sin(Time.time * breathingSpeed) * breathingIntensity;
                transform.localScale = new Vector3(
                    baseScale.x * breathingScale,
                    baseScale.y * breathingScale,
                    baseScale.z
                );
            }
            
            yield return null;
        }
    }

    private IEnumerator ScaleAnimation(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        yield return StartCoroutine(ScaleToBase(scaleRecoveryTime));
        scaleAnimationCoroutine = null;
    }

    private IEnumerator ScaleToBase(float duration)
    {
        Vector3 startScale = transform.localScale;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            transform.localScale = Vector3.Lerp(startScale, baseScale, t);
            yield return null;
        }
        
        transform.localScale = baseScale;
    }
    #endregion

    #region 攻击系统
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"触发器检测到: {other.gameObject.name}");
        // 只对可能是玩家的对象输出详细调试信息
        bool isPotentialPlayer = other.gameObject.layer == 3 || other.GetComponent<PlayerController>() != null || other.GetComponent<IDamageable>() != null;
        
        // if (isPotentialPlayer)
        // {
        //     Debug.Log($"[史莱姆攻击调试] {gameObject.name} 触发器检测到玩家: {other.gameObject.name}" +
        //              $"\n  - 目标Layer: {other.gameObject.layer}" +
        //              $"\n  - 史莱姆playerLayer设置: {playerLayer.value}" +
        //              $"\n  - Layer匹配: {((1 << other.gameObject.layer) & playerLayer) != 0}" +
        //              $"\n  - 当前状态: {currentState}" +
        //              $"\n  - 攻击冷却: {attackCooldownTimer:F2}" +
        //              $"\n  - 可以攻击: {currentState == AIState.Attacking && attackCooldownTimer <= 0}");
        // }
        
        // 使用触发器检测，避免物理碰撞
        if (currentState == AIState.Attacking && attackCooldownTimer <= 0)
        {
            // 优先检查是否有PlayerController组件（更可靠的玩家识别）
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Debug.Log($"[史莱姆攻击调试] 通过PlayerController组件识别到玩家：{other.gameObject.name}");
                PerformAttackOnPlayer(other);
                return;
            }
            
            // 备用检查：Layer匹配
            if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                IDamageable playerDamageable = other.GetComponent<IDamageable>();
                if (playerDamageable != null)
                {
                    // Debug.Log($"[史莱姆攻击调试] 通过Layer匹配识别到玩家：{other.gameObject.name}");
                    PerformAttackOnPlayer(other);
                }
                // else if (isPotentialPlayer)
                // {
                //     Debug.LogWarning($"❌ 玩家 {other.gameObject.name} 没有IDamageable组件！");
                // }
            }
        }
    }
    
    private void PerformAttackOnPlayer(Collider2D playerCollider)
    {
        IDamageable playerDamageable = playerCollider.GetComponent<IDamageable>();
        if (playerDamageable == null) return;
        
        Vector2 knockbackDirection = (playerCollider.transform.position - transform.position).normalized;
        Vector2 attackPoint = playerCollider.ClosestPoint(transform.position);
        
        DamageInfo damageInfo = new DamageInfo(
            attackDamage,
            DamageType.Physical,
            false,
            gameObject,
            attackPoint,
            knockbackDirection,
            knockbackForce
        );
        
        playerDamageable.TakeDamage(damageInfo);
        PlaySound(attackSound);
        attackCooldownTimer = attackCooldown;
        
        // Debug.Log($"✅ 史莱姆成功攻击了玩家 {playerCollider.gameObject.name}，造成 {attackDamage} 点伤害！");
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 持续攻击检测（如果史莱姆一直贴着玩家）
        if (currentState == AIState.Attacking && attackCooldownTimer <= 0)
        {
            if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                IDamageable playerDamageable = other.GetComponent<IDamageable>();
                if (playerDamageable != null)
                {
                    Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
                    Vector2 attackPoint = other.ClosestPoint(transform.position);
                    
                    DamageInfo damageInfo = new DamageInfo(
                        attackDamage,
                        DamageType.Physical,
                        false,
                        gameObject,
                        attackPoint,
                        knockbackDirection,
                        knockbackForce
                    );
                    
                    playerDamageable.TakeDamage(damageInfo);
                    PlaySound(attackSound);
                    attackCooldownTimer = attackCooldown;
                    
                    // Debug.Log($"史莱姆持续攻击玩家，造成 {attackDamage} 点伤害！");
                }
            }
        }
    }
    #endregion

    #region IDamageable接口实现
    public void TakeDamage(DamageInfo damageInfo)
    {
        currentHealth -= damageInfo.baseDamage;
        Debug.Log($"{gameObject.name} 受到 {damageInfo.baseDamage} 点伤害，剩余血量: {currentHealth}");

        // 显示伤害数字
        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.ShowDamage(
                transform.position + Vector3.up * 0.5f, // 在史莱姆上方显示
                Mathf.RoundToInt(damageInfo.baseDamage), 
                damageInfo.isCritical, 
                damageInfo.damageType
            );
        }
        else
        {
            Debug.LogWarning($"[SlimeController] DamageTextManager.Instance为空，无法显示伤害数字");
        }

        PlaySound(hurtSound);

        if (!hasBeenAttacked)
        {
            hasBeenAttacked = true;
            currentState = AIState.Chasing;
            if (target == null) FindPlayerTarget();
        }

        // 受伤时的震动效果 - 停止之前的受伤效果
        if (hurtEffectCoroutine != null)
            StopCoroutine(hurtEffectCoroutine);
        hurtEffectCoroutine = StartCoroutine(HurtEffect());

        if (damageInfo.knockbackForce > 0f)
        {
            Vector2 knockbackDirection = damageInfo.hitDirection;
            if (knockbackDirection == Vector2.zero && damageInfo.source != null)
            {
                knockbackDirection = (transform.position - damageInfo.source.transform.position).normalized;
            }
            rb.AddForce(knockbackDirection * damageInfo.knockbackForce, ForceMode2D.Impulse);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator HurtEffect()
    {
        if (spriteRenderer == null) 
        {
            hurtEffectCoroutine = null;
            yield break;
        }
        
        // 变红
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        
        // 恢复到保存的原始颜色
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalSpriteColor;
        }
        
        // 清理协程引用
        hurtEffectCoroutine = null;
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} 已被击败！");
        PlaySound(deathSound);
        
        // 确保死亡前恢复颜色
        if (spriteRenderer != null)
            spriteRenderer.color = originalSpriteColor;
            
        StopAllCoroutines();
        StartCoroutine(DeathAnimation());
    }

    private IEnumerator DeathAnimation()
        {
        float duration = 1f;
        Vector3 startScale = transform.localScale;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = color;
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
    #endregion

    #region 工具方法
    private void CheckGroundedStatus()
    {
        // 主要地面检测
        bool primaryGroundCheck = false;
        if (groundCheck != null)
        {
            primaryGroundCheck = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        
        // 备用地面检测：使用史莱姆自身位置
        bool secondaryGroundCheck = Physics2D.OverlapCircle(
            transform.position + Vector3.down * 0.1f, 
            0.2f, 
            groundLayer
        );
        
        // 第三种检测：射线检测
        bool raycastGroundCheck = Physics2D.Raycast(
            transform.position, 
            Vector2.down, 
            0.3f, 
            groundLayer
        );
        
        // 任何一种检测成功就认为在地面
        isGrounded = primaryGroundCheck || secondaryGroundCheck || raycastGroundCheck;
        
        // 如果速度很小且Y位置稳定，也认为在地面
        if (!isGrounded && rb != null && Mathf.Abs(rb.velocity.y) < 0.1f && Mathf.Abs(rb.velocity.x) < 0.5f)
        {
            isGrounded = true;
        }
    }

    private void UpdateTimers()
    {
        hopCooldownTimer -= Time.fixedDeltaTime;
        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.fixedDeltaTime;
    }

    private void CheckForceRestart()
    {
        if (Time.time - lastActionTime > MAX_IDLE_TIME)
        {
            Debug.LogWarning($"[SlimeController] {gameObject.name} 长时间无行动，强制重启！");
            
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
                currentAnimationCoroutine = null;
            }
            
            currentAnimState = AnimationState.Idle;
        if (spriteRenderer != null)
        {
                if (idleSprite != null)
                    spriteRenderer.sprite = idleSprite;
                // 恢复原始颜色
                spriteRenderer.color = originalSpriteColor;
        }
            
            hopCooldownTimer = 0f;
            lastActionTime = Time.time;
            transform.localScale = baseScale;
        }
    }

    public bool IsPlayerInSight()
    {
        if (target == null || stats == null) return false;
        return Vector2.Distance(transform.position, target.position) < stats.detectionRadius;
    }

    private bool IsPlayerInAttackRange()
    {
        if (target == null) return false;
        return Vector2.Distance(transform.position, target.position) < attackRange;
    }

    private void FindPlayerTarget()
    {
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            target = playerController.transform;
        }
        else
        {
            Debug.LogWarning("SlimeController: 无法在场景中找到带有PlayerController脚本的玩家对象！");
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        
        if (SoundEffectManager.Instance != null)
            SoundEffectManager.Instance.PlaySoundAtPoint(clip, transform.position);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    private void PrintDebugInfo()
    {
        if (stats == null || target == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, target.position);
        bool playerInSight = IsPlayerInSight();
        bool playerInAttackRange = IsPlayerInAttackRange();
        
        // 详细的地面检测信息
        bool primaryGroundCheck = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        bool secondaryGroundCheck = Physics2D.OverlapCircle(transform.position + Vector3.down * 0.1f, 0.2f, groundLayer);
        bool raycastGroundCheck = Physics2D.Raycast(transform.position, Vector2.down, 0.3f, groundLayer);
        
        Debug.Log($"[史莱姆调试] {gameObject.name}:\n" +
                 $"当前状态: {currentState}\n" +
                 $"动画状态: {currentAnimState}\n" +
                 $"索敌范围: {stats.detectionRadius}\n" +
                 $"攻击范围: {attackRange}\n" +
                 $"到玩家距离: {distanceToPlayer:F2}\n" +
                 $"玩家在视野内: {playerInSight}\n" +
                 $"玩家在攻击范围: {playerInAttackRange}\n" +
                 $"被攻击过: {hasBeenAttacked}\n" +
                 $"在地面: {isGrounded}\n" +
                 $"  - 主要检测: {primaryGroundCheck}\n" +
                 $"  - 备用检测: {secondaryGroundCheck}\n" +
                 $"  - 射线检测: {raycastGroundCheck}\n" +
                 $"当前速度: ({rb.velocity.x:F2}, {rb.velocity.y:F2})\n" +
                 $"跳跃冷却: {hopCooldownTimer:F2}\n" +
                 $"攻击冷却: {attackCooldownTimer:F2}");
    }

    private void OnDrawGizmosSelected()
    {
        // 索敌范围
        if (stats != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stats.detectionRadius);
        }

        // 攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 主要地面检测
        if (groundCheck != null)
        {
            bool isGroundedMain = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            Gizmos.color = isGroundedMain ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // 备用地面检测
        Vector3 secondaryCheckPos = transform.position + Vector3.down * 0.1f;
        bool isGroundedSecondary = Physics2D.OverlapCircle(secondaryCheckPos, 0.2f, groundLayer);
        Gizmos.color = isGroundedSecondary ? Color.cyan : Color.magenta;
        Gizmos.DrawWireSphere(secondaryCheckPos, 0.2f);

        // 射线地面检测
        bool isGroundedRay = Physics2D.Raycast(transform.position, Vector2.down, 0.3f, groundLayer);
        Gizmos.color = isGroundedRay ? Color.white : Color.gray;
        Gizmos.DrawRay(transform.position, Vector3.down * 0.3f);

        // 到玩家的连线
        if (target != null)
        {
            bool playerInSight = IsPlayerInSight();
            Gizmos.color = playerInSight ? Color.green : Color.gray;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
    #endregion

    #region 配置检查
    /// <summary>
    /// 执行配置检查，帮助发现常见的设置问题
    /// </summary>
    private void PerformConfigurationCheck()
    {
        Debug.Log("=== 史莱姆配置检查 ===");
        
        // 检查碰撞体设置
        Collider2D[] colliders = GetComponents<Collider2D>();
        bool hasTrigger = false;
        bool hasPhysicsCollider = false;
        
        foreach (var col in colliders)
        {
            if (col.isTrigger)
                hasTrigger = true;
            else
                hasPhysicsCollider = true;
        }
        
        Debug.Log($"✓ 碰撞体检查: 触发器={hasTrigger}, 物理碰撞体={hasPhysicsCollider}");
        if (!hasTrigger)
            Debug.LogWarning("❌ 缺少触发器碰撞体！史莱姆无法检测玩家攻击。");
        
        // 检查Layer设置
        Debug.Log($"✓ Layer检查: 史莱姆Layer={gameObject.layer}, playerLayer设置={playerLayer.value}");
        
        // 检查玩家配置
        if (target != null)
        {
            Debug.Log($"✓ 玩家检查: 找到玩家 {target.name}, Layer={target.gameObject.layer}");
            
            // 检查Layer匹配
            bool layerMatch = ((1 << target.gameObject.layer) & playerLayer) != 0;
            Debug.Log($"✓ Layer匹配检查: {(layerMatch ? "✅ 匹配" : "❌ 不匹配")}");
            
            // 检查玩家是否有IDamageable
            IDamageable playerDamageable = target.GetComponent<IDamageable>();
            Debug.Log($"✓ 玩家IDamageable检查: {(playerDamageable != null ? "✅ 有" : "❌ 无")}");
        }
        else
        {
            Debug.LogWarning("❌ 未找到玩家目标！");
        }
        
        // 检查基础数据
        Debug.Log($"✓ 基础数据: 攻击力={attackDamage}, 击退力={knockbackForce}, 攻击冷却={attackCooldown}s");
        
        Debug.Log("=== 配置检查完成 ===");
    }
    
    /// <summary>
    /// 自动设置攻击触发器的Layer
    /// </summary>
    private void SetupAttackTrigger()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        Collider2D triggerCollider = null;
        
        // 找到触发器碰撞体
        foreach (var col in colliders)
        {
            if (col.isTrigger)
            {
                triggerCollider = col;
                break;
            }
        }
        
        if (triggerCollider != null)
        {
            // 为触发器碰撞体创建子对象并设置正确的Layer
            GameObject triggerChild = new GameObject("AttackTrigger");
            triggerChild.transform.SetParent(transform);
            triggerChild.transform.localPosition = Vector3.zero;
            triggerChild.layer = attackTriggerLayer;
            
            // 移动触发器到子对象
            Collider2D newTrigger = triggerChild.AddComponent<CircleCollider2D>();
            newTrigger.isTrigger = true;
            
            // 复制原触发器的设置
            if (triggerCollider is CircleCollider2D originalCircle && newTrigger is CircleCollider2D newCircle)
            {
                newCircle.radius = originalCircle.radius;
                newCircle.offset = originalCircle.offset;
            }
            
            // 删除原触发器
            DestroyImmediate(triggerCollider);
            
            // Debug.Log($"✅ 已将攻击触发器设置为Layer {attackTriggerLayer} (EnemyAttack)");
        }
        else
        {
            // Debug.LogWarning("❌ 未找到触发器碰撞体！请手动添加一个Is Trigger的碰撞体。");
        }
    }
    #endregion

    #region Legacy - 行为树兼容方法
    public void PerformHopTowardsTarget()
    {
        PerformChaseHop();
    }

    public void PerformIdleHop()
    {
        PerformWanderHop();
    }
    #endregion
}