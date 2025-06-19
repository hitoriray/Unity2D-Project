using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteGlow : MonoBehaviour
{
    [Tooltip("辉光的颜色")]
    public Color glowColor = Color.yellow;

    [Tooltip("辉光脉冲的速度")]
    public float pulseSpeed = 1.0f;

    [Tooltip("辉光最小的Alpha值")]
    [Range(0, 1)]
    public float minAlpha = 0.3f;

    [Tooltip("辉光最大的Alpha值")]
    [Range(0, 1)]
    public float maxAlpha = 0.7f;

    private SpriteRenderer spriteRenderer;
    private float time;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 使用正弦函数创建一个在 minAlpha 和 maxAlpha 之间平滑变化的alpha值
        time += Time.deltaTime * pulseSpeed;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(time) + 1) / 2);

        // 更新SpriteRenderer的颜色
        spriteRenderer.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
    }
} 