using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Collections;

public class ABManager : SingletonAutoMono<ABManager> {

    // 主包
    private AssetBundle mainAB = null;
    // 依赖包获取用的配置文件
    private AssetBundleManifest mainfest = null;

    // 重复加载包会报错，因此用字典存储加载过的包
    private Dictionary<string, AssetBundle> abDict = new Dictionary<string, AssetBundle>();


    /// <summary>
    /// AB包的存放路径 方便修改
    /// </summary>
    private string PathUrl {
        get {
            return Application.streamingAssetsPath + "/";
        }
    }

    /// <summary>
    /// 主包名 方便修改
    /// </summary>
    private string MainABName {
        get {
#if UNITY_IOS
            return "PC";
#elif UNITY_ANDROID
            return "Android";
#elif UNITY_STANDALONE_WIN
            return "PC";
#else
            return "PC";
#endif
        }
    }

    /// <summary>
    /// 加载AB包
    /// </summary>
    /// <param name="abName"></param>
    private void LoadAB(string abName) {
        // 1.加载AB包
        if (mainAB == null) {
            mainAB = AssetBundle.LoadFromFile(PathUrl + MainABName);
            mainfest = mainAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }
        AssetBundle ab = null;
        // 2.获取依赖包相关信息
        string[] strs = mainfest.GetAllDependencies(abName);
        foreach (string str in strs) {
            if (!abDict.ContainsKey(str)) {
                ab = AssetBundle.LoadFromFile(PathUrl + str);
                abDict.Add(str, ab);
            }
        }

        // 3.加载资源来源包
        if (!abDict.ContainsKey(abName)) {
            ab = AssetBundle.LoadFromFile(PathUrl + abName);
            abDict.Add(abName, ab);
        }
    }

#region 同步加载

    // 同步加载（不指定类型）
    public Object LoadRes(string abName, string resName) {
        // 1.加载AB包
        LoadAB(abName);
        // 2.加载资源
        // 为了外面方便 在加载资源时 判断一下该资源是否是GameObject
        // 如果是 直接实例化 再返回给外部
        Object obj = abDict[abName].LoadAsset(resName);
        if (obj is GameObject) {
            return Instantiate(obj as GameObject);
        }
        return obj;
    }

    // 同步加载（指定类型）
    public Object LoadRes(string abName, string resName, System.Type type) {
        LoadAB(abName);
        Object obj = abDict[abName].LoadAsset(resName, type);
        if (obj is GameObject) {
            return Instantiate(obj as GameObject);
        }
        return obj;
    }

    // 同步加载（泛型）
    public T LoadRes<T>(string abName, string resName) where T : Object {
        LoadAB(abName);
        T obj = abDict[abName].LoadAsset<T>(resName);
        if (obj is GameObject) {
            return Instantiate(obj);
        }
        return obj;
    }

#endregion

#region 异步加载    
    // 异步加载的方法
    // 这里的异步加载 AB包并没有使用异步加载 只是从AB包中加载资源时使用了异步加载
    // 根据名字异步加载资源
    public void LoadResAsync(string abName, string resName, UnityAction<Object> callback) {
        StartCoroutine(RealLoadResAsync(abName, resName, callback));
    }
    private IEnumerator RealLoadResAsync(string abName, string resName, UnityAction<Object> callback) {
        LoadAB(abName);
        AssetBundleRequest abr = abDict[abName].LoadAssetAsync(resName);
        yield return abr;
        // 异步加载结束后，通过委托传递给外部，外部来处理
        if (abr.asset is GameObject) {
            callback(Instantiate(abr.asset as GameObject));
        } else {
            callback(abr.asset);
        }
    }

    // 根据Type异步加载资源
    public void LoadResAsync(string abName, string resName, System.Type type, UnityAction<Object> callback) {
        StartCoroutine(RealLoadResAsync(abName, resName, type, callback));
    }
    private IEnumerator RealLoadResAsync(string abName, string resName, System.Type type, UnityAction<Object> callback) {
        LoadAB(abName);
        AssetBundleRequest abr = abDict[abName].LoadAssetAsync(resName, type);
        yield return abr;
        if (abr.asset is GameObject) {
            callback(Instantiate(abr.asset as GameObject));
        } else {
            callback(abr.asset);
        }
    }

    // 根据泛型异步加载资源
    public void LoadResAsync<T>(string abName, string resName, UnityAction<T> callback) where T : Object {
        StartCoroutine(RealLoadResAsync<T>(abName, resName, callback));
    }
    private IEnumerator RealLoadResAsync<T>(string abName, string resName, UnityAction<T> callback) where T : Object {
        LoadAB(abName);
        AssetBundleRequest abr = abDict[abName].LoadAssetAsync<T>(resName);
        yield return abr;
        if (abr.asset is GameObject) {
            callback(Instantiate(abr.asset) as T);
        } else {
            callback(abr.asset as T);
        }
    }

#endregion

#region 卸载
    // 单个包的卸载
    public void Unload(string abName) {
        if (abDict.ContainsKey(abName)) {
            abDict[abName].Unload(false);
            abDict.Remove(abName);
        }
    }

    // 所有包的卸载
    public void ClearAB() {
        AssetBundle.UnloadAllAssetBundles(false);
        abDict.Clear();
        mainAB = null;
        mainfest = null;
    }
#endregion

}