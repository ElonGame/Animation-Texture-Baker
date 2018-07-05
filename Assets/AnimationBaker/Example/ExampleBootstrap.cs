using System.Collections;
using System.Collections.Generic;
using Animationbaker.Components;
using Animationbaker.Systems;
using BE;
using Example.Components;
using Example.Systems;
using NavJob.Components;
using NavJob.Systems;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Example
{
    public static class ExampleBootstrap
    {
        // ArchetypeReferenceStart - Do not remove
        // ArchetypeReferenceStop - Do not remove

        public static EntityArchetype Unit;
        static EntityManager manager;

        public static List<Animated> Animations = new List<Animated> ();

        static UnitData[] unitDatas;

        [RuntimeInitializeOnLoadMethod (RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot ()
        {
            var world = new World ("Example World");

            world.GetOrCreateManager<NavAgentSystem> ();
            var query = world.GetOrCreateManager<NavMeshQuerySystem> ();
            world.GetOrCreateManager<AnimatedRendererSystem> ();
            world.GetOrCreateManager<NavAgentToPositionSyncSystem> ();
            world.GetOrCreateManager<NavAgentAvoidanceSystem> ();
            var targetSystem = world.GetOrCreateManager<SetTargetSystem> ();
            targetSystem.Enabled = false;
            world.GetOrCreateManager<UpdateMatrixSystem> ();
            manager = world.GetOrCreateManager<EntityManager> ();
            // Uncomment the four lines below if you want to run all the worlds:
            var allWorlds = new World[] { world };
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop (allWorlds);
            World.Active = world;
            Unit = manager.CreateArchetype (
                typeof (SyncPositionFromNavAgent),
                typeof (Position),
                typeof (Unit),
                typeof (Animated),
                typeof (AnimatedState),
                typeof (NavAgent)
            );

            PlayerLoopManager.RegisterDomainUnload (DomainUnloadShutdown, 10000);

            // setup navmesh query
            query.UseCache = false;
            query.MaxQueries = 128;
            query.MaxIterations = 1024;
            // ArchetypeDeclarationStart - Do not remove
            // ArchetypeDeclarationStop - Do not remove
        }

        [RuntimeInitializeOnLoadMethod (RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Run ()
        {
            unitDatas = (UnitData[]) Resources.LoadAll<UnitData> ("UnitData");
            foreach (var unitData in unitDatas)
            {
                Animations.Add (new Animated
                {
                    Mesh = unitData.Mesh,
                        Material = unitData.Material,
                });
            }
            var caddy = GameObject.FindObjectOfType<MobileRTSCam> ();
            IsSpawning = true;
            if (caddy)
                caddy.StartCoroutine (Spawn ());
        }

        public static bool IsSpawning = false;

        static IEnumerator Spawn ()
        {
            yield return new WaitForSeconds (1);
            for (int i = 0; i < 10; i++)
            {
                var position = Vector3.zero;
                position.y = 0;
                position.x = Random.Range (5, 96);
                position.z = Random.Range (5, 96);
                var rotation = Quaternion.identity;
                var state = new AnimatedState { Matrix = Matrix4x4.TRS (position, rotation, Vector3.one), State = 0 };
                var animationIndex = Random.Range (0, Animations.Count);
                var animated = Animations[animationIndex];
                var unitData = unitDatas[animationIndex];
                var unit = new Unit
                {
                    MaxHealth = unitData.Health,
                    CurrentHealth = unitData.Health,
                    Damage = unitData.Damage,
                    AttackDistance = unitData.AttackDistance,
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
                var agent = new NavAgent (position, rotation, 0.5f, unit.MoveSpeed, 1, 2f, -1, 2f);
                var entity = manager.CreateEntity (Unit);
                manager.SetComponentData<Unit> (entity, unit);
                manager.SetComponentData<NavAgent> (entity, agent);
                manager.SetComponentData<AnimatedState> (entity, state);
                manager.SetSharedComponentData<Animated> (entity, animated);
                if (i % 10 == 0) yield return new WaitForEndOfFrame ();
            }
            yield return new WaitForSeconds (1);
            IsSpawning = false;
            var targetSystem = World.Active.GetOrCreateManager<SetTargetSystem> ();
            targetSystem.Enabled = true;
        }

        static void DomainUnloadShutdown ()
        {
            World.DisposeAllWorlds ();
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop ();
        }
    }
}