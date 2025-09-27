using System.Linq;
using System.Text;
using GodDance.Source.Behaviours;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using TeamCherry.Localization;
using UnityEngine;
namespace GodDance.Source.Patches;

internal static class GodDancePatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayMakerFSM), "Start")]
    private static void ModifyDance(PlayMakerFSM __instance)
    {
        //Log.Message("(变换场景还是FSM触发) Starting FSM");
        //if (__instance.name == "Dancer Control" && __instance.FsmName == "Control" )
        //{
        //    __instance.gameObject.AddComponent<DanceModificationMarker>();
        //}
        if (__instance.name == "Dancer A" && __instance.FsmName == "Control" &&
            __instance.gameObject.layer == LayerMask.NameToLayer("Enemies"))
        {
            Log.Info("Modifying Dance，获取到A控制器");
            __instance.gameObject.AddComponent<singleGodDance>();
        }
        else if (__instance.name == "Dancer B" && __instance.FsmName == "Control" &&
            __instance.gameObject.layer == LayerMask.NameToLayer("Enemies"))
        {
            Log.Info("Modifying Dance，获取到B控制器");
            __instance.gameObject.AddComponent<singleGodDance>();
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
    internal class changeHight : MonoBehaviour
    {
        private void Start()
        {
            var _control = gameObject.LocateMyFSM("Check Height");
            var initStateVar = _control.FsmVariables?.GetFsmFloat("Top Row Pos");
            if (initStateVar != null)
            {
                initStateVar.Value += 50f;
            }
            var initState = _control.FsmStates.FirstOrDefault(x => x.Name == "Top Row");
            if (initState != null)
            {
                foreach (var action in initState.Actions)
                {
                    if (action is SetBoolValue setBoolValue)
                    {
                        setBoolValue.boolValue = false;
                    }
                }
            }
        }
    }
}

