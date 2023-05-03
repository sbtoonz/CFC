using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CFC
{
    public static class Patches
    {
        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
        public static class RequirementsTranspiler
        {
            private static readonly MethodInfo ComputeItemQuantity = AccessTools.Method(typeof(Requirements2Transpiler), nameof(Requirements2Transpiler.CheckChestList));
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> HaveReqs(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(useEnd: false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Humanoid), nameof(Humanoid.m_inventory))),
                        new CodeMatch(OpCodes.Ldloc_2),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Piece.Requirement), nameof(Piece.Requirement.m_resItem))),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ItemDrop), nameof(ItemDrop.m_itemData)))
                    )
                    .Advance(8)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_2))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, ComputeItemQuantity))
                    .InstructionEnumeration();
            }
        }
        
        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems), typeof(Recipe), typeof(bool), typeof(int))]
        public static class Requirements2Transpiler
        {
            private static readonly MethodInfo ComputeItemQuantity = AccessTools.Method(typeof(Requirements2Transpiler), nameof(Requirements2Transpiler.CheckChestList));

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> HaveReqs(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(useEnd: false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Humanoid), nameof(Humanoid.m_inventory))),
                        new CodeMatch(OpCodes.Ldloc_2),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Piece.Requirement), nameof(Piece.Requirement.m_resItem))),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ItemDrop), nameof(ItemDrop.m_itemData)))
                    )
                    .Advance(9) 
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_2))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, ComputeItemQuantity))
                    .InstructionEnumeration();
            }

            internal static int  CheckChestList(int fromInventory, Piece.Requirement item, Player player)
            {
                int i = 0;
                ContainerPatch.Continers.RemoveAll(container => container == null);
                foreach (var c in ContainerPatch.Continers)
                {
                    if(c == null) continue;
                    if(Vector3.Distance(player.transform.position, c.transform.position) > CFCMod.ChestDistance?.Value)continue;
                    if (c.m_inventory.HaveItem(item.m_resItem.m_itemData.m_shared.m_name))
                    {
                        i += c.m_inventory.CountItems(item.m_resItem.m_itemData.m_shared.m_name);
                    }
                }
                
                i += fromInventory;
                return i;
            }
        }


        [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
        public static class ContainerPatch
        {
            internal static readonly List<Container> Continers = new List<Container>();

            public static void Postfix(Container __instance)
            {
                if(!Continers.Contains(__instance))Continers.Add(__instance);
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.OnDestroyed))]
        public static class ContainerDestroyPatch
        {
            public static void Prefix(Container __instance)
            {
                if (ContainerPatch.Continers.Contains(__instance)) ContainerPatch.Continers.Remove(__instance);
            }
        }
    }
}