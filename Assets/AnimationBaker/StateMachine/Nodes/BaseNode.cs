using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AnimationBaker.StateMachine.Nodes
{

    [System.Serializable]
    public enum NodeType
    {
        Start,
        State,
        End
    }

    [System.Serializable]
    public abstract class BaseNode : Node
    {

        [HideInInspector]
        public StateRule[] Rules = new StateRule[0];

        public abstract NodeType NodeType { get; set; }
        public virtual bool CanAddOutput { get => false; }
        public virtual bool HasState { get => false; }

        [HideInInspector]
        public float Duration = 0;
        [HideInInspector]
        public AnimationClip Clip;
        [HideInInspector]
        public WrapMode WrapMode;

        // [Input] public Empty enter;
        // [Output] public Empty exit;

        public override object GetValue(NodePort port)
        {
            return null;
        }

        public void AddOutput()
        {
            var portName = "Output";
            RemoveInstancePort(portName);
            AddInstanceOutput(typeof(BaseNode.Empty), ConnectionType.Override, portName);
            UpdateStaticPorts();
        }

        public void MoveNext()
        {
            StateGraph fmGraph = graph as StateGraph;

            // if (fmGraph.current != this)
            // {
            //     Debug.LogWarning("Node isn't active");
            //     return;
            // }

            NodePort exitPort = GetOutputPort("exit");

            if (!exitPort.IsConnected)
            {
                Debug.LogWarning("Node isn't connected");
                return;
            }

            BaseNode node = exitPort.Connection.node as BaseNode;
            node.OnEnter();
        }

        public void OnEnter()
        {
            StateGraph fmGraph = graph as StateGraph;
        }

        [Serializable]
        public class Empty { }
    }

    [System.Serializable]
    public class StateRule
    {
        public VariableType VariableType;
        public Qualifier Qualifier;
        public object Value;
        public System.Type ValueType;
    }
}
