using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using AnimationBaker.Components;

namespace AnimationBaker.Systems
{
    public class SyncStateMachineUnitTransforms : JobComponentSystem
    {

        struct UpdateUnitTransformPositionRotationJob : IJobProcessComponentData<StateMachineUnit, Position, Rotation>
        {
            public void Execute([WriteOnly] ref StateMachineUnit unit, [ReadOnly] ref Position position, [ReadOnly] ref Rotation rotation)
            {
                unit.Matrix = Matrix4x4.TRS(position.Value, rotation.Value, Vector3.one);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new UpdateUnitTransformPositionRotationJob().Schedule(this, 64, inputDeps);
        }
    }
}
