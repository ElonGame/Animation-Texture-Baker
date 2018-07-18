using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using AnimationBaker;
using AnimationBaker.Baked;
using AnimationBaker.Components;
using AnimationBaker.Interfaces;
using AnimationBaker.StateMachine;
using AnimationBaker.Systems;
using Example.Systems;
using NavJob.Components;
using NavJob.Systems;

namespace Example.StateMachine
{
	public class StateMachineBootstrap : MonoBehaviour
	{
		[SerializeField]
		private EntityArchetype Unit;
		public StateGraph[] graphs;

		EntityManager manager;

		void Awake()
		{
			var world = new World("State Machine World");

			world.GetOrCreateManager<NavAgentSystem>();
			world.GetOrCreateManager<NavMeshQuerySystem>().UseCache = false;
			world.GetOrCreateManager<NavAgentToPositionSyncSystem>();
			world.GetOrCreateManager<NavAgentToRotationSyncSystem>();
			world.GetOrCreateManager<SyncStateMachineUnitTransforms>();
			world.GetOrCreateManager<ArcherDebugSystem>();

			AnimatorBootstrap.Create<Archer>(world, graphs[0]);
			AnimatorBootstrap.Create<Cerberus>(world, graphs[1]);
			AnimatorBootstrap.Create<Diablous>(world, graphs[2]);
			AnimatorBootstrap.Create<Knight>(world, graphs[3]);
			AnimatorBootstrap.Create<Mage>(world, graphs[4]);
			AnimatorBootstrap.Create<Sorceress>(world, graphs[5]);

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

			for (int i = 0; i < 100; i++)
			{
				Spawn<Archer>();
				Spawn<Cerberus>();
				Spawn<Mage>();
				Spawn<Diablous>();
				Spawn<Sorceress>();
				Spawn<Knight>();
			}
		}

		private void Spawn<T>() where T : struct, IUnitState
		{
			var position = new Vector3(
				UnityEngine.Random.Range(10f, 90f),
				0,
				UnityEngine.Random.Range(10f, 90f)
			);
			var unit = manager.CreateEntity(Unit);
			manager.SetComponentData<Position>(unit, new Position { Value = position });
			manager.SetComponentData<Rotation>(unit, new Rotation { Value = Quaternion.identity });
			manager.SetComponentData<StateMachineUnit>(unit, new StateMachineUnit { Matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one) });
			manager.SetComponentData<NavAgent>(unit, new NavAgent(position, Quaternion.identity, 0.1f, 2f));
			manager.AddComponent(unit, typeof(T));
		}

		static void DomainUnloadShutdown()
		{
			World.DisposeAllWorlds();
			ScriptBehaviourUpdateOrder.UpdatePlayerLoop();
		}
	}
}
