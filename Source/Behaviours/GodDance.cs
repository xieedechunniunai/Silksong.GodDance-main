using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
internal class GodDance : MonoBehaviour
{
    //private const float GroundY = 13;

    private tk2dSpriteAnimator _anim = null!;
    private Rigidbody2D _body = null!;
    private PlayMakerFSM _control = null!;
    private Transform _heroTransform = null!;

    private void Awake()
    {
        // orig(self)
        // StartCoroutine(SetupBoss());
    }
    private void Start()
    {
    }
    private void Update()
    {
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
        //ChangeBlackThreadVoice();
        //ChangeTextures();
        ModifyDamage();
        IncreaseHealth();
        ModifyFsm();
        Log.Info($" Awake Finish ===================");
        yield return null;
    }

    /// <summary>
    /// Fetch necessary <see cref="Component">components</see> used by this behavior.
    /// </summary>
    private void GetComponents()
    {
        Log.Info($"{gameObject.name} Awake");
        _anim = GetComponent<tk2dSpriteAnimator>();
        _body = GetComponent<Rigidbody2D>();
        _control = FSMUtility.LocateMyFSM(base.gameObject, "Control");
        _heroTransform = HeroController.instance.transform;
    }

    /// <summary>
    /// Distort the boss's voice to be like other void-corrupted enemies.
    /// </summary>
    // private void ChangeBlackThreadVoice()
    // {
    //     var voiceAudio = transform.Find("Audio Loop Voice").GetComponent<AudioSource>();
    //     var blackThreadMixerGroup =
    //         voiceAudio.outputAudioMixerGroup.audioMixer.FindMatchingGroups("Actors VoiceBlackThread");
    //     voiceAudio.outputAudioMixerGroup = blackThreadMixerGroup[0];
    // }

    /// <summary>
    /// Change the <see cref="Texture2D">texture</see> atlases of the boss.
    /// </summary>
    // private void ChangeTextures()
    // {
    //     var sprite = GetComponent<tk2dSprite>();
    //     var cln = sprite.Collection;
    //     cln.materials[0].mainTexture = Plugin.AtlasTextures[0];
    //     cln.materials[1].mainTexture = Plugin.AtlasTextures[1];
    // }

    /// <summary>
    /// Modify the damage behavior of the boss.
    /// </summary>
    private void ModifyDamage()
    {
        var damageFlags = DamagePropertyFlags.Void;
        foreach (var damageHero in GetComponentsInChildren<DamageHero>(true))
        {
            var multiWounderFsm = damageHero.gameObject.LocateMyFSM("hornet_multi_wounder");
            if (multiWounderFsm)
            {
                multiWounderFsm.Fsm.GetFsmBool("z3 Force Black Threaded").Value = true;
                continue;
            }

            damageHero.damageDealt = 1;
            damageHero.damagePropertyFlags = damageFlags;
        }
    }

    /// <summary>
    /// Raise the boss's <see cref="HealthManager">health</see>.
    /// </summary>
    private void IncreaseHealth()
    {

        var health = GetComponent<HealthManager>();
        if (health != null)
        {
            health.hp += 300;
        }
        else
        {
            Log.Error("HealthManager component not found on the boss object.");
        }
        _control.FsmVariables.GetFsmInt("Phase 1 HP").Value += 300;
        _control.FsmVariables.GetFsmInt("Phase 2 HP").Value += 300;
        _control.FsmVariables.GetFsmInt("Phase 3 HP").Value += 300;
        Log.Info("机驱舞者加血成功Health increased.");
    }

    /// <summary>
    /// Remove the boss's ability to be stunned.
    /// </summary>
    // private void RemoveStuns()
    // {
    //     Destroy(gameObject.LocateMyFSM("Stun Control"));
    // }

    /// <summary>
    /// Update the boss's <see cref="PlayMakerFSM">state machine</see>.
    /// </summary>
    private void ModifyFsm()
    {
        //AddAbyssTendrilsToCharge();
        //AddVomitGlobAttack();
        //MakeFacingSecondSlash();
        IncreaseHealth();
        ShortAttackWaitTime();
        ModifyPhase2();
    }

    /// <summary>
    /// Spawn abyss tendrils while the boss is performing a charging slice.
    /// </summary>
    /// <summary>
    /// Add a new attack where the boss spawns abyss vomit globs.
    /// </summary>

    /// <summary>
    /// Make the second slash of the double slash attack face the player.
    /// </summary>
    private void MakeFacingSecondSlash()
    {
        var slash3State = _control.FsmStates.FirstOrDefault(state => state.Name == "Slash 3");
        if (slash3State != null)
        {
            var slashActions = slash3State.Actions.ToList();
            slashActions.Insert(0, new InvokeMethod(FaceHero));
            slash3State.Actions = slashActions.ToArray();
        }
    }

