using UnityEngine;
using Unity.Entities;

namespace AnimationBaker.Components
{
    [System.Serializable]
    public struct AnimatedState : IComponentData
    {
        public int AnimationHash;
        public int Clip;
        public float Timeout;
    }

    [System.Serializable]
    [RequireComponent(typeof(GameObjectEntity))]
    public class AnimatedStateComponent : ComponentDataWrapper<AnimatedState> { }
}
