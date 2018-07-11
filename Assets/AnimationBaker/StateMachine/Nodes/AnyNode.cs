using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimationBaker.StateMachine.XNode;

namespace AnimationBaker.StateMachine.Nodes
{
    [System.Serializable]
    [NodeTint("#005455")]
    public class AnyNode : BaseNode
    {
        public override bool HasState { get => true; }

        public Empty Output;

        public override NodeType NodeType
        {
            get => NodeType.Any;
            set { }
        }
    }
}
