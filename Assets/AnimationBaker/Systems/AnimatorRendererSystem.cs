using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using AnimationBaker.Components;
using AnimationBaker.Interfaces;
using AnimationBaker.StateMachine.ScriptableObjects;

namespace AnimationBaker.Systems
{
    public partial class AnimatorSystem<T> : BaseAnimatorSystem<T> where T : struct, IUnitState
    {
        class RendererData
        {
            public Matrix4x4[] matrices;
            public float[] yPositions;
            public int Length;

            public RendererData(int length)
            {
                this.Length = length;
                matrices = new Matrix4x4[length];
                yPositions = new float[length];
            }
        }

        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        NativeArray<Matrix4x4> matrices;
        NativeArray<float> yPositions;
        RendererData rendererData = new RendererData(0);
        Matrix4x4[] tempMatrices = new Matrix4x4[1023];
        float[] tempYPositions = new float[1023];

        struct InjectRendererData
        {
            public readonly int Length;
            [ReadOnly] public ComponentDataArray<T> components;
            [ReadOnly] public ComponentDataArray<StateMachineUnit> stateDatas;
        }

        [Inject] private InjectRendererData injectRendererDatas;

        protected JobHandle Render(JobHandle inputDeps)
        {
            if (injectRendererDatas.Length != rendererData.Length)
            {
                DestroyRendererArrays();
                CreateRendererArrays(injectRendererDatas.Length);
                rendererData = new RendererData(injectRendererDatas.Length);
            }
            var handle = new UpdateDataJob { matrices = matrices, yPositions = yPositions, units = injectRendererDatas.stateDatas }.Schedule(injectRendererDatas.Length, 64, inputDeps);
            handle.Complete();
            matrices.CopyTo(rendererData.matrices);
            yPositions.CopyTo(rendererData.yPositions);

            for (int i = 0; i < injectRendererDatas.Length; i += 1023)
            {
                var len = Mathf.Min(injectRendererDatas.Length - i, 1023);
                Array.Copy(rendererData.matrices, i, tempMatrices, 0, len);
                Array.Copy(rendererData.yPositions, i, tempYPositions, 0, len);
                materialPropertyBlock.SetFloatArray("_YPos", tempYPositions);
                for (int j = 0; j < StateGraph.rendererData.SubMeshCount; j++)
                {
                    Graphics.DrawMeshInstanced(StateGraph.rendererData.Mesh, j, StateGraph.rendererData.Materials[j], tempMatrices, len, materialPropertyBlock, StateGraph.rendererData.ShadowCastingMode, StateGraph.rendererData.ReceivesShadows);
                }
            }

            return handle;
        }

        [BurstCompile]
        struct UpdateDataJob : IJobParallelFor
        {
            [WriteOnly] public NativeArray<Matrix4x4> matrices;
            [WriteOnly] public NativeArray<float> yPositions;
            [ReadOnly] public ComponentDataArray<StateMachineUnit> units;

            public void Execute(int index)
            {
                matrices[index] = units[index].Matrix;
                yPositions[index] = units[index].YPos;
            }
        }

        protected void DestroyRenderer()
        {
            DestroyRendererArrays();
        }

        private void DestroyRendererArrays()
        {
            if (yPositions.IsCreated)
                yPositions.Dispose();
            if (matrices.IsCreated)
                matrices.Dispose();
        }

        private void CreateRendererArrays(int length)
        {
            yPositions = new NativeArray<float>(length, Allocator.Persistent);
            matrices = new NativeArray<Matrix4x4>(length, Allocator.Persistent);
        }
    }
}
