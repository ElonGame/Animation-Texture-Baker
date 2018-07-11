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
            EditorGUI.BeginChangeCheck();
            DrawPendingSave();
            DrawPrefab();
            GUILayout.Space(8);
            DrawAnimationClips();
            DrawVariables();
            // DrawPreviewGUI();
            if (EditorGUI.EndChangeCheck())
            {
                // EditorUtility.SetDirty(graph);
                graph.IsDirty = true;
                // AssetDatabase.SaveAssets();
                // RepaintGraph();
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
                graph.SetPrefab(prefab);
            }
            else
            {
                graph.PrefabHashCode = 0;
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

        private void DrawAnimationClips()
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
            foreach (var clip in graph.AnimationClips)
            {
                EditorGUILayout.BeginVertical();
                var rect = EditorGUILayout.BeginHorizontal();
                GUILayout.Space(0);
                clip.Name = EditorGUILayout.TextField(clip.Name, GUILayout.MinWidth(100));
                if (clip.Node != null && clip.Name != clip.Node.name)
                {
                    clip.Node.name = clip.Name;
                }

                clip.WrapMode = (WrapMode) EditorGUILayout.EnumPopup(clip.WrapMode, GUILayout.MaxWidth(110));
                if (GUILayout.Button(EditorGUIUtility.IconContent("audio mixer.png"), GUILayout.Width(20)))
                {
                    Selection.activeObject = clip.Node;
                    return;
                }
                if (GUILayout.Button("-", GUILayout.Width(20)) && ConfirmRemoveClip())
                {
                    graph.RemoveClip(clip);
                    if (clip.Node)
                    {
                        UnityEngine.Object.DestroyImmediate(clip.Node, true);
                    }
                    return;
                }
                EditorGUILayout.EndHorizontal();

                var labelRect = rect;
                labelRect.yMin += 16;
                labelRect.height = 14;
                GUILayout.Space(14);
                GUI.Label(labelRect, "Duration: " + clip.Duration.ToString("N2") + "s  Frame Rate: " + clip.FrameRate, EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();
            }
            if (graph.AnimationClips.Count > 0)
                GUILayout.Space(8);
        }

        private void ImportAnimationClips(GameObject prefab)
        {
            var clips = AnimationUtility.GetAnimationClips(prefab);
            for (int i = 0; i < clips.Length; i++)
            {
                if (!graph.HasNode(clips[i].name))
                    CreateNode(typeof(StateNode), clips[i], clips[i].name, i);
            }
        }

        private void RepaintGraph()
        {
            EditorWindow.GetWindow(typeof(NodeEditorWindow)).Repaint();
        }

        private BaseNode CreateNode(Type type, AnimationClip clip = null, string name = "", int clipIndex = 0)
        {
            var node = graph.AddNewClip(type, clip, name, clipIndex);
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
                graph.AddVariable();
            }
            GUILayout.Space(8);
            foreach (var variable in graph.variables)
            {
                var rect = EditorGUILayout.BeginHorizontal();
                GUILayout.Space(0);
                variable.Name = GUILayout.TextField(variable.Name, GUILayout.MinWidth(100));
                if (string.IsNullOrEmpty(variable.Name))
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
                        break;
                    case VariableType.Float:
                        variable.DefaultFloatVal = EditorGUILayout.FloatField(variable.DefaultFloatVal, GUILayout.Width(50));
                        break;
                    case VariableType.Integer:
                        variable.DefaultIntVal = EditorGUILayout.IntField(variable.DefaultIntVal, GUILayout.Width(50));
                        break;
                    default:
                        GUILayout.Space(54);
                        break;
                }
                variable.VariableType = (VariableType) EditorGUILayout.EnumPopup(variable.VariableType, GUILayout.MaxWidth(80));
                if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(20)) && ConfirmRemoveVariable())
                {
                    graph.RemoveVariable(variable);
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (graph.variables.Count > 0)
                GUILayout.Space(8);
        }

        private void AddVariable()
        {
            var variable = new NodeGraphVariable();
            variable.VariableType = VariableType.Boolean;
            variable.Name = "";

            graph.variables.Add(variable);
        }

        private bool ConfirmImportState()
        {
            return EditorUtility.DisplayDialog("Import Animations?", "Do you want to import the prefab animations?", "Yes", "No");
        }

        private bool ConfirmRemoveVariable()
        {
            return EditorUtility.DisplayDialog("Remove variable?", "Are you sure you want to remove this variable?", "Yes", "No");
        }

        private bool ConfirmRemoveClip()
        {
            return EditorUtility.DisplayDialog("Remove clip?", "Are you sure you want to remove this clip?", "Yes", "No");
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }
    }
}
