using UnityEngine;
using UnityEngine.Rendering;

namespace SulfurWeaponTiers
{
    internal sealed class PickupWeaponGlint : MonoBehaviour
    {
        private MeshRenderer source;
        private MeshRenderer overlay;
        private Material material;

        internal void Configure(int tier, MeshRenderer artwork)
        {
            Clear();

            if(tier == 0 || artwork == null)
            {
                return;
            }

            material = TierGlint.CreateMaterial(tier, false);

            if(material == null)
            {
                return;
            }

            source = artwork;
            Material original = source.sharedMaterial;
            material.SetTexture("_MainTex", original.GetTexture("_MainTex"));
            material.SetTextureOffset("_MainTex", original.GetTextureOffset("_MainTex"));
            material.SetTextureScale("_MainTex", original.GetTextureScale("_MainTex"));
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_Billboard", original.IsKeywordEnabled("_BILLBOARD_FACECAMERA") ? 1f : 0f);
            material.SetFloat("_AlphaClipThreshold", original.HasProperty("_AlphaClipThreshold") ? original.GetFloat("_AlphaClipThreshold") : 0.01f);

            // Pickups need their own camera facing layer...
            GameObject layer = new GameObject("Weapon tier glint");
            layer.transform.SetParent(source.transform, false);
            layer.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            overlay = layer.AddComponent<MeshRenderer>();
            overlay.sharedMaterial = material;
            overlay.shadowCastingMode = ShadowCastingMode.Off;
            overlay.receiveShadows = false;
            LateUpdate();
        }

        private void LateUpdate()
        {
            if(overlay == null)
            {
                return;
            }

            overlay.enabled = source != null && source.enabled && !source.forceRenderingOff && source.gameObject.activeInHierarchy;

            if(source != null)
            {
                overlay.renderingLayerMask = source.renderingLayerMask;
                overlay.gameObject.layer = source.gameObject.layer;
                overlay.sortingLayerID = source.sortingLayerID;
                overlay.sortingOrder = source.sortingOrder;
            }
        }

        private void OnDisable()
        {
            if(overlay != null)
            {
                overlay.enabled = false;
            }
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
}
