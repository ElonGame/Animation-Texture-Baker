using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AnimationBaker.StateMachine.Nodes
{
    [System.Serializable]
    [NodeTint("#7c0000")]
    public class EndNode : BaseNode
    {
        [Input(ShowBackingValue.Never, ConnectionType.Multiple)] public Empty enter;

        public override NodeType NodeType
        {
            get => NodeType.End;
            set { }
        }
    }
}
