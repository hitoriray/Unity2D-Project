using UnityEngine;

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
        
        private float journeyDuration = 0.75f; // 飞一整圈的时间
        private float journeyTimer;
        private bool isClockwise; // 随机决定顺时针还是逆时针

        private TrailRenderer tr; // 将TrailRenderer缓存起来

        // 在Inspector中调整椭圆的"胖瘦"
        public float ellipseWidth = 2.5f;
        // 在Inspector中统一所有幻影剑的大小
        public float uniformScale = 1.0f;

        public void Initialize(float dmg, Transform playerTransform, Vector2 mouseTarget, SwordAppearance appearance)
        {
            damage = dmg;
            
            // --- 椭圆参数计算 ---
            Vector2 startPos = playerTransform.position;

            // 1. 计算圆心
            ellipseCenter = (startPos + mouseTarget) / 2f;
            
            // 2. 计算长轴半径和旋转角度
            Vector2 direction = mouseTarget - startPos;
            radiusA = direction.magnitude / 2f;
            rotationAngle = Mathf.Atan2(direction.y, direction.x);
            
            // 3. 设置短轴半径
            radiusB = ellipseWidth;

            // 4. 随机飞行方向
            isClockwise = Random.value > 0.5f;

            // 5. 设置初始位置和计时器
            journeyTimer = 0f;
            transform.position = startPos; // 确保起始位置精确

            GetComponent<SpriteRenderer>().sprite = appearance.swordSprite;
            

            // 获取并缓存TrailRenderer组件
            tr = GetComponentInChildren<TrailRenderer>(); // 使用GetComponentInChildren确保能找到
            if (tr != null)
            {
                // -- 全新的、由代码驱动的颜色渐变 --
                Gradient gradient = new Gradient();

                // 1. 设置颜色关键字 (决定拖尾从头到尾的颜色)
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(appearance.trailColor, 0.0f), new GradientColorKey(appearance.trailColor, 1.0f) },
                    // 2. 设置Alpha关键字 (决定拖尾的淡出效果)
                    new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                );

                // 3. 将这个全新的、完美的渐变应用到Trail Renderer
                // tr.colorGradient = gradient;
                // // -- 终极解决方案：通过脚本直接修改材质的TintColor --
                // // 注意：这会创建一个材质实例，但对于特效来说这是常见的做法
                // tr.material.SetColor("_TintColor", appearance.trailColor);
                // tr.time = journeyDuration; // 初始化拖尾时间
            }
            
            // 强制统一尺寸
            transform.localScale = Vector3.one * uniformScale;

            Destroy(gameObject, journeyDuration);
        }

        private void Update()
        {
            journeyTimer += Time.deltaTime;
            float percent = journeyTimer / journeyDuration;

            // --- 拖尾宽度修正逻辑 ---
            if (tr != null)
            {
                // 动态缩短拖尾的生命周期，使其从起点开始变细
                tr.time = journeyDuration * (1 - percent);
            }

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
            
            // 4. 让剑柄始终朝向圆心 (剑刃朝外)
            Vector2 dirToCenter = ellipseCenter - (Vector2)transform.position;
            if(dirToCenter != Vector2.zero)
            {
                // transform.right 指向剑的"前方"，所以我们让它指向远离中心的方向
                transform.right = -dirToCenter.normalized;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                Debug.Log($"天顶剑击中敌人 {other.name}，造成 {damage} 点伤害");
            }
        }
    }
} 