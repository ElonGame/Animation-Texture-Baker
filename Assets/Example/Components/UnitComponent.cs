using UnityEngine;
using Unity.Entities;

namespace Example.Components
{
    public enum UnitState : short
    {
        FindTarget,
        Pathfinding,
        MoveInRange,
        Attacking,
        AttckCooldown,
        GotHit,
        HitCooldown,
        Dying,
        DyingCooldown,
        Dead,
    }

    [System.Serializable]
    public struct Unit : IComponentData
    {
        public int HasTarget;
        public int TargetIndex;
        public float MaxHealth;
        public float CurrentHealth;
        public float Damage;
        public float AttackDistance;
        public float MoveSpeed;
        public int Faction;
        public float NextRepath;
        public int HitSent;
        public float AttackDuration;
        public int AttackParticle;
        public float AttackDurationTimer;
        public float AttackCooldown;
        public float AttackCooldownTimer;
        public float HitCooldown;
        public float HitCooldownTimer;
        public float DyingCooldown;
        public float DyingCooldownTimer;
        public float DyingDisposeTimer;
        public float DeadOffset;
        public UnitState State;
    }

    [System.Serializable]
    [RequireComponent(typeof(GameObjectEntity))]
    public class UnitComponent : ComponentDataWrapper<Unit>
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
