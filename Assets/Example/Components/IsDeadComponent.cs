using Unity.Entities;
using UnityEngine;

namespace Example.Components
{
    [System.Serializable]
    public struct IsDead : IComponentData
    {

    }

    [System.Serializable]
    [RequireComponent (typeof (GameObjectEntity))]
    public class IsDeadComponent : ComponentDataWrapper<IsDead>
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

