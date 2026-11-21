using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.Audio;
using PerfectRandom.Sulfur.Core.DataStorage;
using PerfectRandom.Sulfur.Core.Items;
using PerfectRandom.Sulfur.Core.Stats;
using PerfectRandom.Sulfur.Core.UI;
using PerfectRandom.Sulfur.Core.UI.Inventory;
using PerfectRandom.Sulfur.Core.UI.ItemDescription;
using TMPro;
using PerfectRandom.Sulfur.Core.Units;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SulfurWeaponTiers
{
    internal static class SulfuriumTincture
    {
        internal const string ItemName = "Item_WeaponTierCrystal";
        internal const string DisplayName = "Sulfurium Tincture";
        internal const string Description = "Magical essence mixed with a foul-smelling sulfur solution. Pour onto a weapon to raise its tier.";
        internal static readonly ItemId ID = new ItemId(53721);
        internal static ItemDefinition Definition { get; private set; }
        internal static int Price = 7500;
        private static ItemDatabase registeredDatabase;
        private static Texture2D texture;
        private static Sprite sprite;
        private static AudioClip glassShatter;
        private static readonly FieldInfo Artwork = AccessTools.Field(typeof(InventoryItem), "artworkSlot");

        internal static void Register(ItemDatabase database)
        {
            List<ItemDefinition> items = database.GetRawList();

            if(Definition != null && items.Contains(Definition))
            {
                return;
            }

            if(items.Any(item => item != null && item.id == ID && item != Definition))
            {
                Debug.LogError("Weapon Tiers: Sulfurium tincture item ID is already in use!");
                return;
            }

            if(Definition == null)
            {
                LoadIcon();

                Definition = ScriptableObject.CreateInstance<ItemDefinition>();
                Definition.name = ItemName;
                Definition.id = ID;
                Definition.displayName = DisplayName;
                Definition.description = Description;
                Definition.flavor = Description;
                Definition.excludeFromLocalization = true;
                Definition.artwork = sprite;
                Definition.useSounds = Resources.FindObjectsOfTypeAll<AudioClip>().Where(clip => clip.name == "UI_Item_Use_Durability_Repair_Liquid_Splash_01").ToArray();

                if(Definition.useSounds.Length == 0)
                {
                    Debug.LogWarning("Weapon Tiers: Tincture liquid splash sound was not loaded!");
                }

                glassShatter = Resources.FindObjectsOfTypeAll<AudioClip>().FirstOrDefault(clip => clip.name == "SFX_Old_Physics_Glass_Destroy_01");

                if(glassShatter == null)
                {
                    Debug.LogWarning("Weapon Tiers: Tincture glass shatter sound was not loaded!");
                }

                Definition.artworkWorldScale = 0.8f;
                Definition.basePrice = Price;
                Definition.useType = UseType.ItemConsumable;
                Definition.slotType = SlotType.None;
                Definition.inventorySize = new Vector2Int(1, 2);
                Definition.maxDurability = 0f;
                Definition.modifiersOnEquipNew = new List<ModifierContainer>();
                Definition.modifiersOnInventoryNew = new List<ModifierContainer>();
                Definition.removeStatusOnConsume = new List<EntityAttributes>();
                Definition.addStatusOnConsume = new List<ApplyStatusContainer>();
                Definition.modifiersOnAttachToItem = new List<ItemModifierContainer>();
                Definition.buffsOnConsume = new List<BuffDefinition>();
                Definition.valueChangeOnItemConsume = new List<ItemConsumeContainer>();
                Definition.resourceOnConsume = new List<WorldResourceModifierContainer>();
                Definition.baseAttributes = new List<ItemAttributeContainer>();
            }

            registeredDatabase = database;
            items.Add(Definition);
        }

        private static void LoadIcon()
        {
            using(Stream resource = typeof(SulfuriumTincture).Assembly.GetManifestResourceStream("SulfurWeaponTiers.SulfuriumTincture"))
            using(MemoryStream bytes = new MemoryStream())
            {
                if(resource == null)
                {
                    throw new InvalidOperationException("Sulfurium tincture icon resource is missing!");
                }

                resource.CopyTo(bytes);
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.name = DisplayName;
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;

                if(!ImageConversion.LoadImage(texture, bytes.ToArray()))
                {
                    throw new InvalidOperationException("Sulfurium tincture icon could not be loaded!");
                }

                // Trim the padding so the bottle fills its inventory slots...
                sprite = Sprite.Create(texture, new Rect(24f, 56f, 208f, 400f), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                sprite.name = DisplayName;
            }
        }

        internal static bool IsTincture(ItemDefinition item)
        {
            return item != null && item.id == ID && item.name == ItemName;
        }

        internal static bool Upgrade(InventoryItem tincture, InventoryItem target)
        {
            if(!tincture.OwnedByPlayer || target == null || !target.OwnedByPlayer || !WeaponTierSystem.Supports(target.weaponDefinition))
            {
                return false;
            }

            int tier = WeaponTierSystem.Read(target.stats);

            if(tier >= 4)
            {
                return false;
            }

            // Keep the actual weapon and its oils, ammo, condition, and XP...
            WeaponTierSystem.Apply(target.stats, tier + 1);
            TierGlint.ApplyInventory(target, (Image)Artwork.GetValue(target));
            tincture.itemDefinition.PlayUseSound();
            AudioClip pourSound = tincture.itemDefinition.UseSounds[0];

            if(pourSound != null)
            {
                StaticInstance<UIManager>.Instance.StartCoroutine(PlayGlassShatter(pourSound.length * 0.35f));
            }

            tincture.GetConsumedByExternalFactor(target.gameObject);

            return true;
        }

        private static IEnumerator PlayGlassShatter(float pourDuration)
        {
            // (Keep the bottle break while the inventory pauses the game...)
            yield return new WaitForSecondsRealtime(pourDuration);

            if(glassShatter != null && StaticInstance<SoundBank>.Instance != null)
            {
                StaticInstance<SoundBank>.Instance.PlayClip(glassShatter, 0.5f);
            }
        }

        internal static void Shutdown()
        {
            if(registeredDatabase != null && Definition != null)
            {
                registeredDatabase.GetRawList().Remove(Definition);
            }

            Object.Destroy(Definition);
            Object.Destroy(sprite);
            Object.Destroy(texture);
            Definition = null;
            registeredDatabase = null;
            sprite = null;
            texture = null;
            glassShatter = null;
        }
    }

    [HarmonyPatch(typeof(ItemDatabase), nameof(ItemDatabase.InitRuntime))]
    internal static class TinctureRegistrationPatch
    {
        static void Prefix(ItemDatabase __instance)
        {
            SulfuriumTincture.Register(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemDatabase), "Item", MethodType.Getter)]
    internal static class TinctureLookupPatch
    {
        static bool Prefix(ItemId id, ref ItemDefinition __result)
        {
            if(id != SulfuriumTincture.ID || SulfuriumTincture.Definition == null)
            {
                return true;
            }

            // Fixed ID keeps saved tinctures independent of the vanilla item count...
            __result = SulfuriumTincture.Definition;
            return false;
        }
    }

    [HarmonyPatch(typeof(ShopKeeper), "AddItems", new Type[] { typeof(List<ItemDefinition>) })]
    internal static class TinctureShopPatch
    {
        static void Prefix(ShopKeeper __instance, ref List<ItemDefinition> staticList)
        {
            string vendor = __instance.Npc != null ? __instance.Npc.UnitIdentifier : null;

            if(vendor != "CongregationScholar" && vendor != "FexShuen")
            {
                return;
            }

            if(SulfuriumTincture.Definition == null || staticList.Any(SulfuriumTincture.IsTincture))
            {
                return;
            }

            // (One tincture per fresh batch, without changing the shared vendor table...)
            staticList = new List<ItemDefinition>(staticList);
            staticList.Add(SulfuriumTincture.Definition);
        }
    }

    [HarmonyPatch(typeof(InventoryItem), nameof(InventoryItem.OnEndDrag))]
    internal static class TinctureDragPatch
    {
        private static readonly MethodInfo EndDrag = AccessTools.Method(typeof(InventoryItem), "GenericDragEnd");

        static bool Prefix(InventoryItem __instance, PointerEventData eventData, InventoryUI ___inventoryUI, ItemGrid ___containingGrid)
        {
            if(!SulfuriumTincture.IsTincture(__instance.itemDefinition) || !__instance.isBeingDragged || !__instance.OwnedByPlayer || eventData.button != PointerEventData.InputButton.Left)
            {
                return true;
            }

            PaperdollSlot slot = ___inventoryUI.paperdoll.hoveredSlot;
            InventoryItem target = slot != null ? slot.itemInSlot : ___containingGrid.HoveredItem ?? ___containingGrid.HoveredSlot?.ItemInSlot;

            if(target == null)
            {
                return true;
            }

            if(!SulfuriumTincture.Upgrade(__instance, target))
            {
                __instance.DroppedOnInvalid();
                return false;
            }

            EndDrag.Invoke(__instance, null);
            GameManager game = StaticInstance<GameManager>.Instance;
            game.SaveInventoryData();
            game.SaveStashData();
            SulfurSaveState.WriteActiveToDisk();

            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryUI), nameof(InventoryUI.ItemDragStart))]
    internal static class TinctureTargetHighlightPatch
    {
        static void Postfix(InventoryUI __instance, InventoryItem item, List<InventoryItem> ___wigglingItems)
        {
            if(!SulfuriumTincture.IsTincture(item.itemDefinition) || !item.OwnedByPlayer)
            {
                return;
            }

            foreach(InventoryItem target in ___wigglingItems)
            {
                target.SetWiggle(false);
            }

            ___wigglingItems.Clear();

            foreach(InventoryItem target in __instance.bagSpaceItemGrid.AllItems())
            {
                if(target.OwnedByPlayer && WeaponTierSystem.Supports(target.weaponDefinition) && WeaponTierSystem.Read(target.stats) < 4)
                {
                    target.SetWiggle(true);
                    ___wigglingItems.Add(target);
                }
            }
        }
    }

    [HarmonyPatch]
    internal static class TinctureTextPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.PropertyGetter(typeof(ItemDefinition), nameof(ItemDefinition.LocalizedDisplayName));
            yield return AccessTools.PropertyGetter(typeof(ItemDefinition), nameof(ItemDefinition.LocalizedFlavor));
        }

        static void Postfix(ItemDefinition __instance, MethodBase __originalMethod, ref string __result)
        {
            if(SulfuriumTincture.IsTincture(__instance))
            {
                __result = __originalMethod.Name == "get_LocalizedDisplayName" ? SulfuriumTincture.DisplayName : SulfuriumTincture.Description;
            }
        }
    }

    [HarmonyPatch(typeof(ItemDescription), nameof(ItemDescription.Setup), new Type[] { typeof(InventoryItem) })]
    internal static class TinctureTooltipNamePatch
    {
        static void Postfix(InventoryItem inventoryItem, TextMeshProUGUI ___titleTextFull)
        {
            if(SulfuriumTincture.IsTincture(inventoryItem.itemDefinition))
            {
                // Vanilla adds a question mark when there is no translation entry...
                ___titleTextFull.text = SulfuriumTincture.DisplayName;
            }
        }
    }

    [HarmonyPatch(typeof(Pickup), nameof(Pickup.SetupAndSpawn))]
    internal static class TincturePickupTextPatch
    {
        static void Postfix(Pickup __instance, ItemDefinition item)
        {
            if(SulfuriumTincture.IsTincture(item) && __instance.itemText != null)
            {
                __instance.itemText.text = SulfuriumTincture.DisplayName;
            }
        }
    }
}

