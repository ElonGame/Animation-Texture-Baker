using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NavJob.Components
{
    [System.Serializable]
    public struct NavAagentAvoidance : IComponentData
    {
        public float radius;
        public float3 partition { get; set; }

        public NavAagentAvoidance (
            float radius = 1f
        )
        {
            this.radius = radius;
            this.partition = new float3 (0);
        }
    }

    public class NavAagentAvoidanceComponent : ComponentDataWrapper<NavAagentAvoidance> { }
}