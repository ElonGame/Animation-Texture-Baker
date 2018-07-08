using System;
using UnityEngine;
using UnityEditor;

namespace AnimationBaker.StateMachine.Editor
{
    [CustomEditor(typeof(StateGraph))]
    public class StateGraphEditor : UnityEditor.Editor
    {

        StateGraph graph;

        public override void OnInspectorGUI()
        {
            graph = (StateGraph) target;
            DrawPrefab();
            DrawAnimationClips();
            DrawVariables();
        }

        private void DrawPrefab()
        {
            EditorGUILayout.LabelField("Prefab", EditorStyles.largeLabel);
            GUILayout.Space(8);
            graph.Prefab = (GameObject) EditorGUILayout.ObjectField(graph.Prefab, typeof(GameObject), false);
            if (graph.Prefab != null && name != graph.Prefab.name)
            {
                name = graph.Prefab.name;
                AssetDatabase.SaveAssets();
            }
            GUILayout.Space(8);
        }

        private void DrawAnimationClips()
        {
            EditorGUILayout.LabelField("Animation Clips", EditorStyles.largeLabel);
            var addVariableRect = GUILayoutUtility.GetLastRect();
            addVariableRect.xMin = addVariableRect.width - 26;
            addVariableRect.width = 40;
            addVariableRect.height = 18;
            if (GUI.Button(addVariableRect, "Add", EditorStyles.miniButton))
            {
                graph.AddNewClip();
                EditorUtility.SetDirty(graph);
                AssetDatabase.SaveAssets();
            }
            GUILayout.Space(8);
            foreach (var clip in graph.AnimationClips)
            {
                var rect = EditorGUILayout.BeginHorizontal();
                GUILayout.Space(0);
                clip.WrapMode = (WrapMode) EditorGUILayout.EnumPopup(clip.WrapMode, GUILayout.MaxWidth(130));
                clip.Clip = EditorGUILayout.ObjectField(clip.Clip, typeof(AnimationClip), false, GUILayout.MinWidth(100)) as AnimationClip;
                if (clip.Clip != null && clip.Clip.name != clip.Node.name)
                {
                    clip.Node.Rename(clip.Clip.name);
                    if (clip.WrapMode == WrapMode.Default)
                    {
                        clip.WrapMode = clip.Clip.wrapMode;
                    }
                }
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    graph.RemoveClip(clip);
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (graph.AnimationClips.Count > 0)
                GUILayout.Space(8);
        }

        private void DrawVariables()
        {
            EditorGUILayout.LabelField("Variables", EditorStyles.largeLabel);
            var addVariableRect = GUILayoutUtility.GetLastRect();
            addVariableRect.xMin = addVariableRect.width - 26;
            addVariableRect.width = 40;
            addVariableRect.height = 18;
            if (GUI.Button(addVariableRect, "Add", EditorStyles.miniButton))
            {
                graph.MachineVariables.Add(new StateMachineVariables { VariableType = VariableType.Boolean, Name = "" });
            }
            GUILayout.Space(8);
            foreach (var variable in graph.MachineVariables)
            {
                var rect = EditorGUILayout.BeginHorizontal();
                GUILayout.Space(0);
                variable.VariableType = (VariableType) EditorGUILayout.EnumPopup(variable.VariableType, GUILayout.MaxWidth(80));
                variable.Name = GUILayout.TextField(variable.Name, GUILayout.MinWidth(100));
                if (string.IsNullOrEmpty(variable.Name))
                {
                    var labelRect = rect;
                    labelRect.xMin = 100;
                    GUI.Label(labelRect, "Name", EditorStyles.boldLabel);
                }
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    graph.MachineVariables.Remove(variable);
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (graph.MachineVariables.Count > 0)
                GUILayout.Space(8);
        }
    }
}
