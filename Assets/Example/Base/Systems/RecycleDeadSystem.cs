using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Example.Base.Components;

namespace Example.Base.Systems
{
    public class RecycleDeadSystem : JobComponentSystem
    {
        struct InjectData
        {
            public readonly int Length;
            public ComponentDataArray<IsDead> deads;
            [ReadOnly] public EntityArray entities;
        }

        [BurstCompile]
        struct RemoveDeadJob : IJobParallelFor
        {
            public float time;
            public EntityCommandBuffer.Concurrent buffer;
            [ReadOnly] public ComponentDataArray<IsDead> deads;
            [ReadOnly] public EntityArray entities;
            public void Execute(int index)
            {
                // remove after 10 seconds
                if (time > deads[index].TimeOfDeath + 10)
                    buffer.DestroyEntity(entities[index]);
            }
        }

        [Inject] InjectData data;
        [Inject] RecycleDeadSystemBarrier barrier;
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var buffer = barrier.CreateCommandBuffer();
            return new RemoveDeadJob
            {
                buffer = barrier.CreateCommandBuffer(),
                    time = Time.time,
                    deads = data.deads,
                    entities = data.entities
            }.Schedule(data.Length, 64, inputDeps);
        }
    }

    public class RecycleDeadSystemBarrier : BarrierSystem { }
}
