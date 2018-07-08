using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using AnimationBaker.Components;
using Example.Components;
using NavJob.Components;

namespace Example.Systems
{
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    public class SpawnerSystem : ComponentSystem
    {

        public int PendingSpawn = 0;

        [Inject] SpawnerSystemBarrier barrier;
        protected override void OnUpdate()
        {
            if (PendingSpawn > 0)
            {
                Spawn(PendingSpawn);
                PendingSpawn = 0;
            }
        }

        private void Spawn(int quantity)
        {
            var buffer = barrier.CreateCommandBuffer();
            for (int i = 0; i < quantity; i++)
            {
                var position = Vector3.zero;
                position.y = 0;
                position.x = Random.Range(5, 96);
                position.z = Random.Range(5, 96);
                var rotation = Quaternion.identity;
                var animationIndex = Random.Range(0, ExampleBootstrap.Animations.Count);
                var animated = ExampleBootstrap.Animations[animationIndex];
                var unitData = ExampleBootstrap.UnitDatas[animationIndex];
                var unit = new Unit
                {
                    MaxHealth = unitData.Health,
                    CurrentHealth = unitData.Health,
                    Damage = unitData.Damage,
                    AttackDistance = unitData.AttackDistance,
                    AttackParticle = unitData.AttackParticle,
                    MoveSpeed = unitData.MoveSpeed,
                    Faction = unitData.Faction,
                    AttackDuration = unitData.AttackDuration,
                    AttackDurationTimer = 0f,
                    AttackCooldown = unitData.AttackCooldown,
                    AttackCooldownTimer = 0f,
                    HitCooldown = unitData.HitCooldown,
                    HitCooldownTimer = 0f,
                    DyingCooldown = unitData.DyingCooldown,
                    DyingCooldownTimer = 0f,
                    DeadOffset = unitData.DeadOffset
                };
                var agent = new NavAgent(position, rotation, 0.5f, unit.MoveSpeed, 1, 2f, -1);
                buffer.CreateEntity(ExampleBootstrap.Unit);
                buffer.SetComponent<Unit>(unit);
                buffer.SetComponent<NavAgent>(agent);
                buffer.SetComponent<AnimatedState>(new AnimatedState());
                buffer.SetSharedComponent<Animated>(animated);
            }
        }
    }

    public class SpawnerSystemBarrier : BarrierSystem { }
}
