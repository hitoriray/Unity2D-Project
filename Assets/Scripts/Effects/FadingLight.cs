using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class FadingLight : MonoBehaviour
{
    [Tooltip("灯光完全熄灭所需的时间")]
    public float fadeDuration = 0.5f;

    private Light2D light2D;
    private float initialIntensity;
    private float fadeTimer;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        if (light2D == null)
        {
            Debug.LogError("FadingLight脚本需要一个Light2D组件!", this);
            enabled = false;
            return;
        }
        initialIntensity = light2D.intensity;
        fadeTimer = 0f;
    }

    void Update()
    {
        fadeTimer += Time.deltaTime;
        float percent = fadeTimer / fadeDuration;

        // 随着时间的推移，线性地将光强从初始值减到0
        light2D.intensity = Mathf.Lerp(initialIntensity, 0f, percent);

        // 当光完全熄灭后，销毁自身以清理场景
        if (percent >= 1.0f)
        {
            Destroy(gameObject);
        }
    }
} 