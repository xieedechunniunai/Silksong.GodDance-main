using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityObject = UnityEngine.Object;
namespace GodDance.Source.Behaviours;

/// <summary>
/// Modifies the behavior of the First Sinner boss.
/// </summary>
[RequireComponent(typeof(tk2dSpriteAnimator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayMakerFSM))]
internal class GodDance : MonoBehaviour
{
    //private const float GroundY = 13;

    private tk2dSpriteAnimator _anim = null!;
    private Rigidbody2D _body = null!;
    private PlayMakerFSM _control = null!;
    private PlayMakerFSM _parentControl = null!;  // 添加父控制器引用
    private Transform _heroTransform = null!;
    private GameObject _spikeMaster = null!;     // 添加spikeMaster引用
    private GameObject _spikeTemplate;
    private GameObject _spikeClone;
    private GameObject _spikeClone2;

    private GameObject _spikeClone3;
    private GameObject _spikeClone4;
    private GameObject _spikeClone5;
    private GameObject spikes = null!;
    private int Phase = 1;                       // 添加Phase变量
    private static bool _assetManagerInitialized = false;
    private bool flag = true;
    private bool flag2 = true;
    private void Awake()
    {
    }

    private void Start()
    {
        StartCoroutine(DelayedSetup());
    }

    private void Update()
    {
        if (flag2 && _parentControl != null)
        {
            Phase = _parentControl.FsmVariables.GetFsmInt("Phase").value;
            if (Phase == 2)
            {
                Log.Info("添加新攻击");
                StartCoroutine(NewSpikeAttack());
                flag2 = false;
            }
        }
    }

    /// <summary>
    /// Set up the modded boss.
    /// </summary>
    private IEnumerator DelayedSetup()
    {
        yield return null;  // 等待一帧
        StartCoroutine(SetupBoss());
    }

    private IEnumerator SetupBoss()
    {
        // 只有第一个实例才加载AssetBundle
        if (!AssetManager.IsInitialized())
        {
            if (!_assetManagerInitialized)
            {
                _assetManagerInitialized = true;
                Log.Info($" 加载资源");
                //yield return AssetManager.ManuallyLoadBundles();
                yield return AssetManager.Initialize();
            }
        }
        else
        {
            // 等待资源初始化完成
            while (!AssetManager.IsInitialized())
            {
                Log.Info($" 等待资源初始化");
                yield return null;
            }
        }

        GetComponents();
        ModifyParentFsm();  // 添加调用父控制器修改方法
        Log.Info($" 总控制修改完成");
        yield return null;
    }
    /// <summary>
    /// Fetch necessary <see cref="Component">components</see> used by this behavior.
    /// </summary>
    private void GetComponents()
    {
        _anim = GetComponent<tk2dSpriteAnimator>();
        _body = GetComponent<Rigidbody2D>();
        _control = FSMUtility.LocateMyFSM(base.gameObject, "Control");
        _heroTransform = HeroController.instance.transform;

        // 添加父控制器和spikeMaster的获取
        var initState = _control.FsmStates.FirstOrDefault(state => state.name == "Init");
        if (initState != null)
        {
            var getParentAction = initState.Actions
                .OfType<GetParent>()
                .FirstOrDefault();

            if (getParentAction != null)
            {
                var parentGO = getParentAction.storeResult.Value;
                if (parentGO != null)
                {
                    _parentControl = parentGO.GetComponent<PlayMakerFSM>();
                    Log.Info("Parent Control FSM found.");
                }
            }
            else
            {
                Log.Info("GetParent action not found in Init state.");
            }
        }
        else
        {
            Log.Error("Init state not found in Control FSM.");
        }
        this._spikeMaster = GameObject.Find("Spike Control");
    }

