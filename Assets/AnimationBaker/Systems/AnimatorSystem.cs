using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using AnimationBaker.Components;
using AnimationBaker.Interfaces;
using AnimationBaker.StateMachine;
using AnimationBaker.StateMachine.Nodes;
using AnimationBaker.StateMachine.XNode;

namespace AnimationBaker.Systems
{
    [UpdateBefore(typeof(SyncStateMachineUnitTransforms))]
    public partial class AnimatorSystem<T> : BaseAnimatorSystem<T> where T : struct, IUnitState
    {
        public StateGraph StateGraph;
        NativeArray<StateData> states;
        NativeArray<float> frameStart;
        NativeMultiHashMap<int, TransitionData> transitions;
        bool HasSetup;

        [BurstCompile]
        public struct UpdateComponentDataJob<U> : IJobParallelFor where U : struct, IUnitState
        {
            public ComponentDataArray<U> components;
            public ComponentDataArray<StateMachineUnit> stateDatas;

            [ReadOnly] public NativeMultiHashMap<int, TransitionData> transitions;
            [ReadOnly] public NativeArray<StateData> states;
            [ReadOnly] public NativeArray<float> frameStart;
            [ReadOnly] public float singleFrame;
            [ReadOnly] public int anyStateIndex;

            public float dt;

            public void Execute(int index)
            {
                var component = components[index];
                var stateData = stateDatas[index];

                component.Runtime += dt;

                if (component.StateTimer > 0)
                    component.StateTimer -= dt;

                var anyStateChild = GetNextState(anyStateIndex, ref component);
                if (anyStateChild.index != anyStateIndex && anyStateChild.index != component.CurrentState)
                {
                    component.PreviousState = component.CurrentState;
                    component.CurrentState = anyStateChild.index;
                    component.Runtime = 0;
                    if (anyStateChild.wrapMode == WrapMode.Once)
                    {
                        component.StateTimer = anyStateChild.duration;
                    }
                }
                else
                {
                    var state = states[component.CurrentState];
                    if (state.duration == 0 || (state.wrapMode == WrapMode.Once && component.StateTimer <= 0) || state.wrapMode == WrapMode.Loop || state.wrapMode == WrapMode.ClampForever)
                    {
                        StateData nextState = GetNextState(component.CurrentState, ref component);
                        if (nextState.index != state.index)
                        {
                            component.PreviousState = component.CurrentState;
                            component.CurrentState = nextState.index;
                            component.Runtime = 0;
                            if (nextState.wrapMode == WrapMode.Once)
                            {
                                component.StateTimer = nextState.duration;
                            }
                        }
                    }
                }

                var currentState = states[component.CurrentState];
                if (currentState.wrapMode == WrapMode.Clamp || currentState.wrapMode == WrapMode.ClampForever)
                {
                    if (component.Runtime > currentState.duration)
                    {
                        component.Runtime = currentState.duration - 0.0001f;
                    }
                }
                if (currentState.duration == 0)
                {
                    stateData.YPos = singleFrame;
                }
                else
                {
                    float currentFrame = ((component.Runtime % currentState.duration) / currentState.duration) * currentState.frames * singleFrame;
                    stateData.YPos = frameStart[component.CurrentState] * singleFrame + currentFrame;
                }

                components[index] = component;
                stateDatas[index] = stateData;
            }

            public StateData GetNextState(int state, ref U data)
            {
                var stateData = states[state];
                if (transitions.TryGetFirstValue(state, out TransitionData item, out NativeMultiHashMapIterator<int> iter))
                {
                    var result = CheckQualifier(item.qualifier, data[item.variable], item);
                    if (result)
                    {
                        stateData = item.toState;
                        if (item.isTrigger == 1)
                        {
                            data[item.variable] = 0;
                        }
                        if (item.qualifier == Qualifier.None)
                        {
                            return GetNextState(item.toState.index, ref data);
                        }
                        else
                        {
                            return stateData;
                        }
                    }
                    while (transitions.TryGetNextValue(out TransitionData nextItem, ref iter))
                    {
                        result = CheckQualifier(nextItem.qualifier, data[nextItem.variable], nextItem);
                        if (result)
                        {
                            stateData = nextItem.toState;
                            if (nextItem.isTrigger == 1)
                            {
                                data[nextItem.variable] = 0;
                            }
                            if (nextItem.qualifier == Qualifier.None)
                            {
                                return GetNextState(nextItem.toState.index, ref data);
                            }
                            else
                            {
                                return stateData;
                            }
                        }
                    }
                }
                return stateData;
            }

            private bool CheckQualifier(Qualifier qualifier, float currentValue, TransitionData nextItem)
            {
                float transitionValue = (nextItem.isFloat == 1 ? nextItem.floatValue : nextItem.intValue);
                switch (qualifier)
                {
                    case Qualifier.Equal:
                        return currentValue == transitionValue;
                    case Qualifier.LessThan:
                        return currentValue < transitionValue;
                    case Qualifier.LessThanOrEqual:
                        return currentValue <= transitionValue;
                    case Qualifier.MoreThan:
                        return currentValue > transitionValue;
                    case Qualifier.MoreThanOrEqual:
                        return currentValue >= transitionValue;
                    case Qualifier.NotEqual:
                        return currentValue != transitionValue;
                    case Qualifier.None:
                        return true;
                }
                return false;
            }
        }

