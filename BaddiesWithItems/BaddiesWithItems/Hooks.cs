﻿using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace BaddiesWithItems
{
    public static class Hooks
    {
        private static System.Random rand = new System.Random();

        // Enemies w/ items hook
        public static void baddiesItems()
        {
            IL.RoR2.CombatDirector.AttemptSpawnOnTarget += il =>
            {
                int locComponent = 0;
                var c = new ILCursor(il);
                c.GotoNext(
                    MoveType.After,
                    i => i.MatchCallvirt("UnityEngine.GameObject", "GetComponent"),
                    i => i.MatchStloc(out locComponent));
                
                c.Emit(OpCodes.Ldloc, locComponent);
                c.EmitDelegate<Action<CharacterMaster>>(component =>
                {
                    // this is it
                    if (Run.instance.stageClearCount >= EnemiesWithItems.StageReq.Value)
                    {
                        CharacterMaster player = PlayerCharacterMasterController.instances[rand.Next(0, Run.instance.livingPlayerCount)].master;
                        component.inventory.CopyItemsFrom(player.inventory);
                        EnemiesWithItems.checkConfig(component.inventory, player);
                    }
                });
            };
        }

        // Enemies drop hook
        public static void enemiesDrop()
        {
            On.RoR2.DeathRewards.OnKilledServer += (orig, self, damageInfo) =>
            {
                orig(self, damageInfo);
                CharacterBody enemy = self.GetComponent<CharacterBody>();
                if (EnemiesWithItems.DropItems.Value && Util.CheckRoll(EnemiesWithItems.ConfigToFloat(EnemiesWithItems.DropChance.Value), 0f, null) && enemy.master.teamIndex.Equals(TeamIndex.Monster))
                {
                    Inventory inventory = enemy.master.inventory;
                    List<PickupIndex> tier1Inventory = new List<PickupIndex>();
                    List<PickupIndex> tier2Inventory = new List<PickupIndex>();
                    List<PickupIndex> tier3Inventory = new List<PickupIndex>();
                    List<PickupIndex> lunarTierInventory = new List<PickupIndex>();
                    foreach (ItemIndex item in ItemCatalog.allItems)
                    {
                        if (EnemiesWithItems.Tier1Items.Value && ItemCatalog.tier1ItemList.Contains(item) && inventory.GetItemCount(item) > 0)
                        {
                            tier1Inventory.Add(new PickupIndex(item));
                        }
                        else if (EnemiesWithItems.Tier2Items.Value && ItemCatalog.tier2ItemList.Contains(item) && inventory.GetItemCount(item) > 0)
                        {
                            tier2Inventory.Add(new PickupIndex(item));
                        }
                        else if (EnemiesWithItems.Tier3Items.Value && ItemCatalog.tier3ItemList.Contains(item) && inventory.GetItemCount(item) > 0)
                        {
                            tier3Inventory.Add(new PickupIndex(item));
                        }
                        else if (EnemiesWithItems.LunarItems.Value && ItemCatalog.lunarItemList.Contains(item) && inventory.GetItemCount(item) > 0)
                        {
                            lunarTierInventory.Add(new PickupIndex(item));
                        }
                    }
                    WeightedSelection<List<PickupIndex>> weightedSelection = new WeightedSelection<List<PickupIndex>>(8);
                    if (EnemiesWithItems.Tier1Items.Value)
                    {
                        weightedSelection.AddChoice(tier1Inventory, 0.9f);
                    }
                    if (EnemiesWithItems.Tier2Items.Value)
                    {
                        weightedSelection.AddChoice(tier2Inventory, 0.1f);
                    }
                    if (EnemiesWithItems.Tier3Items.Value)
                    {
                        weightedSelection.AddChoice(tier3Inventory, 0.05f);
                    }
                    if (EnemiesWithItems.LunarItems.Value)
                    {
                        weightedSelection.AddChoice(lunarTierInventory, 0.01f);
                    }
                    List<PickupIndex> list = weightedSelection.Evaluate(Run.instance.treasureRng.nextNormalizedFloat);
                    if (list.Count == 0)
                    {
                        return;
                    }
                    PickupDropletController.CreatePickupDroplet(list[Run.instance.treasureRng.RangeInt(0, list.Count)], self.transform.position + Vector3.up * 1.5f, Vector3.up * 20f + self.transform.forward * 2f);
                }
            };

        }
    }
}
