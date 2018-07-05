using System;
using System.Collections.Generic;
using Animationbaker.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

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
        public List<Animated> cachedAnimated = new List<Animated> ();
        public Dictionary<Animated, List<AnimatedState>> animatedData = new Dictionary<Animated, List<AnimatedState>> ();
        public Dictionary<int, int> animatedIndex = new Dictionary<int, int> ();
        public Matrix4x4[] matrix = new Matrix4x4[1023];
        public MaterialPropertyBlock block = new MaterialPropertyBlock ();
        public float[] currentAnimations = new float[1023];
        public float[] overrideFrames = new float[1023];

        struct InjectData
        {
            [ReadOnly] public EntityArray entities;
            [ReadOnly] public ComponentDataArray<AnimatedState> animatedStates;
            [ReadOnly] public SharedComponentDataArray<Animated> animateds;
        }

        [Inject] InjectData data;
        // [Inject] AnimatedRendererSystemBarrier barrier;
        protected override void OnUpdate ()
        {
            if (data.entities.Length != lastLength)
            {
                RefreshCache ();
                Render ();
            }
            else
            {
                UpdateTuples ();
                Render ();
            }
        }

        private void Render ()
        {
            foreach (var item in animatedData)
            {
                var currentLength = item.Value.Count;
                var animated = item.Key;
                var values = item.Value;
                var start = 0;
                while (start < currentLength)
                {
                    var len = Math.Min (1023, currentLength - start);
                    for (int j = 0; j < len; j++)
                    {
                        matrix[j] = values[start + j].Matrix;
                        currentAnimations[j] = values[start + j].State;
                        overrideFrames[j] = values[start + j].SetFrame;
                    }
                    block.SetFloatArray ("_OverrideFrame", overrideFrames);
                    block.SetFloatArray ("_CurrentAnimation", currentAnimations);
                    Graphics.DrawMeshInstanced (animated.Mesh, 0, animated.Material, matrix, len, block, ShadowCastingMode.On, true);
                    start += len;
                }
            }
        }

        private void UpdateTuples ()
        {
            for (int i = 0; i < data.entities.Length; i++)
            {
                animatedData[data.animateds[i]][animatedIndex[data.entities[i].Index]] = data.animatedStates[i];
            }
        }

        private void RefreshCache ()
        {
            cachedAnimated.Clear ();
            animatedData.Clear ();
            animatedIndex.Clear ();
            for (int i = 0; i < data.entities.Length; i++)
            {
                var animated = data.animateds[i];
                if (!cachedAnimated.Contains (animated))
                {
                    cachedAnimated.Add (animated);
                    animatedData.Add (animated, new List<AnimatedState> ());
                }
                animatedData[animated].Add (data.animatedStates[i]);
                animatedIndex[data.entities[i].Index] = animatedData[animated].Count - 1;
            }
            lastLength = data.entities.Length;
        }

        protected override void OnCreateManager (int capacity)
        {

        }

        protected override void OnDestroyManager ()
        {

        }
        protected override void OnStartRunning ()
        {

        }

        protected override void OnStopRunning ()
        {

        }
    }

    // public class AnimatedRendererSystemBarrier : BarrierSystem { }
}