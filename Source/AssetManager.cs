using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;
namespace GodDance.Source;

/// <summary>
/// Manages all loaded assets in the mod.
/// </summary>
internal static class AssetManager
{
    private static string[] _standardBundles = new[] {
    "localpoolprefabs_assets_areasong",
    "localpoolprefabs_assets_laceboss"
        };

    private static string[] _scenesBundles = new[] {
    "song_17"
        };
    private static List<AssetBundle> _manuallyLoadedBundles = new();
    private static HashSet<string> _manuallyLoadedBundleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static string[] _assetNames = new[] {
        // "Revenge Crystal Spikes",
        // "Abyss Vomit Glob",
        // "Audio Player Actor Simple",
        // "Lost Lace Ground Tendril",
        // "Spike Collider",
        // "Dust Hit Wall",
        // "Attack",
        // "Collider",
        // "Hit",
        // "Spike",
        // "SpikeCollider",
        // "Broken Cog Spike Collider",
        // "cog_dancer_flash_impact",
        "cog_dancer_blade_sphere"
    };

    private static readonly Dictionary<Type, Dictionary<string, Object>> Assets = new();

    private static bool _initialized;
    static readonly object _lockObject = new object();
    private static bool _resourcesPersistent = false;
    /// <summary>
    /// Load all desired assets from loaded asset bundles.
    /// </summary>
    /// RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Awake()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        // 初始初始化
        GameManager.instance.StartCoroutine(Initialize());
    }

    private static void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        Log.Info($"场景切换: {previousScene.name} -> {newScene.name}");

        if (newScene.name == "Cog_Dancers" || newScene.name == "Cog_Dancers_boss")
        {
            Log.Info("切换到Boss场景，验证资源状态");
            GameManager.instance.StartCoroutine(RevalidateResources());
        }
    }

    private static IEnumerator RevalidateResources()
    {
        Log.Info("重新验证资源状态...");

        // 清理所有已失效的引用
        int removedCount = CleanupNullReferences();
        Log.Info($"清理了 {removedCount} 个失效资源引用");

        // 检查必需资源是否可用
        if (!AreRequiredAssetsAvailable())
        {
            Log.Warn("必需资源不可用，重新初始化资源管理器");
            yield return Reinitialize();
        }
        else
        {
            Log.Info("所有必需资源可用");
        }
    }

    private static IEnumerator Reinitialize()
    {
        lock (_lockObject)
        {
            _initialized = false;
        }

        // 清理所有资源
        ClearAllAssets();

        yield return Initialize();
    }

    private static int CleanupNullReferences()
    {
        int removedCount = 0;
        var typesToRemove = new List<Type>();

        foreach (var typeDict in Assets)
        {
            var keysToRemove = new List<string>();

            foreach (var kvp in typeDict.Value)
            {
                // 检查 WeakReference 是否还活着，或者目标是否为 null
                if ( kvp.Value == null)
                {
                    keysToRemove.Add(kvp.Key);
                    removedCount++;
                }
            }

            // 移除失效的键
            foreach (var key in keysToRemove)
            {
                typeDict.Value.Remove(key);
            }

            // 如果该类型的字典为空，标记为待移除
            if (typeDict.Value.Count == 0)
            {
                typesToRemove.Add(typeDict.Key);
            }
        }

        // 移除空字典
        foreach (var type in typesToRemove)
        {
            Assets.Remove(type);
        }

        return removedCount;
    }
    private static bool AreRequiredAssetsAvailable()
    {
        foreach (var requiredAsset in _assetNames)
        {
            var asset = GetInternal<GameObject>(requiredAsset);
            if (asset == null)
            {
                Log.Warn($"必需资源 '{requiredAsset}' 不可用");
                return false;
            }
        }
        return true;
    }
    internal static IEnumerator Initialize()
    {
        if (_initialized)
        {
            yield break;
        }
        lock (_lockObject) // 添加一个static readonly object _lockObject = new object();
        {
            if (_initialized) yield break;
            _initialized = true;
        }
        // var loadedBundles = AssetBundle.GetAllLoadedAssetBundles();
        // foreach (var bundle in loadedBundles)
        // {
        //     if (bundle != null)
        //     {
        //         ProcessBundleAssets(bundle);
        //     }
        // }
        // // 如果某些必需的资产没有找到，再考虑手动加载
        // if (!AreRequiredAssetsLoaded())
        // {
        //     yield return ManuallyLoadBundles();
        //     // 重新处理新加载的bundles
        //     foreach (var bundle in _manuallyLoadedBundles)
        //     {
        //         ProcessBundleAssets(bundle);
        //     }
        // }
        // 清空现有资源（如果有）
        Log.Info("开始初始化资源管理器...");

        // 清空现有资源
        ClearAllAssets();

        var loadedBundles = AssetBundle.GetAllLoadedAssetBundles().ToList();
        Log.Info($"发现 {loadedBundles.Count} 个已加载的 AssetBundle");

        foreach (var bundle in loadedBundles)
        {
            if (bundle != null)
            {
                ProcessBundleAssets(bundle);
            }
        }

        // 如果必需的资产没有找到，手动加载
        if (!AreRequiredAssetsLoaded())
        {
            Log.Info("必需资源未找到，开始手动加载 AssetBundle...");
            yield return ManuallyLoadBundles();
        }

        Log.Info($"资源管理器初始化完成，加载了 {Assets.Values.Sum(dict => dict.Count)} 个资源");

        // 调试输出所有加载的资源
        //DebugAllLoadedAssets();
    }

    private static bool AreRequiredAssetsLoaded()
    {
        Log.Info("Checking if all required assets are loaded...");
        bool allFound = true;

        foreach (var requiredAsset in _assetNames)
        {
            bool found = false;
            foreach (var assetDict in Assets.Values)
            {
                // 检查是否有包含 requiredAsset 的键（部分匹配）
                if (assetDict.Keys.Any(key =>
                    key.IndexOf(requiredAsset, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    Log.Info($"Asset '{requiredAsset}' found as '{assetDict.Keys.First()}'");
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Log.Warn($"Required asset '{requiredAsset}' not found!");
                allFound = false;
            }
        }

        return allFound;
    }
    private static void ProcessBundleAssets(AssetBundle bundle)
    {
        if (bundle == null) return;

        try
        {
            var assetPaths = bundle.GetAllAssetNames();
            if (assetPaths == null || assetPaths.Length == 0) return;

            Log.Info($"处理 Bundle '{bundle.name}'，包含 {assetPaths.Length} 个资源");

            foreach (var assetPath in assetPaths)
            {
                string assetName = Path.GetFileNameWithoutExtension(assetPath);

                if (_assetNames.Any(name =>
                    assetName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    try
                    {
                        var loadedAsset = bundle.LoadAsset(assetPath);
                        if (loadedAsset != null)
                        {
                            StoreAsset(loadedAsset);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"加载资源 {assetPath} 失败: {e}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"处理 Bundle {bundle.name} 失败: {e}");
        }
    }

    private static void StoreAsset(Object asset)
    {
        Type assetType = asset.GetType();
        string assetName = asset.name;

        if (!Assets.ContainsKey(assetType))
        {
            Assets[assetType] = new Dictionary<string, Object>();
        }

        var assetDict = Assets[assetType];

        // 如果已存在，先移除旧引用
        if (assetDict.ContainsKey(assetName))
        {
            assetDict.Remove(assetName);
        }

        // 使用强引用存储
        assetDict[assetName] = asset;
        Log.Debug($"存储资源: {assetName} ({assetType.Name})");
    }/// <summary>
     /// Manually load asset bundles.
     /// </summary>
    internal static IEnumerator ManuallyLoadBundles()
    {
        string platformFolder = GetPlatformFolder();

        // 处理标准 bundles
        foreach (string bundleName in _standardBundles)
        {
            yield return LoadBundle(bundleName, GetStandardBundlePath, platformFolder);
        }

        // 处理场景 bundles
        foreach (string bundleName in _scenesBundles)
        {
            yield return LoadBundle(bundleName, GetScenesBundlePath, platformFolder);
        }
    }

    private static IEnumerator LoadBundle(string bundleName, Func<string, string, string> pathBuilder, string platformFolder)
    {
        if (IsBundleAlreadyLoaded(bundleName))
        {
            Log.Info($"AssetBundle '{bundleName}' is already loaded, skipping...");
            yield break;
        }

        string bundlePath = pathBuilder(bundleName, platformFolder);

        if (!File.Exists(bundlePath))
        {
            Log.Error($"AssetBundle file not found: {bundlePath}");
            yield break;
        }

        var bundleLoadRequest = AssetBundle.LoadFromFileAsync(bundlePath);
        yield return bundleLoadRequest;

        AssetBundle bundle = bundleLoadRequest.assetBundle;
        if (bundle == null)
        {
            Log.Error($"Failed to load AssetBundle: {bundlePath}");
            yield break;
        }

        _manuallyLoadedBundles.Add(bundle);
        ProcessBundleAssets(bundle);
        _manuallyLoadedBundleNames.Add(bundleName);
    }

    internal static bool IsInitialized()
    {
        return _initialized;
    }
// 修改Get方法，添加同步重试逻辑
   internal static T Get<T>(string assetName) where T : Object
{
    var asset = GetInternal<T>(assetName);
    
    if (asset == null)
    {
        Log.Error($"资源 '{assetName}' ({typeof(T).Name}) 获取失败");
        
        // 同步重新加载资源
        asset = SynchronousReload<T>(assetName);
    }

    return asset;
}

    private static IEnumerator RevalidateAndRecover(string assetName)
    {
        Log.Warn($"尝试恢复资源: {assetName}");
        yield return RevalidateResources();

        // 再次尝试获取
        var recoveredAsset = GetInternal<GameObject>(assetName);
        if (recoveredAsset != null)
        {
            Log.Info($"资源 {assetName} 恢复成功");
        }
        else
        {
            Log.Error($"资源 {assetName} 恢复失败");
        }
    }

    private static T GetInternal<T>(string assetName) where T : Object
    {
        Type assetType = typeof(T);

        if (!Assets.ContainsKey(assetType))
        {
            Log.Error($"资源类型 {assetType.Name} 未注册");
            return null;
        }

        var assetDict = Assets[assetType];

        if (assetDict.ContainsKey(assetName))
        {
            var asset = assetDict[assetName] as T;
            if (asset != null)
            {
                return asset;
            }
            else
            {
                // 资源存在但类型不匹配或为null，移除
                assetDict.Remove(assetName);
                Log.Warn($"资源 {assetName} 已失效，已从缓存移除");
            }
        }

        return null;
    }
    // 添加同步重载方法
private static T SynchronousReload<T>(string assetName) where T : Object
{
    Log.Warn($"同步重新加载资源: {assetName}");
    
    // 尝试从已加载的AssetBundle中重新加载
    foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles())
    {
        if (bundle == null) continue;
        
        try
        {
            var assetPaths = bundle.GetAllAssetNames();
            foreach (var assetPath in assetPaths)
            {
                string currentAssetName = Path.GetFileNameWithoutExtension(assetPath);
                if (currentAssetName.Equals(assetName, StringComparison.OrdinalIgnoreCase))
                {
                    var loadedAsset = bundle.LoadAsset<T>(assetPath);
                    if (loadedAsset != null)
                    {
                        StoreAsset(loadedAsset);
                        Log.Info($"资源 {assetName} 重新加载成功");
                        return loadedAsset;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"重新加载资源 {assetName} 失败: {e}");
        }
    }
    
    Log.Error($"无法重新加载资源 {assetName}");
    return null;
}
    private static void ClearAllAssets()
    {
        Assets.Clear();
        _manuallyLoadedBundleNames.Clear();

        // 注意：不要销毁手动加载的 bundle，让 Unity 管理它们的生命周期
        _manuallyLoadedBundles.Clear();

        Log.Info("已清空所有资源缓存");
    }
    private static bool IsBundleAlreadyLoaded(string bundleName)
    {
        // 检查我们自己已手动加载的记录
        if (_manuallyLoadedBundleNames.Contains(bundleName))
        {
            return true;
        }

        // 检查是否已有包含我们所需资源的 bundle
        foreach (var loadedBundle in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (loadedBundle == null) continue;

            var assetPaths = loadedBundle.GetAllAssetNames();
            if (assetPaths == null || assetPaths.Length == 0)
                continue;

            // 检查 bundle 是否包含我们期望的资源
            foreach (var assetPath in assetPaths)
            {
                string assetName = Path.GetFileNameWithoutExtension(assetPath);
                if (_assetNames.Any(name =>
                    assetName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    // 找到匹配资源，记录该 bundle 为已加载
                    _manuallyLoadedBundleNames.Add(bundleName);
                    return true;
                }
            }
        }

        return false;
    }
    private static string GetPlatformFolder()
    {
        return Application.platform switch
        {
            RuntimePlatform.WindowsPlayer => "StandaloneWindows64",
            RuntimePlatform.OSXPlayer => "StandaloneOSX",
            RuntimePlatform.LinuxPlayer => "StandaloneLinux64",
            _ => ""
        };
    }
    private static string GetStandardBundlePath(string bundleName, string platformFolder)
    {
        return Path.Combine(Addressables.RuntimePath, platformFolder, $"{bundleName}.bundle");
    }

    private static string GetScenesBundlePath(string bundleName, string platformFolder)
    {
        return Path.Combine(Addressables.RuntimePath, platformFolder, "scenes_scenes_scenes", $"{bundleName}.bundle");
    }
    internal static IEnumerable<string> GetAllAssetNames()
    {
        var allNames = new List<string>();
        foreach (var assetDict in Assets.Values)
        {
            allNames.AddRange(assetDict.Keys);
        }
        return allNames;
    }

    // 添加场景加载方法
    // private static void DebugAllLoadedAssets()
    // {
    //     Log.Info("=== 所有已加载资源 ===");
    //     foreach (var typeDict in Assets)
    //     {
    //         int validCount = typeDict.Value.Count(kvp => kvp.Value.IsAlive && kvp.Value.Target != null);
    //         Log.Info($"类型: {typeDict.Key.Name}, 数量: {validCount}/{typeDict.Value.Count}");

    //         foreach (var asset in typeDict.Value)
    //         {
    //             object target = isAlive ? asset.Value.Target : null;
    //             Log.Info($"  - {asset.Key} -> 存活: {isAlive}, 目标: {(target != null ? target.ToString() : "null")}");
    //         }
    //     }
    //     Log.Info("=== 资源列表结束 ===");
    // }
}