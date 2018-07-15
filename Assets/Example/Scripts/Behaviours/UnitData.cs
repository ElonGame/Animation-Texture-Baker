using UnityEngine;
using UnityEngine.Rendering;
using Unity.Entities;

namespace Example.Behaviours
{
    [CreateAssetMenu(fileName = "UnitData", menuName = "Animation Baker Examples/State Machine Unit Data", order = 0)]
    public class UnitData : ScriptableObject
    {
        public Mesh Mesh;
        public int SubMeshIndex;
        public Material Material;
        public ShadowCastingMode ShadowCastingMode;
        public bool ReceivesShadows;
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
