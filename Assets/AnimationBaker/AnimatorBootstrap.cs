using Unity.Entities;
using AnimationBaker.Components;
using AnimationBaker.Interfaces;
using AnimationBaker.StateMachine;
using AnimationBaker.Systems;

namespace AnimationBaker
{
    public static class AnimatorBootstrap
    {
        public static AnimatorSystem<T> Create<T>(World world, StateGraph graph) where T : struct, IUnitState
        {
            var animator = world.GetOrCreateManager<AnimatorSystem<T>>();
            animator.StateGraph = graph;
            return animator;
        }
    }
}
