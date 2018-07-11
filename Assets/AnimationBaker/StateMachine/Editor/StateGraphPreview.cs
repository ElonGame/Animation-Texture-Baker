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
    public partial class StateGraphEditor : UnityEditor.Editor
    {
        PreviewRenderUtility previewRenderUtility;
        GameObject previewObject;
        int lastPreviewHash;
        bool isPlaying = false;
        Camera previewCamera;
        float goHeight;

        void CreateRenderer()
        {
            previewRenderUtility = new PreviewRenderUtility(true);
            previewRenderUtility.cameraFieldOfView = 30f;
            previewRenderUtility.camera.farClipPlane = 1000;
            previewRenderUtility.camera.nearClipPlane = 0.3f;
            previewRenderUtility.camera.clearFlags = CameraClearFlags.Skybox;
            previewObject = previewRenderUtility.InstantiatePrefabInScene(graph.Prefab);
            previewCamera = previewRenderUtility.camera;
        }

        void Cleanup()
        {
            if (previewRenderUtility != null)
                previewRenderUtility.Cleanup();
            DestroyPreviewObject();
        }

        void DestroyPreviewObject()
        {
            if (previewObject == null) return;
            DestroyImmediate(previewObject);
            previewObject = null;
        }

        private void OnDisable()
        {
            Cleanup();
        }

        public override void OnPreviewSettings()
        {
            var playButtonContent = EditorGUIUtility.IconContent("PlayButton");
            var pauseButtonContent = EditorGUIUtility.IconContent("PauseButton");
            var previewButtonSettingsStyle = new GUIStyle("preButton");
            var buttonContent = isPlaying ? pauseButtonContent : playButtonContent;
            isPlaying = GUILayout.Toggle(isPlaying, buttonContent, previewButtonSettingsStyle);
        }

        public override void OnInteractivePreviewGUI(Rect rect, GUIStyle background)
        {
            if (!graph || !graph.Prefab) return;
            if (previewObject == null || lastPreviewHash != graph.Prefab.GetHashCode())
            {
                Cleanup();
                CreateRenderer();
                lastPreviewHash = graph.Prefab.GetHashCode();
                previewObject = previewRenderUtility.InstantiatePrefabInScene(graph.Prefab);
                goHeight = GetHeight(previewObject.transform);
            }

            DrawRuntimeFields();

            previewRenderUtility.BeginPreview(rect, background);

            previewCamera.transform.position = previewObject.transform.position + previewObject.transform.forward * CalcCameraDist(goHeight) + previewObject.transform.up * CalcCameraDist(goHeight) - previewObject.transform.right * CalcCameraDist(goHeight);

            previewCamera.transform.LookAt(previewObject.transform);

            previewRenderUtility.Render();

            previewObject.SetActive(true);

            previewCamera.Render();

            previewRenderUtility.EndAndDrawPreview(rect);

            // http://anchan828.github.io/editor-manual/web/customeditor.html
            // http://anchan828.github.io/editor-manual/web/spriteanimationpreview2.html
        }

        private float GetHeight(Transform source)
        {
            var bounds = new Bounds();
            bounds.center = source.position;
            bounds = AddRenderers(bounds, source);
            return bounds.size.y / 3f;
        }

        private Bounds AddRenderers(Bounds bounds, Transform source)
        {
            var filter = source.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    bounds.Encapsulate(source.TransformPoint(vertex));
                }
            }

            var skinRenderer = source.GetComponent<SkinnedMeshRenderer>();
            if (skinRenderer != null && skinRenderer.sharedMesh != null)
            {
                foreach (var vertex in skinRenderer.sharedMesh.vertices)
                {
                    bounds.Encapsulate(source.TransformPoint(vertex));
                }
            }
            foreach (Transform child in source)
            {
                bounds = AddRenderers(bounds, child);
            }
            return bounds;
        }

        private float CalcCameraDist(float height)
        {
            return height / Mathf.Tan(previewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        private void DrawRuntimeFields()
        {
            EditorGUILayout.BeginVertical();
            foreach (var variable in graph.variables)
            {
                switch (variable.VariableType)
                {
                    case VariableType.Boolean:
                        variable.RuntimeBoolVal = EditorGUILayout.Toggle(variable.Name, variable.RuntimeBoolVal);
                        break;
                    case VariableType.Float:
                        variable.RuntimeFloatVal = EditorGUILayout.FloatField(variable.Name, variable.RuntimeFloatVal);
                        break;
                    case VariableType.Integer:
                        variable.RuntimeIntVal = EditorGUILayout.IntField(variable.Name, variable.RuntimeIntVal);
                        break;
                    case VariableType.Trigger:
                        if (GUILayout.Button("Trigger: " + variable.Name))
                        {
                            SetTrigger(variable);
                        }
                        break;
                }
            }
            EditorGUILayout.BeginVertical();
        }

        private void SetTrigger(NodeGraphVariable variable) { }
    }
}
