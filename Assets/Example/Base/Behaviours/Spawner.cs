// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using Unity.Entities;
// using Example.Base.Systems;

// namespace Example.Base.Behaviours
// {
// 	public class Spawner : MonoBehaviour
// 	{

// 		public int InitialSpawn = 1000;
// 		public GameObject[] particles = new GameObject[0];

// 		public void Spawn(int quantity = 100)
// 		{
// 			World.Active.GetOrCreateManager<SpawnerSystem>().PendingSpawn += quantity;
// 		}
// 	}
// }
