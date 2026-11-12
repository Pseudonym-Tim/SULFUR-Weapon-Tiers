using System;
using System.Reflection.Emit;
using HarmonyLib;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using PerfectRandom.Sulfur.Core;
using PerfectRandom.Sulfur.Core.Items;
using PerfectRandom.Sulfur.Core.Units;
using PerfectRandom.Sulfur.Core.Weapons;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace SulfurWeaponTiers
{
    internal static class TierGlint
    {
        private static Shader worldShader;
        private static Shader inventoryShader;
        private static float strength;
        private static float speed;
        private static bool isReady;
        private static readonly int Clock = Shader.PropertyToID("_SwtGlintTime");

        internal static void Initialize(bool enabled, float glintStrength, float glintSpeed, float glintIntensityScale, ManualLogSource log)
        {
            if(!enabled)
            {
                return;
            }

            // Compiled shaders will ride inside the DLL, so there's no loose bundle to forget!
            Assembly assembly = typeof(TierGlint).Assembly;

            using(Stream resource = assembly.GetManifestResourceStream("SulfurWeaponTiers.GlintShaders"))
            {
                if(resource == null)
                {
                    log.LogError("Tier glint shader resource missing!");
                    return;
                }

                using(MemoryStream bytes = new MemoryStream())
                {
                    resource.CopyTo(bytes);
                    AssetBundle bundle = AssetBundle.LoadFromMemory(bytes.ToArray());

                    if(bundle == null)
                    {
                        log.LogError("Could not load tier glint shaders!");
                        return;
                    }

                    worldShader = bundle.LoadAsset<Shader>("Assets/WorldGlint.shader");
                    inventoryShader = bundle.LoadAsset<Shader>("Assets/InventoryGlint.shader");
                    bundle.Unload(false);
                }
            }

            if(worldShader == null || inventoryShader == null || !worldShader.isSupported || !inventoryShader.isSupported)
            {
                log.LogError("Tier glint shaders unsupported on this graphics device, uh oh!");
                return;
            }

            // Turn down every glint surface together...
            strength = glintStrength * glintIntensityScale;
            speed = glintSpeed;
            isReady = true;
            log.LogInfo("Tier glint shaders loaded!");
        }

        internal static void Tick()
        {
            if(isReady)
            {
                // Inventory pauses game time, (keep the shimmer moving...)
                Shader.SetGlobalFloat(Clock, Time.unscaledTime);
            }
        }

        internal static Material CreateMaterial(int tier, bool inventory)
        {
            if(!isReady || tier == 0)
            {
                return null;
            }

            Color color;
            ColorUtility.TryParseHtmlString("#" + WeaponTierSystem.Colors[tier], out color);
            Material material = new Material(inventory ? inventoryShader : worldShader);
            material.name = "Weapon tier glint";
            material.hideFlags = HideFlags.HideAndDontSave;
            material.SetColor("_TierColor", color);
            material.SetFloat("_Strength", strength);
            material.SetFloat("_Speed", speed);

            return material;
        }

        internal static void ApplyPickup(Pickup pickup)
        {
            PickupWeaponGlint pickupWeaponGlint = pickup.GetComponent<PickupWeaponGlint>();
            int tier = pickup.ItemSO is WeaponSO definition && WeaponTierSystem.Supports(definition) ? WeaponTierSystem.Read(pickup.inventoryData?.attributes) : 0;

            if(isReady && tier > 0 && pickupWeaponGlint == null)
            {
                pickupWeaponGlint = pickup.gameObject.AddComponent<PickupWeaponGlint>();
            }

            if(pickupWeaponGlint != null)
            {
                pickupWeaponGlint.Configure(tier, pickup.itemSprite);
            }
        }

        internal static void ApplyHeld(Weapon weapon)
        {
            HeldWeaponGlint heldWeaponGlint = weapon.GetComponent<HeldWeaponGlint>();
            int tier = !WeaponTierSystem.Supports(weapon.weaponDefinition) || weapon.inventoryItem == null ? 0 : WeaponTierSystem.Read(weapon.inventoryItem.stats);

            if(isReady && tier > 0 && heldWeaponGlint == null)
            {
                heldWeaponGlint = weapon.gameObject.AddComponent<HeldWeaponGlint>();
            }

            if(heldWeaponGlint != null)
            {
                IList<Renderer> renderers = weapon.meshRenderersInWeapon;

                if(weapon.IsMelee && tier > 0)
                {
                    // Melee prefabs leave weaponRendererParent empty, find weapon materials without coating hands...
                    renderers = HeldWeaponGlint.FindMeleeRenderers(weapon.gameObject);
                }

                heldWeaponGlint.Configure(tier, renderers);
            }
        }

        internal static void DrawHeld(CommandBuffer commands, Dictionary<Camera, Player> players, int layerMask, ref RenderingData renderingData)
        {
            Player player;

            if(!isReady || !players.TryGetValue(renderingData.cameraData.camera, out player) || !(player.activeHoldable is Weapon weapon))
            {
                return;
            }

            HeldWeaponGlint heldWeaponGlint = weapon.GetComponent<HeldWeaponGlint>();
            int tier = !WeaponTierSystem.Supports(weapon.weaponDefinition) || weapon.inventoryItem == null ? 0 : WeaponTierSystem.Read(weapon.inventoryItem.stats);

            if(tier == 0)
            {
                if(heldWeaponGlint != null && heldWeaponGlint.Tier != 0)
                {
                    heldWeaponGlint.Clear();
                }

                return;
            }

            if(heldWeaponGlint == null || heldWeaponGlint.Tier != tier)
            {
                ApplyHeld(weapon);
                heldWeaponGlint = weapon.GetComponent<HeldWeaponGlint>();
            }

            if(heldWeaponGlint != null)
            {
                heldWeaponGlint.Draw(commands, layerMask, player.playerRenderLayer);
            }
        }

        internal static void ApplyInventory(InventoryItem item, Image artwork)
        {
            InventoryTierGlint inventoryTierGlint = item.GetComponent<InventoryTierGlint>();
            int tier = !WeaponTierSystem.Supports(item.weaponDefinition) ? 0 : WeaponTierSystem.Read(item.stats);

            if(isReady && tier > 0 && artwork != null && inventoryTierGlint == null)
            {
                inventoryTierGlint = item.gameObject.AddComponent<InventoryTierGlint>();
            }

            if(inventoryTierGlint != null)
            {
                inventoryTierGlint.Configure(artwork, tier);
            }
        }

        internal static void Shutdown()
        {
            foreach(PickupWeaponGlint pickupWeaponGlint in Resources.FindObjectsOfTypeAll<PickupWeaponGlint>())
            {
                pickupWeaponGlint.Clear();
                Object.Destroy(pickupWeaponGlint);
            }

            foreach(HeldWeaponGlint heldWeaponGlint in Resources.FindObjectsOfTypeAll<HeldWeaponGlint>())
            {
                heldWeaponGlint.Clear();
                Object.Destroy(heldWeaponGlint);
            }

            foreach(InventoryTierGlint inventoryTierGlint in Resources.FindObjectsOfTypeAll<InventoryTierGlint>())
            {
                inventoryTierGlint.Clear();
                Object.Destroy(inventoryTierGlint);
            }

            isReady = false;
            Object.Destroy(worldShader);
            Object.Destroy(inventoryShader);
            worldShader = null;
            inventoryShader = null;
        }
    }

    internal sealed class InventoryTierGlint : MonoBehaviour
    {
        private Image source;
        private Image overlay;
        private Material material;

        internal void Configure(Image artwork, int tier)
        {
            Clear();

            if(artwork == null || tier == 0)
            {
                return;
            }

            material = TierGlint.CreateMaterial(tier, true);

            if(material == null)
            {
                return;
            }

            source = artwork;
            GameObject layer = new GameObject("Weapon tier glint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            layer.layer = source.gameObject.layer;
            layer.transform.SetParent(source.transform, false);
            overlay = layer.GetComponent<Image>();
            overlay.rectTransform.anchorMin = Vector2.zero;
            overlay.rectTransform.anchorMax = Vector2.one;
            overlay.rectTransform.offsetMin = Vector2.zero;
            overlay.rectTransform.offsetMax = Vector2.zero;
            overlay.material = material;
            overlay.raycastTarget = false;

            SyncArtwork();
        }

        private void SyncArtwork()
        {
            if(source == null || overlay == null)
            {
                return;
            }

            // Reuse sprite's alpha and UI masks, not just a fuckin' shiny rectangle over the whole slot!
            overlay.sprite = source.overrideSprite;
            overlay.type = source.type;
            overlay.useSpriteMesh = source.useSpriteMesh;
            overlay.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            overlay.fillCenter = source.fillCenter;
            overlay.preserveAspect = source.preserveAspect;
            overlay.fillAmount = source.fillAmount;
            overlay.fillMethod = source.fillMethod;
            overlay.fillOrigin = source.fillOrigin;
            overlay.fillClockwise = source.fillClockwise;
            overlay.maskable = source.maskable;
            overlay.color = source.color;
            overlay.enabled = source.enabled;
        }

        private void LateUpdate()
        {
            SyncArtwork();
        }

        internal void Clear()
        {
            if(overlay != null)
            {
                overlay.enabled = false;
                Destroy(overlay.gameObject);
            }

            Destroy(material);
            source = null;
            overlay = null;
            material = null;
        }

        private void OnDestroy()
        {
            Clear();
        }
    }

    [HarmonyPatch(typeof(Weapon), "SetupRenderers")]
    internal static class HeldTierGlintPatch
    {
        static void Postfix(Weapon __instance)
        {
            TierGlint.ApplyHeld(__instance);
        }
    }

    [HarmonyPatch(typeof(WeaponRendering), nameof(WeaponRendering.Execute))]
    internal static class HeldGlintRenderingPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo drawList = AccessTools.Method(typeof(CommandBuffer), nameof(CommandBuffer.DrawRendererList));
            MethodInfo drawGlint = AccessTools.Method(typeof(TierGlint), nameof(TierGlint.DrawHeld));
            FieldInfo players = AccessTools.Field(typeof(WeaponRendering), "_playerCameras");
            FieldInfo layers = AccessTools.Field(typeof(WeaponRendering), "_weaponLayer");
            int draws = 0;

            foreach(CodeInstruction instruction in instructions)
            {
                yield return instruction;

                if(instruction.Calls(drawList) && ++draws == 2)
                {
                    // Draw before vanilla restores the camera matrix, so stuff still lines up...
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, players);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, layers);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    yield return new CodeInstruction(OpCodes.Call, drawGlint);
                }
            }

            if(draws != 2)
            {
                throw new InvalidOperationException("Weapon render pass has changed, could not set up glint draw!");
            }
        }
    }

    [HarmonyPatch(typeof(InventoryItem), nameof(InventoryItem.Setup), new Type[]
    {
        typeof(Vector2), typeof(ItemDefinition), typeof(ItemGrid), typeof(int), typeof(int), typeof(InventoryData), typeof(bool)
    })]

    internal static class InventoryTierGlintPatch
    {
        static void Postfix(InventoryItem __instance, Image ___artworkSlot)
        {
            TierGlint.ApplyInventory(__instance, ___artworkSlot);
        }
    }
}
