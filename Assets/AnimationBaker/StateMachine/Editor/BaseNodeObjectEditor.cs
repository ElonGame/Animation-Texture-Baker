using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AnimationBaker.StateMachine.Nodes;
using AnimationBaker.StateMachine.XNode;
using AnimationBaker.StateMachine.XNodeEditor;

namespace AnimationBaker.StateMachine.Editor
{
    [CustomEditor(typeof(StateNode))]
    public class StateNodeObjectEditor : UnityEditor.Editor
    {
        BaseNode node;
        StateGraph graph;

        public override void OnInspectorGUI()
        {
            node = (BaseNode) target;
            graph = (StateGraph) node.graph;
            if (node == null || graph == null) return;
            if (!AssetDatabase.IsMainAsset(graph)) return;
            EditorGUI.BeginChangeCheck();
            DrawTitle(graph);
            if (!node.HasState) return;
            DrawTransitions();
            if (EditorGUI.EndChangeCheck())
            {
                graph.IsDirty = true;
                node.IsDirty = true;
            }
        }

        private void RepaintGraph()
        {
            EditorWindow.GetWindow(typeof(NodeEditorWindow)).Repaint();
        }

        private void DrawTransitions()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Transitions", EditorStyles.largeLabel);
            GUILayout.Space(6);

            for (int i = node.Outputs.Count() - 1; i > -1; i--)
            {
                var output = node.Outputs.ElementAt(i);
                for (int j = output.ConnectionCount - 1; j > -1; j--)
                {
                    var connection = output.Connections[j];
                    DrawConnection(connection);
                }
            }
        }

        private void DrawTitle(StateGraph graph)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(graph.name + " | " + node.name, EditorStyles.largeLabel);
            if (GUILayout.Button("Settings"))
            {
                Selection.activeObject = graph;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawConnection(NodeConnection connection)
        {
            var toNodeHash = connection.toNode.name.GetHashCode();
            if (!node.RulesToggles.ContainsKey(toNodeHash))
            {
                node.RulesToggles[toNodeHash] = false;
            }
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(13);
            var rect = EditorGUILayout.BeginVertical();
            node.RulesToggles[toNodeHash] = EditorGUILayout.Foldout(node.RulesToggles[toNodeHash], node.name + " » " + connection.toNode.name);
            if (node.RulesToggles[toNodeHash])
            {
                DrawcontrolButtons(connection, rect);
                EditorGUILayout.LabelField("Rules");
                for (int i = connection.rules.Count - 1; i > -1; i--)
                {
                    DrawRules(connection, connection.rules[i]);
                }
                GUILayout.Space(4);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRules(NodeConnection connection, TransitionRule rule)
        {
            Rect rect = EditorGUILayout.BeginHorizontal();

            var variableNames = graph.variables.Select(x => x.name).ToArray();
            var currentVariableIndex = Array.IndexOf(variableNames, rule.Variable.name);
            var variableIndex = EditorGUILayout.Popup(currentVariableIndex, variableNames);
            if (variableIndex != currentVariableIndex)
            {
                rule.Variable = graph.variables[variableIndex];
                rule.VariableName = rule.Variable.name;
            }
            switch (rule.Variable.VariableType)
            {
                case VariableType.Trigger:
                    rule.Qualifier = Qualifier.Equal;
                    rule.QualifierIntVal = 1;
                    break;
                case VariableType.Boolean:
                    rule.QualifierBoolEnum = (TrueFalse) EditorGUILayout.EnumPopup(rule.QualifierBoolEnum);
                    rule.QualifierIntVal = (int) rule.QualifierBoolEnum;
                    rule.Qualifier = Qualifier.Equal;
                    break;
                case VariableType.Float:
                    rule.Qualifier = (Qualifier) EditorGUILayout.EnumPopup(rule.Qualifier);
                    rule.QualifierFloatVal = EditorGUILayout.FloatField(rule.QualifierFloatVal);
                    break;
                case VariableType.Integer:
                    rule.Qualifier = (Qualifier) EditorGUILayout.EnumPopup(rule.Qualifier);
                    rule.QualifierIntVal = EditorGUILayout.IntField(rule.QualifierIntVal);
                    break;
            }

            if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(23)) && ConfirmRemoveRule())
            {
                connection.RemoveRule(rule);
                return;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawcontrolButtons(NodeConnection connection, Rect rect)
        {
            var addButtonRect = rect;
            addButtonRect.yMin = rect.yMin += 18;
            addButtonRect.xMin = rect.width + 4;
            addButtonRect.width = 24;
            addButtonRect.height = 14;
            if (connection.CanAddRule() && GUI.Button(addButtonRect, "+"))
            {
                var rule = connection.AddRule();
                rule.name = connection.fromNode.name + connection.toNode.name + "Rule_" + connection.rules.Count;
                AssetDatabase.AddObjectToAsset(rule, graph);
                graph.IsDirty = true;
            }
        }

        private bool ConfirmRemoveConnection()
        {
            return EditorUtility.DisplayDialog("Remove transition?", "Are you sure you want to remove this transition?", "Yes", "No");
        }

        private bool ConfirmRemoveRule()
        {
            return EditorUtility.DisplayDialog("Remove rule?", "Are you sure you want to remove this rule?", "Yes", "No");
        }
    }

    [CustomEditor(typeof(StartNode))]
    public class StartNodeObjectEditor : StateNodeObjectEditor { }

    [CustomEditor(typeof(AnyNode))]
    public class AnyNodeObjectEditor : StateNodeObjectEditor { }

    [CustomEditor(typeof(EndNode))]
    public class EndNodeObjectEditor : StateNodeObjectEditor { }
}
