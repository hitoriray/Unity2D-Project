using System;
using System.IO;
using UnityEngine;
using XLua;

public class LuaManager : SingletonAutoMono<LuaManager> {

    private LuaEnv luaEnv;

    public LuaTable Global {
        get {
            return luaEnv.Global;
        }
    }

    private void Awake() {
        if (luaEnv != null) return;
        luaEnv = new LuaEnv();
        luaEnv.AddLoader(CustomLoader);
        luaEnv.AddLoader(CustomABLoader);
    }

    public byte[] CustomLoader(ref string filepath) {
        string path = Application.dataPath + "/Lua/" + filepath + ".lua";
        if (File.Exists(path)) {
            return File.ReadAllBytes(path);
        } else {
            Debug.LogError("Lua file not found: " + filepath);
        }
        return null;
    }

    public byte[] CustomABLoader(ref string filepath) {
        // 通过AB包管理器同步加载lua脚本资源
        TextAsset lua = ABManager.GetInstance().LoadRes<TextAsset>("lua", filepath + ".lua");
        if (lua != null) {
            return lua.bytes;
        }
        Debug.LogError("Lua file not found: " + filepath);
        return null;
    }

    public void DoLuaFile(string filename) {
        DoString(string.Format("require '{0}'", filename));
    }

    public void DoString(string lua) {
        if (luaEnv == null) {
            Debug.LogError("LuaEnv is not initialized");
            return;
        }
        luaEnv.DoString(lua);
    }

    public void Tick() {
        if (luaEnv == null) {
            Debug.LogError("LuaEnv is not initialized");
            return;
        }
        luaEnv.Tick();
    }

    public void OnDestroy() {
        if (luaEnv != null) {
            luaEnv.Dispose();
            luaEnv = null;
        }
    }

}