        struct InjectData
        {
            public readonly int Length;
            public ComponentDataArray<T> components;
            public ComponentDataArray<StateMachineUnit> stateDatas;
        }

        [Inject] private InjectData injectDatas;
        [Inject] private ComponentDataFromEntity<T> currentData;
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (!HasSetup)
            {
                Setup();
            }
            ApplyUpdates(currentData);
            var handle = new UpdateComponentDataJob<T> { dt = Time.deltaTime, components = injectDatas.components, stateDatas = injectDatas.stateDatas, anyStateIndex = anyStateIndex, transitions = transitions, states = states, frameStart = frameStart, singleFrame = 1f / StateGraph.TextureHeight }.Schedule(injectDatas.Length, 64, inputDeps);
            handle.Complete();
            Render();
            return handle;
        }

        int anyStateIndex = -1;

        private void Setup()
        {
            var nodes = StateGraph.nodes.Select(x =>(BaseNode) x).ToArray();
            var clips = new List<AnimationClip>();
            states = new NativeArray<StateData>(nodes.Length, Allocator.Persistent);
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (node.NodeType == NodeType.Any)
                {
                    anyStateIndex = i;
                }
                if (node.Clip != null)
                    clips.Add(node.Clip);
                states[i] = new StateData
                {
                    index = i,
                    nodeType = node.NodeType,
                    clip = Mathf.Max(clips.Count - 1, 0),
                    duration = node.Duration > 0 ? node.Duration : 0,
                    wrapMode = node.WrapMode,
                    frames = node.Frames > 0 ? node.Frames : 0
                };
            }

            List<TransitionData> tempTransitions = new List<TransitionData>();

            foreach (var node in nodes)
            {
                foreach (var port in node.Outputs)
                {
                    foreach (var connection in port.Connections)
                    {
                        var from = (BaseNode) connection.fromNode;
                        var to = (BaseNode) connection.toNode;
                        var fromState = states[Array.IndexOf(nodes, from)];
                        var toState = states[Array.IndexOf(nodes, to)];
                        var trans = new TransitionData
                        {
                            fromState = fromState,
                            toState = toState,
                            qualifier = Qualifier.None,
                        };
                        if (connection.rules.Count > 0)
                        {
                            foreach (var rule in connection.rules)
                            {
                                int intValue = 0;
                                float floatValue = 0;
                                int isTrigger = 0;
                                int isFloat = 0;
                                switch (rule.Variable.VariableType)
                                {
                                    case VariableType.Boolean:
                                        intValue = rule.QualifierBoolVal ? 1 : 0;
                                        break;
                                    case VariableType.Float:
                                        isFloat = 1;
                                        floatValue = rule.QualifierFloatVal;
                                        break;
                                    case VariableType.Integer:
                                        intValue = rule.QualifierIntVal;
                                        break;
                                    case VariableType.Trigger:
                                        isTrigger = 1;
                                        intValue = 1;
                                        break;
                                }
                                trans.variable = StateGraph.variables.IndexOf(rule.Variable);
                                trans.isFloat = isFloat;
                                trans.qualifier = rule.Qualifier;
                                trans.intValue = intValue;
                                trans.floatValue = floatValue;
                                trans.isTrigger = isTrigger;
                                tempTransitions.Add(trans);
                            }
                        }
                        else
                        {
                            tempTransitions.Add(trans);
                        }
                    }
                }
            }
            tempTransitions.Add(new TransitionData { fromState = states[0], toState = states[1], qualifier = Qualifier.None });
            transitions = new NativeMultiHashMap<int, TransitionData>(tempTransitions.Count, Allocator.Persistent);

            foreach (var trans in tempTransitions)
            {
                transitions.Add(trans.fromState.index, trans);
                //     Debug.Log("====================================");
                //     Debug.Log(trans.fromState.index);
                //     Debug.Log(trans.toState.index);
                //     Debug.Log(JsonUtility.ToJson(trans));
            }

            frameStart = new NativeArray<float>(states.Length, Allocator.Persistent);
            float totalFrames = 1;
            float previousFrames = 0;
            for (int i = 0; i < states.Length; i++)
            {
                frameStart[i] = totalFrames + previousFrames;
                totalFrames = frameStart[i];
                previousFrames = states[i].frames;
            }

            HasSetup = true;
        }

        public struct StateData
        {
            public int index;
            public NodeType nodeType;
            public int clip;
            public int frames;
            public float duration;
            public WrapMode wrapMode;
        }

        [System.Serializable]
        public struct TransitionData
        {
            public StateData fromState;
            public StateData toState;
            public int variable;
            public Qualifier qualifier;
            public int isTrigger;
            public int isFloat;
            public float floatValue;
            public int intValue;
        }

        protected override void OnCreateManager(int capacity)
        {
            base.OnCreateManager(capacity);
        }

        protected override void OnDestroyManager()
        {
            DestroyRenderer();
            if (states.IsCreated)
                states.Dispose();
            if (frameStart.IsCreated)
                frameStart.Dispose();
            if (transitions.IsCreated)
                transitions.Dispose();
        }
    }
}
