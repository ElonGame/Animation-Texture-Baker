using System;
using UnityEngine;
using UnityEditor;
using AnimationBaker.StateMachine.Nodes;

namespace AnimationBaker.StateMachine.Nodes.Editor
{
    [CustomEditor(typeof(StateNode))]
    public class StateNodeObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            BaseNode node = (BaseNode) target;
            StateGraph graph = (StateGraph) node.graph;
            // base.OnInspectorGUI();
            if (GUILayout.Button("State Machine Settings"))
            {
                Selection.activeObject = graph;
            }
        }
    }

    [CustomEditor(typeof(StartNode))]
    public class StartNodeObjectEditor : StateNodeObjectEditor { }

    [CustomEditor(typeof(EndNode))]
    public class EndNodeObjectEditor : StateNodeObjectEditor { }
}
