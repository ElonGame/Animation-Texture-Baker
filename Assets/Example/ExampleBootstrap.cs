using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Animationbaker.Components;
using Animationbaker.Systems;
using Example.Behaviours;
using Example.Components;
using Example.Systems;
using NavJob.Components;
using NavJob.Systems;

namespace Example
{
    public static class ExampleBootstrap
    {
        // ArchetypeReferenceStart - Do not remove
        // ArchetypeReferenceStop - Do not remove

        public static EntityArchetype Unit;
        public static List<Animated> Animations = new List<Animated>();
        public static UnitData[] UnitDatas;

        static EntityManager manager;
        static SpawnerSystem spawner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var world = new World("Example World");

            world.GetOrCreateManager<NavAgentSystem>();
            var query = world.GetOrCreateManager<NavMeshQuerySystem>();
            world.GetOrCreateManager<AnimatedRendererSystem>();
            world.GetOrCreateManager<NavAgentToPositionSyncSystem>();
            world.GetOrCreateManager<NavAgentAvoidanceSystem>();
            var targetSystem = world.GetOrCreateManager<SetTargetSystem>();
            spawner = world.GetOrCreateManager<SpawnerSystem>();
            world.GetOrCreateManager<UpdateMatrixSystem>();
            manager = world.GetOrCreateManager<EntityManager>();
            var allWorlds = new World[] { world };
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(allWorlds);
            World.Active = world;
            Unit = manager.CreateArchetype(
                typeof(SyncPositionFromNavAgent),
                typeof(Position),
                typeof(TransformMatrix),
                typeof(Unit),
                typeof(Animated),
                typeof(AnimatedState),
                typeof(NavAgent)
            );

            PlayerLoopManager.RegisterDomainUnload(DomainUnloadShutdown, 10000);

            // setup navmesh query
            query.UseCache = false;
            // ArchetypeDeclarationStart - Do not remove
            // ArchetypeDeclarationStop - Do not remove
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Run()
        {
            UnitDatas = (UnitData[]) Resources.LoadAll<UnitData>("UnitData");
            foreach (var unitData in UnitDatas)
            {
                Animations.Add(new Animated
                {
                    Mesh = unitData.Mesh,
                        Material = unitData.Material,
                });
            }
            var caddy = GameObject.FindObjectOfType<MobileRTSCam>();
            if (caddy)
                spawner.PendingSpawn = 1000;
        }

        static void DomainUnloadShutdown()
        {
            World.DisposeAllWorlds();
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop();
        }
    }
}
