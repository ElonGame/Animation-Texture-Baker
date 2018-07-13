using UnityEngine;
using Unity.Entities;

namespace Example.StateMachine.Behaviours
{
    [CreateAssetMenu(fileName = "UnitData", menuName = "Animation Baker Examples/State Machine Unit Data", order = 0)]
    public class UnitData : ScriptableObject
    {
        public Mesh Mesh;
        public Material Material;
        public float Health;
        public float Damage;
        public float AttackDistance;
        public int AttackParticle;
        public float MoveSpeed;
        public int Faction;
        public float AttackCooldown;
        public float HitCooldown;
    }
}
