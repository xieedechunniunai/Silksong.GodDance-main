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
        "Attack",
        "Collider",
        "Hit",
        "Spike",
        "SpikeCollider",
        "Broken Cog Spike Collider",
        "cog_dancer_flash_impact",
        "cog_dancer_blade_sphere"
    };

    private static readonly Dictionary<Type, Dictionary<string, Object>> Assets = new();

    private static bool _initialized;
    static readonly object _lockObject = new object();
    /// <summary>
    /// Load all desired assets from loaded asset bundles.
    /// </summary>
    internal static IEnumerator Initialize()
    {
        if (_initialized)
        {
            yield break;
        }
        lock (_lockObject) // 添加一个static readonly object _lockObject = new object();
        {
            if (_initialized)
            {
                yield break;
            }

            _initialized = true;
            
            var loadedBundles = AssetBundle.GetAllLoadedAssetBundles();
            foreach (var bundle in loadedBundles)
            {
                if (bundle != null)
                {
                    ProcessBundleAssets(bundle);
                }
            }
            // 如果某些必需的资产没有找到，再考虑手动加载
            if (!AreRequiredAssetsLoaded())
            {
                yield return ManuallyLoadBundles();
                // 重新处理新加载的bundles
                foreach (var bundle in _manuallyLoadedBundles)
                {
                    ProcessBundleAssets(bundle);
                }
            }
        }
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
        if (bundle == null)
        {
            Log.Warn("Encountered null bundle in loaded bundles list");
            return;
        }

        var assetPaths = bundle.GetAllAssetNames();
        if (assetPaths == null || assetPaths.Length == 0)
        {
            Log.Warn($"Bundle '{bundle.name}' has no assets or failed to get asset names");
            return;
        }
        Log.Info($"Processing bundle '{bundle.name}' with {assetPaths.Length} assets");
        foreach (var assetPath in assetPaths)
        {
            string assetName = Path.GetFileNameWithoutExtension(assetPath);// 改进匹配：支持部分匹配和大小写不敏感
            bool isMatch = _assetNames.Any(name =>
                assetName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);

            if (isMatch)
            {
                // 使用同步加载方式
                try
                {
                    var loadedAsset = bundle.LoadAsset(assetPath);
                    if (loadedAsset != null)
                    {
                        Type assetType = loadedAsset.GetType();
                        string assetName1 = loadedAsset.name;

                        if (!Assets.ContainsKey(assetType))
                        {
                            Assets.Add(assetType, new Dictionary<string, Object>());
                        }

                        var assetDict = Assets[assetType];
                        if (assetDict.ContainsKey(assetName1))
                        {
                            if (assetDict[assetName1] == null)
                            {
                                Log.Info($"Key \"{assetName1}\" for sub-dictionary of type \"{assetType}\" exists, but its value is null; Replacing with new asset...");
                                assetDict[assetName1] = loadedAsset;
                            }
                            else
                            {
                                Log.Warn($"There is already an asset \"{assetName1}\" of type \"{assetType}\"!");
                            }
                        }
                        else
                        {
                            Log.Debug($"Adding asset {assetName1} of type {assetType}...");
                            assetDict.Add(assetName1, loadedAsset);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process asset {assetPath} from bundle {bundle.name}: {e}");
                    continue;
                }
            }
        }
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
    /// <summary>
    /// Unload all saved assets.
    /// </summary>
    internal static void UnloadAll()
    {
        foreach (var assetDict in Assets.Values)
        {
            foreach (var asset in assetDict.Values)
            {
                Object.DestroyImmediate(asset);
            }
        }

        Assets.Clear();
        GC.Collect();
    }
    internal static bool IsInitialized()
    {
        return _initialized;
    }
    /// <summary>
    /// Unload bundles that were manually loaded for this mod.
    /// </summary>
    internal static void UnloadManualBundles()
    {
        foreach (var bundle in _manuallyLoadedBundles)
        {
            string bundleName = bundle.name;
            var unloadBundleHandle = bundle.UnloadAsync(true);
            unloadBundleHandle.completed += _ => { Log.Info($"Successfully unloaded bundle \"{bundleName}\""); };
        }

        _manuallyLoadedBundles.Clear();

        foreach (var (_, obj) in Assets[typeof(GameObject)])
        {
            if (obj is GameObject gameObject && gameObject.activeSelf)
            {
                Log.Info($"Recycling all instances of prefab \"{gameObject.name}\"");
                gameObject.RecycleAll();
            }
        }
    }

    /// <summary>
    /// Fetch an asset.
    /// </summary>
    /// <param name="assetName">The name of the asset to fetch.</param>
    /// <typeparam name="T">The type of asset to fetch.</typeparam>
    /// <returns>The fetched object if it exists, otherwise returns null.</returns>
    internal static T? Get<T>(string assetName) where T : Object
    {
        Type assetType = typeof(T);
        if (Assets.ContainsKey(assetType))
        {
            var subDict = Assets[assetType];
            if (subDict != null)
            {
                // // 尝试查找包含该名称的资源
                // var matchingKey = subDict.Keys.FirstOrDefault(k =>
                //     k.IndexOf(assetName, StringComparison.OrdinalIgnoreCase) >= 0);

                // if (matchingKey != null)
                // {
                //     return subDict[matchingKey] as T;
                // }
                if (subDict.ContainsKey(assetName)) {
                    var assetObj = subDict[assetName];
                    if (assetObj != null) {
                        return assetObj as T;
                    }

                    Log.Error($"Failed to get asset \"{assetName}\"; asset is null!");
                    return null;;
                }
            }
        }

        Log.Error($"Could not find asset containing '{assetName}' of type '{assetType}'!");
        return null;
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
    internal static IEnumerable<(string name, string type, Object asset)> GetAllAssetDetails()
    {
        var details = new List<(string, string, Object)>();
        foreach (var typeDict in Assets)
        {
            string typeName = typeDict.Key.Name;
            foreach (var assetEntry in typeDict.Value)
            {
                details.Add((assetEntry.Key, typeName, assetEntry.Value));
            }
        }
        return details;
    }
    // 添加场景加载方法

}