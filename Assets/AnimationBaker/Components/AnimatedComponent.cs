using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace Animationbaker.Components
{
    [System.Serializable]
    public struct Animated : ISharedComponentData
    {
        public Mesh Mesh;
        public Material Material;
        public override int GetHashCode()
        {
            return Mesh.GetHashCode() + Material.GetHashCode();
        }
    }

    [System.Serializable]
    [RequireComponent(typeof(GameObjectEntity))]
    public class AnimatedComponent : SharedComponentDataWrapper<Animated> { }
}
