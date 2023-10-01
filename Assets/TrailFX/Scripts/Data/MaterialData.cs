using UnityEngine;

namespace NKStudio
{
    [System.Serializable]
    public class MaterialData
    {
        public TrailRenderer trailRenderer;
        [HideInInspector] public Vector2 uvTiling;
        [HideInInspector] public float Move;
        [HideInInspector] public Material CashMaterial;

        public MaterialData(TrailRenderer trailRenderer, Material material, Vector2 uvScale, float move)
        {
            this.trailRenderer = trailRenderer;
            uvTiling = uvScale;
            Move = move;
        }
    }
}