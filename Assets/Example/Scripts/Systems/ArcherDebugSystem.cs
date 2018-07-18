using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Entities;
using AnimationBaker.Baked;
using AnimationBaker.Components;
using AnimationBaker.Systems;

namespace Example.Systems
{
    public class ArcherDebugSystem : ComponentSystem
    {

        struct InjectData
        {
            public readonly int Length;
            [ReadOnly] public EntityArray entities;
            [ReadOnly] public ComponentDataArray<Archer> archers;
        }

        bool isMoving = false;
        bool isDead = false;

        [Inject] InjectData components;
        [Inject] AnimatorSystem<Archer> archerSystem;
        protected override void OnUpdate()
        {
            if (components.Length < 1) return;
            if (Input.GetKeyUp(KeyCode.Q))
            {
                archerSystem.SetTrigger(components.entities[0], "Attack");
            }
            if (Input.GetKeyUp(KeyCode.W))
            {
                archerSystem.SetTrigger(components.entities[0], "GotHit");
            }
            if (Input.GetKeyUp(KeyCode.E))
            {
                if (!isMoving)
                {
                    isMoving = true;
                    archerSystem.SetFloat(components.entities[0], "MoveSpeed", 2);
                }
                else
                {
                    isMoving = false;
                    archerSystem.SetFloat(components.entities[0], "MoveSpeed", 0);
                }
            }
            if (Input.GetKeyUp(KeyCode.R))
            {
                if (!isDead)
                {
                    isDead = true;
                }
                else
                {
                    isDead = false;
                }
                archerSystem.SetBool(components.entities[0], "IsDead", isDead);
            }
        }
    }
}
