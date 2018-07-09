using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AnimationBaker.StateMachine.Nodes
{
    [System.Serializable]
    [NodeTint("#008000")]
    public class StartNode : BaseNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        public Empty Output;

        public override NodeType NodeType
        {
            get => NodeType.Start;
            set { }
        }
    }
}
