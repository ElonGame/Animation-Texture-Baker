using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using AnimationBaker.Components;
using AnimationBaker.Interfaces;

namespace AnimationBaker.Baked
{
    [System.Serializable]
    public struct Archer : IUnitState
    {
        public float Runtime
        {
            get;
            set;
        }
        public float StateTimer
        {
            get;
            set;
        }
        public int CurrentState
        {
            get;
            set;
        }
        public int PreviousState
        {
            get;
            set;
        }

        public int DoAttack; // 0
        public int IsHit; // 1
        public float MoveSpeed; // 2
        public int IsDead; // 3

        public float this [int index]
        {
            get
            {
                switch (index)
                {

                    case 0:
                        return DoAttack;
                    case 1:
                        return DoAttack;
                    case 2:
                        return DoAttack;
                    case 3:
                        return DoAttack;
                }
                return 0;
            }
            set
            {
                switch (index)
                {

                    case 0:
                        DoAttack = (int) value;
                        break;
                    case 1:
                        IsHit = (int) value;
                        break;
                    case 2:
                        MoveSpeed =  value;
                        break;
                    case 3:
                        IsDead = (int) value;
                        break;
                }
            }
        }
    }

    [RequireComponent(typeof(StateMachineUnitComponent), typeof(PositionComponent), typeof(RotationComponent))]
    public class ArcherComponent : ComponentDataWrapper<Archer> { }
}

