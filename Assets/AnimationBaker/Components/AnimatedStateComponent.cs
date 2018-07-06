using UnityEngine;
using Unity.Entities;

namespace Animationbaker.Components
{
    [System.Serializable]
    public struct AnimatedState : IComponentData
    {
        public int AnimationHash;
        public int Clip;
        public float OverrideFrame;
    }

    [System.Serializable]
    [RequireComponent(typeof(GameObjectEntity))]
    public class AnimatedStateComponent : ComponentDataWrapper<AnimatedState> { }
}
