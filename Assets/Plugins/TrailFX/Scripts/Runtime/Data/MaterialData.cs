using UnityEngine;

namespace NKStudio
{
    [System.Serializable]
    public class MaterialData
    {
        public TrailRenderer trailRender;
        public Vector2 uvTiling { get; private set; }
        public float Move { get; private set; }
        
        public Material Origin;
        [HideInInspector] public Material CashMaterial;

        public MaterialData(TrailRenderer trailRender, Material material, Vector2 uvScale, float move)
        {
            this.trailRender = trailRender;
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