using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CFC
{
    public static class Patches
    {
        [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
        [HarmonyPriority(0)]
        public static class ContainerAwakePatch
        {
            internal static readonly List<Container> Continers = new List<Container>();
            public static void Postfix(Container __instance)
            {
                if(Player.m_localPlayer != null)
                {
                    if (Player.m_localPlayer.m_placementGhost == __instance.gameObject) return;
                }
                if(!Continers.Contains(__instance))Continers.Add(__instance);
                if (__instance.m_nview != null)
                {
                    if(__instance.m_nview.GetZDO() != null && __instance.m_nview.GetZDO().GetInt("ChestType") <= 0)
                        __instance.m_nview.GetZDO().Set("ChestType", 6);
                }
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.OnDestroyed))]
        [HarmonyPriority(0)]
        public static class ContainerDestroyPatch
        {
            public static void Prefix(Container __instance)
            {
                if (ContainerAwakePatch.Continers.Contains(__instance)) ContainerAwakePatch.Continers.Remove(__instance);
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
        public static class ContainerHoverTextPostfix
        {
            public static void Postfix(Container __instance, ref string __result)
            {
                ChestType type = (ChestType)__instance.m_nview.GetZDO().GetInt("ChestType");
                __result += Localization.instance.Localize("<br><b><color=yellow>[L-Ctrl + $KEY_Use</color></b>] To Set Chest Type" +
                                                           "<br> Current Type: " + "<b><color=red>"+type+"</b></color>");
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
        public static class ContainerSetTypePatch
        {
            public static bool Prefix(Container __instance)
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    var i = __instance.m_nview.GetZDO().GetInt("ChestType");
                    ++i;
                    if(i >6)
                        i=1;
                    __instance.m_nview.GetZDO().Set("ChestType", i);
                    return false;
                }

                return true;
            }
        }
        
    }

    public enum ChestType : byte
    {
        Kiln = 1,
        BlastFurnace = 2,
        Smelter = 3,
        Fire = 4,
        SapCollector = 5,
        None = 6
    }
}