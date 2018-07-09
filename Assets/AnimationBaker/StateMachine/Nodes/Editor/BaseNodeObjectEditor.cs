using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AnimationBaker.StateMachine.Nodes;
using AnimationBaker.StateMachine.Transitions;
using AnimationBaker.StateMachine.Variables;
using XNode;

namespace AnimationBaker.StateMachine.Nodes.Editor
{
    [CustomEditor(typeof(StateNode))]
    public class StateNodeObjectEditor : UnityEditor.Editor
    {
        BaseNode node;

        public override void OnInspectorGUI()
        {
            node = (BaseNode) target;
            StateGraph graph = (StateGraph) node.graph;
            if (node == null || graph == null) return;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(graph.name + " | " + node.name, EditorStyles.largeLabel);
            if (GUILayout.Button("Settings"))
            {
                Selection.activeObject = graph;
            }
            EditorGUILayout.EndHorizontal();

            if (!node.HasState) return;
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Transitions", EditorStyles.largeLabel);
            GUILayout.Space(6);

            foreach (var port in node.InstanceOutputs)
            {
                var count = port.ConnectionCount;
                if (count == 0) continue;
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        DrawConnection(port, port.GetConnection(i));
                    }
                    catch { }
                }
            }
        }

        private void DrawConnection(NodePort fromPort, NodePort toPort)
        {
            var nodePortHash = toPort.node.name.GetHashCode();
            if (!node.RulesToggles.ContainsKey(nodePortHash))
            {
                node.RulesToggles[nodePortHash] = false;
            }
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(13);
            var rect = EditorGUILayout.BeginVertical();
            node.RulesToggles[nodePortHash] = EditorGUILayout.Foldout(node.RulesToggles[nodePortHash], node.name + " » " + toPort.node.name);
            if (node.RulesToggles[nodePortHash])
            {
                DrawcontrolButtons(fromPort, toPort, nodePortHash, rect);
                EditorGUILayout.LabelField("Rules");
                foreach (var item in node.Rules)
                {
                    if (item.Key.Equals(toPort.node as BaseNode))
                    {
                        DrawRules(item);
                    }
                }
                GUILayout.Space(4);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRules(KeyValuePair<BaseNode, TransitionRules> item)
        {
            foreach (var rules in item.Value.Rules)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(item.Key.ToString());
                if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(24)))
                {
                    node.RemoveRule(item.Key, item.Value);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawcontrolButtons(NodePort fromPort, NodePort toPort, int toPortHash, Rect rect)
        {
            var removeBtnRect = rect;
            removeBtnRect.yMin = rect.yMin += 18;
            removeBtnRect.xMin = rect.width + 4;
            removeBtnRect.width = 24;
            removeBtnRect.height = 14;
            if (GUI.Button(removeBtnRect, "x") && ConfirmRemoveConnection())
            {
                node.RemoveVariables(toPort.node as BaseNode);
                fromPort.Disconnect(toPort);
            }
            var addButtonRect = removeBtnRect;
            addButtonRect.xMin = removeBtnRect.xMin - 26;
            addButtonRect.width = 24;
            if (GUI.Button(addButtonRect, "+"))
            {
                node.AddRule(toPort.node as BaseNode);
            }
        }

        private bool ConfirmRemoveConnection()
        {
            return EditorUtility.DisplayDialog("Remove transition?", "Are you sure you want to remove this transition?", "Yes", "No");
        }
    }

    [CustomEditor(typeof(StartNode))]
    public class StartNodeObjectEditor : StateNodeObjectEditor { }

    [CustomEditor(typeof(EndNode))]
    public class EndNodeObjectEditor : StateNodeObjectEditor { }
}
