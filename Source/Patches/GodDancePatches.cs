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
        if (Plugin.IsBossSaveLoaded)
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
        }
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Language), nameof(Language.Get), typeof(string), typeof(string))]
    private static void ChangeDancerTitle(string key, string sheetTitle, ref string __result)
    {
        __result = key switch
        {
            "COGWORK_DANCERS_SUPER" => Language.CurrentLanguage() switch
            {
                LanguageCode.EN => "God",
                LanguageCode.ZH => "暴躁的",
                _ => __result
            },
            "COGWORK_DANCERS_SUB" => Language.CurrentLanguage() switch
            {
                // LanguageCode.EN => "Dancers",
                LanguageCode.ZH => "他们真的很暴躁",
                _ => __result
            },
            "COGWORK_DANCERS_MAIN" => Language.CurrentLanguage() switch
            {
                LanguageCode.EN => "CogWork Dancers",
                LanguageCode.ZH => "机驱舞神",
                _ => __result
            },
            _ => __result
        };
    }

    [HarmonyPatch(typeof(HeroController), nameof(HeroController.CanPlayNeedolin))]
    public class CanPlayNeedolinPatch
    {
        public static bool Prefix(HeroController __instance, ref bool __result)
        {
            // 如果已加载 BOSS 存档，则允许播放 Needolin
            if (Plugin.IsBossSaveLoaded)
            {
                __result = true;
                return false; // 跳过原方法执行
            }

            // 否则执行原方法逻辑
            return true;
        }
    }
}

