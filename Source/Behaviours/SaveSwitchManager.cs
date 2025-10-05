using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using TeamCherry.SharedUtils;
using System.Linq;
using HarmonyLib;
using BepInEx;
using GodDance.Source.Patches;
using GodDance.Source.Behaviours;
using UnityEngine.EventSystems; // 新增：用于事件系统支持
using UnityEngine.UI;

namespace GodDance.Source.Behaviours
{
    /// <summary>
    /// 持久化存档切换管理器
    /// 在游戏启动时创建，不可销毁，负责监听D键和切换存档
    /// </summary>
    internal class SaveSwitchManager : MonoBehaviour
    {
        public static SaveSwitchManager Instance { get; private set; }
        private const string BOSS_SAVE_FILE = "GodDance.dat";
        private const string TARGET_SCENE = "Cog_Dancers";
        private const string RESPAWN_MARKER_NAME = "BossRetryRespawnMarker_CogDancers";
        private const float TARGET_POS_X = 39.75f;
        private const float TARGET_POS_Y = 4.6f;
        private const float TARGET_POS_Z = 0.0f;
        // 当前选择的存档槽编号
        private int _currentSelectedSlot = 1;
        // 音频检测相关字段
        private AudioSource _needolinAudioSource;
        private float _audioPlayingTimer = 0f;
        private const float REQUIRED_PLAYING_TIME = 3f; // 需要连续播放3秒
        private bool _isCheckingAudio = false;
        private bool _hasSwitchedThisSession = false; // 防止重复切换

        private void Awake()
        {// 设置单例实例
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            // 监听场景切换
            SceneManager.activeSceneChanged += OnSceneChanged;
            Log.Info("持久化存档切换管理器已初始化");
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            // 更新是否在Cog_Dancers房间的状态
            Plugin.IsInCogDancersRoom = newScene.name == "Cog_Dancers" || newScene.name == "Cog_Dancers_boss";

            if (Plugin.IsInCogDancersRoom)
            {// 重置音频检测状态
                _hasSwitchedThisSession = false;
                _isCheckingAudio = false;
                _audioPlayingTimer = 0f;
                Log.Info($"进入机枢舞者房间: {newScene.name}");

                // 启动音频检测设置
                StartCoroutine(SetupAudioDetection());
            }
            else
            {
                // 如果离开BOSS场景且BOSS存档已加载，恢复原存档
                if ((oldScene.name == "Cog_Dancers" || oldScene.name == "Cog_Dancers_boss") &&
                    Plugin.IsBossSaveLoaded)
                {
                    Log.Info($"离开BOSS场景，开始恢复原存档");
                    StartCoroutine(RestoreOriginalSaveOnSceneChange());
                    // 重新加载存档
                    StartCoroutine(LoadSaveGame());
                }
                // 如果切换到主菜单场景且BOSS存档已加载，恢复原存档
                if (newScene.name == "Menu_Title" && Plugin.IsBossSaveLoaded)
                {
                    Log.Info($"切换到主菜单，开始恢复原存档");
                    StartCoroutine(RestoreOriginalSaveOnSceneChange());
                }
            }

            // 新增：当进入主菜单时，尝试查找并设置存档槽监听器
            if (newScene.name == "Menu_Title")
            {
                Log.Info("进入主菜单，开始查找存档槽UI组件");
                StartCoroutine(SetupSaveSlotListeners());
            }
        }

