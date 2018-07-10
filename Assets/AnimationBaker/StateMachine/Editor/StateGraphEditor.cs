using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using AnimationBaker.StateMachine.Nodes;
using AnimationBaker.StateMachine.Variables;
using AnimationBaker.Utils.XNodeEditor;

namespace AnimationBaker.StateMachine.Editor
{
    [CustomEditor(typeof(StateGraph))]
    public class StateGraphEditor : UnityEditor.Editor
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
            DrawPrefab();
            GUILayout.Space(8);
            DrawAnimationClips();
            DrawVariables();
        }

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

            var addClipRect = GUILayoutUtility.GetLastRect();
            addClipRect.xMin = addClipRect.width - 36;
            addClipRect.width = 50;
            addClipRect.height = 18;
            if (GUI.Button(addClipRect, "Add", EditorStyles.miniButton))
            {
                CreateNode(typeof(StateNode));
            }

            if (graph.HasAnimation)
            {
                var importClipRect = addClipRect;
                importClipRect.xMin -= 52;
                importClipRect.width = 50;
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

                clip.WrapMode = (WrapMode) EditorGUILayout.EnumPopup(clip.WrapMode, GUILayout.MaxWidth(110));
                if (GUILayout.Button(EditorGUIUtility.IconContent("audio mixer.png"), GUILayout.Width(20)))
                {
                    Selection.activeObject = clip.Node;
                    return;
                }
                if (GUILayout.Button("-", GUILayout.Width(20)) && ConfirmRemoveClip())
                {
                    graph.RemoveClip(clip);
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
                CreateNode(typeof(StateNode), clips[i], clips[i].name, i);
            }
        }

        private BaseNode CreateNode(Type type, AnimationClip clip = null, string name = "", int clipIndex = 0)
        {
            var node = graph.AddNewClip(type, clip, name, clipIndex);
            Repaint();
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
                AddVariable();
            }
            GUILayout.Space(8);
            foreach (var variable in graph.MachineVariables)
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
                else if (variable.name != "VA_" + variable.Name)
                {
                    variable.name = "VA_" + variable.Name;
                    EditorUtility.SetDirty(variable);
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
                    graph.MachineVariables.Remove(variable);
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (graph.MachineVariables.Count > 0)
                GUILayout.Space(8);
        }

        private void AddVariable()
        {
            MachineVariable variable = ScriptableObject.CreateInstance<MachineVariable>();
            variable.VariableType = VariableType.Boolean;
            variable.Name = "";
            graph.MachineVariables.Add(variable);
            AssetDatabase.AddObjectToAsset(variable, graph);
            AssetDatabase.SaveAssets();
            Repaint();

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
    }
}
