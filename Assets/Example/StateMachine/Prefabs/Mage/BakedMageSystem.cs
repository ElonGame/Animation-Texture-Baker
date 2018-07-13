using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using AnimationBaker.Components;

namespace AnimationBaker.Baked
{
    // Animations:
    // 0: Idle
    // 1: Walk
    // 2: Die
    // 3: Attack
    // 4: Hit

    public class BakedMageSystem : JobComponentSystem
    {
        public float TexHeight = 256f;
        public NativeArray<float> FrameStart;
        public NativeArray<float> FrameLengths;

        public struct UpdateComponentDataJob : IJobProcessComponentData<BakedMage, StateMachineUnit>
        {
            [ReadOnly] public NativeArray<float> frameStart;
            [ReadOnly] public NativeArray<float> frameLengths;
            [ReadOnly] public float singleFrame;
            public float dt;
            public void Execute(ref BakedMage data, ref StateMachineUnit stateData)
            {
                data.Runtime += dt;
                if (data.RemainInState > 0)
                    data.RemainInState -= dt;
                if (data.IsDead == 1)
                {
                    if (data.Animation != 2)
                    {
                        data.Runtime = 0;
                    }
                    else if (data.Runtime > frameLengths[2])
                    {
                        data.Runtime = frameLengths[2] - 0.001f;
                    }
                    data.Animation = 2;
                }
                else if (data.IsHit == 1)
                {
                    data.IsHit = 0;
                    data.Animation = 4;
                    data.RemainInState = frameLengths[4];
                    data.Runtime = 0;
                }
                else if (data.DoAttack == 1)
                {
                    data.DoAttack = 0;
                    data.Animation = 3;
                    data.RemainInState = frameLengths[3];
                    data.Runtime = 0;
                }
                else if (data.RemainInState <= 0)
                {
                    if (data.MoveSpeed > 0)
                    {
                        data.Animation = 1;
                    }
                    else
                    {
                        data.Animation = 0;
                    }
                }
                float currentRuntime = data.Runtime % frameLengths[data.Animation];
                stateData.YPos = frameStart[data.Animation] * singleFrame + currentRuntime * singleFrame;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var handle = new UpdateComponentDataJob { dt = Time.deltaTime, frameLengths = FrameLengths, frameStart = FrameStart, singleFrame = 1f / TexHeight }.Schedule(this, 64, inputDeps);
            return handle;
        }

        protected override void OnCreateManager(int capacity)
        {
            FrameStart = new NativeArray<float>(new float[]
            {
                1,
                45,
                89,
                121,
                146
            }, Allocator.Persistent);
            FrameLengths = new NativeArray<float>(new float[]
            {
                1.04f,
                1.04f,
                0.75f,
                0.58f,
                0.46f
            }, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            if (FrameLengths.IsCreated)
                FrameLengths.Dispose();
            if (FrameStart.IsCreated)
                FrameStart.Dispose();
        }
    }
}
