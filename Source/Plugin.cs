using System.IO;
using System.Reflection;
using System.Collections;
using BepInEx;
using HarmonyLib;
using GodDance.Source.Patches;
using GodDance.Source.Behaviours;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GodDance.Source;

/// <summary>
/// The main plugin class.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static Harmony _harmony = null!;

    internal static Texture2D[] AtlasTextures = new Texture2D[2];
    public static bool IsBossSaveLoaded { get; set; } = false;
    public static string OriginalSaveBackupPath { get; set; } = null;
    public static bool IsInCogDancersRoom { get; set; } = false;
    public static string CurrentSaveFileName { get; set; } = null;
    private static GameObject _persistentManager;


    private void Awake()
    {
        Log.Init(Logger);

        //LoadSinnerTextures();
        Log.Info("God Dance plugin loaded!1");
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        SceneManager.activeSceneChanged += OnSceneChange;
        // 创建持久化管理器
        CreatePersistentManager();
        Log.Info("God Dance plugin loaded2");
    }

    private void OnSceneChange(Scene oldScene, Scene newScene)
    {
        // 更新是否在Cog_Dancers房间的状态
        Plugin.IsInCogDancersRoom = newScene.name == "Cog_Dancers" || newScene.name == "Cog_Dancers_boss";

        if (Plugin.IsInCogDancersRoom)
        {
            Log.Info($"切换到Boss场景: {newScene.name}");
        }
        // Only change things when loading a save file
        if (oldScene.name != "Menu_Title")
        {
            return;
        }

        _harmony.UnpatchSelf();
        _harmony.PatchAll(typeof(GodDancePatches));

    }

    /// <summary>
    /// Load textures embedded in the assembly.
    /// </summary>
    // private void LoadSinnerTextures() {
    //     var assembly = Assembly.GetExecutingAssembly();
    //     foreach (string resourceName in assembly.GetManifestResourceNames()) {
    //         using Stream? stream = assembly.GetManifestResourceStream(resourceName);
    //         if (stream == null) continue;

    //         if (resourceName.Contains("atlas0")) {
    //             var buffer = new byte[stream.Length];
    //             stream.Read(buffer, 0, buffer.Length);
    //             var atlasTex = new Texture2D(2, 2);
    //             atlasTex.LoadImage(buffer);
    //             AtlasTextures[0] = atlasTex;
    //         } else if (resourceName.Contains("atlas1")) {
    //             var buffer = new byte[stream.Length];
    //             stream.Read(buffer, 0, buffer.Length);
    //             var atlasTex = new Texture2D(2, 2);
    //             atlasTex.LoadImage(buffer);
    //             AtlasTextures[1] = atlasTex;
    //         }
    //     }
    // }
     private void CreatePersistentManager()
    {
        // 查找是否已存在持久化管理器
        _persistentManager = GameObject.Find("GodDancePersistentManager");
        if (_persistentManager == null)
        {
            _persistentManager = new GameObject("GodDancePersistentManager");
            UnityEngine.Object.DontDestroyOnLoad(_persistentManager);
            
            // 添加存档管理器组件
            _persistentManager.AddComponent<SaveSwitchManager>();
            Log.Info("创建持久化存档切换管理器");
        }
        else
        {
            Log.Info("找到已存在的持久化存档切换管理器");
        }
    }
    private void OnDestroy()
    {
        _harmony.UnpatchSelf();
        AssetManager.UnloadAll();
    }
}