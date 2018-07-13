using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using AnimationBaker.Baked;
using AnimationBaker.Components;
using AnimationBaker.Systems;
using Example.Base.Systems;
using Example.StateMachine.Behaviours;
using NavJob.Components;
using NavJob.Systems;

namespace Example.StateMachine
{
	public class StateMachineBootstrap : MonoBehaviour
	{
		[SerializeField]
		private UnitData[] unitDatas = new UnitData[0];
		private EntityArchetype Unit;

		EntityManager manager;
		Entity Entity;

		void Awake()
		{
			var world = new World("State Machine World");

			world.GetOrCreateManager<NavAgentSystem>();
			world.GetOrCreateManager<NavMeshQuerySystem>().UseCache = false;
			world.GetOrCreateManager<NavAgentToPositionSyncSystem>();
			world.GetOrCreateManager<NavAgentToRotationSyncSystem>();
			world.GetOrCreateManager<SyncStateMachineUnitTransforms>();
			world.GetOrCreateManager<BakedArcherSystem>();
			world.GetOrCreateManager<BakedMageSystem>();
			var renderer = world.GetOrCreateManager<StateMachineUnitRendererSystem>();

			manager = world.GetOrCreateManager<EntityManager>();
			var allWorlds = new World[] { world };
			ScriptBehaviourUpdateOrder.UpdatePlayerLoop(allWorlds);
			World.Active = world;
			Unit = manager.CreateArchetype(
				typeof(Position),
				typeof(Rotation),
				typeof(SyncPositionFromNavAgent),
				typeof(SyncRotationFromNavAgent),
				typeof(StateMachineUnit),
				typeof(NavAgent)
			);
			PlayerLoopManager.RegisterDomainUnload(DomainUnloadShutdown, 10000);

			foreach (var data in unitDatas)
			{
				renderer.RegisterRenderer(data.GetHashCode(), data.Material, data.Mesh);
				Spawn(data);
			}
		}

		private void Spawn(UnitData data)
		{
			var position = new Vector3(
				10,
				0,
				10
			);
			// var position = new Vector3(
			// 	UnityEngine.Random.Range(-10, 10),
			// 	0,
			// 	UnityEngine.Random.Range(-10, 10)
			// );
			var unit = Entity = manager.CreateEntity(Unit);
			manager.SetComponentData<Position>(unit, new Position { Value = position });
			manager.SetComponentData<Rotation>(unit, new Rotation { Value = Quaternion.identity });
			manager.SetComponentData<StateMachineUnit>(unit, new StateMachineUnit { Matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one), RendererHash = data.GetHashCode() });
			manager.SetComponentData<NavAgent>(unit, new NavAgent(position, Quaternion.identity, 0.1f, 2f));
			switch (data.Mesh.name)
			{
				case "ArcherMesh":
					manager.AddComponent(unit, typeof(BakedArcher));
					break;
				case "MageMesh":
					manager.AddComponent(unit, typeof(BakedMage));
					break;
			}
		}

		static void DomainUnloadShutdown()
		{
			World.DisposeAllWorlds();
			ScriptBehaviourUpdateOrder.UpdatePlayerLoop();
		}
	}
}
