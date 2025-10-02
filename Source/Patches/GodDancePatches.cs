using System.Linq;
using System.Collections.Generic;
using System.Text;
using GodDance.Source.Behaviours;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using TeamCherry.Localization;
using UnityEngine.SceneManagement;
using UnityEngine;
namespace GodDance.Source.Patches;

internal static class GodDancePatches
{
    private static readonly HashSet<GameObject> _modifiedDancers = new();
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayMakerFSM), "Start")]
    private static void ModifyDance(PlayMakerFSM __instance)
    {
        // 检查是否已经修改过这个对象
        if (_modifiedDancers.Contains(__instance.gameObject))
            return;
        if (__instance.name == "Dancer A" && __instance.FsmName == "Control" &&
            __instance.gameObject.layer == LayerMask.NameToLayer("Enemies"))
        {
            Log.Info("Modifying Dance，获取到A控制器");
            // 检查是否已经有组件
            if (__instance.gameObject.GetComponent<singleGodDance>() == null)
            {
                __instance.gameObject.AddComponent<singleGodDance>();
            }
            if (__instance.gameObject.GetComponent<Behaviours.GodDance>() == null)
            {
                __instance.gameObject.AddComponent<Behaviours.GodDance>();
            }
            
            _modifiedDancers.Add(__instance.gameObject);
        }
        else if (__instance.name == "Dancer B" && __instance.FsmName == "Control" &&
            __instance.gameObject.layer == LayerMask.NameToLayer("Enemies"))
        {
            Log.Info("Modifying Dance，获取到B控制器");
            if (__instance.gameObject.GetComponent<singleGodDance>() == null)
            {
                __instance.gameObject.AddComponent<singleGodDance>();
            }
            
            _modifiedDancers.Add(__instance.gameObject);

        }
        // else if (__instance.name == "Dancer A" && __instance.FsmName == "Check Height" &&
        //     __instance.gameObject.layer == LayerMask.NameToLayer("Enemies"))
        // {
        //     __instance.gameObject.AddComponent<changeHight>();
        // }
        // else if (__instance.name == "Pin Projectiles")
        // {
        //     __instance.gameObject.AddComponent<PinProjectiles>();
        // }

    }
}

