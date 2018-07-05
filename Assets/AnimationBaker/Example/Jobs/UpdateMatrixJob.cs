using Animationbaker.Components;
using NavJob.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Example.Jobs
{
    // [RequireComponentTag(typeof(SOMECOMPONENT))]
    // [RequireSubtractiveComponent (typeof (SOMESUBTRACTIVECOMPONENT))]
    [BurstCompile]
    public struct UpdateMatrixJob : IJobProcessComponentData<NavAgent, AnimatedState>
    {
        float dt;

        public UpdateMatrixJob (float dt)
        {
            this.dt = dt;
        }

        public void Execute ([ReadOnly] ref NavAgent agent, ref AnimatedState state)
        {
            state.Matrix = Matrix4x4.TRS (agent.position, agent.rotation, Vector3.one);
        }
    }
}