using UnityEngine;
using Combat.Interfaces; // 使用我们创建的接口命名空间
using Utility;           // 新增：为了能引用 ObjectPool
using System.Collections.Generic;

namespace Terraira.Combat
{
    public class PhantomSword : MonoBehaviour
    {
        private float damage;
        
        // --- 椭圆轨迹参数 ---
        private Vector2 ellipseCenter;
        private float radiusA; // 长轴半径
        private float radiusB; // 短轴半径
        private float rotationAngle; // 椭圆的旋转角度
        
        [Tooltip("飞一整圈的时间")]
        public float journeyDuration = 0.75f; // 飞一整圈的时间
        private float journeyTimer;
        private bool isClockwise; // 随机决定顺时针还是逆时针
        private const float spriteOffsetAngle = 45f;

        private TrailRenderer tr; // 将TrailRenderer缓存起来

        // 在Inspector中调整椭圆的"胖瘦"
        public float ellipseWidth = 2.5f;
        // 在Inspector中统一所有幻影剑的大小
        public float uniformScale = 1.0f;

        [Header("Lighting Effect")]
        [Tooltip("是否启用拖尾光效")]
        public bool enableLightTrail = true;
        [Tooltip("拖尾光效的半径")]
        public float lightRadius = 4f;
        [Tooltip("拖尾光效的持续时间")]
        public float lightLifetime = 0.4f;
        [Tooltip("拖尾光效的初始强度")]
        public float lightIntensity = 0.8f;

        [Tooltip("生成拖尾灯光的间隔时间(秒)")]
        public float lightSpawnInterval = 0.05f;

        private float lightSpawnTimer;

        [Header("Collision & Damage")]
        [Tooltip("敌人所在的图层")]
        public LayerMask enemyLayer;
        [Tooltip("击中敌人时播放的特效预制体 (可选)")]
        public GameObject hitEffectPrefab;
        [Tooltip("击中敌人时播放的音效 (可选)")]
        public AudioClip hitSound;

        private DamageInfo damageInfo;
        private List<int> hitEnemyInstanceIDs;

        public void Initialize(DamageInfo dmgInfo, Transform playerTransform, Vector2 mouseTarget, SwordAppearance appearance)
        {
            damage = dmgInfo.baseDamage;
            
            // --- 椭圆参数计算 (引入随机性) ---

            // 1. 目标点随机偏移：在鼠标指针周围的一个小半径内随机选择一个点
            Vector2 randomOffset = Random.insideUnitCircle * 2f; // 1.5f 是随机半径，可以调整
            Vector2 finalEndPoint = mouseTarget + randomOffset;

            // 2. 使用随机化后的目标点来计算椭圆基础参数
            ellipseCenter = ((Vector2)playerTransform.position + finalEndPoint) / 2;
            Vector2 vecToTarget = finalEndPoint - (Vector2)playerTransform.position;
            radiusA = vecToTarget.magnitude / 2; // 半长轴

            // 3. 椭圆扁度随机化：让胖瘦程度在一个范围内变化
            radiusB = radiusA * Random.Range(0.4f, 0.6f); // 半短轴 (椭圆的扁平程度)
            
            rotationAngle = Mathf.Atan2(vecToTarget.y, vecToTarget.x); // 椭圆的旋转角度 (弧度)

            // 4. 随机飞行方向
            isClockwise = Random.value > 0.5f;

            // 5. 设置初始位置和计时器
            journeyTimer = 0f;
            transform.position = playerTransform.position; // 确保起始位置精确

            GetComponent<SpriteRenderer>().sprite = appearance.swordSprite;
            

            // 获取并缓存TrailRenderer组件
            tr = GetComponentInChildren<TrailRenderer>();
            if (tr != null)
            {
                // -- 全新的、由代码驱动的颜色渐变 --
                Gradient gradient = new Gradient();

                // 1. 设置颜色关键字 (决定拖尾从头到尾的颜色)
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(appearance.trailColor, 0.0f), new GradientColorKey(appearance.trailColor, 1.0f) },
                    // 2. 设置Alpha关键字 (决定拖尾的淡出效果)
                    new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                );

