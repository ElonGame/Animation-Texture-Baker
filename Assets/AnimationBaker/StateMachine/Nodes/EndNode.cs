using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimationBaker.Utils.XNode;

namespace AnimationBaker.StateMachine.Nodes
{
    [System.Serializable]
    [NodeTint("#7c0000")]
    public class EndNode : BaseNode
    {
        [Input(ShowBackingValue.Never, ConnectionType.Multiple)] public Empty Input;

        public override NodeType NodeType
        {
            get => NodeType.End;
            set { }
        }
    }
}
