using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Example.Systems;

namespace Example.Behaviours
{
	public class Spawner : MonoBehaviour
	{
		public void Spawn(int quantity = 100)
		{
			World.Active.GetOrCreateManager<SpawnerSystem>().PendingSpawn += quantity;
		}
	}
}