    /// <summary>
    /// Shorten the duration of the boss's healing bind.
    /// </summary>
    private void ShortAttackWaitTime()
    {
        var bindState = _control.FsmStates.FirstOrDefault(state => state.Name == "Pendulum Prepare");
        if (bindState != null)
        {
            foreach (var action in bindState.Actions)
            {
                if (action is Wait wait)
                {
                    wait.time = 1f;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Make changes when the boss enters phase 2.
    /// </summary>
    private void ModifyPhase2()
    {
        var p2StartState = _control.FsmStates.FirstOrDefault(state => state.name == "Set Phase 2");
        if (p2StartState != null)
        {
            // p2StartState.Actions = p2StartState.Actions
            //     .Append(new InvokeMethod(IncreaseChargeSliceSpeed))
            //     .Append(new InvokeMethod(SpeedUpPhase2Pins))
            //     .Append(new InvokeMethod(SlashToCharge))
            //     .ToArray();
            int aa = 0;
            foreach (var action in p2StartState.Actions)
            {
                if (action is SetFsmFloat setFsmFloat)
                {
                    setFsmFloat.setValue = 0.25f;
                    aa += 1;
                }
                if (action is SetHP SetHP)
                {
                    SetHP.hp = 600;
                    aa += 1;
                }
                if (aa >= 2) { break; }
            }
        }
    }
    private void ModifyPhase3()
    {
        var p2StartState = _control.FsmStates.FirstOrDefault(state => state.name == "Set Phase 3");
        if (p2StartState != null)
        {
            // p2StartState.Actions = p2StartState.Actions
            //     .Append(new InvokeMethod(IncreaseChargeSliceSpeed))
            //     .Append(new InvokeMethod(SpeedUpPhase2Pins))
            //     .Append(new InvokeMethod(SlashToCharge))
            //     .ToArray();
            int aa = 0;
            foreach (var action in p2StartState.Actions)
            {
                if (action is SetFsmFloat setFsmFloat)
                {
                    setFsmFloat.setValue = 0.25f;
                    aa += 1;
                }
                if (action is SetHP SetHP)
                {
                    SetHP.hp = 600;
                    aa += 1;
                }
                if (aa >= 4) { break; }
            }
        }
    }

    /// <summary>
    /// Transition from the boss's double slash attack to its charging slice attack.
    /// </summary>
    private void SlashToCharge()
    {
        FsmState? afterSlashState = null;
        FsmState? sliceChargeAnticState = null;
        foreach (var state in _control.FsmStates)
        {
            if (state.Name == "After Slash")
            {
                afterSlashState = state;
            }
            else if (state.Name == "Slice Charge Antic")
            {
                sliceChargeAnticState = state;
            }
        }

        if (afterSlashState != null && sliceChargeAnticState != null)
        {
            afterSlashState.Transitions = new FsmTransition[] {
                new FsmTransition {
                    toFsmState = sliceChargeAnticState,
                    toState = afterSlashState.Name,
                    FsmEvent = FsmEvent.Finished
                }
            };
        }
    }

    /// <summary>
    /// Increase the speed of the charging slice attack.
    /// </summary>
    private void IncreaseChargeSliceSpeed()
    {
        var sliceChargeState = _control.FsmStates.First(state => state.Name == "Slice Charge");
        var sliceChargeActions = sliceChargeState.Actions;
        if (sliceChargeActions[1] is SetVelocityByScale setVelocity)
        {
            setVelocity.speed = -12;
        }

        if (sliceChargeActions[2] is AccelerateToXByScale accelerateToX)
        {
            accelerateToX.accelerationFactor = 0.65f;
            accelerateToX.targetSpeed = 40;
        }

        if (sliceChargeActions[6] is Wait wait)
        {
            wait.time = 1f;
        }
    }

    /// <summary>
    /// Increase the firing speed of pins in phase 2 of the boss fight. 
    /// </summary>
    private void SpeedUpPhase2Pins()
    {
        foreach (var pinFsm in FindObjectsByType<PlayMakerFSM>(FindObjectsSortMode.None)
                     .Where(fsm => fsm.name.Contains("FW Pin Projectile")))
        {
            var fireState = pinFsm.FsmStates.First(state => state.Name == "Fire");
            var setVelAction = fireState.Actions.First(action => action is SetVelocityAsAngle) as SetVelocityAsAngle;
            setVelAction!.speed = 18;

            var threadPullState = pinFsm.FsmStates.First(state => state.Name == "Thread Pull");
            if (threadPullState.Actions[3] is Wait wait)
            {
                wait.time = 4f;
            }
        }
    }

    /// <summary>
    /// Perform the new abyss vomit glob attack.
    /// </summary>

    /// <summary>
    /// Face the player.
    /// </summary>
    private void FaceHero()
    {
        if (_heroTransform.position.x > transform.position.x && transform.localScale.x > 0 ||
            _heroTransform.position.x < transform.position.x && transform.localScale.x < 0)
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }
}