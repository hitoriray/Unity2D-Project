using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("管理器引用")]
    public PickupText3DManager pickupText;

    private static GameManager _instance;

    void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(gameObject);
        if (PickupText3DManager.Instance == null)
        {
            if (pickupText == null)
            {
                GameObject simpleTextObj = new GameObject("PickupText3DManager");
                pickupText = simpleTextObj.AddComponent<PickupText3DManager>();
            }
        }
    }

    public static GameManager Instance
    {
        get
        {
            return _instance;
        }
    }

    void Start()
    {
        // todo
        // UIManager.Instance.OpenPanel(UIConst.MainPanel);
        // print(GetPackageLocalData().Count);
        // print(GetPackageTable().DataList.Count);
    }
    
    
}

public class GameConst
{
    // 武器类型
    public const int PackageTypeWeapon = 1;
    // 食物类型
    public const int PackageTypeFood = 2;
}
