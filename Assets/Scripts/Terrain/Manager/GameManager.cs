using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("管理器引用")]
    public PickupText3DManager pickupText;
    
    void Awake()
    {
        if (PickupText3DManager.Instance == null)
        {
            if (pickupText == null)
            {
                GameObject simpleTextObj = new GameObject("PickupText3DManager");
                pickupText = simpleTextObj.AddComponent<PickupText3DManager>();
            }
        }
    }   
}
