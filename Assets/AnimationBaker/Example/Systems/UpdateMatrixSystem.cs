using Animationbaker.Systems;
using Example.Jobs;
using NavJob.Systems;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Example.Systems
{
    [UpdateAfter (typeof (NavAgentToPositionSyncSystem))]
    [UpdateBefore (typeof (AnimatedRendererSystem))]
    public class UpdateMatrixSystem : JobComponentSystem
    {
        struct InjectData
        {
            public int Length;
        }

        protected override JobHandle OnUpdate (JobHandle inputDeps)
        {
            inputDeps.Complete ();
            return new UpdateMatrixJob ().Schedule (this, 64, inputDeps);
        }
    }
}