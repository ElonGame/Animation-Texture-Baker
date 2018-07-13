using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using AnimationBaker.Components;

namespace AnimationBaker.Baked
{
    [System.Serializable]
    public struct BakedMage : IComponentData
    {
        public float Runtime;
        public float RemainInState;
        public int Animation;
        public int IsDead;
        public int DoAttack;
        public int IsHit;
        public float MoveSpeed;
    }

    [RequireComponent(typeof(StateMachineUnitComponent), typeof(PositionComponent), typeof(RotationComponent))]
    public class BakedMageComponent : ComponentDataWrapper<BakedMage> { }
}
