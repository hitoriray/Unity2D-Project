using System.Collections;
using UnityEngine;
using System; // 引入System命名空间，为了使用Action回调
using Box;

public class BoxController : MonoBehaviour
{
    [Header("核心数据")]
    public BoxData BoxData;

    [Header("子对象引用")]
    [SerializeField] private SpriteRenderer rendererTopLeft;
    [SerializeField] private SpriteRenderer rendererTopRight;
    [SerializeField] private SpriteRenderer rendererBottomLeft;
    [SerializeField] private SpriteRenderer rendererBottomRight;
    
    // 公开的属性，用于从外部查询宝箱的状态，但只能在此脚本内部修改
    public bool IsOpen { get; private set; }
    public bool IsAnimating { get; private set; }

    void Start()
    {
        // 游戏开始时初始化外观
        if (BoxData != null) { ApplyBoxLook(); }
        else { Debug.LogError("BoxData 未分配!", this.gameObject); }
    }

    public void ApplyBoxLook()
    {
        rendererBottomLeft.sprite = BoxData.bottomLeftSprites[(int)BoxState.Closed];
        rendererBottomRight.sprite = BoxData.bottomRightSprites[(int)BoxState.Closed];
        rendererTopLeft.sprite = BoxData.topLeftSprites[(int)BoxState.Closed];
        rendererTopRight.sprite = BoxData.topRightSprites[(int)BoxState.Closed];
    }

    /// <summary>
    /// 打开宝箱，并提供一个动画播放完毕后的“回调函数”
    /// </summary>
    public void OpenBox(Action onAnimationComplete = null)
    {
        if (!IsOpen && !IsAnimating)
        {
            StartCoroutine(OpenAnimationCoroutine(onAnimationComplete));
        }
    }

    /// <summary>
    /// 关闭宝箱
    /// </summary>
    public void CloseBox()
    {
        if (IsOpen && !IsAnimating)
        {
            StartCoroutine(CloseAnimationCoroutine());
        }
    }

    private IEnumerator OpenAnimationCoroutine(Action onAnimationComplete)
    {
        IsAnimating = true;
        
        // 半开 -> 全开
        rendererBottomLeft.sprite = BoxData.bottomLeftSprites[(int)BoxState.Opening];
        rendererBottomRight.sprite = BoxData.bottomRightSprites[(int)BoxState.Opening];
        rendererTopLeft.sprite = BoxData.topLeftSprites[(int)BoxState.Opening];
        rendererTopRight.sprite = BoxData.topRightSprites[(int)BoxState.Opening];
        yield return new WaitForSeconds(BoxData.openAnimationSpeed);
        rendererTopLeft.sprite = BoxData.topLeftSprites[(int)BoxState.Open];
        rendererTopRight.sprite = BoxData.topRightSprites[(int)BoxState.Open];
        rendererBottomLeft.sprite = BoxData.bottomLeftSprites[(int)BoxState.Open];
        rendererBottomRight.sprite = BoxData.bottomRightSprites[(int)BoxState.Open];

        IsAnimating = false;
        IsOpen = true;

        // 动画结束，执行传入的回调函数 (如果它存在)
        onAnimationComplete?.Invoke();
    }

    private IEnumerator CloseAnimationCoroutine()
    {
        IsAnimating = true;

        // 全开 -> 半开
        rendererBottomLeft.sprite = BoxData.bottomLeftSprites[(int)BoxState.Open];
        rendererBottomRight.sprite = BoxData.bottomRightSprites[(int)BoxState.Open];
        rendererTopLeft.sprite = BoxData.topLeftSprites[(int)BoxState.Opening];
        rendererTopRight.sprite = BoxData.topRightSprites[(int)BoxState.Opening];
        yield return new WaitForSeconds(BoxData.openAnimationSpeed);
        // 半开 -> 关闭
        rendererTopLeft.sprite = BoxData.topLeftSprites[(int)BoxState.Closed];
        rendererTopRight.sprite = BoxData.topRightSprites[(int)BoxState.Closed];
        rendererBottomLeft.sprite = BoxData.bottomLeftSprites[(int)BoxState.Closed];
        rendererBottomRight.sprite = BoxData.bottomRightSprites[(int)BoxState.Closed];
        
        IsAnimating = false;
        IsOpen = false;
    }
}