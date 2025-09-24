using System;
using System.Collections.Generic;
using System.Text;
using GodDance.Source.Behaviours;
using HarmonyLib;
using TeamCherry.Localization;
using UnityEngine;
namespace GodDance.Source.Patches;
internal static class GodDancePatches
 {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayMakerFSM), "Start")]
    private static void ModifyDance(PlayMakerFSM __instance)
    {
        if (__instance.name == "Dancer Control" && __instance.FsmName == "Control" &&
            __instance.gameObject.layer == LayerMask.NameToLayer("Enemies"))
        {
            __instance.gameObject.AddComponent<GodDance.Source.Behaviours.GodDance>();
        }
        else if (__instance.name == "Dancer A" && __instance.FsmName == "Control" &&
            __instance.gameObject.layer == LayerMask.NameToLayer("Enemies"))
        {
                __instance.gameObject.AddComponent<GodDance.Source.Behaviours.singleGodDance>();
            }
        else if (__instance.name == "Dancer B" && __instance.FsmName == "Control" &&
            __instance.gameObject.layer == LayerMask.NameToLayer("Enemies"))
        {
                __instance.gameObject.AddComponent<GodDance.Source.Behaviours.singleGodDance>();
            }
        // else if (__instance.name == "Pin Projectiles")
        // {
        //     __instance.gameObject.AddComponent<PinProjectiles>();
        // }
    }
}

