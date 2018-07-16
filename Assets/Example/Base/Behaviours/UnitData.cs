using UnityEngine;

namespace Example.Base.Behaviours
{
    [CreateAssetMenu(fileName = "UnitData", menuName = "Animation Baker Examples/Base Unit Data", order = 0)]
    public class UnitData : ScriptableObject
    {
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
