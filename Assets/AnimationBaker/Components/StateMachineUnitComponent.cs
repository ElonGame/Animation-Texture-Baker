using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace AnimationBaker.Components
{
    [System.Serializable]
    public struct StateMachineUnit : IComponentData
    {
        public int RendererHash;
        public Matrix4x4 Matrix;
        public float YPos;
    }

    [System.Serializable]
    [RequireComponent(typeof(GameObjectEntity))]
    public class StateMachineUnitComponent : ComponentDataWrapper<StateMachineUnit> { }
}
