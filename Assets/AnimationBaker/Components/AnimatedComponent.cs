using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Animationbaker.Components
{
    [System.Serializable]
    public struct Animated : ISharedComponentData
    {
        public Mesh Mesh;
        public Material Material;
    }

    [System.Serializable]
    [RequireComponent (typeof (GameObjectEntity))]
    public class AnimatedComponent : SharedComponentDataWrapper<Animated>
    {
        // EntityManager manager;
        // Entity entity;
        // private void Start ()
        // {
        //     manager = GetComponent<GameObjectEntity> ().EntityManager;
        //     entity = GetComponent<GameObjectEntity> ().Entity;
        // }
        // private void Update () { }
    }
}