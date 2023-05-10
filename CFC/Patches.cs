using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CFC
{
    public static class Patches
    {
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
        public static class SetupReqTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> SetupRequirementsTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(useEnd: false,
                        new CodeMatch(OpCodes.Ldarg_2),
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Humanoid), nameof(Humanoid.GetInventory))),
                        new CodeMatch(OpCodes.Ldarg_1),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Piece.Requirement), nameof(Piece.Requirement.m_resItem))),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ItemDrop), nameof(ItemDrop.m_itemData))))
                    .Advance(9)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_1))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_2))
                    .InsertAndAdvance(Transpilers.EmitDelegate<Func<int, Piece.Requirement, Player, int>>(CheckChestList))
                    .InstructionEnumeration();
            }

        }
        
        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems), typeof(Recipe), typeof(bool), typeof(int))]
        public static class RequirementItemsTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> HaveReqsItemTranspiler(IEnumerable<CodeInstruction> instructions)
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
                    .InsertAndAdvance(Transpilers.EmitDelegate<Func<int,Piece.Requirement, Player, int>>(CheckChestList))
                    .InstructionEnumeration();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
        public static class HaveReqsTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> il = instructions.ToList();

                for (int i = 0; i < il.Count; ++i)
                {
                    if (il[i].Calls(AccessTools.Method(typeof(Inventory), nameof(Inventory.CountItems))))
                    {
                        il.Insert(++i, new CodeInstruction(OpCodes.Ldloc_2));
                        il.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                        il.Insert(++i, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patches), nameof(CheckChestList))));
                    }
                }

                return il.AsEnumerable();
            }
        }
        
        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
        public static class ConsumeResourcesTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> ConsumeRes(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(
                        useEnd: false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Humanoid), nameof(Humanoid.m_inventory))),
                        new CodeMatch(OpCodes.Ldloc_2))
                    .Advance(1)
                    .RemoveInstructions(9)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_2))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_3))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_3))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patches), nameof(RemoveItemsFromChests))))
                    .InstructionEnumeration();

            }
        }
        private static void RemoveItemsFromChests(Player player, Piece.Requirement item, int amount, int itemQuality)
        {
            int inventoryAmount = player.m_inventory.CountItems(item.m_resItem.m_itemData.m_shared.m_name);
            player.m_inventory.RemoveItem(item.m_resItem.m_itemData.m_shared.m_name, amount, itemQuality);
            amount -= inventoryAmount;
            if (amount <= 0) return;

            foreach (var c in ContainerAwakePatch.Continers)
            {
                inventoryAmount = c.m_inventory.CountItems(item.m_resItem.m_itemData.m_shared.m_name);
                c.m_inventory.RemoveItem(item.m_resItem.m_itemData.m_shared.m_name, amount, itemQuality);
                amount -= inventoryAmount;
                if(amount <=0)break;
            }
            
        }
        
        private static int  CheckChestList(int fromInventory, Piece.Requirement? item, Player player)
        {
            if (item == null || player == null) return fromInventory;
            int i = 0;
            ContainerAwakePatch.Continers.RemoveAll(container => container == null);
            foreach (var c in ContainerAwakePatch.Continers)
            {
                if(c == null ||item == null || player ==null) continue;
                if (Vector3.Distance(player.transform.position, c.transform.position) >
                    CFCMod.ChestDistance?.Value) continue;
                if(c.m_inventory == null) continue;
                if(c.gameObject.name.StartsWith("Player"))continue;
                if (c.m_inventory.HaveItem(item.m_resItem.m_itemData.m_shared.m_name))
                {
                    i += c.m_inventory.CountItems(item.m_resItem.m_itemData.m_shared.m_name);
                }
            }
            i += fromInventory;
            return i;
        }

        [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
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
        public static class ContainerDestroyPatch
        {
            public static void Prefix(Container __instance)
            {
                if (ContainerAwakePatch.Continers.Contains(__instance)) ContainerAwakePatch.Continers.Remove(__instance);
            }
        }
    }
}