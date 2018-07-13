using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using AnimationBaker.Components;

namespace AnimationBaker.Baked
{
    // Animations:
    // 0: Attack
    // 1: Hit
    // 2: Die
    // 3: Idle
    // 4: Walk

    public class BakedArcherSystem : JobComponentSystem
    {
        public float TexHeight = 256f;
        public NativeArray<float> Frames;
        public NativeArray<float> FrameStart;
        public NativeArray<float> FrameLengths;

        public struct UpdateComponentDataJob : IJobProcessComponentData<BakedArcher, StateMachineUnit>
        {
            [ReadOnly] public NativeArray<float> frames;
            [ReadOnly] public NativeArray<float> frameStart;
            [ReadOnly] public NativeArray<float> frameLengths;
            [ReadOnly] public float singleFrame;
            public float dt;
            public void Execute(ref BakedArcher data, ref StateMachineUnit stateData)
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
                    data.Animation = 1;
                    data.RemainInState = frameLengths[1];
                    data.Runtime = 0;
                }
                else if (data.DoAttack == 1)
                {
                    data.DoAttack = 0;
                    data.Animation = 0;
                    data.RemainInState = frameLengths[0];
                    data.Runtime = 0;
                }
                else if (data.RemainInState <= 0)
                {
                    if (data.MoveSpeed > 0)
                    {
                        data.Animation = 4;
                    }
                    else
                    {
                        data.Animation = 3;
                    }
                }
                float currentFrame = (data.Runtime % frameLengths[data.Animation]) * frames[data.Animation] * singleFrame;
                stateData.YPos = frameStart[data.Animation] * singleFrame + currentFrame;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var handle = new UpdateComponentDataJob { dt = Time.deltaTime, frames = Frames, frameLengths = FrameLengths, frameStart = FrameStart, singleFrame = 1f / TexHeight }.Schedule(this, 64, inputDeps);
            return handle;
        }

        protected override void OnCreateManager(int capacity)
        {
            Frames = new NativeArray<float>(new float[]
            {
                73,
                18,
                35,
                35,
                56
            }, Allocator.Persistent);
            FrameStart = new NativeArray<float>(new float[]
            {
                1,
                75,
                93,
                127,
                163
            }, Allocator.Persistent);
            FrameLengths = new NativeArray<float>(new float[]
            {
                1.75f,
                0.42f,
                0.83f,
                0.83f,
                1.33f
            }, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            if (Frames.IsCreated)
                Frames.Dispose();
            if (FrameLengths.IsCreated)
                FrameLengths.Dispose();
            if (FrameStart.IsCreated)
                FrameStart.Dispose();
        }
    }
}
