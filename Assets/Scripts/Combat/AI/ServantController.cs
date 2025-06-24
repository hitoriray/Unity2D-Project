using UnityEngine;
using Combat.Interfaces;
using UI;
using System.Collections;
using System.Linq;

/// <summary>
/// 克苏鲁之眼仆从控制器
/// </summary>
public class ServantController : MonoBehaviour, IDamageable
{
    [Header("移动设置")]
    [Tooltip("移动速度")]
    public float speed = 3f;
    [Tooltip("旋转速度")]
    public float rotationSpeed = 5f;
    [Tooltip("最小追踪距离")]
    public float minChaseDistance = 1f;
    [Tooltip("最大追踪距离")]
    public float maxChaseDistance = 20f;
    [Tooltip("朝向角度偏移（用于调整sprite朝向）")]
    public float faceAngleOffset = 90f;
    
    [Header("战斗设置")]
    [Tooltip("最大生命值")]
    public float maxHealth = 50f;
    [Tooltip("当前生命值")]
    public float currentHealth;
    [Tooltip("接触伤害")]
    public float contactDamage = 10f;
    [Tooltip("击退力度")]
    public float knockbackForce = 5f;
    [Tooltip("攻击冷却时间")]
    public float attackCooldown = 1f;
    [Tooltip("死亡后消失延迟")]
    public float deathDelay = 0.5f;
    
    [Header("特效和音效")]
    [Tooltip("受伤特效")]
    public GameObject hurtEffectPrefab;
    [Tooltip("死亡特效")]
    public GameObject deathEffectPrefab;
    [Tooltip("攻击特效")]
    public GameObject attackEffectPrefab;
    [Tooltip("受伤音效")]
    public AudioClip hurtSound;
    [Tooltip("死亡音效")]
    public AudioClip deathSound;
    [Tooltip("攻击音效")]
    public AudioClip attackSound;
    
    [Header("生命周期")]
    [Tooltip("存活时间（0为无限）")]
    public float lifeTime = 30f;
    [Tooltip("超出距离后自毁")]
    public bool destroyWhenTooFar = true;

    [Tooltip("攻击触发器Layer")]
    public int attackTriggerLayer = 11; // EnemyAttack层
    [Tooltip("玩家层级")]
    public LayerMask playerLayerMask = 1 << 6;
    
    [Header("Sprite动画配置")]
    [Tooltip("动画帧（2帧切换）")]
    public Sprite[] animationFrames = new Sprite[2];
    [Tooltip("动画播放速度（帧/秒）")]
    public float animationSpeed = 6f;
    [Tooltip("是否启用Sprite动画")]
    public bool enableSpriteAnimation = true;
    
    // 私有变量
    private Transform _playerTransform;
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    private AudioSource _audioSource;
    private Collider2D _collider;
    
    private float _lifeTimer;
    private float _lastAttackTime;
    private bool _isDead = false;
    private Color _originalColor;
    
    // 受伤闪烁效果
    private Coroutine _hurtFlashCoroutine;
    
    // 动画系统
    private Coroutine _spriteAnimationCoroutine;
    private int _currentAnimationFrame = 0;
    private float _animationTimer = 0f;
    private bool _isPlayingHurtEffect = false;
    
    // 攻击控制
    private bool _canAttack = true;
    
    // 攻击触发器系统
    private GameObject _attackTriggerObject;
    
    // 日志节流
    private float _lastAttackFailedLog = 0f;
    private const float LOG_COOLDOWN = 5f; // 增加到5秒冷却
    
    // 性能优化标志
    private static bool s_enableDebugLogs = false; // 全局禁用调试日志

    void Start()
    {
        // 查找玩家对象（优先通过PlayerController组件）
        PlayerController playerController = GameObject.FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            _playerTransform = playerController.transform;
        }
        
        if (_playerTransform == null)
        {
            Debug.LogWarning($"[ServantController] 未找到玩家对象！仆从可能无法正常工作。");
        }
        
