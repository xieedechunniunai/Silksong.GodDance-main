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
internal class singleGodDance : MonoBehaviour
{   
     private tk2dSpriteAnimator _anim = null!;
    private Rigidbody2D _body = null!;
    private PlayMakerFSM _control = null!;
    private Transform _heroTransform = null!;
    private void Awake()
    {
        StartCoroutine(SetupBoss());
        
    }
    private IEnumerator SetupBoss()
    {
        yield return AssetManager.Initialize();
        yield return AssetManager.ManuallyLoadBundles();
        GetComponents();
        ModifyFsm();
    }
        private void GetComponents()
    {
        _anim = GetComponent<tk2dSpriteAnimator>();
        _body = GetComponent<Rigidbody2D>();
        _control = gameObject.LocateMyFSM("Control");
        _heroTransform = HeroController.instance.transform;
    }
    private void ModifyFsm()
    {
        var Windup = _control.FsmStates.FirstOrDefault(state => state.name == "Windup");
        if (Windup != null)
        {
            foreach (var action in Windup.Actions)
            {
                if (action is Wait Wait)
                {
                    Wait.time = 0.25f;
                }
            }
        }
         var WindupOB = _control.FsmStates.FirstOrDefault(state => state.name == "Windup OB");
        if (WindupOB != null)
        {
            foreach (var action in WindupOB.Actions)
            {
                if (action is Wait Wait)
                {
                    Wait.time =  0.15f;
                }
            }
        }
    }
 }