    /// <summary>
    /// Modify parent FSM behaviors.
    /// </summary>
    private void ModifyParentFsm()
    {
        if (_parentControl == null) return;

        var bindState = _parentControl.FsmStates.FirstOrDefault(state => state.Name == "Pendulum Prepare");
        if (bindState != null)
        {
            foreach (var action in bindState.Actions)
            {
                if (action is Wait wait)
                {
                    wait.time = 0.3f;
                    break;
                }
            }
        }

        ModifyPhase2();
        ModifyPhase3();
        ModifyFinalPhase();
        IncreaseHealth();
    }

    /// <summary>
    /// Raise the boss's <see cref="HealthManager">health</see>.
    /// </summary>
    private void IncreaseHealth()
    {
        _parentControl.FsmVariables.GetFsmInt("Phase 1 HP").Value = 274;
        var InitState = _parentControl.FsmStates.FirstOrDefault(state => state.name == "Init");
        if (InitState != null)
        {
            foreach (var action in InitState.Actions)
            {
                if (action is SetHP setHP)
                {
                    setHP.hp =274;
                }
            }
        }
        _parentControl.FsmVariables.GetFsmInt("Phase 2 HP").Value += 300;
        _parentControl.FsmVariables.GetFsmInt("Phase 3 HP").Value += 300;
        _parentControl.FsmVariables.GetFsmInt("Phase 4 HP").Value = 1314;
        Log.Info("机驱舞者加血成功Health increased.");
    }

    private void ModifyPhase2()
    {
        var p2StartState = _parentControl.FsmStates.FirstOrDefault(state => state.name == "Set Phase 2");
        if (p2StartState != null)
        {
            foreach (var action in p2StartState.Actions)
            {
                if (action is SetFsmFloat setFsmFloat)
                {
                    setFsmFloat.setValue = 0.43f;
                }
            }
        }
    }

    private void ModifyPhase3()
    {
        var p2StartState = _parentControl.FsmStates.FirstOrDefault(state => state.name == "Set Phase 3");
        if (p2StartState != null)
        {
            foreach (var action in p2StartState.Actions)
            {
                if (action is SetFsmFloat setFsmFloat)
                {
                    setFsmFloat.setValue = 0.36f;
                }
            }
        }
    }

    private void ModifyFinalPhase()
    {
        var p2StartState = _parentControl.FsmStates.FirstOrDefault(state => state.name == "Set Final Phase");
        if (p2StartState != null)
        {
            foreach (var action in p2StartState.Actions)
            {
                if (action is SetFsmFloat setFsmFloat)
                {
                    setFsmFloat.setValue = 0.15f;
                }
                if (action is SetHP SetHP)
                {
                    SetHP.hp.value += 100;
                    break;
                }
            }
        }
    }

