using UnityEngine;
using System.Collections.Generic; // 新增：为了能使用 List<>
using Combat.Interfaces; // 使用我们创建的接口命名空间
using Utility;           // 新增：为了能引用 ObjectPool

namespace Combat.Weapons // 建议添加命名空间
{
    public class StarProjectile : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("星星飞行速度")]
        public float speed = 15f;
        [Tooltip("星星存活时间（秒）")]
        public float lifetime = 5f;

        [Header("Collision & Damage")]
        [Tooltip("敌人所在的图层")]
        public LayerMask enemyLayer;
        [Tooltip("击中敌人时播放的特效预制体 (可选, 如果为空则可能尝试使用源武器的)")]
        public GameObject hitEffectPrefab;
        [Tooltip("击中敌人时播放的音效 (可选, 如果为空则可能尝试使用源武器的)")]
        public AudioClip hitSound;

        private Vector2 targetPosition;
        private DamageInfo damageInfo;
        private float currentLifetime;
        private Rigidbody2D rb;
        private ObjectPool myPool;
        private List<int> hitEnemyInstanceIDs;

        // 光照相关参数
        private TerrainGeneration terrainGenRef;
        private bool createsLight;
        private float lightIntensity;
        private int lightRadius;
        private float lightDuration;
        // private float lightPulseInterval = 0.2f; // 移除：不再使用固定时间间隔
        // private float timeSinceLastLightPulse = 0f; // 移除
        private Vector2Int lastLitGridPosition; // 新增：用于跟踪上一个被照亮的格子位置

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("StarProjectile requires a Rigidbody2D component!");
                // 可以选择在这里添加一个，或者要求预制体必须有
                // rb = gameObject.AddComponent<Rigidbody2D>();
                // rb.isKinematic = true; // 或根据需要设置
            }
        }

        /// <summary>
        /// 初始化投射物。
        /// </summary>
        public void Initialize(Vector2 target, DamageInfo dmgInfo, ObjectPool pool,
                               TerrainGeneration terrainGen, bool createsLight, float lightIntensity,
                               int lightRadius, float lightDuration)
        {
            this.targetPosition = target;
            this.damageInfo = dmgInfo;
            this.myPool = pool;
            this.currentLifetime = 0f;

            this.terrainGenRef = terrainGen;
            this.createsLight = createsLight;
            this.lightIntensity = lightIntensity;
            this.lightRadius = lightRadius;
            this.lightDuration = lightDuration;
            // this.timeSinceLastLightPulse = 0f; // 移除
            this.lastLitGridPosition = new Vector2Int(int.MinValue, int.MinValue); // 初始化为一个无效位置

            if (hitEnemyInstanceIDs == null)
            {
                hitEnemyInstanceIDs = new List<int>();
            }
            else
            {
                hitEnemyInstanceIDs.Clear();
            }

            // 使星星朝向目标方向 (视觉调整)
            Vector2 direction = (target - (Vector2)transform.position).normalized;
            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f); // 减去90度通常是为了让Sprite的“顶部”朝向目标
            }

            // 如果使用 Rigidbody 进行移动，可以在这里设置速度
            // if (rb != null && !rb.isKinematic)
            // {
            //     rb.velocity = direction * speed;
            // }
        }

        void Update()
        {
            currentLifetime += Time.deltaTime;
            // timeSinceLastLightPulse += Time.deltaTime; // 移除

            if (currentLifetime > lifetime)
            {
                DestroyProjectile();
                return;
            }

            Vector2 previousPosition = transform.position; // 记录移动前的位置
            // 使用Transform直接移动 (如果Rigidbody是Kinematic或者没有Rigidbody)
            if (rb == null || rb.isKinematic)
            {
                transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            }

            // 处理光照：当星星移动到新的格子时触发
            if (createsLight && terrainGenRef != null)
            {
                Vector2Int currentGridPosition = new Vector2Int(
                    Mathf.RoundToInt(transform.position.x - 0.5f),
                    Mathf.RoundToInt(transform.position.y - 0.5f)
                );

                // 只有当星星移动到新的格子，或者这是第一次（lastLitGridPosition是初始无效值）
                if (currentGridPosition != lastLitGridPosition)
                {
                    LightingManager.PulseTemporaryLight(this, terrainGenRef, currentGridPosition.x, currentGridPosition.y, lightIntensity, lightRadius, lightDuration);
                    // Debug.Log($"[StarProjectile '{gameObject.name}'] Pulsing light at new grid ({currentGridPosition.x},{currentGridPosition.y})");
                    lastLitGridPosition = currentGridPosition;
                }
            }

            float distanceToTarget = Vector2.Distance(transform.position, targetPosition);
            if (distanceToTarget < 0.1f)
            {
                DestroyProjectile();
                return;
            }
            CheckScreenBounds();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // 检查是否碰撞到敌人层上的对象
            if (((1 << other.gameObject.layer) & enemyLayer) != 0)
            {
                IDamageable enemyHealth = other.GetComponent<IDamageable>();
                if (enemyHealth != null)
                {
                    int enemyInstanceID = other.gameObject.GetInstanceID();
                    if (!hitEnemyInstanceIDs.Contains(enemyInstanceID)) // 检查是否已击中过此敌人实例
                    {
                        enemyHealth.TakeDamage(damageInfo);

                        // 播放击中特效和音效
                        PlayHitEffects(other.transform.position); // 在敌人位置播放
                        
                        hitEnemyInstanceIDs.Add(enemyInstanceID); // 记录已击中的敌人

                        // 星星不再因为击中敌人而销毁，它将继续飞行直到lifetime或出界
                        // 如果未来需要限制总击中次数，可以在这里加入类似 remainingPierceCount-- 的逻辑，
                        // 并在次数用尽时 DestroyProjectile()，但当前需求是继续飞行。
                    }
                }
                else
                {
                    Debug.LogWarning($"[StarProjectile '{gameObject.name}'] Collided with {other.name} on enemy layer, but it has no IDamageable component.");
                }
            }
        }

        void PlayHitEffects(Vector3 position)
        {
            GameObject effectToPlay = hitEffectPrefab;
            AudioClip soundToPlay = hitSound;

            // 如果此投射物没有指定特效/音效，尝试从源武器获取
            if (damageInfo.sourceWeaponAsset != null)
            {
                if (effectToPlay == null) effectToPlay = damageInfo.sourceWeaponAsset.hitEffect;
                if (soundToPlay == null) soundToPlay = damageInfo.sourceWeaponAsset.hitSound;
            }

            if (effectToPlay != null)
            {
                Instantiate(effectToPlay, position, Quaternion.identity); // 在碰撞点播放特效
            }
            if (soundToPlay != null)
            {
                AudioSource.PlayClipAtPoint(soundToPlay, position);
            }
        }


        void CheckScreenBounds()
        {
            if (Camera.main == null) return; // 防止Camera.main为空

            Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);

            bool isOffScreenX = screenPoint.x < -0.1f || screenPoint.x > 1.1f;
            // 增加Y轴上边界的容忍度，允许星星从更高的屏幕外位置生成并飞入
            bool isOffScreenY = screenPoint.y < -0.2f || screenPoint.y > 1.8f;

            if (isOffScreenX || isOffScreenY)
            {
                DestroyProjectile();
            }
        }

        void DestroyProjectile()
        {
            if (myPool != null)
            {
                myPool.ReturnObjectToPool(gameObject);
            }
            else
            {
                // 如果没有可用的池，则作为后备方案销毁对象
                Destroy(gameObject);
                Debug.LogWarning("StarProjectile's pool reference is null. Destroying object instead of returning to pool.");
            }
        }
    }
}