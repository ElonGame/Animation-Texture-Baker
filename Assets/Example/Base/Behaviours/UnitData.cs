using UnityEngine;

namespace Example.Base.Behaviours
{
    [CreateAssetMenu(fileName = "UnitData", menuName = "Animation Baker Examples/Base Unit Data", order = 0)]
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
        public float AttackDuration;
        public float AttackCooldown;
        public float HitCooldown;
        public float DyingCooldown;
        public float DeadOffset;
    }
}
