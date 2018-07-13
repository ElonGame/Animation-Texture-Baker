using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using AnimationBaker.StateMachine.Nodes;
using AnimationBaker.StateMachine.XNode;
using AnimationBaker.StateMachine.XNodeEditor;
using UEditor = UnityEditor.Editor;

namespace AnimationBaker.StateMachine.Editor
{
    [CustomEditor(typeof(StateGraph))]
    public partial class StateGraphEditor : UnityEditor.Editor
    {

        StateGraph _graph;
        StateGraph graph
        {
            get
            {
                if (_graph == null)
                {
                    _graph = (StateGraph) target;
                }
                return _graph;
            }
        }

        public override void OnInspectorGUI()
        {
            if (graph == null) return;
            if (!AssetDatabase.IsMainAsset(graph)) return;
            if (graph.startNode == null) return;
            if (graph.endNode == null) return;
            if (graph.anyNode == null) return;
            EditorGUI.BeginChangeCheck();
            DrawPendingSave();
            DrawPrefab();
            GUILayout.Space(8);
            DrawAnimations();
            DrawVariables();
            if (!graph.animationLoaded)
            {
                if (GUILayout.Button("Reload Animations"))
                {
                    graph.LoadAnimationStates();
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                graph.IsDirty = true;
            }
            if (graph.isPlaying)
            {
                Repaint();
            }
            else
            {
                DrawBaking();
            }
        }

        private void DrawPendingSave() { }

        private void DrawPrefab()
        {
            var rect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prefab", EditorStyles.largeLabel, GUILayout.Width(116));
            var prefab = (GameObject) EditorGUILayout.ObjectField(graph.Prefab, typeof(GameObject), false);
            if (prefab != null && prefab.GetHashCode() != graph.PrefabHashCode)
            {
                graph.PrefabHashCode = prefab.GetHashCode();
                graph.SetPrefab(prefab);
            }

            EditorGUILayout.EndHorizontal();

            if (graph.Prefab == null) return;

            GUILayout.Space(4);

            rect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Casts Shadows", GUILayout.Width(116));
            graph.ShadowCastingMode = (ShadowCastingMode) EditorGUILayout.EnumPopup(graph.ShadowCastingMode);
            EditorGUILayout.EndHorizontal();

            rect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Receives Shadows", GUILayout.Width(116));
            graph.ReceivesShadows = EditorGUILayout.Toggle(graph.ReceivesShadows);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawAnimations()
        {
            EditorGUILayout.LabelField("Animation Clips", EditorStyles.largeLabel);
            if (graph.HasAnimation)
            {
                var importClipRect = GUILayoutUtility.GetLastRect();
                importClipRect.xMin = importClipRect.width - 36;
                importClipRect.width = 50;
                importClipRect.height = 18;
                if (GUI.Button(importClipRect, "Import", EditorStyles.miniButton) && ConfirmImportState())
                {
                    ImportAnimationClips(graph.Prefab);
                }
            }

            GUILayout.Space(8);
            foreach (BaseNode node in graph.nodes)
            {
                if (node.NodeType != NodeType.State) continue;
                EditorGUILayout.BeginVertical();
                var rect = EditorGUILayout.BeginHorizontal();
                GUILayout.Space(0);

                EditorGUILayout.LabelField(node.name, GUILayout.MinWidth(100));
                node.WrapMode = (WrapMode) EditorGUILayout.EnumPopup(node.WrapMode, GUILayout.MaxWidth(110));
                if (GUILayout.Button(EditorGUIUtility.IconContent("audio mixer.png"), GUILayout.Width(20)))
                {
                    Selection.activeObject = node;
                    return;
                }
                if (GUILayout.Button("-", GUILayout.Width(20)) && ConfirmRemoveAnimations())
                {
                    graph.RemoveNode(node);
                    UnityEngine.Object.DestroyImmediate(node, true);
                    return;
                }
                EditorGUILayout.EndHorizontal();

                var labelRect = rect;
                labelRect.yMin += 16;
                labelRect.height = 14;
                GUILayout.Space(14);
                GUI.Label(labelRect, "Duration: " + node.Duration.ToString("N2") + "s  Frame Rate: " + node.FrameRate + " Frames: " + node.Frames, EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();
            }
            GUILayout.Space(8);
        }

        private void ImportAnimationClips(GameObject prefab)
        {
            foreach (AnimationClip clip in AnimationUtility.GetAnimationClips(prefab))
            {
                if (!graph.HasNode(clip.name))
                    CreateNode(typeof(StateNode), clip, clip.name);
            }
        }

        private void RepaintGraph()
        {
            EditorWindow.GetWindow(typeof(NodeEditorWindow)).Repaint();
        }

        private BaseNode CreateNode(Type type, AnimationClip clip = null, string name = "")
        {
            var node = graph.AddNewAnimation(type, clip, name);
            AssetDatabase.AddObjectToAsset(node, graph);
            return node;
        }

        private void DrawVariables()
        {
            EditorGUILayout.LabelField("Variables", EditorStyles.largeLabel);
            var addVariableRect = GUILayoutUtility.GetLastRect();
            addVariableRect.xMin = addVariableRect.width - 36;
            addVariableRect.width = 50;
            addVariableRect.height = 18;
            if (GUI.Button(addVariableRect, "Add", EditorStyles.miniButton))
            {
                var variable = graph.AddVariable();
                variable.VariableType = VariableType.Boolean;
                variable.name = "";
                AssetDatabase.AddObjectToAsset(variable, graph);
                graph.IsDirty = true;
            }
            GUILayout.Space(8);
            foreach (var variable in graph.variables)
            {
                var rect = EditorGUILayout.BeginHorizontal();
                GUILayout.Space(0);
                variable.name = GUILayout.TextField(variable.name, GUILayout.MinWidth(100));
                if (string.IsNullOrEmpty(variable.name))
                {
                    var labelRect = rect;
                    labelRect.xMin = 16;
                    labelRect.yMin -= 1;
                    GUI.Label(labelRect, "Variable Name", EditorStyles.miniBoldLabel);
                }
                switch (variable.VariableType)
                {
                    case VariableType.Boolean:
                        variable.DefaultBoolVal = GUILayout.Toggle(variable.DefaultBoolVal, new GUIContent(), GUILayout.Width(50));
                        if (!graph.isPlaying)
                        {
                            variable.RuntimeBoolVal = variable.DefaultBoolVal;
                        }
                        break;
                    case VariableType.Float:
                        variable.DefaultFloatVal = EditorGUILayout.FloatField(variable.DefaultFloatVal, GUILayout.Width(50));
                        if (!graph.isPlaying)
                        {
                            variable.RuntimeFloatVal = variable.DefaultFloatVal;
                        }
                        break;
                    case VariableType.Integer:
                        variable.DefaultIntVal = EditorGUILayout.IntField(variable.DefaultIntVal, GUILayout.Width(50));
                        if (!graph.isPlaying)
                        {
                            variable.RuntimeIntVal = variable.DefaultIntVal;
                        }
                        break;
                    default:
                        GUILayout.Space(54);
                        break;
                }
                variable.VariableType = (VariableType) EditorGUILayout.EnumPopup(variable.VariableType, GUILayout.MaxWidth(80));
                if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(20)) && ConfirmRemoveVariable())
                {
                    graph.RemoveVariable(variable);
                    DestroyImmediate(variable, true);
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (graph.variables.Count > 0)
                GUILayout.Space(8);
        }

        private bool ConfirmImportState()
        {
            return EditorUtility.DisplayDialog("Import Animations?", "Do you want to import the prefab animations?", "Yes", "No");
        }

        private bool ConfirmRemoveVariable()
        {
            return EditorUtility.DisplayDialog("Remove Variable?", "Are you sure you want to remove this variable?", "Yes", "No");
        }

        private bool ConfirmRemoveAnimations()
        {
            return EditorUtility.DisplayDialog("Remove Animations?", "Are you sure you want to remove this Animations?", "Yes", "No");
        }
    }
}
