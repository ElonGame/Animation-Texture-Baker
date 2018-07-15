using UnityEngine;
using UnityEngine.Rendering;
using AnimationBaker.StateMachine;

namespace AnimationBaker.StateMachine.ScriptableObjects
{
    public class RendererData : ScriptableObject
    {
        public Mesh Mesh;
        public int SubMeshCount;
        public Material[] Materials;
        public ShadowCastingMode ShadowCastingMode;
        public bool ReceivesShadows;
    }
}
