using HarmonyLib;
using UnityEngine;
namespace CFC
{
    public class ItemDrawer_Patches
    {
        [HarmonyPatch(typeof(DrawerContainer), nameof(DrawerContainer.Awake))]
        public static class DrawerAwakePatch
        {
            public static void Postfix(Container __instance)
            {
                if(Player.m_localPlayer != null)
                {
                    if (Player.m_localPlayer.m_placementGhost == __instance.gameObject) return;
                }
                if(!Patches.ContainerAwakePatch.Continers.Contains(__instance))Patches.ContainerAwakePatch.Continers.Add(__instance);
                if (__instance.m_nview != null)
                {
                    if(__instance.m_nview.GetZDO() != null && __instance.m_nview.GetZDO().GetInt("ChestType") <= 0)
                        __instance.m_nview.GetZDO().Set("ChestType", 9);
                }
            }
        }
        

        [HarmonyPatch(typeof(DrawerContainer), nameof(DrawerContainer.OnDestroyed))]
        public static class DrawerDestroyPatch
        {
            public static void Prefix(DrawerContainer __instance)
            {
                if (Patches.ContainerAwakePatch.Continers.Contains(__instance)) Patches.ContainerAwakePatch.Continers.Remove(__instance);
            }
        }

        [HarmonyPatch(typeof(DrawerContainer), nameof(DrawerContainer.GetHoverText))]
        public static class DrawerHoverTextPatch
        {
            public static void Postfix(DrawerContainer __instance, ref string __result)
            {
                ChestType type = (ChestType)__instance.m_nview.GetZDO().GetInt("ChestType");
                __result += Localization.instance.Localize("<br><b><color=yellow>[L-Ctrl + $KEY_Use</color></b>] To Set Chest Type" +
                                                           "<br> Current Type: " + "<b><color=red>"+type+"</b></color>");
            }
        }

        [HarmonyPatch(typeof(DrawerContainer), nameof(DrawerContainer.Interact))]
        public static class DrawerInteractPatch
        {
            public static bool Prefix(DrawerContainer __instance)
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    var i = __instance.m_nview.GetZDO().GetInt("ChestType");
                    ++i;
                    if(i >9)
                        i=1;
                    __instance.m_nview.GetZDO().Set("ChestType", i);
                    return false;
                }

                return true;
            }
        }
    }
}