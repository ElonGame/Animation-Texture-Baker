using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimationBaker.StateMachine.XNode;
using AnimationBaker.Utils;

namespace AnimationBaker.StateMachine.Nodes
{
    [System.Serializable]
    public enum NodeType
    {
        Start,
        Any,
        State,
        End
    }

    [System.Serializable]
    public class RulesToggleDictionary : SerializableDictionary<int, bool> { }

    [System.Serializable]
    public abstract class BaseNode : Node
    {
        public abstract NodeType NodeType { get; set; }
        public virtual bool HasState { get => false; }
        public float FrameRate;
        public WrapMode WrapMode;
        public float Duration = 0;
        public RulesToggleDictionary RulesToggles = new RulesToggleDictionary();
        public AnimationClip Clip { get; set; }
        public bool HasClip { get => Clip != null; set { } }
        public override object GetValue(NodePort port)
        {
            return null;
        }
        public int Frames
        {
            get
            {
                var delta = FrameRate * 0.001f;
                return Mathf.CeilToInt(Duration / delta);
            }
        }

        [Serializable]
        public class Empty { }

        public virtual AnimationClip Evaluate(AnimationClip lastState)
        {
            foreach (var port in Outputs)
            {
                foreach (var connection in port.Connections)
                {
                    var result = true;
                    foreach (var rule in connection.rules)
                    {
                        if (!rule.Evaluate())
                        {
                            result = false;
                        }
                    }
                    if (result)
                    {
                        var baseNode = (BaseNode) connection.toNode;
                        if (baseNode.Clip != null)
                        {
                            lastState = baseNode.Clip;
                        }
                        return baseNode.Evaluate(lastState);
                    }
                }
            }
            return lastState;
        }

        public override void OnRemoveConnection(NodePort port, NodeConnection connection)
        {
            for (int i = connection.rules.Count - 1; i > -1; i--)
            {
                var rule = connection.rules[i];
                DestroyImmediate(rule, true);
            }
        }
    }
}
