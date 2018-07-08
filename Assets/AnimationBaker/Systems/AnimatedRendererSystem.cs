using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Animationbaker.Components;

namespace Animationbaker.Systems
{
    public class AnimatedRendererSystem : ComponentSystem
    {

        private int lastLength;
        private Dictionary<int, Animated> cachedAnimated = new Dictionary<int, Animated>();
        private Dictionary<int, List<Matrix4x4>> matricesData = new Dictionary<int, List<Matrix4x4>>();
        private Dictionary<int, List<float>> entityIndices = new Dictionary<int, List<float>>();
        private Dictionary<int, List<float>> clipDatas = new Dictionary<int, List<float>>();
        private Dictionary<int, List<float>> overrideFrames = new Dictionary<int, List<float>>();
        private MaterialPropertyBlock block = new MaterialPropertyBlock();

        private Matrix4x4[] tempMatrices = new Matrix4x4[1023];
        private float[] tempIndices = new float[1023];
        private float[] tempClipDatas = new float[1023];
        private float[] overrideFrameDatas = new float[1023];

        ComputeBuffer randomWriteBuffer;

        struct InjectData
        {
            public readonly int Length;
            [ReadOnly] public EntityArray entities;
            [ReadOnly] public ComponentDataArray<TransformMatrix> matrices;
            public ComponentDataArray<AnimatedState> animatedStates;
            [ReadOnly] public SharedComponentDataArray<Animated> animateds;
        }

        [Inject] InjectData data;
        protected override void OnUpdate()
        {
            if (data.entities.Length != lastLength)
            {
                RefreshCache();
            }
            UpdateState();
            Render();
        }

        private void Render()
        {
            foreach (var item in cachedAnimated)
            {
                var hash = item.Key;
                var animated = item.Value;
                var matrices = matricesData[hash];
                var entityIds = entityIndices[hash];
                var clipData = clipDatas[hash];
                var currentLength = matrices.Count;
                var start = 0;
                while (start < currentLength)
                {
                    var len = Math.Min(1023, currentLength - start);
                    matrices.CopyTo(start, tempMatrices, 0, len);
                    entityIds.CopyTo(start, tempIndices, 0, len);
                    clipData.CopyTo(start, tempClipDatas, 0, len);
                    block.SetFloatArray("_CurrentAnimation", tempClipDatas);
                    block.SetFloatArray("_EntityID", tempIndices);
                    Graphics.DrawMeshInstanced(animated.Mesh, 0, animated.Material, tempMatrices, len, block, ShadowCastingMode.On, true);
                    start += len;
                }
            }
        }

        private void UpdateState()
        {
            foreach (var item in matricesData)
            {
                item.Value.Clear();
            }
            foreach (var item in clipDatas)
            {
                item.Value.Clear();
            }
            foreach (var item in overrideFrames)
            {
                item.Value.Clear();
            }
            for (int i = 0; i < data.entities.Length; i++)
            {
                var matrix = data.matrices[i].Value;
                var state = data.animatedStates[i];
                var hash = state.AnimationHash;
                matricesData[hash].Add(new Matrix4x4(matrix.c0, matrix.c1, matrix.c2, matrix.c3));
                clipDatas[hash].Add(state.Clip);
                overrideFrames[hash].Add(state.OverrideFrame);
            }
            lastLength = data.entities.Length;
        }

        private void RefreshCache()
        {
            cachedAnimated.Clear();
            entityIndices.Clear();
            matricesData.Clear();
            clipDatas.Clear();
            overrideFrames.Clear();
            for (int i = 0; i < data.entities.Length; i++)
            {
                int index = data.entities[i].Index;
                var animated = data.animateds[i];
                var state = data.animatedStates[i];
                var hash = animated.GetHashCode();
                if (!cachedAnimated.ContainsKey(hash))
                {
                    cachedAnimated.Add(hash, animated);
                    matricesData.Add(hash, new List<Matrix4x4>());
                    entityIndices.Add(hash, new List<float>());
                    clipDatas.Add(hash, new List<float>());
                    overrideFrames.Add(hash, new List<float>());
                    animated.Material.SetBuffer("_RandomWrite", randomWriteBuffer);
                    Graphics.SetRandomWriteTarget(1, randomWriteBuffer);
                }
                var matrix = data.matrices[i].Value;
                entityIndices[hash].Add(index);
                state.AnimationHash = hash;
                data.animatedStates[i] = state;
            }
            lastLength = data.entities.Length;
        }

        protected override void OnCreateManager(int capacity)
        {
            randomWriteBuffer = new ComputeBuffer(1024 * 1024, 4 * 4, ComputeBufferType.Default);
        }

        protected override void OnDestroyManager()
        {
            Graphics.ClearRandomWriteTargets();
            randomWriteBuffer.Dispose();
        }
    }
}
