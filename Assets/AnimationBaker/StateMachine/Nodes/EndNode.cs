using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimationBaker.StateMachine.XNode;

namespace AnimationBaker.StateMachine.Nodes
{
    [System.Serializable]
    [NodeTint("#7c0000")]
    public class EndNode : BaseNode
    {
        public Empty Output;

        public override NodeType NodeType
        {
            get => NodeType.End;
            set { }
        }
    }
}