        private void Update()
        {
            // 检查是否在Cog_Dancers房间且按下D键
            if (Plugin.IsInCogDancersRoom && _isCheckingAudio && !_hasSwitchedThisSession)
            {
                // Log.Info("检测到D键按下，开始切换存档");
                // StartCoroutine(SwitchSaveFile());
                CheckAudioPlayingStatus();
            }
        }
        /// <summary>
        /// 设置音频检测
        /// </summary>
        private IEnumerator SetupAudioDetection()
        {
            yield return new WaitForSeconds(1f); // 等待场景加载完成

            try
            {
                Log.Info("开始设置音频检测...");

                // 获取HeroController实例
                if (HeroController.instance == null)
                {
                    Log.Warn("HeroController.instance为null");
                    yield break;
                }

                GameObject heroObject = HeroController.instance.gameObject;
                Log.Info($"找到Hero对象: {heroObject.name}");

                // 查找Sounds子组件
                Transform soundsTransform = heroObject.transform.Find("Sounds");
                if (soundsTransform == null)
                {
                    Log.Warn("未找到Sounds子组件");
                    yield break;
                }

                Log.Info($"找到Sounds组件: {soundsTransform.name}");

                // 查找Needolin Memory子组件
                Transform needolinTransform = soundsTransform.Find("Needolin Memory");
                if (needolinTransform == null)
                {
                    Log.Warn("未找到Needolin Memory子组件");
                    yield break;
                }

                Log.Info($"找到Needolin Memory组件: {needolinTransform.name}");

                // 获取AudioSource组件
                _needolinAudioSource = needolinTransform.GetComponent<AudioSource>();
                if (_needolinAudioSource == null)
                {
                    Log.Warn("未找到AudioSource组件");
                    yield break;
                }

                Log.Info($"成功获取AudioSource组件，准备开始音频检测");
                _isCheckingAudio = true;
                _audioPlayingTimer = 0f;
            }
            catch (Exception ex)
            {
                Log.Error($"设置音频检测失败: {ex.Message}");
                _isCheckingAudio = false;
            }
        }
        /// <summary>
        /// 检查音频播放状态
        /// </summary>
        private void CheckAudioPlayingStatus()
        {
            if (_needolinAudioSource == null)
            {
                Log.Warn("AudioSource为null，重新设置音频检测");
                StartCoroutine(SetupAudioDetection());
                return;
            }

            // 检查音频是否正在播放
            if (_needolinAudioSource.isPlaying)
            {
                _audioPlayingTimer += Time.deltaTime;
                Log.Info($"音频正在播放，计时器: {_audioPlayingTimer:F2}秒");

                // 如果连续播放时间达到要求
                if (_audioPlayingTimer >= REQUIRED_PLAYING_TIME)
                {
                    Log.Info($"音频连续播放{REQUIRED_PLAYING_TIME}秒，开始切换存档");
                    _hasSwitchedThisSession = true;
                    _isCheckingAudio = false;
                    StartCoroutine(SwitchSaveFile());
                }
            }
            else
            {
                // 音频停止播放，重置计时器
                if (_audioPlayingTimer > 0f)
                {
                    Log.Info($"音频停止播放，重置计时器");
                    _audioPlayingTimer = 0f;
                }
            }
        }
        /// <summary>
        /// 设置存档槽监听器
        /// </summary>
        private IEnumerator SetupSaveSlotListeners()
        {
            yield return new WaitForSeconds(0.5f); // 等待UI加载完成

            try
            {
                Log.Info("开始查找存档槽UI组件...");

                // 查找UIManager
                GameObject uiManagerObj = GameObject.Find("_UIManager");
                if (uiManagerObj == null)
                {
                    Log.Warn("未找到_UIManager对象");
                    yield break;
                }

                Log.Info($"找到_UIManager: {uiManagerObj.name}");

                // 查找UICanvas
                Transform uiCanvas = uiManagerObj.transform.Find("UICanvas");
                if (uiCanvas == null)
                {
                    Log.Warn("未找到UICanvas子对象");
                    yield break;
                }

                Log.Info($"找到UICanvas: {uiCanvas.name}");

                // 查找SaveProfileScreen
                Transform saveProfileScreen = uiCanvas.Find("SaveProfileScreen");
                if (saveProfileScreen == null)
                {
                    Log.Warn("未找到SaveProfileScreen对象");
                    yield break;
                }

                Log.Info($"找到SaveProfileScreen: {saveProfileScreen.name}");

                // 查找Content
                Transform content = saveProfileScreen.Find("Content");
                if (content == null)
                {
                    Log.Warn("未找到Content对象");
                    yield break;
                }

                Log.Info($"找到Content: {content.name}");

                // 查找SaveSlots
                Transform saveSlots = content.Find("SaveSlots");
                if (saveSlots == null)
                {
                    Log.Warn("未找到SaveSlots对象");
                    yield break;
                }

                Log.Info($"找到SaveSlots: {saveSlots.name}");

                // 查找所有存档槽
                string[] slotNames = { "SlotOne", "SlotTwo", "SlotThree", "SlotFour" };
                int slotsFound = 0;

                for (int i = 0; i < slotNames.Length; i++)
                {
                    Transform slot = saveSlots.Find(slotNames[i]);
                    if (slot != null)
                    {
                        Log.Info($"找到存档槽: {slotNames[i]}");

                        // 检查是否已经有SaveSlotButton组件
                        Selectable existingSelectable = slot.GetComponent<Selectable>();
                        if (existingSelectable != null)
                        {
                            Log.Info($"存档槽 {slotNames[i]} 已有Selectable组件: {existingSelectable.GetType().Name}");
                        }

                        // 添加或获取存档槽监听器组件
                        SaveSlotSelector selector = slot.GetComponent<SaveSlotSelector>();
                        if (selector == null)
                        {
                            selector = slot.gameObject.AddComponent<SaveSlotSelector>();
                        }

                        // 设置存档槽编号
                        selector.SetSlotNumber(i + 1);
                        slotsFound++;
                    }
                    else
                    {
                        Log.Warn($"未找到存档槽: {slotNames[i]}");
                    }
                }

                Log.Info($"成功设置 {slotsFound} 个存档槽监听器");
            }
            catch (Exception ex)
            {
                Log.Error($"设置存档槽监听器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 当存档槽被选中时调用
        /// </summary>
        public void OnSlotSelected(int slotNumber)
        {
            Log.Info($"存档管理器收到存档槽选择通知: {slotNumber}");

            // 更新当前选择的存档槽
            _currentSelectedSlot = slotNumber;

            // 更新当前存档文件名
            string saveFileName = $"user{slotNumber}.dat";
            Plugin.CurrentSaveFileName = saveFileName;

            Log.Info($"当前存档文件已更新为: {saveFileName}");
        }

        /// <summary>
        /// 获取当前选择的存档槽编号
        /// </summary>
        public int GetCurrentSelectedSlot()
        {
            return _currentSelectedSlot;
        }
        private IEnumerator SwitchSaveFile()
        {
            string userSavePath = GetUserSaveFilePath();
            if (string.IsNullOrEmpty(userSavePath))
            {
                Log.Error("无法获取用户存档路径");
                yield break;
            }

            // 检查当前存档是否是GodDance存档
            bool isCurrentlyGodDanceSave = IsGodDanceSave(userSavePath);

            Log.Info($"当前存档状态检测 - 是否是GodDance存档: {isCurrentlyGodDanceSave}");
            // 在切换存档前禁用用户操作0.5秒
            yield return DisablePlayerInput();
            if (isCurrentlyGodDanceSave)
            {
                // 如果当前是GodDance存档，切换回原存档
                Log.Info("切换回原存档");
                yield return SwitchToOriginalSave();
            }
            else
            {
                // 如果当前是原存档，切换到GodDance存档
                Log.Info("切换到GodDance存档");
                yield return SwitchToGodDanceSave();
            }
            // 重新启用用户操作
            yield return EnablePlayerInput();
        }
        private bool IsGodDanceSave(string savePath)
        {
            try
            {
                if (!File.Exists(savePath))
                {
                    Log.Warn("存档文件不存在，默认为原存档");
                    return false;
                }
                // 通过检查备份文件是否存在来判断 如果备份存在，说明当前是GodDance存档
                string backupPath = savePath + ".backup";
                if (Plugin.IsBossSaveLoaded && File.Exists(backupPath))
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"判断存档类型失败: {ex.Message}");
                return false;
            }
        }

        private IEnumerator SwitchToGodDanceSave()
        {
            Log.Info("开始切换到GodDance存档");

            // 获取GodDance存档文件路径
            string godDanceSavePath = GetBossSaveFilePath();
            if (string.IsNullOrEmpty(godDanceSavePath) || !File.Exists(godDanceSavePath))
            {
                Log.Error($"GodDance存档文件不存在: {godDanceSavePath}");
                yield break;
            }

            // 获取当前用户存档路径
            string userSavePath = GetUserSaveFilePath();
            if (string.IsNullOrEmpty(userSavePath))
            {
                Log.Error("无法获取用户存档路径");
                yield break;
            }

            Log.Info($"GodDance存档路径: {godDanceSavePath}");
            Log.Info($"用户存档路径: {userSavePath}");
            // 备份原存档
            string backupPath = userSavePath + ".backup";
            try
            {
                if (File.Exists(userSavePath))
                {
                    File.Copy(userSavePath, backupPath, true);
                    Log.Info($"用户原存档已备份到: {backupPath}");
                }
                else
                {
                    Log.Warn("原存档文件不存在，跳过备份");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"备份原存档失败: {ex.Message}");
                // 备份失败不影响主要切换逻辑，继续执行
            }
            // 直接替换存档文件（原存档会被覆盖）
            File.Copy(godDanceSavePath, userSavePath, true);
            Log.Info($"GodDance存档已复制到用户存档位置");
            // 创建重生标记
            CreateRespawnMarker();

            // 加载存档
            yield return LoadSaveGame();

            // 设置状态
            Plugin.IsBossSaveLoaded = true;
            Log.Info("GodDance存档加载完成，当前状态：GodDance存档已激活");
        }

        private IEnumerator SwitchToOriginalSave()
        {
            Log.Info("开始切换回原存档");

            // 获取当前用户存档路径
            string userSavePath = GetUserSaveFilePath();
            if (string.IsNullOrEmpty(userSavePath))
            {
                Log.Error("无法获取用户存档路径");
                yield break;
            }
            // 检查是否存在备份文件
            string backupPath = userSavePath + ".backup";
            if (File.Exists(backupPath))
            {
                Log.Info($"检测到备份文件存在: {backupPath}");
                // 从备份恢复原存档
                File.Copy(backupPath, userSavePath, true);
                Log.Info("原存档已从备份恢复");

            }
            else
            {
                Log.Warn("未找到备份文件，无法恢复原存档");
                yield break;
            }
            // 加载存档（会触发游戏重新加载场景）
            yield return LoadSaveGame();

            // 设置状态
            Plugin.IsBossSaveLoaded = false;
            Log.Info("原存档恢复完成，当前状态：原存档已激活");
        }
        // 以下方法从BossRetryManager复制，保持原有功能
        private string GetBossSaveFilePath()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = $"GodDance.Assets.{BOSS_SAVE_FILE}";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Log.Error($"找不到嵌入资源: {resourceName}");
                        string[] allResources = assembly.GetManifestResourceNames();
                        Log.Info($"可用嵌入资源: {string.Join(", ", allResources)}");
                        return null;
                    }

                    string tempPath = Path.GetTempFileName();
                    using (FileStream fileStream = File.Create(tempPath))
                    {
                        stream.CopyTo(fileStream);
                    }

                    Log.Info($"从嵌入资源创建临时文件: {tempPath}");
                    return tempPath;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"获取BOSS存档路径失败: {ex.Message}");
                return null;
            }
        }
        private string GetUserSaveFilePath()
        {
            try
            {
                Type platformType = Platform.Current.GetType();
                FieldInfo onlineSubsystemField = platformType.GetField("onlineSubsystem", BindingFlags.Instance | BindingFlags.NonPublic);
                string saveSlot = "user1.dat";

                // 如果静态变量中有当前存档文件名，优先使用它
                if (!string.IsNullOrEmpty(Plugin.CurrentSaveFileName))
                {
                    saveSlot = Plugin.CurrentSaveFileName;
                    Log.Info($"使用存档槽选择确定的存档文件: {saveSlot}");
                }
                else
                {
                    // 如果没有通过存档槽选择确定，使用默认存档
                    Log.Info("未检测到存档槽选择，使用默认存档user1.dat");
                }
                if (onlineSubsystemField != null)
                {
                    var onlineSubsystem = onlineSubsystemField.GetValue(Platform.Current) as DesktopOnlineSubsystem;
                    if (onlineSubsystem != null)
                    {
                        string userSavePath = Path.Combine(Application.persistentDataPath, onlineSubsystem.UserId, saveSlot);
                        Log.Info($"用户存档路径(在线模式): {userSavePath}");
                        return userSavePath;
                    }
                }

                // 离线模式
                string offlineSavePath = Path.Combine(Application.persistentDataPath, "default", saveSlot);
                Log.Info($"用户存档路径(离线模式): {offlineSavePath}");
                return offlineSavePath;
            }
            catch (Exception ex)
            {
                Log.Error($"获取用户存档路径失败: {ex.Message}");
                return null;
            }
        }

        // 移除 DetectCurrentSaveSlot 和 DetectCurrentSaveByMultipleMethods 方法
        // 不再需要基于文件修改时间的检测逻辑

        private void CreateRespawnMarker()
        {
            try
            {
                GameObject respawnMarker = GameObject.Find(RESPAWN_MARKER_NAME);

                if (respawnMarker == null)
                {
                    respawnMarker = new GameObject(RESPAWN_MARKER_NAME);
                    UnityEngine.Object.DontDestroyOnLoad(respawnMarker);
                    Log.Info($"创建新的重生标记: {RESPAWN_MARKER_NAME}");
                }

                Vector3 respawnPosition = new Vector3(TARGET_POS_X, TARGET_POS_Y, TARGET_POS_Z);
                respawnMarker.transform.position = respawnPosition;
                respawnMarker.transform.rotation = Quaternion.identity;
                respawnMarker.transform.localScale = Vector3.one;

                RespawnMarker markerComponent = respawnMarker.GetComponent<RespawnMarker>();
                if (markerComponent == null)
                {
                    markerComponent = respawnMarker.AddComponent<RespawnMarker>();
                }

                markerComponent.respawnFacingRight = true;
                markerComponent.customWakeUp = false;
                markerComponent.customFadeDuration = new OverrideFloat
                {
                    IsEnabled = false,
                    Value = 0f
                };
                markerComponent.overrideMapZone = new OverrideMapZone
                {
                    IsEnabled = false,
                    Value = 0
                };

                SceneTeleportMap.AddRespawnPoint(TARGET_SCENE, RESPAWN_MARKER_NAME);
                Log.Info($"重生标记已设置到位置: {respawnPosition}");
            }
            catch (Exception ex)
            {
                Log.Error($"创建重生标记失败: {ex.Message}");
            }
        }

        private IEnumerator LoadSaveGame()
        {
            yield return new WaitForSeconds(0.1f);
            yield return null;

            try
            {
                Log.Info("开始加载存档...");
                // 使用存档槽选择确定的槽位
                int currentSaveSlot = _currentSelectedSlot;
                Log.Info($"使用存档槽选择确定的槽位: {currentSaveSlot}");
                // 使用PreloadOperation来获取存档数据，使用正确的槽位
                PreloadOperation preloadOperation = new PreloadOperation(currentSaveSlot, GameManager.instance);

                preloadOperation.WaitForComplete(delegate (PreloadOperation.PreloadState state)
                {
                    Log.Info("预加载完成，开始配置存档数据");

                    if (preloadOperation.SaveStats != null && preloadOperation.SaveStats.saveGameData != null)
                    {
                        SaveGameData saveData = preloadOperation.SaveStats.saveGameData;

                        if (saveData != null && saveData.playerData != null)
                        {
                            // 设置重生信息
                            saveData.playerData.respawnType = 0;
                            saveData.playerData.respawnScene = TARGET_SCENE;
                            saveData.playerData.respawnMarkerName = RESPAWN_MARKER_NAME;

                            // 保留当前玩家的部分能力
                            PlayerData currentPlayerData = GameManager.instance.playerData;
                            if (currentPlayerData != null)
                            {
                                // 保留关键能力
                                saveData.playerData.UnlockedExtraBlueSlot = currentPlayerData.UnlockedExtraBlueSlot;
                                saveData.playerData.UnlockedExtraYellowSlot = currentPlayerData.UnlockedExtraYellowSlot;
                                saveData.playerData.hasDash = currentPlayerData.hasDash || saveData.playerData.hasDash;
                                saveData.playerData.hasWalljump = currentPlayerData.hasWalljump || saveData.playerData.hasWalljump;
                                saveData.playerData.hasDoubleJump = currentPlayerData.hasDoubleJump || saveData.playerData.hasDoubleJump;
                                saveData.playerData.maxHealth = currentPlayerData.maxHealth;
                                saveData.playerData.health = currentPlayerData.maxHealth;
                                saveData.playerData.silkMax = currentPlayerData.silkMax;
                                saveData.playerData.silkRegenMax = currentPlayerData.silkRegenMax;
                                saveData.playerData.silk = currentPlayerData.silkMax;
                                saveData.playerData.nailUpgrades = currentPlayerData.nailUpgrades;
                                saveData.playerData.geo = currentPlayerData.geo;
                                saveData.playerData.maxHealthBase = currentPlayerData.maxHealthBase;
                                saveData.playerData.healthBlue = currentPlayerData.healthBlue;
                                saveData.playerData.IsSilkSpoolBroken = false;
                                saveData.playerData.HasSeenNeedolin = true;
                                saveData.playerData.HasSeenNeedolinDown = true;
                                saveData.playerData.HasSeenNeedolinUp = true;
                                saveData.playerData.hasNeedolin = true;
                                saveData.playerData.hasNeedolinMemoryPowerup = true;
                                saveData.playerData.hasSilkBossNeedle = true;
                                saveData.playerData.ToolPouchUpgrades = currentPlayerData.ToolPouchUpgrades;
                                saveData.playerData.ToolKitUpgrades = currentPlayerData.ToolKitUpgrades;
                                saveData.playerData.CurrentCrestID = currentPlayerData.CurrentCrestID;
                                saveData.playerData.PreviousCrestID = currentPlayerData.PreviousCrestID;
                                if (currentPlayerData.Tools != null)
                                {
                                    saveData.playerData.Tools = currentPlayerData.Tools;
                                }
                                if (currentPlayerData.ToolLiquids != null)
                                {
                                    saveData.playerData.ToolLiquids = currentPlayerData.ToolLiquids;
                                }
                                // 保留工具装备
                                if (currentPlayerData.ToolEquips != null)
                                {
                                    saveData.playerData.ToolEquips = currentPlayerData.ToolEquips;
                                }
                                if (currentPlayerData.ExtraToolEquips != null)
                                {
                                    saveData.playerData.ExtraToolEquips = currentPlayerData.ExtraToolEquips;
                                }

                            }

                            Log.Info("玩家数据配置完成");
                        }
                    }

                    // 创建重生标记
                    CreateRespawnMarker();

                    // 加载存档
                    if (UIManager.instance != null)
                    {
                        try
                        {
                            UIManager.instance.UIContinueGame(currentSaveSlot, preloadOperation.SaveStats?.saveGameData);
                            Log.Info("存档加载完成");
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"调用UIContinueGame失败: {ex.Message}");
                            TryLoadGameDirectly(preloadOperation.SaveStats?.saveGameData);
                        }
                    }
                    else
                    {
                        Log.Error("UIManager.instance为空，尝试直接加载存档");
                        TryLoadGameDirectly(preloadOperation.SaveStats?.saveGameData);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error($"加载存档失败: {ex.Message}");
            }
            yield return null;
        }
        // 不再需要基于文件名的槽位检测逻辑

        private void TryLoadGameDirectly(SaveGameData saveData)
        {
            try
            {
                if (GameManager.instance != null && saveData != null)
                {
                    Log.Info("尝试通过GameManager直接加载存档");
                    GameManager.instance.LoadGameFromUI(_currentSelectedSlot, saveData); // 使用存档槽选择确定的槽位
                    Log.Info("直接加载存档完成");
                }
                else
                {
                    Log.Error("GameManager或存档数据为空，无法直接加载");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"直接加载存档失败: {ex.Message}");
            }
        }

        // 移除 UpdateCurrentSaveFileName 方法
        // 不再需要基于文件修改时间的存档检测

        private IEnumerator RestoreOriginalSaveOnSceneChange()
        {
            Log.Info("开始场景切换时的存档恢复");

            // 获取当前用户存档路径
            string userSavePath = GetUserSaveFilePath();
            if (string.IsNullOrEmpty(userSavePath))
            {
                Log.Error("无法获取用户存档路径");
                yield break;
            }

            // 检查是否存在备份文件
            string backupPath = userSavePath + ".backup";
            if (File.Exists(backupPath))
            {
                Log.Info($"检测到备份文件存在: {backupPath}");
                // 从备份恢复原存档
                File.Copy(backupPath, userSavePath, true);
                Log.Info("原存档已从备份恢复");



                // 删除备份文件
                try
                {
                    File.Delete(backupPath);
                    Log.Info($"备份文件已删除: {backupPath}");
                }
                catch (Exception ex)
                {
                    Log.Error($"删除备份文件失败: {ex.Message}");
                }
            }
            else
            {
                Log.Warn("未找到备份文件，跳过恢复");
            }

            Plugin.IsBossSaveLoaded = false;
            Log.Info("场景切换时存档恢复完成");
        }
                /// <summary>
        /// 禁用玩家输入和操作
        /// </summary>
        private IEnumerator DisablePlayerInput()
        {
            Log.Info("开始禁用玩家操作...");

            try
            {
                // 获取HeroController实例
                if (HeroController.instance != null)
                {
                     // 方法1：直接设置玩家状态为不可控制
                    HeroController.instance.cState.needolinPlayingMemory = false;
                    
                    // 禁用玩家输入
                    HeroController.instance.StopAnimationControl();
                    HeroController.instance.RelinquishControl();

                    Log.Info("玩家操作已禁用");
                }
                else
                {
                    Log.Warn("HeroController.instance为null，无法禁用玩家操作");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"禁用玩家操作失败: {ex.Message}");
            }

            // 等待0.5秒确保操作完全禁用
            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// 重新启用玩家输入和操作
        /// </summary>
        private IEnumerator EnablePlayerInput()
        {
            Log.Info("开始重新启用玩家操作...");

            try
            {
                // 获取HeroController实例
                if (HeroController.instance != null)
                {
                    // HeroControllerConfig heroControllerConfig = new HeroControllerConfig();
                    // // 设置HeroControllerConfig的canPlayNeedolin属性为true
                    // heroControllerConfig.canPlayNeedolin = true;
                    bool hasNeedolin = HeroController.instance.HasNeedolin();
                    
                    // 重新启用玩家输入
                    HeroController.instance.RegainControl();
                    HeroController.instance.StartAnimationControl();

                    Log.Info("玩家操作已重新启用");
                }
                else
                {
                    Log.Warn("HeroController.instance为null，无法启用玩家操作");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"启用玩家操作失败: {ex.Message}");
            }

            yield return null;
        }
    }

    // 存档槽选择监听器
    /// <summary>
    /// 存档槽选择监听器
    /// 监听用户在存档选择界面选择的存档槽
    /// </summary>
    public class SaveSlotSelector : MonoBehaviour, IPointerClickHandler
    {
        public int slotNumber = 1;
        private Selectable _selectable;

        private void Start()
        {
            _selectable = GetComponent<Selectable>();
            if (_selectable == null)
            {
                Log.Warn($"存档槽 {slotNumber} 没有找到Selectable组件");
                return;
            }

            Log.Info($"存档槽 {slotNumber} 已初始化点击监听器");
        }

        // 实现 IPointerClickHandler 接口方法
        public void OnPointerClick(PointerEventData eventData)
        {
            // 额外安全检查
            if (eventData.button != PointerEventData.InputButton.Left) return;

            Log.Info($"点击了存档槽: {slotNumber}");
            SaveSwitchManager.Instance?.OnSlotSelected(slotNumber);
        }

        public void SetSlotNumber(int number) => slotNumber = number;

        /// <summary>
        /// 当存档槽被点击时调用
        /// </summary>
        private void OnSlotClicked()
        {
            Log.Info($"点击了存档槽: {slotNumber}");

            // 通知存档管理器当前选择的存档槽
            SaveSwitchManager.Instance?.OnSlotSelected(slotNumber);
        }

        private void OnDestroy()
        {
            // 清理事件监听
            if (_selectable is Button button)
            {
                button.onClick.RemoveListener(OnSlotClicked);
            }
        }

    }

}