        // 获取组件
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _audioSource = GetComponent<AudioSource>();
        _collider = GetComponent<Collider2D>();
        
        // 初始化
        currentHealth = maxHealth;
        _lifeTimer = 0f;
        _lastAttackTime = -attackCooldown;
        
        
        // 设置攻击触发器
        SetupAttackTrigger();
        
        // 初始化动画系统
        InitializeSpriteAnimation();
    }

    void FixedUpdate()
    {
        if (_isDead) return;
        
        if (_playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);
            
            // 检查是否超出最大距离
            if (destroyWhenTooFar && distanceToPlayer > maxChaseDistance)
            {
                Destroy(gameObject);
                return;
            }
            
            // 只在合适的距离内追踪玩家
            if (distanceToPlayer > minChaseDistance)
            {
                MoveTowardsPlayer();
            }
            else
            {
                // 太近时稍微减速
                _rb.velocity *= 0.9f;
            }
            
            // 始终面向玩家
            FacePlayer();
        }
    }
    
    /// <summary>
    /// 向玩家移动
    /// </summary>
    private void MoveTowardsPlayer()
    {
        Vector2 direction = (_playerTransform.position - transform.position).normalized;
        
        // 添加一些随机性，让移动更自然
        float randomAngle = Mathf.Sin(Time.time * 2f) * 0.3f;
        direction = Quaternion.Euler(0, 0, randomAngle * Mathf.Rad2Deg) * direction;
        
        _rb.velocity = direction * speed;
    }
    
    /// <summary>
    /// 面向玩家
    /// </summary>
    private void FacePlayer()
    {
        Vector2 directionToPlayer = (_playerTransform.position - transform.position).normalized;
        
        // 计算朝向角度（使用可配置的角度偏移）
        float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg + faceAngleOffset;
        
        // 平滑旋转
        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
    }

    // 实现IDamageable接口
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_isDead) return;
        
        currentHealth -= damageInfo.baseDamage;
        
        // 显示伤害数字
        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.ShowDamage(transform.position, (int)damageInfo.baseDamage, damageInfo.isCritical, damageInfo.damageType);
        }
        
        // 播放受伤特效
        if (hurtEffectPrefab != null)
        {
            Instantiate(hurtEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // 播放受伤音效
        PlaySound(hurtSound);
        
        // 受伤闪烁
        StartHurtFlash();
        
        // 受击击退
        if (_rb != null && damageInfo.knockbackForce > 0)
        {
            Vector2 knockbackDirection = -damageInfo.hitDirection;
            _rb.AddForce(knockbackDirection * damageInfo.knockbackForce, ForceMode2D.Impulse);
        }
        
        // 检查死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 死亡处理
    /// </summary>
    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        
        // 停止Sprite动画
        StopSpriteAnimation();
        
        // 播放死亡特效
        if (deathEffectPrefab != null)
        {
            GameObject deathEffect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(deathEffect, 2f);
        }
        
        // 播放死亡音效
        PlaySound(deathSound);
        
        // 禁用碰撞和移动
        if (_collider != null) _collider.enabled = false;
        if (_rb != null) _rb.velocity = Vector2.zero;
        
        // 死亡动画（淡出）
        StartCoroutine(DeathFadeOut());
    }
    
    /// <summary>
    /// 死亡淡出效果
    /// </summary>
    private IEnumerator DeathFadeOut()
    {
        float fadeTime = deathDelay;
        float elapsed = 0f;
        
        while (elapsed < fadeTime && _spriteRenderer != null)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            
            Color color = _spriteRenderer.color;
            color.a = alpha;
            _spriteRenderer.color = color;
            
            // 死亡时向上飘
            transform.position += Vector3.up * Time.deltaTime * 0.5f;
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 受伤闪烁效果
    /// </summary>
    private void StartHurtFlash()
    {
        if (_hurtFlashCoroutine != null)
            StopCoroutine(_hurtFlashCoroutine);
        _hurtFlashCoroutine = StartCoroutine(HurtFlashCoroutine());
    }
    
    private IEnumerator HurtFlashCoroutine()
    {
        if (_spriteRenderer == null) yield break;
        
        // 标记正在播放受伤效果
        _isPlayingHurtEffect = true;
        
        // 闪烁3次
        for (int i = 0; i < 3; i++)
        {
            _spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            _spriteRenderer.color = _originalColor;
            yield return new WaitForSeconds(0.1f);
        }
        
        // 结束受伤效果
        _isPlayingHurtEffect = false;
        _hurtFlashCoroutine = null;
    }

    // 注意：现在使用触发器系统处理接触伤害，不再需要物理碰撞
    
    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }
    
    
    

    #region Sprite动画系统
    
    /// <summary>
    /// 初始化Sprite动画系统
    /// </summary>
    private void InitializeSpriteAnimation()
    {
        if (!enableSpriteAnimation || _spriteRenderer == null) 
        {
            Debug.Log($"[ServantController] {gameObject.name}: 动画系统未启用或缺少SpriteRenderer");
            return;
        }
        
        // 验证动画帧
        if (animationFrames == null || animationFrames.Length < 2)
        {
            Debug.LogWarning($"[ServantController] {gameObject.name}: 动画帧不足（当前: {animationFrames?.Length ?? 0}），需要至少2帧！");
            enableSpriteAnimation = false;
            return;
        }
        
        // 检查动画帧是否为空，统计有效帧数
        int validFrameCount = 0;
        for (int i = 0; i < animationFrames.Length; i++)
        {
            if (animationFrames[i] != null)
            {
                validFrameCount++;
            }
            else
            {
                Debug.LogWarning($"[ServantController] {gameObject.name}: 动画帧 {i} 为空！");
            }
        }
        
        if (validFrameCount < 2)
        {
            Debug.LogError($"[ServantController] {gameObject.name}: 有效动画帧不足（{validFrameCount}/2），禁用动画");
            enableSpriteAnimation = false;
            return;
        }
        
        // 找到第一个有效帧作为初始帧
        for (int i = 0; i < animationFrames.Length; i++)
        {
            if (animationFrames[i] != null)
            {
                _currentAnimationFrame = i;
                _spriteRenderer.sprite = animationFrames[i];
                Debug.Log($"[ServantController] {gameObject.name}: 设置初始帧为第 {i} 帧");
                break;
            }
        }
        
        // 开始播放动画
        StartSpriteAnimation();
    }
    
    /// <summary>
    /// 开始播放Sprite动画
    /// </summary>
    private void StartSpriteAnimation()
    {
        if (_spriteAnimationCoroutine != null)
            StopCoroutine(_spriteAnimationCoroutine);
        
        if (enableSpriteAnimation && !_isDead)
        {
            _spriteAnimationCoroutine = StartCoroutine(PlaySpriteAnimation());
        }
    }
    
    /// <summary>
    /// 停止Sprite动画
    /// </summary>
    private void StopSpriteAnimation()
    {
        if (_spriteAnimationCoroutine != null)
        {
            StopCoroutine(_spriteAnimationCoroutine);
            _spriteAnimationCoroutine = null;
        }
    }
    
    /// <summary>
    /// Sprite动画播放协程
    /// </summary>
    private IEnumerator PlaySpriteAnimation()
    {
        float frameTime = 1f / animationSpeed; // 每帧的时间
        
        while (!_isDead && enableSpriteAnimation && _spriteRenderer != null)
        {
            // 如果正在播放受伤效果，暂停动画但保持当前帧
            if (_isPlayingHurtEffect)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }
            
            // 切换到下一帧
            _currentAnimationFrame = (_currentAnimationFrame + 1) % animationFrames.Length;
            
            // 确保当前帧有效，如果无效则跳过
            if (animationFrames[_currentAnimationFrame] != null)
            {
                _spriteRenderer.sprite = animationFrames[_currentAnimationFrame];
            }
            else
            {
                Debug.LogWarning($"[ServantController] 动画帧 {_currentAnimationFrame} 为空，跳过！");
                // 如果当前帧为空，尝试找到下一个有效帧
                for (int i = 0; i < animationFrames.Length; i++)
                {
                    int nextFrame = (i + 1) % animationFrames.Length;
                    if (animationFrames[nextFrame] != null)
                    {
                        _currentAnimationFrame = nextFrame;
                        _spriteRenderer.sprite = animationFrames[nextFrame];
                        break;
                    }
                }
            }
            
            // 等待帧时间
            yield return new WaitForSeconds(frameTime);
        }
    }
    
    #endregion
    
    #region 攻击触发器系统
    
    /// <summary>
    /// 设置攻击触发器（模仿Boss的实现）
    /// </summary>
    private void SetupAttackTrigger()
    {
        // 创建攻击触发器子对象
        _attackTriggerObject = new GameObject("ServantAttackTrigger");
        _attackTriggerObject.transform.SetParent(transform);
        _attackTriggerObject.transform.localPosition = Vector3.zero;
        _attackTriggerObject.transform.localScale = Vector3.one;
        _attackTriggerObject.layer = attackTriggerLayer;
        
        // 添加触发器碰撞体
        CircleCollider2D triggerCollider = _attackTriggerObject.AddComponent<CircleCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = 2f;
        
        // 添加仆从攻击触发器脚本
        var attackTrigger = _attackTriggerObject.AddComponent<ServantContactTrigger>();
        attackTrigger.servantController = this;
    }
    
    /// <summary>
    /// 处理玩家接触伤害（由攻击触发器调用）
    /// </summary>
    public void OnPlayerContact(Collider2D playerCollider)
    {
        // 检查是否可以攻击（包括enemyAttack层检测）
        if (!CanAttack())
        {
            return;
        }
        
        // 检查攻击冷却
        if (Time.time - _lastAttackTime < attackCooldown) return;
        
        // 通过组件识别玩家
        IDamageable damageable = playerCollider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // 计算伤害方向（从仆从指向玩家）
            Vector2 damageDirection = (playerCollider.transform.position - transform.position).normalized;
            
            // 创建接触伤害信息
            DamageInfo contactDamageInfo = new DamageInfo(
                contactDamage,
                DamageType.Physical,
                false, // 接触伤害不是暴击
                gameObject,
                playerCollider.transform.position,
                damageDirection,
                knockbackForce
            );
            
            // 造成伤害
            damageable.TakeDamage(contactDamageInfo);
            _lastAttackTime = Time.time;
            
            Debug.Log($"[ServantController] 仆从对玩家造成了 {contactDamage} 点接触伤害!");
        }
    }
    
    #endregion
    
    /// <summary>
    /// 检查是否可以攻击
    /// </summary>
    public bool CanAttack()
    {
        return _canAttack && !_isDead;
    }
    
    
    /// <summary>
    /// 销毁时清理资源
    /// </summary>
    private void OnDestroy()
    {
        // 停止所有协程
        StopSpriteAnimation();
        
        if (_hurtFlashCoroutine != null)
        {
            StopCoroutine(_hurtFlashCoroutine);
            _hurtFlashCoroutine = null;
        }
        
        // 清理攻击触发器
        if (_attackTriggerObject != null)
        {
            Destroy(_attackTriggerObject);
            _attackTriggerObject = null;
        }
    }
    
    
    /// <summary>
    /// 仆从接触触发器内部类（模仿Boss的实现）
    /// </summary>
    public class ServantContactTrigger : MonoBehaviour
    {
        [HideInInspector]
        public ServantController servantController;
        
        void OnTriggerStay2D(Collider2D other)
        {
            if (servantController == null) return;
            
            // 检查是否是玩家层级
            if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                // 通知仆从控制器处理接触伤害
                servantController.OnPlayerContact(other);
            }
        }
        
        void OnTriggerEnter2D(Collider2D other)
        {
            if (servantController == null) return;
            
            // 检查是否是玩家层级
            if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                // 进入时也触发接触伤害
                servantController.OnPlayerContact(other);
            }
        }
    }
} 