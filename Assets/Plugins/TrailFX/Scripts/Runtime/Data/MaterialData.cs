using UnityEngine;

namespace NKStudio
{
    [System.Serializable]
    public class MaterialData
    {
        public TrailRenderer trailRender;
        public Vector2 uvTiling;
        public float Move;
#if UNITY_EDITOR
        public Material Origin;
        public Material InstanceMaterial;
#endif
    }
}
