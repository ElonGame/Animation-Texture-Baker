using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Animationbaker.Components;
using Example.Components;
using NavJob.Components;
using NavJob.Systems;

namespace Example.Systems
{
    [UpdateAfter(typeof(NavAgentToPositionSyncSystem))]
    public class SetTargetSystem : JobComponentSystem
    {
        NativeQueue<DamageInfo> sendDamage;
        NativeQueue<PathfindingInfo> needsPathfinding;

        struct DamageInfo
        {
            public int Index;
            public float Damage;
        }

        struct PathfindingInfo
        {
            public Entity Entity;
            public NavAgent Agent;
            public float3 Destination;
        }

        struct SetTargetJob : IJobParallelFor
        {
            [ReadOnly] EntityArray entities;
            ComponentDataArray<AnimatedState> animations;
            [ReadOnly] ComponentDataArray<Position> positions;
            ComponentDataArray<NavAgent> agents;
            ComponentDataArray<Unit> units;
            [WriteOnly] EntityCommandBuffer.Concurrent buffer;
            [WriteOnly] public NativeQueue<DamageInfo>.Concurrent sendDamage;
            [WriteOnly] public NativeQueue<PathfindingInfo>.Concurrent needsPathfinding;
            [ReadOnly] float dt;
            [ReadOnly] float time;

            public SetTargetJob(float dt, float time, EntityCommandBuffer buffer, NativeQueue<DamageInfo>.Concurrent sendDamage, NativeQueue<PathfindingInfo>.Concurrent needsPathfinding, EntityArray entities, ComponentDataArray<NavAgent> agents, ComponentDataArray<AnimatedState> animations, ComponentDataArray<Position> positions, ComponentDataArray<Unit> units)
            {
                this.dt = dt;
                this.animations = animations;
                this.entities = entities;
                this.agents = agents;
                this.positions = positions;
                this.units = units;
                this.sendDamage = sendDamage;
                this.needsPathfinding = needsPathfinding;
                this.buffer = buffer;
                this.time = time;
            }

            [BurstCompile]
            public void Execute(int id)
            {
                var unit = units[id];
                var agent = agents[id];
                var animation = animations[id];
                if (unit.TargetIndex >= positions.Length)
                {
                    unit.TargetIndex = 0;
                    unit.State = UnitState.FindTarget;
                    units[id] = unit;
                    return;
                }
                switch (unit.State)
                {
                    case UnitState.FindTarget:
                        if (id + 1 < entities.Length)
                            unit.TargetIndex = id + 1;
                        else if (entities.Length > 1)
                            unit.TargetIndex = 0;

                        if (unit.TargetIndex != 0)
                        {
                            needsPathfinding.Enqueue(new PathfindingInfo { Entity = entities[id], Agent = agent, Destination = positions[unit.TargetIndex].Value });
                            unit.State = UnitState.Pathfinding;
                        }
                        break;
                    case UnitState.Pathfinding:
                        if (agent.totalWaypoints > 0)
                        {
                            animation.Clip = 1;
                            unit.State = UnitState.MoveInRange;
                            unit.NextRepath = 0.5f;
                        }
                        break;
                    case UnitState.MoveInRange:
                        var dist = math.length(positions[id].Value - positions[unit.TargetIndex].Value);
                        unit.NextRepath -= dt;
                        if (dist < unit.AttackDistance)
                        {
                            unit.AttackDurationTimer = unit.AttackDuration;
                            animation.Clip = 2;
                            unit.State = UnitState.Attacking;
                            agent.totalWaypoints = 0;
                            agent.remainingDistance = 0;
                            agent.nextWaypointIndex = 0;
                        }
                        else if (unit.NextRepath <= 0)
                        {
                            unit.NextRepath = 2f;
                            needsPathfinding.Enqueue(new PathfindingInfo { Entity = entities[id], Agent = agent, Destination = positions[unit.TargetIndex].Value });
                        }
                        else if (agent.totalWaypoints == 0)
                        {
                            animation.Clip = 0;
                            unit.State = UnitState.FindTarget;
                        }
                        break;
                    case UnitState.Attacking:
                        agent.totalWaypoints = 0;
                        agent.remainingDistance = 0;
                        agent.nextWaypointIndex = 0;
                        unit.AttackDurationTimer -= dt;
                        var heading = positions[unit.TargetIndex].Value - positions[id].Value;
                        if (math.length(heading) > 0.001f)
                        {
                            var rot = Quaternion.LookRotation(heading, Vector3.up);
                            agent.rotation = Quaternion.Slerp(agent.rotation, rot, 0.5f);
                        }
                        if (unit.AttackDurationTimer <= unit.AttackDuration / 2 && unit.HitSent == 0)
                        {
                            unit.HitSent = 1;
                            sendDamage.Enqueue(new DamageInfo { Index = unit.TargetIndex, Damage = unit.Damage });
                        }
                        if (unit.AttackDurationTimer <= 0)
                        {
                            animation.Clip = 0;
                            unit.AttackCooldownTimer = unit.AttackCooldown;
                            unit.State = UnitState.AttckCooldown;
                        }
                        break;
                    case UnitState.AttckCooldown:
                        unit.HitSent = 0;
                        unit.AttackCooldownTimer -= dt;
                        if (unit.AttackCooldownTimer <= 0)
                        {
                            animation.Clip = 0;
                            unit.State = UnitState.MoveInRange;
                        }
                        break;
                    case UnitState.GotHit:
                        animation.Clip = 3;
                        if (unit.HitCooldownTimer <= 0)
                            unit.HitCooldownTimer = unit.HitCooldown;
                        agent.totalWaypoints = 0;
                        agent.remainingDistance = 0;
                        agent.nextWaypointIndex = 0;
                        unit.State = UnitState.HitCooldown;
                        break;
                    case UnitState.HitCooldown:
                        unit.HitCooldownTimer -= dt;
                        if (unit.HitCooldownTimer <= 0)
                        {
                            animation.Clip = 0;
                            unit.State = UnitState.MoveInRange;
                        }
                        break;
                    case UnitState.Dying:
                        agent.totalWaypoints = 0;
                        agent.remainingDistance = 0;
                        agent.nextWaypointIndex = 0;
                        unit.DyingCooldownTimer = unit.DyingCooldown;
                        unit.State = UnitState.DyingCooldown;
                        animation.Clip = 4;
                        break;
                    case UnitState.DyingCooldown:
                        unit.DyingCooldownTimer -= dt;
                        if (unit.DyingCooldownTimer <= 0)
                        {
                            unit.State = UnitState.Dead;
                            animation.OverrideFrame = unit.DeadOffset;
                            buffer.RemoveComponent<NavAgent>(entities[id]);
                            buffer.AddComponent<IsDead>(entities[id], new IsDead { TimeOfDeath = time });
                        }
                        break;
                }
                animations[id] = animation;
                units[id] = unit;
                agents[id] = agent;
            }
        }

