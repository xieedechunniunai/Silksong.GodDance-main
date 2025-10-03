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
    private int Phase = 1;                       // 添加Phase变量
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
        if (_parentControl != null)
        {
            Phase = _parentControl.FsmVariables.GetFsmInt("Phase").value;
        }

        if (flag2 && _parentControl != null)
        {
            if (Phase == 2)
            {
                Log.Info("添加新攻击");
                StartCoroutine(NewSpikeAttack());
                flag2 = false;
            }
        }
        if (flag && _control != null)
        {
            if (Phase == 3)
            {
                ModifyDivePhaseFlowForP3();
                flag = false;
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
        // this._spikeMaster = GameObject.Find("Spike Control");
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
        _parentControl.FsmVariables.GetFsmInt("Phase 2 HP").Value += 300;
        _parentControl.FsmVariables.GetFsmInt("Phase 3 HP").Value += 300;
        _parentControl.FsmVariables.GetFsmInt("Phase 4 HP").Value = 84;
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
                    setFsmFloat.setValue = 0.4f;
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
                    setFsmFloat.setValue = 0.36f;
                }
            }
        }
    }

    private IEnumerator NewSpikeAttack()
    {
        Vector3 spawnPosition1 = new Vector3(50, 11, 1); // 设置你想要的坐标
        Vector3 spawnPosition2 = new Vector3(10, 11, 1); // 设置你想要的坐标
        Quaternion spawnRotation = Quaternion.identity; // 设置你想要的旋转
        yield return null;
    }
    private void ModifyDivePhaseFlowForP3()
    {
        try
        {
            Log.Info("P3阶段：修改合体技能第二个技能流程，将BEAT事件跳转改为FINISHED事件跳转");

            // 修改Dive Pattern 2状态：FINISHED事件跳转到Diving 2状态
            var divePattern2State = _parentControl.FsmStates.FirstOrDefault(state => state.Name == "Dive Pattern 2");
            if (divePattern2State != null)
            {
                var actions = divePattern2State.Actions.ToList();
                var target = new FsmEventTarget();
                foreach (var action in actions)
                {
                    if (action is SendEventByName sendEventByName)
                    {
                        target = sendEventByName.eventTarget;
                        break;
                    }
                }
                actions.Add(new SendEventByName
                {
                    eventTarget = target,
                    sendEvent = "ATTACK",
                    delay = 0.3f,
                    everyFrame = false
                });
                divePattern2State.Actions = actions.ToArray();
                // 查找BEAT事件过渡并改为FINISHED事件
                // var beatTransition = divePattern2State.Transitions.FirstOrDefault(t => t.FsmEvent.Name == "BEAT");
                // if (beatTransition != null && beatTransition.toState == "Diving 2")
                // {
                //     beatTransition.FsmEvent = FsmEvent.Finished;
                //     Log.Info("已将Dive Pattern 2状态的BEAT事件跳转改为FINISHED事件跳转到Diving 2");
                // }
            }

            // 修改Diving 2状态：FINISHED事件跳转到Diving 3状态
            var diving2State = _parentControl.FsmStates.FirstOrDefault(state => state.Name == "Diving 2");
            if (diving2State != null)
            {
                // 查找BEAT事件过渡并改为FINISHED事件
                var beatTransition = diving2State.Transitions.FirstOrDefault(t => t.FsmEvent.Name == "BEAT");
                if (beatTransition != null && beatTransition.toState == "Diving 3")
                {
                    beatTransition.FsmEvent = FsmEvent.Finished;
                    Log.Info("已将Diving 2状态的BEAT事件跳转改为FINISHED事件跳转到Diving 3");
                }
            }

            Log.Info("P3阶段合体技能第二个技能流程修改完成：所有BEAT事件跳转已改为FINISHED事件跳转");
        }
        catch (System.Exception ex)
        {
            Log.Error($"修改合体技能流程时出错: {ex.Message}");
        }
    }

}