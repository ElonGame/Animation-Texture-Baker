using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimationBaker.StateMachine.XNode;

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

        public float Duration = 0;
        public RulesToggleDictionary RulesToggles = new RulesToggleDictionary();

        public override object GetValue(NodePort port)
        {
            return null;
        }

        [Serializable]
        public class Empty { }
    }
}
