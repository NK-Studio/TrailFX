using UnityEngine;

namespace NKStudio
{
    [System.Serializable]
    public class MaterialData
    {
        public TrailRenderer trailRenderer;
        public Vector2 uvTiling { get; private set; }
        public float Move { get; private set; }
        [HideInInspector] public Material CashMaterial;

        public MaterialData(TrailRenderer trailRenderer, Material material, Vector2 uvScale, float move)
        {
            this.trailRenderer = trailRenderer;
            uvTiling = uvScale;
            Move = move;
        }
        
        public void SetMove(float move)
        {
            Move = move;
        }
        
        
        public void SetUVTiling(Vector2 uvTiling)
        {
            this.uvTiling = uvTiling;
        }
    }
}