    private IEnumerator NewSpikeAttack()
    {
        // var allAssets = AssetManager.GetAllAssetDetails();
        // foreach (var (name, type, asset) in allAssets)
        // {
        //     Log.Info($"Asset Name: {name}, Type: {type}, Instance: {asset}");
        // }
        Vector3 spawnPosition1 = new Vector3(50, 11, 1); // 设置你想要的坐标
        Vector3 spawnPosition2 = new Vector3(10, 11, 1); // 设置你想要的坐标
        Quaternion spawnRotation = Quaternion.identity; // 设置你想要的旋转
        // 先尝试从 AssetManager 获取 Spike Collider
        // var spikeColliderPrefab = AssetManager.Get<GameObject>("Spike Collider");
        // if (spikeColliderPrefab != null)
        // {
        //     // 实例化 AssetManager 中的预制体
        //     Vector3 spawnPosition = new Vector3(50, 11, 1);
        //     Instantiate(spikeColliderPrefab, spawnPosition, Quaternion.identity);
        //     Log.Info("从 AssetManager 实例化 Spike Collider 成功");
        // }
        // else
        // {
        //     // 如果 AssetManager 中没有，则从场景中查找并复制
        //     GameObject spikeColliderInstance = GameObject.Find("Spike Collider");
        //     if (spikeColliderInstance != null)
        //     {
        //         // 实例化场景中的对象
        //         Vector3 spawnPosition = spawnPosition1;
        //         Instantiate(spikeColliderInstance, spawnPosition, Quaternion.identity);
        //         Log.Info("从场景中实例化 Spike Collider 成功");
        //     }
        //     else
        //     {
        //         Log.Error("未能在场景中找到 Spike Collider");
        //     }
        // }
        // var spikesPict2 = AssetManager.Get<GameObject>("cog_dancer_blade_sphere");
        // if (spikesPict2 == null)
        // {
        //     Log.Error("未能找到单个刺贴图的gameobject.");
        //     yield break;
        // }
        // else
        // {
        //     Instantiate(spikesPict2, spawnPosition1, spawnRotation);
        //     Instantiate(spikesPict2, spawnPosition2, spawnRotation);
        //     Log.Info("已实例化刺.");
        // }
        _spikeTemplate = AssetManager.Get<GameObject>("cog_dancer_flash_impact");
        if (_spikeTemplate == null)
        {
            Log.Error("未能找到模板刺的gameobject.");
            yield break;
        }
        else
        {
            this._spikeClone = UnityObject.Instantiate<GameObject>(this._spikeTemplate);
            Extensions.SetPositionX(this._spikeClone.transform, 50);
            Extensions.SetPositionY(this._spikeClone.transform, 11);
            this._spikeClone2 = UnityObject.Instantiate<GameObject>(this._spikeTemplate);
            Extensions.SetPositionX(this._spikeClone2.transform, 50.5f);
            Extensions.SetPositionY(this._spikeClone2.transform, 11);
            this._spikeClone3 = UnityObject.Instantiate<GameObject>(this._spikeTemplate);
            Extensions.SetPositionX(this._spikeClone3.transform, 51f);
            Extensions.SetPositionY(this._spikeClone3.transform, 11);
            this._spikeClone4 = UnityObject.Instantiate<GameObject>(this._spikeTemplate);
            Extensions.SetPositionX(this._spikeClone4.transform, 51.5f);
            Extensions.SetPositionY(this._spikeClone4.transform, 11);
            this._spikeClone5 = UnityObject.Instantiate<GameObject>(this._spikeTemplate);
            Extensions.SetPositionX(this._spikeClone5.transform, 52f);
            Extensions.SetPositionY(this._spikeClone5.transform, 11);
            Instantiate(this._spikeClone, spawnPosition1, spawnRotation);
            Instantiate(this._spikeClone2, spawnPosition1, spawnRotation);
            Instantiate(this._spikeClone3, spawnPosition1, spawnRotation);
            Instantiate(this._spikeClone4, spawnPosition1, spawnRotation);
            Instantiate(this._spikeClone5, spawnPosition1, spawnRotation);
            Log.Info("已实例化一堆刺.");
        }
        // var SpikeDamage = AssetManager.Get<GameObject>("Spike Collider");
        // if (SpikeDamage == null)
        // {
        //     Log.Error("未能找到伤害刺的gameobject Spike Damage Prefab not found.");
        //     yield break;
        // }
        // else
        // {
        //     // 实例化游戏对象并设置位置
        //     Instantiate(SpikeDamage, spawnPosition1, spawnRotation);
        //     Instantiate(SpikeDamage, spawnPosition2, spawnRotation);
        //     Log.Info("已生成刺伤害.");
        // }
        // var spikesWallPict = AssetManager.Get<GameObject>("wall");
        // if (spikesWallPict == null)
        // {
        //     Log.Error("未能找到单个刺贴图墙的gameobject.");
        //     yield break;
        // }
        // else
        // {
        //     Instantiate(spikesWallPict, spawnPosition1, spawnRotation);
        //     Instantiate(spikesWallPict, spawnPosition2, spawnRotation);
        //     Log.Info("已生成刺贴图墙.");
        // }
        yield return null;
    }
}