                // 3. 将这个全新的、完美的渐变应用到Trail Renderer
                tr.colorGradient = gradient;

                AnimationCurve widthCurve = new AnimationCurve(
                    new Keyframe(0f, tr.startWidth),
                    new Keyframe(1f, 0f)
                );
                tr.widthCurve = widthCurve;
            }
            
            // 强制统一尺寸
            transform.localScale = Vector3.one * uniformScale;

            // 重置灯光生成计时器
            lightSpawnTimer = 0;

            this.damageInfo = dmgInfo;

            if (hitEnemyInstanceIDs == null)
            {
                hitEnemyInstanceIDs = new List<int>();
            }
            else
            {
                hitEnemyInstanceIDs.Clear();
            }

            Destroy(gameObject, journeyDuration);
        }

        private void Update()
        {
            journeyTimer += Time.deltaTime;
            float percent = journeyTimer / journeyDuration;

            // --- 拖尾宽度修正逻辑 ---
            // if (tr != null)
            // {
            //     // 动态缩短拖尾的生命周期，使其从起点开始变细
            //     tr.time = journeyDuration * (1 - percent);
            // }

            // 1. 计算当前在椭圆上的角度 (theta)
            // 我们希望从玩家位置开始，所以起点角度是 PI
            float theta = Mathf.PI + (isClockwise ? -1 : 1) * (percent * 2 * Mathf.PI);

            // 2. 使用标准椭圆方程计算局部坐标
            Vector2 localPos = new Vector2(
                radiusA * Mathf.Cos(theta),
                radiusB * Mathf.Sin(theta)
            );

            // 3. 将局部坐标旋转，并移动到世界坐标中的圆心位置
            // (Quaternion * Vector) 用于旋转向量
            Vector2 worldOffset = Quaternion.Euler(0, 0, rotationAngle * Mathf.Rad2Deg) * localPos;
            transform.position = ellipseCenter + worldOffset;
            
            UpdateLightTrail();

            // 4. 让剑柄始终朝向圆心 (剑刃朝外)
            Vector2 dirToCenter = ellipseCenter - (Vector2)transform.position;
            if (dirToCenter.sqrMagnitude > 0.0001f)
            {
                // 本地 +X（right）要指向"远离圆心"的方向
                Vector2 outward = -dirToCenter.normalized;
                // Atan2 返回的是向量与 +X 轴的夹角（弧度），转成度数
                float rawAngle = Mathf.Atan2(outward.y, outward.x) * Mathf.Rad2Deg;
                // 扣掉 Sprite 自身的 45° 旋转，才能对齐到"剑尖指向远处"
                float correctedAngle = rawAngle - spriteOffsetAngle;
                transform.rotation = Quaternion.Euler(0f, 0f, correctedAngle);
            }
        }

        private void UpdateLightTrail()
        {
            if (!enableLightTrail || DynamicLightManager.Instance == null) return;

            lightSpawnTimer += Time.deltaTime;
            if (lightSpawnTimer >= lightSpawnInterval)
            {
                lightSpawnTimer = 0f;
                DynamicLightManager.Instance.AddLight(transform.position, lightRadius, lightLifetime, lightIntensity);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 检查是否碰撞到敌人层上的对象
            if (((1 << other.gameObject.layer) & enemyLayer) != 0)
            {
                IDamageable enemyHealth = other.GetComponent<IDamageable>();
                if (enemyHealth != null)
                {
                    int enemyInstanceID = other.gameObject.GetInstanceID();
                    if (!hitEnemyInstanceIDs.Contains(enemyInstanceID)) 
                    {
                        enemyHealth.TakeDamage(damageInfo);
                        
                        PlayHitEffects(other.ClosestPoint(transform.position));
                        
                        hitEnemyInstanceIDs.Add(enemyInstanceID);
                    }
                }
            }
        }

        private void PlayHitEffects(Vector3 position)
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
                Instantiate(effectToPlay, position, Quaternion.identity);
            }
            if (soundToPlay != null)
            {
                AudioSource.PlayClipAtPoint(soundToPlay, position);
            }
        }
    }
} 