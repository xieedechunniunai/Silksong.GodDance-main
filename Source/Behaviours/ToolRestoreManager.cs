using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GodDance.Source.Behaviours
{
    /// <summary>
    /// 持久化道具恢复管理器
    /// 在BOSS存档加载时激活，负责检测玩家重生并恢复道具
    /// </summary>
    internal class ToolRestoreManager : MonoBehaviour
    {
        public static ToolRestoreManager Instance { get; private set; }
        
        // 状态变量
        private bool _isPlayerDead = false;
        private bool _isPlayerRespawned = false;
        private string _currentSceneName = "";
        private bool _isActiveInBossSave = false;
        
        // 重生检测相关
        private float _respawnCheckTimer = 0f;
        private const float RESPAWN_CHECK_INTERVAL = 0.5f;
        private int _respawnCheckCount = 0;
        private const int MAX_RESPAWN_CHECKS = 10;

        private void Awake()
        {
            // 设置单例实例
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
            Log.Info("持久化道具恢复管理器已初始化");
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            _currentSceneName = newScene.name;
            
            // 检测是否进入BOSS场景
            bool isBossScene = newScene.name == "Cog_Dancers" || newScene.name == "Cog_Dancers_boss";
            
            if (isBossScene && Plugin.IsBossSaveLoaded)
            {
                // 进入BOSS场景且BOSS存档已加载
                _isActiveInBossSave = true;
                _isPlayerDead = false;
                _isPlayerRespawned = false;
                _respawnCheckCount = 0;
                Log.Info("道具恢复管理器在BOSS存档中激活");
            }
            else if (!isBossScene || !Plugin.IsBossSaveLoaded)
            {
                // 离开BOSS场景或BOSS存档未加载
                _isActiveInBossSave = false;
                _isPlayerDead = false;
                _isPlayerRespawned = false;
                Log.Info("道具恢复管理器已停用");
            }
        }

        private void Update()
        {
            if (!_isActiveInBossSave) return;
            
            // 定期检查重生状态
            _respawnCheckTimer += Time.deltaTime;
            if (_respawnCheckTimer >= RESPAWN_CHECK_INTERVAL)
            {
                _respawnCheckTimer = 0f;
                CheckPlayerRespawnStatus();
            }
            
            // 检测玩家死亡
            if (!_isPlayerDead && IsPlayerDead())
            {
                _isPlayerDead = true;
                _isPlayerRespawned = false;
                _respawnCheckCount = 0;
                Log.Info("检测到玩家死亡，等待重生...");
            }
        }

        /// <summary>
        /// 检查玩家是否死亡
        /// </summary>
        private bool IsPlayerDead()
        {
            try
            {
                if (HeroController.instance == null) return false;
                return HeroController.instance.cState.dead;
            }
            catch (Exception ex)
            {
                Log.Warn($"检测玩家死亡状态失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查玩家重生状态并恢复道具
        /// </summary>
        private void CheckPlayerRespawnStatus()
        {
            if (!_isPlayerDead || _isPlayerRespawned) return;
            
            // 检查玩家是否已经完全重生
            if (IsPlayerFullyRespawned())
            {
                _isPlayerRespawned = true;
                _isPlayerDead = false;
                _respawnCheckCount = 0;
                
                Log.Info("检测到玩家重生完成，开始恢复道具...");
                RestorePlayerTools();
            }
            else
            {
                _respawnCheckCount++;
                if (_respawnCheckCount >= MAX_RESPAWN_CHECKS)
                {
                    Log.Warn($"重生检测超时({MAX_RESPAWN_CHECKS}次)，强制重置状态");
                    _isPlayerDead = false;
                    _isPlayerRespawned = false;
                    _respawnCheckCount = 0;
                }
            }
        }

        /// <summary>
        /// 检查玩家是否已经完全重生
        /// </summary>
        private bool IsPlayerFullyRespawned()
        {
            try
            {
                if (HeroController.instance == null) return false;
                
                return HeroController.instance.cState.onGround &&
                       !HeroController.instance.cState.dead;
            }
            catch (Exception ex)
            {
                Log.Warn($"检测玩家重生状态失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 恢复玩家道具
        /// </summary>
        private void RestorePlayerTools()
        {
            try
            {
                Log.Info("尝试调用ToolItemManager.TryReplenishTools...");
                
                TryCallToolItemManager();
                
            }
            catch (Exception ex)
            {
                Log.Error($"恢复道具失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试调用ToolItemManager.TryReplenishTools方法
        /// </summary>
        private void TryCallToolItemManager()
        {
            ToolItemManager.TryReplenishTools(true, ToolItemManager.ReplenishMethod.QuickCraft);
            GameManager.instance.playerData.ShellShards = 700;

        }
    }
}