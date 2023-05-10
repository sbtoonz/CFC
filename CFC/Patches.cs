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
    }
}