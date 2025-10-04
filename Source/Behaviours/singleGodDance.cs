using System.Collections;
using System;
using System.Linq;
using GenericVariableExtension;
using GlobalEnums;
using System.Reflection;
using System.Collections.Generic;
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
    private static GameObject _cachedBladeSphere;

    private bool flag = true;
    private bool flag2 = true;
    private bool flag3 = true;
    // 新增：定时切换隐身状态
    private bool _isStealthActive = false;
    private float _stealthTimer = 0f;
    private const float STEALTH_TOGGLE_INTERVAL = 5f; // 5秒切换一次

    private void Awake()
    {

    }
    private void Start()
    {
        // 初始化引用
        _createdObjectRef = new FsmGameObject();

        StartCoroutine(SetupBoss());
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
                    // 在P2阶段修改攻击GameObject的属性
                    ModifyNewBladeSphereForP2();
                    flag2 = false;
                }

            }
            if (flag && Phase == 3)
            {
                var dashSpeedVar = _control.FsmVariables?.GetFsmFloat("Dash Speed");
                if (dashSpeedVar != null)
                {
                    dashSpeedVar.Value += 40f;
                }
                var DropDashState = _control.FsmStates.FirstOrDefault(state => state.name == "Stomp");
                if (DropDashState != null)
                {
                    foreach (var action in DropDashState.Actions)
                    {
                        if (action is FloatMultiply floatMultiply)
                        {
                            floatMultiply.multiplyBy = 6f;
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
                            break;
                        }

                    }
                }
                RestoreOriginalComboFlow();
                // P3阶段：隐身
                SetupSimpleStealthToggle();
                flag = false;
            }
            if (Phase == 3)
            {
                HandleStealthToggle();
            }
            if (Phase == 4 && flag3)
            {
                var meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.enabled = false;
                    Log.Info("P4初始状态：隐身");
                }
                flag3 = false;
            }
        }
    }
    /// <summary>
    /// Set up the modded boss.
    /// </summary>
    /// 

    private IEnumerator SetupBoss()
    {
        yield return null;
        // 确保 AssetManager 已初始化
        if (!AssetManager.IsInitialized())
        {
            yield return AssetManager.Initialize();
        }
        GetComponents();
        // 使用持久化加载方案
        yield return LoadAssetWithPersistence();

        if (aroundAtt == null)
        {
            Log.Error("资源加载完全失败！");
            yield break;
        }
        ModifyFsm();
        newSetCogwork();
    }
    /// <summary>
    /// 获取当前游戏对象的相关组件和父级控制FSM
    /// 该方法负责初始化控制FSM并查找父级控制器的引用
    /// </summary>
    private void GetComponents()
    {
        _control = gameObject.LocateMyFSM("Control");
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
    private IEnumerator LoadAssetWithPersistence()
    {
        // 如果静态缓存存在且有效，直接使用
        if (_cachedBladeSphere != null)
        {
            aroundAtt = _cachedBladeSphere;
            Log.Info("使用静态缓存的资源");
            yield break;
        }
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
        else if (gameObject.name == "Dancer B" && _parentControl != null)
        {
            Transform parentTransform = _parentControl.gameObject.transform;
            Transform dancerATransform = parentTransform.Find("Dancer A");
            // 其他 Boss 直接从资源加载
            if (dancerATransform != null)
            {
                // 查找Dancer A上的singleGodDance组件
                singleGodDance dancerAComponent = dancerATransform.GetComponent<singleGodDance>();

                // 如果找到了并且已经有加载好的资源
                if (dancerAComponent != null && dancerAComponent.aroundAtt != null)
                {
                    aroundAtt = dancerAComponent.aroundAtt;
                    Log.Info("从Dancer A组件获取到资源引用");
                    _cachedBladeSphere = aroundAtt; // 缓存资源
                    yield break;
                }
                else
                {

                    aroundAtt = AssetManager.Get<GameObject>("cog_dancer_blade_sphere");
                    Log.Info(aroundAtt.name + "从资源加载");
                    _cachedBladeSphere = aroundAtt; // 缓存资源
                    if (aroundAtt == null)
                    {
                        aroundAtt = dancerATransform.Find("cog_dancer_blade_sphere(Clone)")?.gameObject;
                        if (aroundAtt != null)
                            Log.Info(aroundAtt.name + "从Dancer A组件的场景实例被找到");
                    }
                }
            }
        }

        if (aroundAtt != null)
        {
            // 保存到静态缓存
            _cachedBladeSphere = aroundAtt;
            Log.Info("资源已保存到静态缓存");
        }
        else
        {
            Log.Error("无法加载资源，所有方案都失败了");
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
        var TeleOut = _control.FsmStates.FirstOrDefault(state => state.name == "Tele Out");
        if (TeleOut != null)
        {
            foreach (var action in TeleOut.Actions)
            {
                if (action is FaceObjectV2 faceObjectV2)
                {
                    faceObjectV2.pauseBetweenTurns = 0.2f;
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
            var Actions = FloatDown.Actions.ToList();
            foreach (var action in Actions)
            {
                if (action is AccelerateToY accelerateToY)
                {
                    accelerateToY.accelerationFactor.value = 0.5f;
                    break;
                }

            }
            Actions.Add(new ActivateGameObjectDelay
            {
                gameObject = new FsmOwnerDefault()
                {
                    OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                    GameObject = _createdObjectRef
                },
                activate = new FsmBool() { Value = true },
                delay = new FsmFloat() { Value = 0f }
            });
            Actions.Add(new ActivateGameObjectDelay
            {
                gameObject = new FsmOwnerDefault()
                {
                    OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                    GameObject = _createdObjectRef
                },
                activate = new FsmBool() { Value = false },
                delay = new FsmFloat() { Value = 0.2f }
            });
            Actions.Add(new ActivateGameObjectDelay
            {
                gameObject = new FsmOwnerDefault()
                {
                    OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                    GameObject = _createdObjectRef
                },
                activate = new FsmBool() { Value = true },
                delay = new FsmFloat() { Value = 0.4f }
            });
            Actions.Add(new ActivateGameObjectDelay
            {
                gameObject = new FsmOwnerDefault()
                {
                    OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                    GameObject = _createdObjectRef
                },
                activate = new FsmBool() { Value = false },
                delay = new FsmFloat() { Value = 0.6f }
            });
            FloatDown.Actions = Actions.ToArray();
            // foreach (var action in FloatDown.Actions)
            // {
            //     if (action is AccelerateToY accelerateToY)
            //     {
            //         accelerateToY.accelerationFactor.value = 0.5f;
            //         break;
            //     }

            // }
        }
    }
    private void newSetCogwork()
    {
        if (aroundAtt == null)
        {
            Log.Error("aroundAtt 为 null，无法创建对象");
            return;
        }
        Log.Info($"准备创建对象: {aroundAtt.name}, 是预制体: {!aroundAtt.scene.IsValid()}");

        // 手动创建对象
        GameObject createdObject = null;
        try
        {
            // 总是从预制体实例化，确保获得干净的对象
            createdObject = Instantiate(aroundAtt, transform);
            createdObject.transform.localPosition = Vector3.zero;
            createdObject.transform.localRotation = Quaternion.identity;
            createdObject.SetActive(false);
            createdObject.name = "cog_dancer_blade_sphere(Clone)"; // 确保名称一致

            Log.Info("实例化预制体成功");
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
    private void ModifyAllBladeSphereInstancesForP2()
    {
        try
        {
            // 使用正确的Unity API来查找所有GameObject
            List<GameObject> bladeSphereInstances = new List<GameObject>();
            foreach (Transform child in gameObject.transform)
            {
                if (child.name.Contains("cog_dancer_blade_sphere(Clone)"))
                {
                    bladeSphereInstances.Add(child.gameObject);
                }
            }
            if (bladeSphereInstances.Count > 0)
            {
                Log.Info($"找到 {bladeSphereInstances.Count} 个cog_dancer_blade_sphere实例，正在修改P2属性");

                foreach (GameObject bladeSphere in bladeSphereInstances)
                {
                    ModifyBladeSphereProperties(bladeSphere);
                }
            }
            else
            {
                Log.Info("未找到cog_dancer_blade_sphere实例，可能尚未创建");
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"查找blade sphere实例时出错: {ex.Message}");
        }
    }
    // 修改：只修改新添加的bladeSphere对象
    private void ModifyNewBladeSphereForP2()
    {
        try
        {
            // 只修改通过_createdObjectRef创建的新对象
            if (_createdObjectRef != null && _createdObjectRef.Value != null)
            {
                GameObject newBladeSphere = _createdObjectRef.Value;
                Log.Info($"找到新添加的bladeSphere对象: {newBladeSphere.name}");

                // 修改新对象的属性
                ModifyBladeSphereProperties(newBladeSphere);

                // 添加新逻辑
                AddComboJoinEvents();
            }
            else
            {
                Log.Info("新添加的bladeSphere对象尚未创建或为空");
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"修改新bladeSphere对象时出错: {ex.Message}");
        }
    }
    // P3阶段修改合体技能第二个技能流程
    // 添加LCR 2状态的事件
    private void AddComboJoinEvents()
    {
        try
        {
            // 1. 修改Combo Join状态，使其BEAT事件跳转到新状态而不是Combo Aim
            var comboJoinState = _parentControl.FsmStates.FirstOrDefault(state => state.Name == "Combo Join");
            if (comboJoinState != null)
            {
                // 查找Combo Join状态中BEAT事件的过渡
                var beatTransition = comboJoinState.Transitions.FirstOrDefault(t =>
                    t.FsmEvent != null && t.FsmEvent.Name == "BEAT");

                if (beatTransition != null)
                {
                    // 保存原始的Combo Aim状态引用
                    var originalComboAimState = beatTransition.toFsmState;

                    // 创建新的"Combo Activate"状态
                    var comboActivateState = new FsmState(_parentControl.Fsm)
                    {
                        Name = "Combo Activate",
                        Description = "P2阶段激活物品的状态"
                    };
                    // 设置Combo Activate状态的动作：激活物品
                    comboActivateState.Actions = new FsmStateAction[]
                    {
                        // 激活物品
                        new ActivateGameObjectDelay
                        {
                            gameObject = new FsmOwnerDefault()
                            {
                                OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                                GameObject = _createdObjectRef
                            },
                            activate = new FsmBool() { Value = true },
                            delay = new FsmFloat() { Value = 0.33f }
                        },
                        
                        // 个Wait：激活物品后等待0.5秒
                        new Wait
                        {
                            time = new FsmFloat() { Value = 0.88f },
                            finishEvent = FsmEvent.Finished // 等待结束后触发FINISHED事件
                        }
                    };
                    // 设置Combo Activate状态的过渡：FINISHED事件跳转到Combo Aim
                    comboActivateState.Transitions = new FsmTransition[]
                    {
                        new FsmTransition
                        {
                            FsmEvent = FsmEvent.Finished, // 使用FINISHED事件而不是BEAT事件
                            toState = originalComboAimState.Name,
                            toFsmState = originalComboAimState
                        }
                    };
                    // // 将新状态添加到状态机
                    _parentControl.Fsm.States = _parentControl.FsmStates.Append(comboActivateState).ToArray();
                    // 修改Combo Join状态的BEAT事件过渡，跳转到新状态
                    beatTransition.toState = "Combo Activate";
                    beatTransition.toFsmState = comboActivateState;

                    Log.Info("成功创建Combo Activate状态并修改Combo Join的BEAT事件过渡");
                }
                else
                {
                    Log.Error("未找到Combo Join状态的BEAT事件过渡");
                }
            }
            else
            {
                Log.Error("未找到Combo Join状态");
            }

            Log.Info("P2阶段状态流程重构完成：Combo Join → Combo Activate → Combo Aim");
        }
        catch (System.Exception ex)
        {
            Log.Error($"重构状态流程时出错: {ex.Message}");
        }
    }
    // P3阶段恢复原样状态流程
    private void RestoreOriginalComboFlow()
    {
        try
        {
            Log.Info("P3阶段：恢复原样，移除额外攻击逻辑");

            // 查找Combo Join状态
            var comboJoinState = _parentControl.FsmStates.FirstOrDefault(state => state.Name == "Combo Join");
            if (comboJoinState != null)
            {
                // 查找BEAT事件过渡
                var beatTransition = comboJoinState.Transitions.FirstOrDefault(t => t.FsmEvent.Name == "BEAT");
                if (beatTransition != null)
                {
                    // 查找原始的Combo Aim状态
                    var originalComboAimState = _parentControl.FsmStates.FirstOrDefault(state => state.Name == "Combo Aim");
                    if (originalComboAimState != null)
                    {
                        // 恢复BEAT事件跳转到原始的Combo Aim状态
                        beatTransition.toState = "Combo Aim";
                        beatTransition.toFsmState = originalComboAimState;
                        Log.Info("已恢复Combo Join的BEAT事件跳转到Combo Aim");
                    }
                }
            }

            // 移除Combo Activate状态
            var comboActivateState = _parentControl.FsmStates.FirstOrDefault(state => state.Name == "Combo Activate");
            if (comboActivateState != null)
            {
                // 从状态机中移除Combo Activate状态
                var remainingStates = _parentControl.FsmStates.Where(state => state.Name != "Combo Activate").ToArray();
                _parentControl.Fsm.States = remainingStates;
                Log.Info("已移除Combo Activate状态");
            }

            Log.Info("P3阶段状态流程已恢复原样：Combo Join → Combo Aim");
            var TeleIN = _control.FsmStates.FirstOrDefault(state => state.Name == "Tele IN");
            if (TeleIN != null)
            {
                foreach (var action in TeleIN.Actions)
                {
                    if (action is SetMeshRenderer setMeshRenderer)
                    {
                        setMeshRenderer.active = false;
                        break;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"恢复原样状态流程时出错: {ex.Message}");
        }
    }
    // 添加P2阶段修改攻击属性的方法
    private void ModifyBladeSphereProperties(GameObject bladeSphere)
    {
        try
        {
            // 修改Animator速度
            Animator animator = bladeSphere.GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed = 0.6f; // 降低动画速度为原来的60%
                Log.Info($"修改Animator速度: {bladeSphere.name}");
                animator.applyRootMotion = false;
                // 方案3：直接修改Transform，并添加持续监控
                Transform targetTransform = bladeSphere.transform;

                // 立即设置目标尺寸
                targetTransform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
                Log.Info($"立即设置Transform尺寸: {bladeSphere.name}");

                // 方案4：添加强力的尺寸控制器，持续覆盖任何修改
                if (!bladeSphere.GetComponent<ForceScaleController>())
                {
                    var scaleController = bladeSphere.AddComponent<ForceScaleController>();
                    scaleController.targetScale = new Vector3(2.5f, 2.5f, 2.5f);
                    scaleController.forceMode = true; // 强制模式
                    Log.Info("添加强制尺寸控制器");
                }
            }

            // 修改Transform尺寸
            // Transform transform = bladeSphere.transform;
            // transform.localScale = new Vector3(3f, 3f, 3f); // 放大1.5倍
            // Log.Info($"修改Transform尺寸: {bladeSphere.name}");

            // 改进的延迟组件查找逻辑
            bool foundDelayComponent = false;
            MonoBehaviour[] components = bladeSphere.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (component != null)
                {
                    string componentTypeName = component.GetType().Name;

                    // 尝试多种可能的组件名称
                    if (componentTypeName.Contains("DeactivateAfterDelay"))
                    {
                        Log.Info($"找到延迟组件: {componentTypeName}");
                        // 使用反射来查找可能的延迟字段
                        System.Reflection.FieldInfo[] fields = component.GetType().GetFields(
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                        foreach (System.Reflection.FieldInfo field in fields)
                        {
                            if (field.FieldType == typeof(float) &&
                                (field.Name.ToLower().Contains("delay") ||
                                 field.Name.ToLower().Contains("time") ||
                                 field.Name.ToLower().Contains("duration")))
                            {
                                try
                                {
                                    float currentDelay = (float)field.GetValue(component);
                                    float newDelay = currentDelay * 1.5f; // 延长50%
                                    field.SetValue(component, newDelay);
                                    Log.Info($"修改{componentTypeName}延迟: {currentDelay} -> {newDelay}");
                                    foundDelayComponent = true;
                                    break;
                                }
                                catch (System.Exception ex)
                                {
                                    Log.Error($"修改{componentTypeName}延迟失败: {ex.Message}");
                                }
                            }
                        }

                        if (foundDelayComponent) break;
                    }
                }
            }

            if (!foundDelayComponent)
            {
                Log.Info($"未找到延迟相关组件: {bladeSphere.name}");
            }

            // 添加视觉效果增强
            AddVisualEffects(bladeSphere);
        }
        catch (System.Exception ex)
        {
            Log.Error($"修改blade sphere属性时出错: {ex.Message}");
        }
    }
    private void AddVisualEffects(GameObject bladeSphere)
    {
        // 改进的材质修改逻辑
        Renderer[] renderers = bladeSphere.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    try
                    {
                        // 保存原始颜色以便恢复
                        if (!renderer.gameObject.GetComponent<OriginalColorStorage>())
                        {
                            var colorStorage = renderer.gameObject.AddComponent<OriginalColorStorage>();
                            colorStorage.originalColor = renderer.material.color;
                        }

                        // 设置为红色调
                        renderer.material.color = new Color(0.92f, 0.45f, 0.05f, renderer.material.color.a);
                        Log.Info($"修改材质颜色: {renderer.gameObject.name}");
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"修改{renderer.gameObject.name}材质失败: {ex.Message}");
                    }
                }
            }
        }
        else
        {
            Log.Info($"对象{bladeSphere.name}没有Renderer组件");
        }
    }
    // 新增：处理定时隐身切换
    private void HandleStealthToggle()
    {
        _stealthTimer += Time.deltaTime;

        if (_stealthTimer >= STEALTH_TOGGLE_INTERVAL)
        {
            _stealthTimer = 0f;
            _isStealthActive = !_isStealthActive;

            // 切换Mesh Renderer状态
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null && gameObject.name == "Dancer A")
            {
                meshRenderer.enabled = !_isStealthActive;
                Log.Info($"Dance A隐身状态切换：{(_isStealthActive ? "隐身" : "可见")}");
            }
            else if (meshRenderer != null && gameObject.name == "Dancer B")
            {
                meshRenderer.enabled = _isStealthActive;
                Log.Info($"Dance B隐身状态切换：{(_isStealthActive ? "可见" : "隐身")}");
            }
        }
    }
    // 新增：简单的隐身切换设置
    private void SetupSimpleStealthToggle()
    {
        try
        {
            Log.Info("P3阶段：设置简单定时隐身切换系统");

            // 初始设置为隐身
            _isStealthActive = true;
            _stealthTimer = 0f;

            // 设置初始隐身状态
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
                Log.Info("P3初始状态：隐身");
            }
            Log.Info("P3简单定时隐身切换系统设置完成");
        }
        catch (System.Exception ex)
        {
            Log.Error($"设置简单隐身切换系统时出错: {ex.Message}");
        }
    }
    // 辅助组件用于存储原始颜色
    public class OriginalColorStorage : MonoBehaviour
    {
        public Color originalColor;
    }

    public class ForceScaleController : MonoBehaviour
    {
        public Vector3 targetScale = Vector3.one;
        public bool forceMode = true;
        private Vector3 lastScale = Vector3.one;

        void Start()
        {
            // 立即设置目标尺寸
            transform.localScale = targetScale;
            lastScale = targetScale;
        }

        void Update()
        {
            if (forceMode)
            {
                // 强制模式：每帧都设置目标尺寸
                transform.localScale = targetScale;
            }
            else
            {
                // 监控模式：只在尺寸被修改时修正
                if (transform.localScale != targetScale && transform.localScale != lastScale)
                {
                    transform.localScale = targetScale;
                    lastScale = targetScale;
                }
            }
        }

        void LateUpdate()
        {
            // 在LateUpdate中再次确保尺寸正确
            if (transform.localScale != targetScale)
            {
                transform.localScale = targetScale;
            }
        }

        void OnDestroy()
        {
            // 可选：恢复原始尺寸
            transform.localScale = new Vector3(1.335f, 1.335f, 1.335f);
        }
    }
}