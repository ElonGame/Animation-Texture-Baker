using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using AnimationBaker.Systems;
using NavJob.Components;
using NavJob.Systems;

namespace Example.Systems
{
    [UpdateAfter(typeof(NavAgentToPositionSyncSystem))]
    [UpdateBefore(typeof(AnimatedRendererSystem))]
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    public class UpdateMatrixSystem : JobComponentSystem
    {

        [BurstCompile]
        public struct UpdateMatrixJob : IJobProcessComponentData<NavAgent, TransformMatrix>
        {
            float dt;

            public UpdateMatrixJob(float dt)
            {
                this.dt = dt;
            }

            public void Execute([ReadOnly] ref NavAgent agent, ref TransformMatrix matrix)
            {
                matrix.Value = Matrix4x4.TRS(agent.position, agent.rotation, Vector3.one);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new UpdateMatrixJob().Schedule(this, 64, inputDeps);
        }
    }
}
