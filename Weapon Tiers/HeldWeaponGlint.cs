using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SulfurWeaponTiers
{
    internal sealed class HeldWeaponGlint : MonoBehaviour
    {
        private sealed class Part
        {
            internal Renderer Renderer;
            internal int SubMeshes;
        }

        private readonly List<Part> parts = new List<Part>();
        private Material material;
        internal int Tier { get; private set; }

        internal void Configure(int tier, IList<Renderer> renderers)
        {
            if(tier != Tier)
            {
                Clear();
                material = TierGlint.CreateMaterial(tier, false);
                Tier = tier;
            }

            if(material == null)
            {
                return;
            }

            parts.Clear();

            foreach(Renderer renderer in renderers)
            {
                if(renderer == null)
                {
                    continue;
                }

                SkinnedMeshRenderer skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = skinnedMeshRenderer != null ? skinnedMeshRenderer.sharedMesh : filter != null ? filter.sharedMesh : null;

                if(mesh != null)
                {
                    parts.Add(new Part { Renderer = renderer, SubMeshes = mesh.subMeshCount });
                }
            }

            material.SetFloat("_UseAlpha", 0f);
            material.SetFloat("_HeldGlint", 1f);
        }

        internal static List<Renderer> FindMeleeRenderers(GameObject model)
        {
            List<Renderer> renderersList = new List<Renderer>();

            foreach(Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material material = renderer.sharedMaterial;

                // Several melee models share the world shader with hands, so shader filtering loses the weapon...
                bool isHands = material != null && (material.name == "Hands" || material.name.StartsWith("Hands ", StringComparison.Ordinal));

                if((renderer is MeshRenderer || renderer is SkinnedMeshRenderer) && material != null && !isHands)
                {
                    renderersList.Add(renderer);
                }
            }

            return renderersList;
        }

        internal void Draw(CommandBuffer commands, int layerMask, uint renderingLayer)
        {
            if(material == null)
            {
                return;
            }

            foreach(Part part in parts)
            {
                Renderer renderer = part.Renderer;

                if(renderer == null || !renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy || (renderer.renderingLayerMask & renderingLayer) == 0 || ((1 << renderer.gameObject.layer) & layerMask) == 0)
                {
                    continue;
                }

                // Reuse real renderer, including current bones and blend shapes...
                for(int subMesh = 0; subMesh < part.SubMeshes; subMesh++)
                {
                    commands.DrawRenderer(renderer, material, subMesh, 0);
                }
            }
        }

        internal void Clear()
        {
            Destroy(material);
            material = null;
            parts.Clear();
            Tier = 0;
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
