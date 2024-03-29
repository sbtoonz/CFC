﻿using System.Collections.Generic;
using System;
using System.Linq;
using HarmonyLib;
using System.Reflection.Emit;
using UnityEngine;
using Object = UnityEngine.Object;

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
        [HarmonyPriority(Priority.VeryHigh)]
        [HarmonyWrapSafe]
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
        [HarmonyPriority(Priority.VeryHigh)]
        [HarmonyWrapSafe]
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
        [HarmonyPriority(Priority.VeryHigh)]
        [HarmonyWrapSafe]
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
        [HarmonyPriority(Priority.VeryHigh)]
        [HarmonyWrapSafe]
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
                    .RemoveInstructions(10)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_2))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_3))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_3))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyTranspilers), nameof(RemoveItemsFromChests))))
                    .InstructionEnumeration();

            }
        }

        [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.UpdateFireplace))]
        [HarmonyPriority(Priority.VeryHigh)]
        [HarmonyWrapSafe]
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
        [HarmonyWrapSafe]
        [HarmonyPriority(Priority.First)]
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
                    .Advance(1)
                    .RemoveInstructions(6)
                    .MatchForward(
                        useEnd: false,
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Inventory), nameof(Inventory.HaveItem))))
                    .RemoveInstruction()
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Player), nameof(Player.GetInventory))),
                        new CodeInstruction(OpCodes.Stloc,0),
                        new CodeInstruction(OpCodes.Ldloca, 0),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(
                            OpCodes.Callvirt, AccessTools.Method(typeof(HarmonyTranspilers), nameof(RemoveFuelFromChest))))
                    .InstructionEnumeration();
            }
        }

        #region CookingStation
        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnInteract))]
        [HarmonyPriority(Priority.VeryHigh)]
        [HarmonyWrapSafe]
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
        
        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnAddFuelSwitch))]
        [HarmonyPriority(Priority.VeryHigh)]
        [HarmonyWrapSafe]
        public static class CookingOnAddFuelTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> CookingAddFuel(IEnumerable<CodeInstruction> instructions)
            {
                return  new CodeMatcher(instructions)
                    .MatchForward(useEnd: false,
                        new CodeMatch(OpCodes.Ldarg_2),
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Humanoid), nameof(Humanoid.GetInventory))),
                        new CodeMatch(OpCodes.Ldarg_0)
                    )
                    .Advance(3)
                    .RemoveInstructions(6)
                    .InsertAndAdvance(Transpilers.EmitDelegate<Func<Inventory, CookingStation,bool>>(CookingStationAddFuelHook))
                    .InstructionEnumeration();
            }
        }
        #endregion

        #region Fermenter
        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.FindCookableItem))]
        [HarmonyPriority(Priority.VeryHigh)]
        [HarmonyWrapSafe]
        public static class FermenterInteractTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> FermenterInteract(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(useEnd: false,
                        new CodeMatch(OpCodes.Ldc_I4_M1),
                        new CodeMatch(OpCodes.Ldc_I4_0))
                    .Advance(3)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_1))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_1))
                    .InsertAndAdvance(Transpilers.EmitDelegate<Func<Fermenter,Inventory, Fermenter.ItemConversion, ItemDrop.ItemData>>(GetFermentableFromChest))
                    .InstructionEnumeration();
            }

            
        }
        #endregion

        #region Smelter

        [HarmonyPatch(typeof(Smelter),nameof(Smelter.OnAddFuel))]
        [HarmonyPriority(Priority.VeryHigh)]
        [HarmonyWrapSafe]
        public static class OnSmeltFuelTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> SmeltFuel(IEnumerable<CodeInstruction> instructions)
            {
                var cm = new CodeMatcher(instructions)
                    .MatchForward(useEnd: false,
                        new CodeMatch(OpCodes.Ldarg_2),
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Humanoid),nameof(Humanoid.GetInventory))))
                    .Advance(3)
                    .RemoveInstructions(6)
                    .InsertAndAdvance(Transpilers.EmitDelegate<Func<Inventory,Smelter, bool>>(SmelterAddFuel))
                    .InstructionEnumeration();
                return cm;
            }
            

           
        }
        
        [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddOre))]
        [HarmonyPriority(Priority.VeryHigh)]
        [HarmonyWrapSafe]
        public static class OnSmeltAddOreTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> SmelterAddOre(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(useEnd: false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldarg_2),
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Humanoid), nameof(Humanoid.GetInventory)))
                    )
                    .Advance(3)
                    .RemoveInstruction()
                    .InsertAndAdvance(Transpilers.EmitDelegate<Func<Smelter,Inventory, ItemDrop.ItemData>>(GetSmeltableFromChest))
                    .InstructionEnumeration();
            }
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.UpdateSmelter))]
        [HarmonyPriority(Priority.VeryHigh)]
        [HarmonyWrapSafe]
        public static class UpdateSmelterTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> SmelterUpdate(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(useEnd: false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Smelter), nameof(Smelter.GetQueuedOre))),
                        new CodeMatch(OpCodes.Ldstr)
                    )
                    .Advance(1)
                    .RemoveInstructions(3)
                    .InsertAndAdvance(Transpilers.EmitDelegate<Func<Smelter, bool>>(AutoFeedSmelter))
                    .Advance(7)
                    .RemoveInstruction()
                    .InsertAndAdvance(Transpilers.EmitDelegate<Func<Smelter, float>>(AutoFuelSmelter))
                    .InstructionEnumeration();
            }
        }


        [HarmonyPatch(typeof(Smelter), nameof(Smelter.Spawn))]
        [HarmonyPriority(Priority.VeryHigh)]
        [HarmonyWrapSafe]
        public static class SmelterAutoDepositTranspiler
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> SmelterDeposit(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(useEnd: false,
                        new CodeMatch(OpCodes.Pop),
                        new CodeMatch(OpCodes.Ldloc_0)
                    )
                    .Advance(1)
                    .RemoveInstructions(16)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_2))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_1))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyTranspilers), nameof(SmelterDepositHook))))
                    .InstructionEnumeration();
            }

           
        }

        #endregion

        #endregion
        #region Transpiler Methods
        private static void RemoveItemsFromChests(Player? player, Piece.Requirement item, int amount, int itemQuality)
        {
            if(player is not null && player.NoCostCheat()) return;
            var inventoryAmount = player!.m_inventory.CountItems(item.m_resItem.m_itemData.m_shared.m_name);
            if(inventoryAmount >= amount) player.m_inventory.RemoveItem(item.m_resItem.m_itemData.m_shared.m_name, amount, itemQuality);
            amount -= inventoryAmount;
            if (amount <= 0) return;

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var c in Patches.ContainerAwakePatch.Continers)
            {
                if(c.m_nview == null) continue;
                if(!CFCMod.ShouldSearchWardedAreas!.Value && c.m_privacy != Container.PrivacySetting.Public  && !c.CheckAccess(player.GetPlayerID()) && !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
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
                if (c.gameObject.name.StartsWith("Container_"))
                {
                    if (Vector3.Distance(player.transform.position, c.transform.position) >
                        CFCMod.ChestDistance?.Value) continue;
                    if(c.m_inventory == null) continue;
                    if (c.m_inventory.HaveItem(item.m_resItem.m_itemData.m_shared.m_name))
                    {
                        i += c.m_inventory.CountItems(item.m_resItem.m_itemData.m_shared.m_name);
                    }
                }
                if(c.m_nview == null) continue;
                if (c.m_piece == null || c.m_piece.m_creator == -1) continue;
                if(!CFCMod.ShouldSearchWardedAreas!.Value && c.m_privacy != Container.PrivacySetting.Public  && !c.CheckAccess(player.GetPlayerID()) && !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
                if (Vector3.Distance(player.transform.position, c.transform.position) >
                    CFCMod.ChestDistance?.Value) continue;
                if(c.m_inventory == null) continue;
                if (c.m_inventory.HaveItem(item.m_resItem.m_itemData.m_shared.m_name))
                {
                    i += c.m_inventory.CountItems(item.m_resItem.m_itemData.m_shared.m_name);
                }
            }
            i += fromInventory;
            return i;
        }
        
        private static bool RemoveFuelFromChest(ref Inventory inventory, Fireplace fireplace)
        {
            if (inventory == null) inventory = Player.m_localPlayer.GetInventory();
            if (inventory.HaveItem(fireplace.m_fuelItem.m_itemData.m_shared.m_name)) return true;
            foreach (var c in Patches.ContainerAwakePatch.Continers)
            {
                if (Player.m_localPlayer == null) break;
                if(!CFCMod.ShouldSearchWardedAreas!.Value && c.m_privacy != Container.PrivacySetting.Public  && !c.CheckAccess(Player.m_localPlayer.GetPlayerID()) && !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
                if(Vector3.Distance(c.transform.position, fireplace.transform.position) > CFCMod.FuelingDistance!.Value)continue;
                if(c.m_inventory == null) continue;
                if(c.m_nview == null) continue;
                var t = (ChestType)c.m_nview.GetZDO().GetInt("ChestType");
                if(t != ChestType.Fire) continue;
                if (c.m_inventory.HaveItem(fireplace.m_fuelItem.m_itemData.m_shared.m_name))
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
            if (currentZdoFuel <= CFCMod.LowFuelValue!.Value || _elapsedTime <= CFCMod.SearchInterval!.Value) return;
            foreach (var c in Patches.ContainerAwakePatch.Continers)
            {
                if(c == null) continue;
                if(c.m_nview == null) continue;
                if(Player.m_localPlayer == null)break;
                if(!CFCMod.ShouldSearchWardedAreas!.Value && c.m_privacy != Container.PrivacySetting.Public  && !c.CheckAccess(Player.m_localPlayer.GetPlayerID()) && 
                   !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
                if(Vector3.Distance(c.transform.position, fireplace.transform.position) > CFCMod.FuelingDistance!.Value) continue;
                if(c.m_inventory == null) continue;
                var z = c.m_nview.GetZDO();
                ChestType t = ChestType.None;
                if (z != null) t = (ChestType)z.GetInt("ChestType");
                if(t != ChestType.Fire) continue;
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

        #region CookingStations

        private static ItemDrop.ItemData GetCookableFromChest(CookingStation cookingStation, Inventory inventory)
        {
            var t = cookingStation.FindCookableItem(inventory);
            if (t != null) return t!;
            foreach (var c in from c in Patches.ContainerAwakePatch.Continers.TakeWhile(c => Player.m_localPlayer != null) 
                     where CFCMod.ShouldSearchWardedAreas!.Value ||
                           c.CheckAccess(Player.m_localPlayer.GetPlayerID()) ||
                           PrivateArea.CheckAccess(c.transform.position, 0, false, true) 
                     where !(Vector3.Distance(c.transform.position, cookingStation.transform.position) > CFCMod.FuelingDistance!.Value) 
                     where c.m_inventory != null select c)
            {
                t = cookingStation.FindCookableItem(c.m_inventory);
                if (t != null)
                {
                    c.m_inventory.RemoveItem(t.m_shared.m_name, 1, -1);
                    return t;
                }
            }
            return t!;
        }
        private static bool CookingStationAddFuelHook(Inventory inventory, CookingStation station)
        {
            foreach (var c in Patches.ContainerAwakePatch.Continers)
            {
                if (Player.m_localPlayer == null) break;
                if (c.m_nview == null) continue;
                if (!CFCMod.ShouldSearchWardedAreas!.Value && c.m_privacy != Container.PrivacySetting.Public &&
                    !c.CheckAccess(Player.m_localPlayer.GetPlayerID()) &&
                    !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
                if (Vector3.Distance(c.transform.position, station.transform.position) >
                    CFCMod.FuelingDistance!.Value) continue;
                if (c.m_inventory == null) continue;
                var i = c.m_inventory.CountItems(station.m_fuelItem.m_itemData.m_shared.m_name);
                if (i > 0)
                {
                    c.m_inventory.RemoveItem(station.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                    return true;
                }
                 
            }

            return inventory.HaveItem(station.m_fuelItem.m_itemData.m_shared.m_name);
        }
        

        #endregion
        /*private static ItemDrop.ItemData CookingAddFoodHook(ItemDrop.ItemData item, CookingStation cookingStation)
        {
            foreach (var c in Patches.ContainerAwakePatch.Continers)
            {
                if (Player.m_localPlayer == null) break;
                if (c.m_nview == null) continue;
                if (!CFCMod.ShouldSearchWardedAreas!.Value && c.m_privacy != Container.PrivacySetting.Public &&
                    !c.CheckAccess(Player.m_localPlayer.GetPlayerID()) &&
                    !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
                if (Vector3.Distance(c.transform.position, Player.m_localPlayer.transform.position) >
                    CFCMod.FuelingDistance!.Value) continue;
                if (c.m_inventory == null) continue;
                var id = cookingStation.FindCookableItem(c.m_inventory);
                if (id != null)
                {
                    c.m_inventory.RemoveItem(id.m_shared.m_name, 1, -1);
                    return id;
                }
            }
            return item;
        }*/
        private static ItemDrop.ItemData GetFermentableFromChest(Fermenter fermenter, Inventory inventory, Fermenter.ItemConversion conversionItem)
        {
            switch (inventory.HaveItem(conversionItem.m_from.m_itemData.m_shared.m_name))
            {
                case true:
                    var tx = inventory.GetItem(conversionItem.m_from.m_itemData.m_shared.m_name);
                    if (tx == null) return null!;
                    if (fermenter.GetStatus() != Fermenter.Status.Empty || !fermenter.IsItemAllowed(tx)) return null!;
                    inventory.RemoveOneItem(tx);
                    fermenter.m_nview.InvokeRPC("AddItem", tx.m_dropPrefab.name);
                    return tx;
                case false:
                    foreach (var c in Patches.ContainerAwakePatch.Continers)
                    {
                        if (c == null) continue;
                        if (c.m_nview == null) continue;
                        if (Player.m_localPlayer == null) break;
                        if (!CFCMod.ShouldSearchWardedAreas!.Value && c.m_privacy != Container.PrivacySetting.Public &&
                            !c.CheckAccess(Player.m_localPlayer.GetPlayerID()) &&
                            !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
                        if (Vector3.Distance(c.transform.position, fermenter.transform.position) >
                            CFCMod.FuelingDistance!.Value) continue;
                        if (c.m_inventory == null) continue;
                        var t = c.m_inventory.GetItem(conversionItem.m_from.m_itemData.m_shared.m_name);
                        if (t == null) continue;
                        if (fermenter.GetStatus() != Fermenter.Status.Empty || !fermenter.IsItemAllowed(t)) continue;
                        c.m_inventory.RemoveOneItem(t);
                        fermenter.m_nview.InvokeRPC("AddItem", t.m_dropPrefab.name);
                        return t;
                    }
                    break;
            }

            return null!;
        }

        #region Smelter

        private static ItemDrop.ItemData GetSmeltableFromChest(Smelter smelter, Inventory inventory)
        {
            var t = smelter.FindCookableItem(inventory);
            if (t == null)
            {
                foreach (var c in Patches.ContainerAwakePatch.Continers)
                {
                    if(c == null) continue;
                    if(c.m_nview == null) continue;
                    if(Player.m_localPlayer == null)break;
                    if(!CFCMod.ShouldSearchWardedAreas!.Value && c.m_privacy != Container.PrivacySetting.Public  && !c.CheckAccess(Player.m_localPlayer.GetPlayerID()) && 
                       !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
                    if(Vector3.Distance(c.transform.position, smelter.transform.position) > CFCMod.SmelterFuelDistnace!.Value) continue;
                    if(c.m_inventory == null) continue;
                    var type = (ChestType)c.m_nview.GetZDO().GetInt("ChestType");
                    switch (smelter.gameObject.name)
                    {
                        case "smelter(Clone)":
                            if (type == ChestType.Smelter)
                            {
                                t = smelter.FindCookableItem(c.m_inventory);
                                if (t != null)
                                {
                                    if (smelter.GetQueueSize() >= smelter.m_maxOre)
                                    {
                                        return t;
                                    }
                                    c.m_inventory.RemoveOneItem(t);
                                    return t;
                                }
                            }
                            break;
                        case "windmill(Clone)":
                            if (type == ChestType.Smelter)
                            {
                                t = smelter.FindCookableItem(c.m_inventory);
                                if (t != null)
                                {
                                    if (smelter.GetQueueSize() >= smelter.m_maxOre)
                                    {
                                        return t;
                                    }
                                    c.m_inventory.RemoveOneItem(t);
                                    return t;
                                }
                            }
                            break;
                        case "blastfurnace(Clone)":
                            if (type == ChestType.BlastFurnace)
                            {
                                t = smelter.FindCookableItem(c.m_inventory);
                                if (t != null)
                                {
                                    if (smelter.GetQueueSize() >= smelter.m_maxOre)
                                    {
                                        return t;
                                    }
                                    c.m_inventory.RemoveOneItem(t);
                                    return t;
                                }
                            }
                            break;
                        case "JF_KilnReimagined(Clone)":
                            if (type == ChestType.BlastFurnace)
                            {
                                t = smelter.FindCookableItem(c.m_inventory);
                                if (t != null)
                                {
                                    if (smelter.GetQueueSize() >= smelter.m_maxOre)
                                    {
                                        return t;
                                    }
                                    c.m_inventory.RemoveOneItem(t);
                                    return t;
                                }
                            }
                            break;
                        case "charcoal_kiln(Clone)":
                            if (type == ChestType.Kiln)
                            {
                                t = smelter.FindCookableItem(c.m_inventory);
                                if (t != null)
                                {
                                    if (smelter.GetQueueSize() >= smelter.m_maxOre)
                                    {
                                        return t;
                                    }
                                    c.m_inventory.RemoveOneItem(t);
                                    return t;
                                }
                            }
                            break;
                        case "piece_sapcollector(Clone)":
                            if (type == ChestType.SapCollector)
                            {
                                t = smelter.FindCookableItem(c.m_inventory);
                                if (t != null)
                                {
                                    if (smelter.GetQueueSize() >= smelter.m_maxOre)
                                    {
                                        return t;
                                    }
                                    c.m_inventory.RemoveOneItem(t);
                                    return t;
                                }
                            }
                            break;
                        case "eitrrefinery(Clone)":
                            if (type == ChestType.EitrRefinery)
                            {
                                t = smelter.FindCookableItem(c.m_inventory);
                                if (t != null)
                                {
                                    if (smelter.GetQueueSize() >= smelter.m_maxOre)
                                    {
                                        return t;
                                    }
                                    c.m_inventory.RemoveOneItem(t);
                                    return t;
                                }
                            }
                            break;
                        default:
                            if (type == ChestType.None)
                            {
                                t = smelter.FindCookableItem(c.m_inventory);
                                if (t != null)
                                {
                                    if (smelter.GetQueueSize() >= smelter.m_maxOre)
                                    {
                                        return t;
                                    }
                                    c.m_inventory.RemoveOneItem(t);
                                    return t;
                                }
                            }
                            break;
                    }
                    
                }
            }

            return t!;
        }
        private static bool SmelterAddFuel(Inventory inventory,Smelter smelter)
        {
            if (!inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
            {
                foreach (var c in Patches.ContainerAwakePatch.Continers)
                {
                    if(c == null) continue;
                    if(c.m_nview == null) continue;
                    if(Player.m_localPlayer == null)break;
                    if(!CFCMod.ShouldSearchWardedAreas!.Value && c.m_privacy != Container.PrivacySetting.Public  && !c.CheckAccess(Player.m_localPlayer.GetPlayerID()) && 
                       !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
                    if(Vector3.Distance(c.transform.position, smelter.transform.position) > CFCMod.SmelterFuelDistnace!.Value) continue;
                    if(c.m_inventory == null) continue;
                    var type = (ChestType)c.m_nview.GetZDO().GetInt("ChestType");
                    switch (smelter.gameObject.name)
                    {
                        case "smelter(Clone)":
                            if (type == ChestType.Smelter)
                            {
                                if (c.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                {
                                    c.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                    return true;
                                }
                            }
                            break;
                        case "windmill(Clone)":
                            if (type == ChestType.Smelter)
                            {
                                if (c.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                {
                                    c.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                    return true;
                                }
                            }
                            break;
                        case "blastfurnace(Clone)":
                            if (type == ChestType.BlastFurnace)
                            {
                                if (c.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                {
                                    c.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                    return true;
                                }
                            }
                            break;
                        case "charcoal_kiln(Clone)":
                            if (type == ChestType.Kiln)
                            {
                                if (c.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                {
                                    c.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                    return true;
                                }
                            }
                            break;
                        case "JF_KilnReimagined(Clone)":
                            if (type == ChestType.Kiln)
                            {
                                if (c.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                {
                                    c.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                    return true;
                                }
                            }
                            break;
                        case "piece_sapcollector(Clone)":
                            if (type == ChestType.SapCollector)
                            {
                                if (c.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                {
                                    c.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                    return true;
                                }
                            }
                            break;
                        case "eitrrefinery(Clone)":
                            if (type == ChestType.EitrRefinery)
                            {
                                if (c.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                {
                                    c.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                    return true;
                                }
                            }
                            break;
                        default:
                            if (type == ChestType.None)
                            {
                                if (c.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                {
                                    c.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                    return true;
                                }
                            }
                            break;
                    }
                    
                } 
            }
            return inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name);
        }
        
        private static float _elapsedTime2 = 0f;
        private static bool AutoFeedSmelter(Smelter smelter)
        {
            _elapsedTime2 += Time.deltaTime;
            var q = smelter.GetQueueSize();
            if(q >= smelter.m_maxOre)return smelter.GetQueuedOre() == "";
            if(q <= CFCMod.LowSmelterOreValue!.Value)return smelter.GetQueuedOre() == "";
            if(_elapsedTime2 <= CFCMod.SearchInterval!.Value)return smelter.GetQueuedOre() == "";
            foreach (var container in Patches.ContainerAwakePatch.Continers)
            {
                if (container == null) continue;
                if (container.m_nview == null) continue;
                if (Player.m_localPlayer == null) break;
                if (!CFCMod.ShouldSearchWardedAreas!.Value && container.m_privacy != Container.PrivacySetting.Public &&
                    !container.CheckAccess(Player.m_localPlayer.GetPlayerID()) &&
                    !PrivateArea.CheckAccess(container.transform.position, 0, false, true)) continue;
                if (Vector3.Distance(container.transform.position, smelter.transform.position) >
                    CFCMod.SmelterFuelDistnace!.Value) continue;
                if (container.m_inventory == null) continue;
                var id = smelter.FindCookableItem(container.m_inventory);
                var type = (ChestType)container.m_nview.GetZDO().GetInt("ChestType");
                if (id == null) continue;
                var countItems = container.m_inventory.CountItems(id.m_shared.m_name);
                if(countItems <=0)continue;
                var toAdd = smelter.m_maxOre - smelter.GetQueueSize();
                if (countItems > toAdd)
                    countItems = toAdd;
                for (int i = 0; i < countItems; i++)
                {
                    switch (smelter.gameObject.name)
                        {
                            case "smelter(Clone)":
                                if (type == ChestType.Smelter)
                                {
                                    container.m_inventory.RemoveItem(id.m_shared.m_name, 1, -1);
                                    smelter.m_nview.InvokeRPC("AddOre", id.m_dropPrefab.name);
                                    smelter.m_addedOreTime = Time.time;
                                }
                                break;
                            case "windmill(Clone)":
                                if (type == ChestType.Windmill)
                                {
                                    container.m_inventory.RemoveItem(id.m_shared.m_name, 1, -1);
                                    smelter.m_nview.InvokeRPC("AddOre", id.m_dropPrefab.name);
                                    smelter.m_addedOreTime = Time.time;
                                }
                                break;
                            case "piece_spinningwheel(Clone)":
                                if (type == ChestType.SpinningWheel)
                                {
                                    container.m_inventory.RemoveItem(id.m_shared.m_name, 1, -1);
                                    smelter.m_nview.InvokeRPC("AddOre", id.m_dropPrefab.name);
                                    smelter.m_addedOreTime = Time.time;
                                }
                                break;
                            case "blastfurnace(Clone)":
                                if (type == ChestType.BlastFurnace)
                                {
                                    container.m_inventory.RemoveItem(id.m_shared.m_name, 1, -1);
                                    smelter.m_nview.InvokeRPC("AddOre", id.m_dropPrefab.name);
                                    smelter.m_addedOreTime = Time.time;
                                }
                                break;
                            case "charcoal_kiln(Clone)":
                                if (type == ChestType.Kiln)
                                {
                                    container.m_inventory.RemoveItem(id.m_shared.m_name, 1, -1);
                                    smelter.m_nview.InvokeRPC("AddOre", id.m_dropPrefab.name);
                                    smelter.m_addedOreTime = Time.time;
                                }
                                break;
                            case "JF_KilnReimagined(Clone)":
                                if (type == ChestType.Kiln)
                                {
                                    container.m_inventory.RemoveItem(id.m_shared.m_name, 1, -1);
                                    smelter.m_nview.InvokeRPC("AddOre", id.m_dropPrefab.name);
                                    smelter.m_addedOreTime = Time.time;
                                }
                                break;
                            case "piece_sapcollector(Clone)":
                                if (type == ChestType.SapCollector)
                                {
                                    container.m_inventory.RemoveItem(id.m_shared.m_name, 1, -1);
                                    smelter.m_nview.InvokeRPC("AddOre", id.m_dropPrefab.name);
                                    smelter.m_addedOreTime = Time.time;
                                }
                                break;
                            case "eitrrefinery(Clone)":
                                if (type == ChestType.EitrRefinery)
                                {
                                    container.m_inventory.RemoveItem(id.m_shared.m_name, 1, -1);
                                    smelter.m_nview.InvokeRPC("AddOre", id.m_dropPrefab.name);
                                    smelter.m_addedOreTime = Time.time;
                                }
                                break;
                            default:
                                if (type == ChestType.None)
                                {
                                    container.m_inventory.RemoveItem(id.m_shared.m_name, 1, -1);
                                    smelter.m_nview.InvokeRPC("AddOre", id.m_dropPrefab.name);
                                    smelter.m_addedOreTime = Time.time;
                                }
                                break;
                        }
                    
                }
                _elapsedTime2 = 0;
                return false;
            }
            return smelter.GetQueuedOre() == "";
        }
        
        private static float AutoFuelSmelter(Smelter smelter)
        {
            int toAdd = smelter.m_maxFuel - Mathf.CeilToInt(smelter.GetFuel());

            if (smelter.GetFuel() <= CFCMod.LowSmelterFuelValue!.Value) return smelter.GetFuel();
            
            if (smelter.GetFuel() > CFCMod.LowSmelterFuelValue!.Value || smelter.GetFuel() == 0)
            {
                foreach (var container in Patches.ContainerAwakePatch.Continers)
                {
                    if(Player.m_localPlayer == null)break;
                    if(container.m_nview == null) continue;
                    if(!CFCMod.ShouldSearchWardedAreas!.Value && container.m_privacy != Container.PrivacySetting.Public  &&!container.CheckAccess(Player.m_localPlayer.GetPlayerID()) && 
                       !PrivateArea.CheckAccess(container.transform.position, 0, false, true)) continue;
                    if(Vector3.Distance(container.transform.position, smelter.transform.position) > CFCMod.SmelterFuelDistnace!.Value) continue;
                    if(container.m_inventory == null) continue;
                    if(container.m_nview == null)continue;
                    var type = (ChestType)container.m_nview.GetZDO().GetInt("ChestType");
                    for (int i = 0; i < toAdd; i++)
                    {
                        switch (smelter.gameObject.name)
                        {
                            case "smelter(Clone)":
                                if (type == ChestType.Smelter)
                                {
                                    if (container.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                    {
                                        container.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                        smelter.m_nview.InvokeRPC("AddFuel");
                                    }
                                }
                                break;
                            case "windmill(Clone)":
                                if (type == ChestType.Windmill)
                                {
                                    if (container.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                    {
                                        container.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                        smelter.m_nview.InvokeRPC("AddFuel");
                                    }
                                }
                                break;
                            case "piece_spinningwheel(Clone)":
                                if (type == ChestType.SpinningWheel)
                                {
                                    if (container.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                    {
                                        container.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                        smelter.m_nview.InvokeRPC("AddFuel");
                                    }
                                }
                                break;
                            case "blastfurnace(Clone)":
                                if (type == ChestType.BlastFurnace)
                                {
                                    if (container.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                    {
                                        container.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                        smelter.m_nview.InvokeRPC("AddFuel");
                                    }
                                }
                                break;
                            case "charcoal_kiln(Clone)":
                                if (type == ChestType.Kiln)
                                {
                                    if (container.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                    {
                                        container.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                        smelter.m_nview.InvokeRPC("AddFuel");
                                    }
                                }
                                break;
                            case "JF_KilnReimagined(Clone)":
                                if (type == ChestType.Kiln)
                                {
                                    if (container.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                    {
                                        container.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                        smelter.m_nview.InvokeRPC("AddFuel");
                                    }
                                }
                                break;
                            case "piece_sapcollector(Clone)":
                                if (type == ChestType.SapCollector)
                                {
                                    if (container.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                    {
                                        container.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                        smelter.m_nview.InvokeRPC("AddFuel");
                                    }
                                }
                                break;
                            case "eitrrefinery(Clone)":
                                if (type == ChestType.EitrRefinery)
                                {
                                    if (container.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                    {
                                        container.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                        smelter.m_nview.InvokeRPC("AddFuel");
                                    }
                                }
                                break;
                            default:
                                
                                if (type == ChestType.None)
                                {
                                    if (container.m_inventory.HaveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name))
                                    {
                                        container.m_inventory.RemoveItem(smelter.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
                                        smelter.m_nview.InvokeRPC("AddFuel");
                                    }
                                }
                                break;
                        }
                    }
                    
                }
            }
            return smelter.GetFuel();
        }
        
        public static void SmelterDepositHook(Smelter smelter, int stack, string ore)
        {
            bool spawnedItemFromSwitch = false;
            foreach (var c in Patches.ContainerAwakePatch.Continers)
            {
                if(c == null) continue;
                if(Player.m_localPlayer == null)break;
                if(c.m_nview == null) continue;
                if(!CFCMod.ShouldSearchWardedAreas!.Value && c.m_privacy != Container.PrivacySetting.Public  &&!c.CheckAccess(Player.m_localPlayer.GetPlayerID()) && 
                   !PrivateArea.CheckAccess(c.transform.position, 0, false, true)) continue;
                if(Vector3.Distance(c.transform.position, smelter.transform.position) > CFCMod.FuelingDistance!.Value) continue;
                if(c.m_inventory == null) continue;
                if(c.m_nview == null)continue;
                var type = (ChestType)c.m_nview.GetZDO().GetInt("ChestType");
                var itemC = smelter.GetItemConversion(ore);
                if(itemC ==null)return;
               
                switch (smelter.gameObject.name)
                {
                    case "smelter(Clone)":
                        if (type == ChestType.Smelter)
                        {
                            int i = -1;
                            i=c.m_inventory.FindFreeStackSpace(itemC.m_to.m_itemData.m_shared.m_name,0);
                            if(c.m_inventory.HaveItem(itemC.m_to.m_itemData.m_shared.m_name) && i != -1 || c.m_inventory.HaveEmptySlot() )
                            {
                                c.m_inventory.AddItem(itemC.m_to.gameObject, stack);
                                spawnedItemFromSwitch = true;
                                break;
                            }
                            Object
                                .Instantiate(itemC.m_to.gameObject, smelter.m_outputPoint.position,
                                    smelter.m_outputPoint.rotation).GetComponent<ItemDrop>().m_itemData.m_stack = stack;
                            spawnedItemFromSwitch = true;
                        }
                        break;
                    case "windmill(Clone)":
                        if (type == ChestType.Windmill)
                        {
                            int i = -1;
                            i=c.m_inventory.FindFreeStackSpace(itemC.m_to.m_itemData.m_shared.m_name,0);
                            if(c.m_inventory.HaveItem(itemC.m_to.m_itemData.m_shared.m_name) && i != -1 || c.m_inventory.HaveEmptySlot() )
                            {
                                c.m_inventory.AddItem(itemC.m_to.gameObject, stack);
                                spawnedItemFromSwitch = true;
                                break;
                            }
                            Object
                                .Instantiate(itemC.m_to.gameObject, smelter.m_outputPoint.position,
                                    smelter.m_outputPoint.rotation).GetComponent<ItemDrop>().m_itemData.m_stack = stack;
                            spawnedItemFromSwitch = true;
                        }
                        break;
                    case "piece_spinningwheel(Clone)":
                        if (type == ChestType.SpinningWheel)
                        {
                            int i = -1;
                            i=c.m_inventory.FindFreeStackSpace(itemC.m_to.m_itemData.m_shared.m_name,0);
                            if(c.m_inventory.HaveItem(itemC.m_to.m_itemData.m_shared.m_name) && i != -1 || c.m_inventory.HaveEmptySlot() )
                            {
                                c.m_inventory.AddItem(itemC.m_to.gameObject, stack);
                                spawnedItemFromSwitch = true;
                                break;
                            }
                            Object
                                .Instantiate(itemC.m_to.gameObject, smelter.m_outputPoint.position,
                                    smelter.m_outputPoint.rotation).GetComponent<ItemDrop>().m_itemData.m_stack = stack;
                            spawnedItemFromSwitch = true;
                        }
                        break;
                    case "blastfurnace(Clone)":
                        if (type == ChestType.BlastFurnace)
                        {
                            int i = -1;
                            i=c.m_inventory.FindFreeStackSpace(itemC.m_to.m_itemData.m_shared.m_name,0);
                            if(c.m_inventory.HaveItem(itemC.m_to.m_itemData.m_shared.m_name) && i != -1 || c.m_inventory.HaveEmptySlot() )
                            {
                                c.m_inventory.AddItem(itemC.m_to.gameObject, stack);
                                spawnedItemFromSwitch = true;
                                break;
                            }
                            spawnedItemFromSwitch = true;
                            Object
                                .Instantiate(itemC.m_to.gameObject, smelter.m_outputPoint.position,
                                    smelter.m_outputPoint.rotation).GetComponent<ItemDrop>().m_itemData.m_stack = stack;
                        }
                        break;
                    case "charcoal_kiln(Clone)":
                        if (type == ChestType.Kiln)
                        {
                            int i = -1;
                            i=c.m_inventory.FindFreeStackSpace(itemC.m_to.m_itemData.m_shared.m_name,0);
                            if(c.m_inventory.HaveItem(itemC.m_to.m_itemData.m_shared.m_name) && i != -1 || c.m_inventory.HaveEmptySlot() )
                            {
                                c.m_inventory.AddItem(itemC.m_to.gameObject, stack);
                                spawnedItemFromSwitch = true;
                                break;
                            }
                            spawnedItemFromSwitch = true;
                            Object
                                .Instantiate(itemC.m_to.gameObject, smelter.m_outputPoint.position,
                                    smelter.m_outputPoint.rotation).GetComponent<ItemDrop>().m_itemData.m_stack = stack;
                        }
                        break;
                    case "piece_sapcollector(Clone)":
                        if (type == ChestType.SapCollector)
                        {
                            int i = -1;
                            i=c.m_inventory.FindFreeStackSpace(itemC.m_to.m_itemData.m_shared.m_name,0);
                            if(c.m_inventory.HaveItem(itemC.m_to.m_itemData.m_shared.m_name) && i != -1 || c.m_inventory.HaveEmptySlot() )
                            {
                                c.m_inventory.AddItem(itemC.m_to.gameObject, stack);
                                spawnedItemFromSwitch = true;
                                break;
                            }
                            spawnedItemFromSwitch = true;
                            Object
                                .Instantiate(itemC.m_to.gameObject, smelter.m_outputPoint.position,
                                    smelter.m_outputPoint.rotation).GetComponent<ItemDrop>().m_itemData.m_stack = stack;
                            
                        }
                        break;
                    case "eitrrefinery(Clone)":
                        if (type == ChestType.EitrRefinery)
                        {
                            int i = -1;
                            i=c.m_inventory.FindFreeStackSpace(itemC.m_to.m_itemData.m_shared.m_name,0);
                            if(c.m_inventory.HaveItem(itemC.m_to.m_itemData.m_shared.m_name) && i != -1 || c.m_inventory.HaveEmptySlot() )
                            {
                                c.m_inventory.AddItem(itemC.m_to.gameObject, stack);
                                spawnedItemFromSwitch = true;
                                break;
                            }
                            spawnedItemFromSwitch = true;
                            Object
                                .Instantiate(itemC.m_to.gameObject, smelter.m_outputPoint.position,
                                    smelter.m_outputPoint.rotation).GetComponent<ItemDrop>().m_itemData.m_stack = stack;
                            
                        }
                        break;
                    case "JF_KilnReimagined(Clone)":
                        if (type == ChestType.Kiln)
                        {
                            int i = -1;
                            i=c.m_inventory.FindFreeStackSpace(itemC.m_to.m_itemData.m_shared.m_name,0);
                            if(c.m_inventory.HaveItem(itemC.m_to.m_itemData.m_shared.m_name) && i != -1 || c.m_inventory.HaveEmptySlot() )
                            {
                                c.m_inventory.AddItem(itemC.m_to.gameObject, stack);
                                spawnedItemFromSwitch = true;
                                break;
                            }
                            spawnedItemFromSwitch = true;
                            Object
                                .Instantiate(itemC.m_to.gameObject, smelter.m_outputPoint.position,
                                    smelter.m_outputPoint.rotation).GetComponent<ItemDrop>().m_itemData.m_stack = stack;
                        }
                        break;
                    default:
                        if (type == ChestType.None)
                        {
                            int i = -1;
                            i = c.m_inventory.FindFreeStackSpace(itemC.m_to.m_itemData.m_shared.m_name,0);
                            if (c.m_inventory.HaveItem(itemC.m_to.m_itemData.m_shared.m_name) && i != -1 ||
                                c.m_inventory.HaveEmptySlot())
                            {
                                c.m_inventory.AddItem(itemC.m_to.gameObject, stack);
                                spawnedItemFromSwitch = true;
                                break;
                            }
                        }
                        var o = Object
                            .Instantiate(itemC.m_to.gameObject, smelter.m_outputPoint.position,
                                smelter.m_outputPoint.rotation).GetComponent<ItemDrop>().m_itemData.m_stack = stack;
                        spawnedItemFromSwitch = true;
                        break;
                }
            } 
            var itemCd = smelter.GetItemConversion(ore);
            if(!spawnedItemFromSwitch)Object
                .Instantiate(itemCd.m_to.gameObject, smelter.m_outputPoint.position,
                    smelter.m_outputPoint.rotation).GetComponent<ItemDrop>().m_itemData.m_stack = stack;
            
        }

        #endregion
      
        #endregion
    }
}