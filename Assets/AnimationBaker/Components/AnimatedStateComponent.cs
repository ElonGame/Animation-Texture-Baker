using Unity.Entities;
using UnityEngine;

namespace Animationbaker.Components
{
    [System.Serializable]
    public struct AnimatedState : IComponentData
    {
        public int State;
        public Matrix4x4 Matrix;
        public float Remainder;
        public float SetFrame;
    }

    [System.Serializable]
    [RequireComponent (typeof (GameObjectEntity))]
    public class AnimatedStateComponent : ComponentDataWrapper<AnimatedState>
    {
        // EntityManager manager;
        // Entity entity;
        // private void Start ()
        // {
        //     manager = GetComponent<GameObjectEntity> ().EntityManager;
        //     entity = GetComponent<GameObjectEntity> ().Entity;
        // }
        // private void Update () { }
    }
}