        struct InjectionData
        {
            public readonly int Length;
            [ReadOnly] public ComponentDataArray<Position> positions;
            [ReadOnly] public EntityArray entities;
            public ComponentDataArray<NavAgent> agents;
            public ComponentDataArray<AnimatedState> animations;
            public ComponentDataArray<Unit> units;
            public SubtractiveComponent<IsDead> sub;
        }

        [Inject] InjectionData data;
        [Inject] SetTargetSystemBarrier barrier;
        [Inject] NavAgentSystem navAgentSystem;
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var buffer = barrier.CreateCommandBuffer();
            int h = 0;
            while (sendDamage.TryDequeue(out DamageInfo info))
            {
                if (info.Index < data.Length)
                {
                    var unit = data.units[info.Index];
                    unit.CurrentHealth -= info.Damage;
                    if (unit.CurrentHealth <= 0)
                    {
                        if (unit.State != UnitState.Dying && unit.State != UnitState.DyingCooldown && unit.State != UnitState.Dead)
                            unit.State = UnitState.Dying;
                    }
                    else
                    {
                        unit.State = UnitState.GotHit;
                    }
                    data.units[info.Index] = unit;
                }
                h++;
                if (h > 1000)
                {
                    break;
                }
            }
            h = 0;
            while (needsPathfinding.TryDequeue(out PathfindingInfo info))
            {
                navAgentSystem.SetDestination(info.Entity, info.Agent, info.Destination);
                h++;
                if (h > 1000)
                {
                    break;
                }
            }
            var dt = Time.deltaTime;
            var setTargetJob = new SetTargetJob(dt, Time.time, buffer, sendDamage, needsPathfinding, data.entities, data.agents, data.animations, data.positions, data.units).Schedule(data.Length, 64, inputDeps);
            return setTargetJob;
        }

        protected override void OnCreateManager(int capacity)
        {
            sendDamage = new NativeQueue<DamageInfo>(Allocator.Persistent);
            needsPathfinding = new NativeQueue<PathfindingInfo>(Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            sendDamage.Dispose();
            needsPathfinding.Dispose();
        }

        protected override void OnStartRunning() { }

        protected override void OnStopRunning() { }
    }

    public class SetTargetSystemBarrier : BarrierSystem { }
}
