using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.Items;
using PerfectRandom.Sulfur.Core.Weapons;
using PerfectRandom.Sulfur.Core.Stats;
using PerfectRandom.Sulfur.Core.UI.ItemDescription;
using TMPro;
using System.Collections.Generic;
using BepInEx.Configuration;
using PerfectRandom.Sulfur.Core.CharacterStats;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SulfurWeaponTiers
{
    [BepInPlugin("pseudotim.sulfur.weapontiers", ModName, ModVersion)]
    public sealed class WeaponTiersPlugin : BaseUnityPlugin
    {
        public const string ModName = "Weapon Tiers";
        public const string ModVersion = "1.0.0";

        internal static double[] Weights = WeaponTierSystem.DefaultWeights;
        private Harmony harmony;

        private void Awake()
        {
            ConfigEntry<string> weights = Config.Bind("Drops", "Weights", "70,20,7,2.5,0.5", "Relative weights: (Standard, Blessed, Hallowed, Sacred, Divine)");
            
            try
            {
                Weights = weights.Value.Split(',').Select(s => double.Parse(s.Trim(), CultureInfo.InvariantCulture)).ToArray();
                WeaponTierSystem.Roll(0, Weights);
            }
            catch(Exception ex)when (ex is FormatException || ex is OverflowException || ex is ArgumentException)
            {
                Weights = WeaponTierSystem.DefaultWeights;
                Logger.LogWarning("Invalid tier weights, using defaults!" + ex.Message);
            }

            harmony = new Harmony("pseudotim.sulfur.weapontiers");

            bool glintEnabled = Config.Bind("Visuals", "GlintEnabled", true, "Show a subtle colored animated shimmer effect on tiered weapons").Value;
            float glintStrength = Config.Bind("Visuals", "GlintStrength", 0.39f, new ConfigDescription("Glint brightness", new AcceptableValueRange<float>(0f, 0.5f))).Value;
            float glintSpeed = Config.Bind("Visuals", "GlintSpeed", 0.3f, new ConfigDescription("Glint shimmer speed", new AcceptableValueRange<float>(0.02f, 0.5f))).Value;
            float glintIntensityScale = Config.Bind("Visuals", "GlintIntensityScale", 0.5f, new ConfigDescription("Overall glint intensity multiplier for held weapons, pickups, and inventory icons. (0 disables brightness, 1 uses full GlintStrength)", new AcceptableValueRange<float>(0f, 1f))).Value;
            TierGlint.Initialize(glintEnabled, glintStrength, glintSpeed, glintIntensityScale, Logger);

            try
            {
                harmony.PatchAll(typeof(WeaponTiersPlugin).Assembly);
            }
            catch(Exception ex)
            {
                harmony.UnpatchSelf();
                TierGlint.Shutdown();
                Logger.LogError($"{ModName} couldn't patch this game version, all patches removed!" + ex);
                return;
            }

            Logger.LogInfo($"{ModName} v{ModVersion} loaded!");
        }

        private void Update()
        {
            TierGlint.Tick();
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            TierGlint.Shutdown();
        }
    }

    [HarmonyPatch(typeof(Pickup), nameof(Pickup.SetupAndSpawn))]
    internal static class DropPatch
    {
        private static readonly MethodInfo SetData = AccessTools.PropertySetter(typeof(Pickup), nameof(Pickup.inventoryData));

        static void Postfix(Pickup __instance, ItemDefinition item, InventoryData _inventoryData)
        {
            if(!(item is WeaponSO weapon) || !WeaponTierSystem.Supports(weapon))
            {
                TierGlint.ApplyPickup(__instance);
                return;
            }

            int tier;

            // Already owned guns keep their roll...
            if(_inventoryData != null)
            {
                tier = WeaponTierSystem.Read(_inventoryData.attributes);
            }
            else
            {
                tier = WeaponTierSystem.Roll(Random.value, WeaponTiersPlugin.Weights);

                if(tier == 0)
                {
                    TierGlint.ApplyPickup(__instance);
                    return;
                }

                ItemStats itemStats = new ItemStats();
                itemStats.SetBaseAttributes(weapon);
                WeaponTierSystem.Apply(itemStats, tier);

                int ammo = weapon.startsEmpty ? 0 : weapon.iAmmoMax;
                ItemAttributeCollectionData attributes = itemStats.SerializedAttributeData();
                ItemId[] attachments = Array.Empty<ItemId>();
                ItemId[] contents = Array.Empty<ItemId>();
                int width = item.inventorySize.x;
                int height = item.inventorySize.y;

                InventoryData invData = new InventoryData(item.id, 0, 0, 1, ammo, weapon.caliber, attributes, attachments, contents, 0, width, height);

                // Let vanilla finish the spawn first...
                SetData.Invoke(__instance, new object[] { invData });
            }

            if(tier > 0 && __instance.itemText != null)
            {
                __instance.itemText.text = __instance.itemText.text + " " + WeaponTierSystem.Label(tier);
            }

            TierGlint.ApplyPickup(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemStats), nameof(ItemStats.SetBaseAttributes), new Type[] { typeof(ItemDefinition) })]
    internal static class WeaponDefinitionPatch
    {
        static void Postfix(ItemStats __instance, ItemDefinition itemDefinition)
        {
            WeaponTierSystem.Register(__instance, itemDefinition);
        }
    }

    [HarmonyPatch(typeof(ItemStats), nameof(ItemStats.SerializedAttributeData))]
    internal static class SavePatch
    {
        static void Postfix(ItemStats __instance, ItemAttributeCollectionData __result)
        {
            WeaponTierSystem.Write(__result, WeaponTierSystem.Read(__instance));
        }
    }

    [HarmonyPatch(typeof(ItemStats), nameof(ItemStats.LoadAttributesFromData))]
    internal static class LoadPatch
    {
        static void Postfix(ItemStats __instance, ItemAttributeCollectionData data)
        {
            WeaponTierSystem.Apply(__instance, WeaponTierSystem.Read(data));
        }
    }

    [HarmonyPatch(typeof(ItemStats), nameof(ItemStats.SetAttributeBaseValue))]
    internal static class BaseValuePatch
    {
        static void Prefix(ItemStats __instance, out int __state)
        {
            __state = WeaponTierSystem.Read(__instance);
        }

        static void Postfix(ItemStats __instance, ItemAttributes attribute, int __state)
        {
            WeaponTierSystem.Refresh(__instance, attribute, __state);
        }
    }

    [HarmonyPatch(typeof(ItemStats), nameof(ItemStats.GetPelletDamage))]
    internal static class PelletPatch
    {
        static void Postfix(ItemStats __instance, int pellets, ref float __result)
        {
            if(pellets <= 1 || WeaponTierSystem.Read(__instance) == 0)
            {
                return;
            }

            List<StatModifier> mods = __instance.itemAttributes[(int)ItemAttributes.Damage].statModifiers;
            
            float tierBonus = 0;
            float additive = 0;
            float multiplicative = 1;

            foreach(StatModifier mod in mods)
            {
                if(mod.SourceId == WeaponTierSystem.Source)
                {
                    tierBonus += mod.Value;
                }

                if(mod.Type == StatModType.PercentAdd)
                {
                    additive += mod.Value;
                }

                if(mod.Type == StatModType.PercentMult)
                {
                    multiplicative *= 1 + mod.Value;
                }
            }

            // Vanilla spreads flat bonuses across pellets, so this bonus belongs on every pellet...
            __result += tierBonus * (1 - 1f / pellets) * Mathf.Clamp(1 + additive, 0.01f, 10f) * multiplicative;
        }
    }

    [HarmonyPatch]
    internal static class TierBonusTooltipPatch
    {
        private static readonly MethodInfo AddAttribute = AccessTools.Method(typeof(ItemDescription), "AddAttribute", new Type[]
        {
            typeof(string), typeof(int).MakeByRefType(), typeof(string), typeof(string), typeof(bool)
        });

        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ItemDescription), "AddDurability");
            yield return AccessTools.Method(typeof(ItemDescription), "AddFlavorText");
        }

        static void Prefix(ItemDescription __instance, MethodBase __originalMethod, ref int childIndex, InventoryItem ___inventoryRepresentation)
        {
            if(___inventoryRepresentation == null || ___inventoryRepresentation.weaponDefinition == null)
            {
                return;
            }

            WeaponSO weaponScriptable = ___inventoryRepresentation.weaponDefinition;

            // Melee skips the condition row, so add its bonuses before flavor text instead...
            if(!WeaponTierSystem.Supports(weaponScriptable) || (__originalMethod.Name == "AddFlavorText") != weaponScriptable.IsMelee)
            {
                return;
            }

            int tier = WeaponTierSystem.Read(___inventoryRepresentation.stats);

            if(tier == 0)
            {
                return;
            }

            ItemStats itemStats = ___inventoryRepresentation.stats;

            if(WeaponTierSystem.AppliesTo(weaponScriptable, itemStats, ItemAttributes.Damage))
            {
                AddBonus(__instance, "Tier damage", WeaponTierSystem.ColoredMultiplier(ItemAttributes.Damage, tier), tier, ref childIndex);
            }

            if(weaponScriptable.IsMelee)
            {
                return;
            }

            if(WeaponTierSystem.AppliesTo(weaponScriptable, itemStats, ItemAttributes.RPM))
            {
                AddBonus(__instance, "Tier fire rate", WeaponTierSystem.ColoredMultiplier(ItemAttributes.RPM, tier), tier, ref childIndex);
            }

            if(WeaponTierSystem.AppliesTo(weaponScriptable, itemStats, ItemAttributes.ReloadSpeed))
            {
                AddBonus(__instance, "Tier reload speed", WeaponTierSystem.ColoredMultiplier(ItemAttributes.ReloadSpeed, tier), tier, ref childIndex);
            }

            if(WeaponTierSystem.AppliesTo(weaponScriptable, itemStats, ItemAttributes.MaxDurability))
            {
                AddBonus(__instance, "Tier max durability", WeaponTierSystem.ColoredMultiplier(ItemAttributes.MaxDurability, tier), tier, ref childIndex);
            }
        }

        private static void AddBonus(ItemDescription description, string label, string multiplier, int tier, ref int childIndex)
        {
            string coloredLabel = $"<color=#{ WeaponTierSystem.Colors[tier] }>{ label }:</color>";
            object[] arguments = { coloredLabel, childIndex, multiplier, "", true };
            AddAttribute.Invoke(description, arguments);
            childIndex = (int)arguments[1];
        }
    }

    [HarmonyPatch(typeof(ItemDescription), nameof(ItemDescription.Setup), new Type[] { typeof(InventoryItem) })]
    internal static class TooltipPatch
    {
        static void Postfix(InventoryItem inventoryItem, TextMeshProUGUI ___titleTextFull, TextMeshProUGUI ___titleTextShort)
        {
            int tier = WeaponTierSystem.Supports(inventoryItem.weaponDefinition) ? WeaponTierSystem.Read(inventoryItem.stats) : 0;

            if(tier == 0)
            {
                return;
            }

            string label = " " + WeaponTierSystem.Label(tier);
            ___titleTextFull.text += label;
            ___titleTextShort.text += label;
        }
    }
}
