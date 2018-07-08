using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace AnimationBaker.Components
{

    [System.Serializable]
    public enum Qualifier
    {
        Equals = 1,
        Less = 2,
        More = 4,
    }

    [System.Serializable]
    public struct UnblittableVariable
    {
        string name;
        int id;
        System.Type type;
    }

    [System.Serializable]
    public struct Variable
    {
        int id;
        System.Type type;
    }

    [System.Serializable]
    public struct State
    {
        public int StateID;
        public float Duration;
        public Variable Variable;
        public Qualifier Qualifier;
        public float Value;
    }

    [System.Serializable]
    public struct Animated : ISharedComponentData
    {
        public Mesh Mesh;
        public Material Material;
        public State[] States;
        public override int GetHashCode()
        {
            return Mesh.GetHashCode() + Material.GetHashCode();
        }
    }

    [System.Serializable]
    [RequireComponent(typeof(GameObjectEntity))]
    public class AnimatedComponent : SharedComponentDataWrapper<Animated> { }
}
