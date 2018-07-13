using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Entities;
using AnimationBaker.Components;

namespace AnimationBaker.Systems
{
    public class StateMachineUnitRendererSystem : ComponentSystem
    {

        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();

        struct Renderer
        {
            public Mesh Mesh;
            public Material Material;
        }

        Dictionary<int, Renderer> renderers = new Dictionary<int, Renderer>();

        struct InjectData
        {
            public readonly int Length;
            public ComponentDataArray<StateMachineUnit> stateData;
        }

        [Inject] InjectData components;
        protected override void OnUpdate()
        {
            for (int i = 0; i < components.Length; i++)
            {
                var stateData = components.stateData[i];
                var renderer = renderers[stateData.RendererHash];
                materialPropertyBlock.SetFloatArray("_YPos", new float[] { stateData.YPos });
                Graphics.DrawMeshInstanced(renderer.Mesh, 0, renderer.Material, new Matrix4x4[] { stateData.Matrix }, 1, materialPropertyBlock, ShadowCastingMode.On, true);
            }
        }

        public void RegisterRenderer(int hashCode, Material material, Mesh mesh)
        {
            renderers.Add(hashCode, new Renderer { Mesh = mesh, Material = material });
        }
    }
}
