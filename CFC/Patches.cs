using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CFC
{
    public static class Patches
    {
      
        /*
         * IL_00e9: ldarg.2      // player
    IL_00ea: callvirt     instance class Inventory Humanoid::GetInventory()
    IL_00ef: ldarg.1      // req
    IL_00f0: ldfld        class ItemDrop Piece/Requirement::m_resItem
    IL_00f5: ldfld        class ItemDrop/ItemData ItemDrop::m_itemData
    IL_00fa: ldfld        class ItemDrop/ItemData/SharedData ItemDrop/ItemData::m_shared
    IL_00ff: ldfld        string ItemDrop/ItemData/SharedData::m_name
    IL_0104: ldc.i4.m1
    IL_0105: callvirt     instance int32 Inventory::CountItems(string, int32)
    IL_010a: stloc.s      num
         */
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
        
        
        /*
         * IL_0089: ldarg.0      // this
      IL_008a: ldfld        class Inventory Humanoid::m_inventory
      IL_008f: ldloc.2      // resource
      IL_0090: ldfld        class ItemDrop Piece/Requirement::m_resItem
      IL_0095: ldfld        class ItemDrop/ItemData ItemDrop::m_itemData
      IL_009a: ldfld        class ItemDrop/ItemData/SharedData ItemDrop/ItemData::m_shared
      IL_009f: ldfld        string ItemDrop/ItemData/SharedData::m_name
      IL_00a4: ldc.i4.m1
      IL_00a5: callvirt     instance int32 Inventory::CountItems(string, int32)
      IL_00aa: stloc.s      num
         */
        
        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems), typeof(Recipe), typeof(bool), typeof(int))]
        public static class RequirementItemsTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> HaveReqsTranspiler(IEnumerable<CodeInstruction> instructions)
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

        private static int  CheckChestList(int fromInventory, Piece.Requirement item, Player player)
        {
            int i = 0;
            ContainerAwakePatch.Continers.RemoveWhere(container => container == null);
            foreach (var c in ContainerAwakePatch.Continers)
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

        [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
        public static class ContainerAwakePatch
        {
            internal static readonly HashSet<Container> Continers = new HashSet<Container>();

            public static void Postfix(Container __instance)
            {
                Continers.Add(__instance);
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