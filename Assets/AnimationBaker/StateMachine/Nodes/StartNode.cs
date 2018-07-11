using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimationBaker.StateMachine.XNode;

namespace AnimationBaker.StateMachine.Nodes
{
    [System.Serializable]
    [NodeTint("#008000")]
    public class StartNode : BaseNode
    {
        public Empty Output;

        public override NodeType NodeType
        {
            get => NodeType.Start;
            set { }
        }
    }
}
