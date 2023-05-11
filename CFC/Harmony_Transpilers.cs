using System.Collections.Generic;
using System;
using System.Linq;
using HarmonyLib;
using System.Reflection.Emit;
using UnityEngine;

namespace CFC
{
    public static class HarmonyTranspilers
    {
        private static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if(val.CompareTo(max) > 0) return max;
            else return val;
        }

        #region Transpilers

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
        [HarmonyPriority(0)]
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
        [HarmonyPriority(0)]
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
        [HarmonyPriority(0)]
        public static class HaveReqsTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions).MatchForward(useEnd: false,
                        new CodeMatch(OpCodes.Callvirt,
                            AccessTools.Method(typeof(Inventory), nameof(Inventory.CountItems))))
                    .Advance(1)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_2))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyTranspilers), nameof(CheckChestList))))
                    .InstructionEnumeration();
            }
        }
        
        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
        [HarmonyPriority(0)]
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
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyTranspilers), nameof(RemoveItemsFromChests))))
                    .InstructionEnumeration();

            }
        }

        [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.UpdateFireplace))]
        [HarmonyPriority(0)]
        public static class FirePlaceFuelTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> FireUpdateFuel(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(useEnd: false,
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Fireplace), nameof(Fireplace.m_nview))),
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ZNetView), nameof(ZNetView.GetZDO))))
                    .Advance(4)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyTranspilers), nameof(FuelFromChest))))
                    .InstructionEnumeration();
            }
        }

        [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.Interact))]
        [HarmonyPriority(0)]
        public static class FirePlaceInteractFuelTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> FireInteractFuel(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(useEnd: false,
                        new CodeMatch(OpCodes.Ldloc_0),
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Fireplace), nameof(Fireplace.m_fuelItem))))
                    .RemoveInstruction()
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloca, 0))
                    .Advance(3)
                    .RemoveInstructions(3)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyTranspilers), nameof(RemoveFuelFromChest))))
                    .InstructionEnumeration();
            }

            
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnInteract))]
        [HarmonyPriority(0)]
        public static class CookingInteractTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> CookInteract(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(useEnd: false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldarg_1),
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Humanoid), nameof(Humanoid.GetInventory))),
                        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(CookingStation), nameof(CookingStation.FindCookableItem))))
                    .Advance(3)
                    .RemoveInstruction()
                    .InsertAndAdvance(Transpilers.EmitDelegate<Func<CookingStation,Inventory, ItemDrop.ItemData>>(GetCookableFromChest))
                    .InstructionEnumeration();
            }

          
        }
        
        #endregion
        #region Transpiler Methods
        private static void RemoveItemsFromChests(Player player, Piece.Requirement item, int amount, int itemQuality)
        {
            var inventoryAmount = player.m_inventory.CountItems(item.m_resItem.m_itemData.m_shared.m_name);
            if(inventoryAmount <=0)return;
            player.m_inventory.RemoveItem(item.m_resItem.m_itemData.m_shared.m_name, amount, itemQuality);
            amount -= inventoryAmount;
            if (amount <= 0) return;

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var c in Patches.ContainerAwakePatch.Continers)
            {
                if(!CFCMod.ShouldSearchWardedAreas!.Value && !c.CheckAccess(player.GetPlayerID()) && !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
                if(Vector3.Distance(c.transform.position, player.transform.position) > CFCMod.ChestDistance!.Value)continue;
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
            Patches.ContainerAwakePatch.Continers.RemoveAll(container => container == null);
            foreach (var c in Patches.ContainerAwakePatch.Continers)
            {
                if(c == null ||item == null || player ==null) continue;
                if(!CFCMod.ShouldSearchWardedAreas!.Value && !c.CheckAccess(player.GetPlayerID()) && !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
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
        private static bool RemoveFuelFromChest(ref Inventory inventory, ItemDrop.ItemData itemData, Fireplace fireplace)
        {
            if (inventory.HaveItem(itemData.m_shared.m_name)) return true;
            foreach (var c in Patches.ContainerAwakePatch.Continers)
            {
                if (Player.m_localPlayer == null) break;
                if(!CFCMod.ShouldSearchWardedAreas!.Value && !c.CheckAccess(Player.m_localPlayer.GetPlayerID()) && !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
                if(Vector3.Distance(c.transform.position, Player.m_localPlayer.transform.position) > CFCMod.FuelingDistance!.Value)continue;
                if(c.m_inventory == null) continue;
                if (c.m_inventory.HaveItem(itemData.m_shared.m_name))
                {
                    inventory = c.m_inventory;
                    return true;
                }
            }
            return false;
        }
        private static float _elapsedTime = 0f;
        private static void FuelFromChest(Fireplace fireplace)
        {
            _elapsedTime += Time.deltaTime;
            var currentZdoFuel = Mathf.CeilToInt(fireplace.m_nview.GetZDO().GetFloat("fuel"));
            if (currentZdoFuel >= CFCMod.LowFuelValue!.Value || _elapsedTime <= CFCMod.SearchInterval!.Value) return;
            foreach (var c in Patches.ContainerAwakePatch.Continers)
            {
                if(c == null) continue;
                if(Player.m_localPlayer == null)break;
                if(!CFCMod.ShouldSearchWardedAreas!.Value && !c.CheckAccess(Player.m_localPlayer.GetPlayerID()) && !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
                if(Vector3.Distance(c.transform.position, Player.m_localPlayer.transform.position) > CFCMod.FuelingDistance!.Value) continue;
                if(c.m_inventory == null) continue;
                var addedFuel = c.m_inventory.CountItems(fireplace.m_fuelItem.m_itemData.m_shared.m_name, -1);
                var clampedfuel = Clamp(addedFuel,0, Mathf.CeilToInt(fireplace.m_maxFuel));
                if (clampedfuel <= 0) continue;
                for (int i = 0; i < clampedfuel; i++)
                {
                    fireplace.m_nview.InvokeRPC("AddFuel");
                }
                c.m_inventory.RemoveItem(fireplace.m_fuelItem.m_itemData.m_shared.m_name, clampedfuel, -1);
                if(clampedfuel == Mathf.CeilToInt(fireplace.m_maxFuel))break;
            }
            _elapsedTime = 0;
        }
        
        private static ItemDrop.ItemData GetCookableFromChest(CookingStation cookingStation, Inventory inventory)
        {
            var t = cookingStation.FindCookableItem(inventory);
            if (t == null)
            {
                foreach (var c in Patches.ContainerAwakePatch.Continers)
                {
                    if (Player.m_localPlayer == null) break;
                    if(!CFCMod.ShouldSearchWardedAreas!.Value && !c.CheckAccess(Player.m_localPlayer.GetPlayerID()) && !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
                    if(Vector3.Distance(c.transform.position, Player.m_localPlayer.transform.position) > CFCMod.FuelingDistance!.Value)continue;
                    if(c.m_inventory == null) continue;
                    t = cookingStation.FindCookableItem(c.m_inventory);
                    if (t != null) return t;
                }
            }
            return t!;
        }
        #endregion
    }
}

