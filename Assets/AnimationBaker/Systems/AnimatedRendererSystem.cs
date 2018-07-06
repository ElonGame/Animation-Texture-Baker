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

    // [AlwaysUpdateSystem]
    // [DisableAutoCreationAttribute]
    // [UpdateBefore (typeof (SomeType))]
    // [UpdateAfter (typeof (SomeType))]
    // [ExecuteInEditMode]
    public class AnimatedRendererSystem : ComponentSystem
    {

        public int lastLength;
        public Dictionary<int, Animated> cachedAnimated = new Dictionary<int, Animated>();
        public Dictionary<int, List<AnimatedState>> animatedData = new Dictionary<int, List<AnimatedState>>();
        public Dictionary<int, List<Matrix4x4>> matricesData = new Dictionary<int, List<Matrix4x4>>();
        public Dictionary<int, int> animatedIndex = new Dictionary<int, int>();
        public Matrix4x4[] matrix = new Matrix4x4[1023];
        public MaterialPropertyBlock block = new MaterialPropertyBlock();
        public float[] currentAnimations = new float[1023];
        public float[] overrideFrames = new float[1023];

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
                Render();
            }
            else
            {
                UpdateState();
                Render();
            }
        }

        private void Render()
        {
            foreach (var item in animatedData)
            {
                var currentLength = item.Value.Count;
                var values = item.Value;
                var start = 0;
                var hash = item.Key;
                var animated = cachedAnimated[hash];
                var matrices = matricesData[hash];
                while (start < currentLength)
                {
                    var len = Math.Min(1023, currentLength - start);
                    for (int j = 0; j < len; j++)
                    {
                        matrix[j] = matrices[start + j];
                        currentAnimations[j] = values[start + j].State;
                        overrideFrames[j] = values[start + j].SetFrame;
                    }
                    block.SetFloatArray("_OverrideFrame", overrideFrames);
                    block.SetFloatArray("_CurrentAnimation", currentAnimations);
                    Graphics.DrawMeshInstanced(animated.Mesh, 0, animated.Material, matrix, len, block, ShadowCastingMode.On, true);
                    start += len;
                }
            }
        }

        private void UpdateState()
        {
            for (int i = 0; i < data.entities.Length; i++)
            {
                var matrix = data.matrices[i].Value;
                var hash = data.animatedStates[i].AnimationHash;
                var index = animatedIndex[data.entities[i].Index];
                animatedData[hash][index] = data.animatedStates[i];
                matricesData[hash][index] = new Matrix4x4(matrix.c0, matrix.c1, matrix.c2, matrix.c3);
            }
            lastLength = data.entities.Length;
        }

        private void RefreshCache()
        {
            cachedAnimated.Clear();
            animatedData.Clear();
            animatedIndex.Clear();
            matricesData.Clear();
            for (int i = 0; i < data.entities.Length; i++)
            {
                var animated = data.animateds[i];
                var state = data.animatedStates[i];
                var hash = animated.GetHashCode();
                if (!cachedAnimated.ContainsKey(hash))
                {
                    cachedAnimated.Add(hash, animated);
                    animatedData.Add(hash, new List<AnimatedState>());
                    matricesData.Add(hash, new List<Matrix4x4>());
                }
                animatedData[hash].Add(data.animatedStates[i]);
                var matrix = data.matrices[i].Value;
                matricesData[hash].Add(new Matrix4x4(matrix.c0, matrix.c1, matrix.c2, matrix.c3));
                animatedIndex[data.entities[i].Index] = animatedData[hash].Count - 1;
                state.AnimationHash = hash;
                data.animatedStates[i] = state;
            }
            lastLength = data.entities.Length;
        }
    }
}
