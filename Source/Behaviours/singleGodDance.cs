using System.Collections;
using System;
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
    private int Phase = 1;
    private GameObject aroundAtt;
    private FsmGameObject _createdObjectRef; // 需要初始化

    private bool flag = true;
    private bool flag2 = true;

    private void Awake()
    {

    }
    private void Start()
    {
        // 初始化引用
        _createdObjectRef = new FsmGameObject();

        StartCoroutine(DelayedSetup());
    }
    private void Update()
    {
        if (_parentControl != null)
        {
            Phase = _parentControl.FsmVariables.GetFsmInt("Phase").value;
            if (flag2 && Phase == 2)
            {
                Log.Info("检查到进入P2");
                var move = _control.FsmStates.FirstOrDefault(state => state.Name == "Move Type");
                if (move != null)
                {
                    // 使用 ToList() 创建副本避免循环修改问题
                    var moveActions = move.Actions.ToList();
                    // 使用 Where 过滤掉不需要的动作
                    var filteredActions = moveActions.Where(action =>
                        !(action is ActivateGameObject activateGameObject &&
                          activateGameObject.gameObject.GameObject.Value == _createdObjectRef.Value)
                    ).ToArray();

                    move.Actions = filteredActions;
                    Log.Info("P2,完成取消环绕攻击");
                    flag2 = false;
                }

            }
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
        yield return null;
        GetComponents();
        ModifyFsm();
        newSetCogwork();
    }
    private void GetComponents()
    {
        _control = gameObject.LocateMyFSM("Control");

        // 改进的 aroundAtt 获取逻辑
        if (gameObject.name == "Dancer A")
        {
            // 查找场景中已有的实例
            aroundAtt = gameObject.transform.Find("cog_dancer_blade_sphere(Clone)")?.gameObject;
            if (aroundAtt == null)
            {
                // 如果没找到，从资源加载
                aroundAtt = AssetManager.Get<GameObject>("cog_dancer_blade_sphere");
                Log.Info(aroundAtt.name + "从资源加载");
            }
            else
            {
                Log.Info(aroundAtt.name + "场景实例被找到");
            }
        }
        else
        {
            // 其他 Boss 直接从资源加载
            aroundAtt = AssetManager.Get<GameObject>("cog_dancer_blade_sphere");
            Log.Info(aroundAtt.name + "被其他地方找到");
        }

        var initState = _control.FsmStates.FirstOrDefault(state => state.name == "Init");
        if (initState != null)
        {
            var getParentAction = initState.Actions.OfType<GetParent>().FirstOrDefault();
            if (getParentAction != null)
            {
                var parentGO = getParentAction.storeResult.Value;
                if (parentGO != null)
                {
                    _parentControl = parentGO.GetComponent<PlayMakerFSM>();
                    Log.Info("Parent Control FSM found.");
                }
            }
        }
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
                    accelerateToY.accelerationFactor.value = 0.5f;
                    break;
                }

            }
        }
        var JumpV2State = _control.FsmStates.FirstOrDefault(state => state.name == "Jump v2");
        if (JumpV2State != null)
        {
            var aa = 0;
            foreach (var action in JumpV2State.Actions)
            {
                if (action is AnimatePositionTo animatePositionTo)
                {
                    animatePositionTo.speed.value += 0.3f;
                    animatePositionTo.time = 0.25f;
                    aa += 1;
                }
                if (action is SetRotationDelay setRotationDelay)
                {
                    setRotationDelay.delay.value = 0.15f;
                    aa += 1;
                }
                if (action is FlipScaleDelay flipScaleDelay)
                {
                    flipScaleDelay.delay.value = 0.15f;
                    aa += 1;
                }
                if (aa >= 3) { break; }
            }
        }
    }
    private void newSetCogwork()
    {
        if (aroundAtt != null)
        {
            Log.Info($"准备创建对象: {aroundAtt.name}, 是预制体: {!aroundAtt.scene.IsValid()}");

            // 手动创建对象
            GameObject createdObject = null;
            try
            {
                if (aroundAtt.scene.IsValid())
                {
                    // 如果是场景实例，直接使用（但需要确保是合适的实例）
                    createdObject = aroundAtt;
                    Log.Info("使用现有的场景实例");
                }
                else
                {
                    // 如果是预制体，实例化它
                    createdObject = Instantiate(aroundAtt, transform);
                    createdObject.transform.localPosition = Vector3.zero;
                    createdObject.transform.localRotation = Quaternion.identity;
                    createdObject.SetActive(false); // 初始不激活
                    Log.Info("手动实例化预制体成功");
                }

                _createdObjectRef.Value = createdObject;
                Log.Info($"对象引用设置: {_createdObjectRef.Value != null}");
            }
            catch (Exception e)
            {
                Log.Error($"对象创建失败: {e.Message}");
                return;
            }

            // 修改 FSM 状态，移除创建逻辑，只使用激活逻辑
            ModifyFSMForManualCreation();
        }
    }
    private void ModifyFSMForManualCreation()
    {
        var SetCogwork = _control.FsmStates.FirstOrDefault(state => state.Name == "Set Cogwork");
        if (SetCogwork != null)
        {
            // 移除原有的创建相关动作
            var newActions = SetCogwork.Actions.Where(action =>
                !(action is CreateObject) && !(action is SetParent)
            ).ToList();

            // 添加激活和设置父对象的动作
            newActions.Insert(0, new ActivateGameObject
            {
                gameObject = new FsmOwnerDefault()
                {
                    OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                    GameObject = _createdObjectRef
                },
                activate = new FsmBool() { Value = true },
                recursive = false,
                resetOnExit = false
            });

            newActions.Insert(1, new SetParent
            {
                gameObject = new FsmOwnerDefault()
                {
                    OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                    GameObject = _createdObjectRef
                },
                parent = new FsmGameObject() { Value = gameObject },
                resetLocalPosition = true
            });

            SetCogwork.Actions = newActions.ToArray();
            Log.Info("修改Set Cogwork状态完成");
        }

        // Move Type 状态的修改保持不变
        var move = _control.FsmStates.FirstOrDefault(state => state.Name == "Move Type");
        if (move != null)
        {
            var moveActions = move.Actions.ToList();
            moveActions.Insert(0, new ActivateGameObject
            {
                gameObject = new FsmOwnerDefault()
                {
                    OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                    GameObject = _createdObjectRef
                },
                activate = new FsmBool() { Value = true },
                recursive = false
            });
            move.Actions = moveActions.ToArray();
            Log.Info("修改Move Type状态完成");
        }

    }



    private void OnDestroy()
    {
        // 清理创建的副本对象
        if (aroundAtt != null && aroundAtt.scene.rootCount == 0) // 检查是否为未激活的副本
        {
            Destroy(aroundAtt);
        }
    }
}