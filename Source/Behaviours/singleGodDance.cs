using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GenericVariableExtension;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace GodDance.Source.Behaviours;

/// <summary>
/// Modifies the behavior of the First Sinner boss.
/// </summary>
[RequireComponent(typeof(tk2dSpriteAnimator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayMakerFSM))]
internal class singleGodDance : MonoBehaviour
{
    private PlayMakerFSM _control = null!;
    private PlayMakerFSM _parentControl = null!;
    
    private GameObject _spikeMaster = null!;
    private int Phase = 1;

    private bool flag = true;
    private void Awake()
    {

    }
    private void Start()
    {
        // 延迟到Start执行，确保Boss初始化完成
        StartCoroutine(DelayedSetup());
    }
    private void Update()
    {
        if (_parentControl != null)
        {
            Phase = _parentControl.FsmVariables.GetFsmInt("Phase").value;
            if (flag && Phase == 3)
            {
                var dashSpeedVar = _control.FsmVariables?.GetFsmFloat("Dash Speed");
                if (dashSpeedVar != null)
                {
                    dashSpeedVar.Value += 70f;
                }
                var DropDashState = _control.FsmStates.FirstOrDefault(state => state.name == "Stomp");
                if (DropDashState != null)
                {
                    foreach (var action in DropDashState.Actions)
                    {
                        if (action is FloatMultiply floatMultiply)
                        {
                            floatMultiply.multiplyBy = 6f;
                            Log.Info("Modified Stomp Speed完成速度减6倍");
                            break;
                        }
                    }
                }
                var StompState = _control.FsmStates.FirstOrDefault(state => state.name == "Stomp");
                if (StompState != null)
                {
                    foreach (var action in StompState.Actions)
                    {
                        if (action is FloatMultiply floatMultiply)
                        {
                            floatMultiply.multiplyBy = 6f;
                            Log.Info("Modified Stomp Speed完成速度减6倍");
                            break;
                        }

                    }
                }
                flag = false;
            }
        }
    }
    /// <summary>
    /// Set up the modded boss.
    /// </summary>
    /// 
    private IEnumerator DelayedSetup()
    {
        yield return null;  // 等待一帧
        StartCoroutine(SetupBoss());
    }

    private IEnumerator SetupBoss()
    {
        GetComponents();
        ModifyFsm();
        yield return null;
        ModifyParentFsm();
    }
    private void GetComponents()
    {
        _control = gameObject.LocateMyFSM("Control");
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
    private void ModifyFsm()
    {
        // 添加空值检查
        if (_control == null || _control.FsmStates == null)
        {
            Log.Error("Control FSM or its states are null");
            return;
        }
        // 安全地修改变量
        var dashSpeedVar = _control.FsmVariables?.GetFsmFloat("Dash Speed");
        if (dashSpeedVar != null)
        {
            dashSpeedVar.Value += 36f;
        }
        var WindUpState = _control.FsmStates.FirstOrDefault(state => state.name == "First Windup?");
        if (WindUpState != null)
        {
            foreach (var action in WindUpState.Actions)
            {
                if (action is IntCompare intCompare)
                {
                    intCompare.integer2 = 5;
                }
                if (action is SetBoolValue setBoolValue)
                {
                    setBoolValue.boolValue = false;
                    break;
                }

            }
        }
        var WindUp = _control.FsmStates.FirstOrDefault(state => state.name == "WindUp");
        if (WindUp != null)
        {
            _control.SendEvent("WINDUP"); // 发送触发WindUp状态的事件
        }
        var TeleOut = _control.FsmStates.FirstOrDefault(state => state.name == "Tele Out");
        if (TeleOut != null)
        {
            foreach (var action in TeleOut.Actions)
            {
                if (action is FaceObjectV2 faceObjectV2)
                {
                    faceObjectV2.pauseBetweenTurns = 0.05f;
                    break;
                }

            }
        }
        var TeleOut2 = _control.FsmStates.FirstOrDefault(state => state.name == "Tele Out 2");
        if (TeleOut2 != null)
        {
            foreach (var action in TeleOut2.Actions)
            {
                if (action is FaceObjectV2 faceObjectV2)
                {
                    faceObjectV2.pauseBetweenTurns = 0.05f;
                    break;
                }

            }
        }
        var CatchAlone = _control.FsmStates.FirstOrDefault(state => state.name == "Catch Alone");
        if (CatchAlone != null)
        {
            foreach (var action in CatchAlone.Actions)
            {
                if (action is Wait wait)
                {
                    wait.time = 0.2f;
                    break;
                }

            }
        }
        var FloatDown = _control.FsmStates.FirstOrDefault(state => state.name == "Float Down");
        if (FloatDown != null)
        {
            foreach (var action in FloatDown.Actions)
            {
                if (action is AccelerateToY accelerateToY)
                {
                    accelerateToY.targetSpeed.value -= 8f;
                    break;
                }

            }
        }
        // var JumpV2State = _control.FsmStates.FirstOrDefault(state => state.name == "Jump v2");
        // if (JumpV2State != null)
        // {
        //     var aa = 0;
        //     foreach (var action in JumpV2State.Actions)
        //     {
        //         if (action is AnimatePositionTo animatePositionTo)
        //         {
        //             animatePositionTo.speed.value += 0.02f;
        //             animatePositionTo.time = 0.25f;
        //             break;
        //         }
        //         if (action is SetRotationDelay setRotationDelay)
        //         {
        //             setRotationDelay.delay.value = 0.15f;
        //             aa += 1;
        //         }
        //         if (action is FlipScaleDelay flipScaleDelay)
        //         {
        //             flipScaleDelay.delay.value = 0.15f;
        //             aa += 1;
        //         }
        //         if (aa >= 3) { break; }
        //     }
        // }
    }

    private void ModifyParentFsm()
    {
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
    private void IncreaseHealth()
    {
        _parentControl.FsmVariables.GetFsmInt("Phase 1 HP").Value += 100;
        _parentControl.FsmVariables.GetFsmInt("Phase 2 HP").Value += 200;
        _parentControl.FsmVariables.GetFsmInt("Phase 3 HP").Value += 300;
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
                // if (action is SetHP SetHP)
                // {
                //     SetHP.hp.value += 400;
                //     break;
                // }
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
                // if (action is SetHP SetHP)
                // {
                //     SetHP.hp = 700;
                //     break;
                // }
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
}