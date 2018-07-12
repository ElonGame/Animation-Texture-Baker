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
        Camera previewCamera;
        float goHeight;
        Rect previewRect;
        float deltaTime = 0;
        float cameraZoom = 2;
        Bounds rendererBounds = new Bounds();
        Vector3 lastPosition = new Vector3(0, 10, 10);
        Vector3 offset = Vector3.zero;
        Animation animation;

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
            var buttonContent = graph.isPlaying ? pauseButtonContent : playButtonContent;
            graph.isPlaying = GUILayout.Toggle(graph.isPlaying, buttonContent, previewButtonSettingsStyle);
        }

        public override void OnInteractivePreviewGUI(Rect rect, GUIStyle background)
        {
            if (!graph || !graph.Prefab) return;
            if (graph.isPlaying)
                deltaTime = (float) EditorApplication.timeSinceStartup;
            if (previewObject == null || lastPreviewHash != graph.Prefab.GetHashCode())
            {
                Cleanup();
                CreateRenderer();
                lastPreviewHash = graph.Prefab.GetHashCode();
                previewObject = previewRenderUtility.InstantiatePrefabInScene(graph.Prefab);
                goHeight = GetHeight(previewObject.transform);
                animation = graph.Prefab.GetComponent<Animation>();
            }

            DrawRuntimeFields();

            if (graph.isPlaying)
            {
                try
                {
                    foreach (AnimationState aState in graph.PrefabAnimation)
                    {
                        aState.enabled = false;
                    }
                }
                catch (System.Exception exception)
                {
                    Debug.Log(exception.Message);
                }
                var state = graph.Evaluate(deltaTime);
                if (state != null)
                {
                    state.wrapMode = state.clip.wrapMode;
                    state.enabled = true;
                    state.time = graph.internalCounter;
                    state.weight = 1;
                    animation.Sample();
                    RepaintGraph();
                }
            }

            previewRenderUtility.BeginPreview(rect, background);

            if (lastPosition == Vector3.zero)
            {
                lastPosition.x = previewCamera.transform.position.x;
                lastPosition.z = previewCamera.transform.position.z;
            }

            foreach (var light in previewRenderUtility.lights)
            {
                light.transform.rotation = previewCamera.transform.rotation;
            }

            previewRenderUtility.Render();

            previewCamera.Render();

            previewRenderUtility.EndAndDrawPreview(rect);

            if (rect.size.x > 1 && rect.size.y > 1)
                previewRect = rect;

            // http://anchan828.github.io/editor-manual/web/customeditor.html
            // http://anchan828.github.io/editor-manual/web/spriteanimationpreview2.html
            UpdatePosition();
        }

        private float GetHeight(Transform source)
        {
            rendererBounds = GetRendererBounds(rendererBounds, source);
            return rendererBounds.size.y / 2f;
        }

        private Bounds GetRendererBounds(Bounds bounds, Transform source)
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
                bounds = GetRendererBounds(bounds, child);
            }
            return bounds;
        }

        private float CalcCameraDist(float height)
        {
            return height / Mathf.Tan(previewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * cameraZoom;
        }

        private void DrawRuntimeFields()
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;
            EditorGUILayout.BeginVertical();
            foreach (var variable in graph.variables)
            {
                if (String.IsNullOrEmpty(variable.name)) continue;
                switch (variable.VariableType)
                {
                    case VariableType.Boolean:
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(variable.name, GUILayout.Width(100));
                        variable.RuntimeBoolVal = EditorGUILayout.Toggle(variable.RuntimeBoolVal);
                        GUILayout.EndHorizontal();
                        break;
                    case VariableType.Float:
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(variable.name, GUILayout.Width(100));
                        variable.RuntimeFloatVal = EditorGUILayout.FloatField(variable.RuntimeFloatVal);
                        if (GUILayout.Button("+", GUILayout.Width(20)))
                        {
                            variable.RuntimeFloatVal += 1;
                        }
                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            variable.RuntimeFloatVal -= 1;
                        }
                        GUILayout.EndHorizontal();
                        break;
                    case VariableType.Integer:
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(variable.name, GUILayout.Width(100));
                        variable.RuntimeIntVal = EditorGUILayout.IntField(variable.RuntimeIntVal);
                        if (GUILayout.Button("+", GUILayout.Width(20)))
                        {
                            variable.RuntimeIntVal += 1;
                        }
                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            variable.RuntimeIntVal -= 1;
                        }
                        GUILayout.EndHorizontal();
                        break;
                    case VariableType.Trigger:
                        if (GUILayout.Button("Trigger: " + variable.name))
                        {
                            SetTrigger(variable);
                        }
                        break;
                }
            }

            EditorGUILayout.BeginVertical();
            EditorGUIUtility.labelWidth = oldWidth;
        }

        private void SetTrigger(NodeGraphVariable variable)
        {
            variable.RuntimeTriggerVal = true;
        }

        private void PreviewEventListeners()
        {
            var evt = Event.current;
            switch (evt.type)
            {
                case EventType.ScrollWheel:
                    cameraZoom += evt.delta.y * 0.1f;
                    evt.Use();
                    UpdatePosition();
                    break;
                case EventType.MouseDown:
                    if (evt.button == 0)
                        lastPosition = evt.mousePosition;
                    break;
                case EventType.MouseDrag:
                    if (evt.button == 0)
                    {
                        lastPosition.y = ClampAngle(lastPosition.y + evt.delta.y, -20, 80);
                        lastPosition.x = lastPosition.x + evt.delta.x;
                        UpdatePosition();
                    }
                    else if (evt.button == 1)
                    {
                        var off = new Vector3();
                        off.x = -evt.delta.x;
                        off.y = evt.delta.y;
                        off *= 0.01f * cameraZoom;
                        offset += previewCamera.transform.rotation * off;
                    }
                    break;
            }

        }

        private void UpdatePosition()
        {

            var rotation = Quaternion.Euler(lastPosition.y, lastPosition.x, 0);

            var distance = CalcCameraDist(goHeight);
            var position = offset + rotation * new Vector3(0, 0, -distance) + Vector3.zero;

            previewCamera.transform.rotation = rotation;
            previewCamera.transform.position = position;

            Repaint();
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F)
                angle += 360F;
            if (angle > 360F)
                angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
