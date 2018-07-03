using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace AnimationBaker
{
    public class AnimationBakerWindow : EditorWindow
    {
        public ComputeShader infoTexGen;
        public Shader playShader;

        GameObject chosen;
        Transform chosenTransform;
        GameObject lastChosen;
        SkinnedMeshRenderer meshRenderer;
        List<AnimationClip> clips = new List<AnimationClip> ();
        List<bool> clipsImport = new List<bool> ();
        Animation animation;
        AnimationClip idleAnimation
        {
            get
            {
                for (int i = 0; i < totalClips; i++)
                {
                    if (clipsImport[i]) return clips[i];
                }
                return null;
            }
        }
        int bakeClipsCount
        {
            get
            {
                return clipsImport.Where (x => x).Count ();
            }
        }
        string outputDir
        {
            get
            {
                return PlayerPrefs.GetString ("animationbakeroutput", "");
            }
        }
        string outputPath
        {
            get
            {
                return outputDir.Substring (Application.dataPath.Length - 6);
            }
        }
        Transform rootBone;
        int totalClips = 0;

        [MenuItem ("Assets/Create/Baked Animation Character", false, 0)]
        private static void ShowWindow ()
        {
            var window = GetWindow<AnimationBakerWindow> ("Baked Animation Character", true);
            window.minSize = new Vector3 (320, 320);
            window.Show ();
        }

        private void OnGUI ()
        {
            GUILayout.Space (4);
            var horizontalRect = EditorGUILayout.BeginHorizontal ();
            var changeButtonRect = horizontalRect;
            changeButtonRect.x = horizontalRect.width - 50;
            changeButtonRect.width = 50;
            if (GUI.Button (changeButtonRect, "Change", EditorStyles.miniButton))
            {
                var path = EditorUtility.OpenFolderPanel ("Choose Output Path",
                    outputDir, "");
                if (!string.IsNullOrEmpty (path))
                {
                    PlayerPrefs.SetString ("animationbakeroutput", path);
                }
            }
            EditorGUILayout.LabelField ("Output: " + Utils.ClampPath (outputDir, horizontalRect.width - 110));

            if (string.IsNullOrEmpty (outputDir)) return;
            if (!Directory.Exists (outputDir)) return;

            EditorGUILayout.EndHorizontal ();
            GUILayout.Space (4);
            chosen = EditorGUILayout.ObjectField ("Choose Character", chosen, typeof (GameObject), false) as GameObject;
            if (!chosen) return;
            GUILayout.Space (4);
            chosenTransform = chosen.transform;
            if (chosen != lastChosen)
            {
                totalClips = 0;
                clipsImport.Clear ();
                clips.Clear ();
                lastChosen = chosen;
                rootBone = null;
                if (meshRenderer = FindSkinnedMeshRenderer (chosen.transform))
                {
                    rootBone = meshRenderer.rootBone;
                }
                animation = FindAnimation (chosen.transform);
            }
            if (meshRenderer == null)
            {
                EditorGUILayout.HelpBox ("A SkinnedMeshRenderer Component was not found on the GameObject's children ", MessageType.Error);
                GUILayout.Space (8);
                return;
            }
            else
            {
                EditorGUILayout.HelpBox ("✔ Found Skinned Mesh Renderer ", MessageType.None);
            }
            if (rootBone == null)
            {
                EditorGUILayout.HelpBox ("✔ The Root Bone is not found on the GameObject 's Skinned Mesh Renderer", MessageType.Error);
                GUILayout.Space (8);
                return;
            }
            else
            {
                EditorGUILayout.HelpBox ("✔ Found Root Bone: " + rootBone.name, MessageType.None);
            }
            if (animation == null)
            {
                EditorGUILayout.HelpBox ("The Animation Component was not found on the GameObject's children.We need a reference to it 's animation clips in order to bake them.", MessageType.Error);
                EditorGUILayout.HelpBox ("Importing your model with Legacy Animations will automatically add this component.", MessageType.Info);
                GUILayout.Space (8);
                return;
            }
            if ((totalClips = animation.GetClipCount ()) == 0)
            {
                EditorGUILayout.HelpBox ("The Animation Component does not have any animation clips.", MessageType.Error);
                GUILayout.Space (8);
                return;
            }
            else
            {
                EditorGUILayout.HelpBox ($"✔ Found {totalClips} animation clips", MessageType.None);
                if (clips.Count == 0)
                {
                    for (int i = 0; i < totalClips; i++)
                    {
                        clipsImport.Add (false);
                    }
                    clips.AddRange (AnimationUtility.GetAnimationClips (chosen));
                }
            }
            if (rootBone.rotation != Quaternion.identity)
            {
                EditorGUILayout.HelpBox ("✔ Root Bone rotation will be adjusted to identity", MessageType.None);
            }
            if (rootBone.localScale != Vector3.one)
            {
                EditorGUILayout.HelpBox ("✔ Root Bone scale will be adjusted to world space", MessageType.None);
            }
            if (meshRenderer.transform.rotation != Quaternion.identity)
            {
                EditorGUILayout.HelpBox ("✔ Mesh rotation will be adjusted to identity", MessageType.None);
            }
            if (meshRenderer.transform.localScale != Vector3.one)
            {
                EditorGUILayout.HelpBox ("✔ Mesh scale will be adjusted to world space", MessageType.None);
            }

            EditorGUILayout.LabelField ("Bake (" + bakeClipsCount + ") Animations: ");

            for (int i = 0; i < totalClips; i++)
            {
                EditorGUILayout.BeginHorizontal ();
                clipsImport[i] = EditorGUILayout.ToggleLeft (clips[i].name, clipsImport[i]);
                if (i == 0)
                {
                    GUI.enabled = false;
                }
                // if (GUILayout.Button ("↑", EditorStyles.miniButton)) MoveClip (i, i - 1);
                if (i == 0)
                {
                    GUI.enabled = true;
                }
                if (i == totalClips - 1)
                {
                    GUI.enabled = false;
                }
                // if (GUILayout.Button ("↓", EditorStyles.miniButton)) MoveClip (i, i + 1);
                if (i == totalClips - 1)
                {
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal ();
            }

            GUILayout.Space (4);

            if (bakeClipsCount > 0 && GUILayout.Button ("Generate"))
            {
                Generate ();
            }
        }

        private void Generate ()
        {
            if (!infoTexGen)
            {
                infoTexGen = (ComputeShader) Resources.Load ("MeshInfoTextureGen", typeof (ComputeShader));
            }
            if (!playShader)
            {
                playShader = (Shader) Resources.Load ("BakedAnimPlayer", typeof (Shader));
            }
            var instance = Instantiate (chosen, Vector3.zero, Quaternion.identity);
            var skinRenderer = FindSkinnedMeshRenderer (instance.transform);
            var newAnimation = FindAnimation (instance.transform);
            var skinTransform = skinRenderer.transform;
            var oldMesh = skinRenderer.sharedMesh;
            Mesh newMesh = new Mesh ();
            var offset = PopulateMesh (newMesh, oldMesh, skinRenderer.transform);
            var verticesCount = newMesh.vertexCount;
            var texWidth = Mathf.NextPowerOfTwo (verticesCount);
            var prefabPath = Utils.Combine (outputPath, chosen.name);
            var prefabDir = Utils.Combine (outputDir, chosen.name);
            if (!Directory.Exists (prefabDir))
                Directory.CreateDirectory (prefabDir);
            AssetDatabase.Refresh ();

            // Save mesh
            AssetDatabase.CreateAsset (newMesh, prefabPath + "/" + chosen.name + "Mesh.asset");
            var animMesh = new Mesh ();
            foreach (AnimationClip clip in AnimationUtility.GetAnimationClips (instance))
            {
                var clipIndex = clips.IndexOf (clip);
                Debug.Log (clip.name);
                if (clipIndex == -1) continue;
                Debug.Log (1);
                if (clipsImport[clipIndex] == false) continue;
                Debug.Log (2);

                var fileName = Utils.CreateFileName (clip.name);
                var statePath = Utils.Combine (prefabPath, fileName);
                var stateDir = Utils.Combine (prefabDir, fileName);
                if (!Directory.Exists (stateDir))
                    Directory.CreateDirectory (stateDir);
                AssetDatabase.Refresh ();

                newAnimation.Play (clip.name);
                var frames = Mathf.NextPowerOfTwo ((int) (clip.length / 0.05f));
                var frameRate = clip.length / frames;
                var time = 0f;
                var infoList = new List<VertInfo> ();

                var positionsRenderTexture = new RenderTexture (texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
                positionsRenderTexture.name = string.Format ("{0}.{1}.Positions", chosen.name, fileName);
                var normalRenderTexture = new RenderTexture (texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
                normalRenderTexture.name = string.Format ("{0}.{1}.Normals", chosen.name, fileName);
                foreach (var rt in new [] { positionsRenderTexture, normalRenderTexture })
                {
                    rt.enableRandomWrite = true;
                    rt.Create ();
                    RenderTexture.active = rt;
                    GL.Clear (true, true, Color.clear);
                }

                var scale = Vector3.one;
                scale.x = 1 / skinTransform.localScale.x;
                scale.y = 1 / skinTransform.localScale.y;
                scale.z = 1 / skinTransform.localScale.z;

                var scaledOffset = Vector3.Scale (offset, skinTransform.localScale);

                for (var i = 0; i < frames; i++)
                {
                    clip.SampleAnimation (instance, time);
                    skinRenderer.BakeMesh (animMesh);
                    for (int j = 0; j < verticesCount; j++)
                    {
                        var vert = Vector3.Scale (skinTransform.TransformPoint (animMesh.vertices[j]), scale) + scaledOffset;
                        infoList.Add (new VertInfo { position = vert, normal = animMesh.normals[j] });
                    }
                    time += frameRate;
                }
                var buffer = new ComputeBuffer (infoList.Count, System.Runtime.InteropServices.Marshal.SizeOf (typeof (VertInfo)));
                buffer.SetData (infoList.ToArray ());

                var kernel = infoTexGen.FindKernel ("CSMain");
                uint x, y, z;
                infoTexGen.GetKernelThreadGroupSizes (kernel, out x, out y, out z);

                infoTexGen.SetInt ("VertCount", verticesCount);
                infoTexGen.SetBuffer (kernel, "Info", buffer);
                infoTexGen.SetTexture (kernel, "OutPosition", positionsRenderTexture);
                infoTexGen.SetTexture (kernel, "OutNormal", normalRenderTexture);
                infoTexGen.Dispatch (kernel, verticesCount / (int) x + 1, frames / (int) y + 1, 1);

                buffer.Release ();

                var posTex = RenderTextureToTexture2D.Convert (positionsRenderTexture);
                var normTex = RenderTextureToTexture2D.Convert (normalRenderTexture);
                Graphics.CopyTexture (positionsRenderTexture, posTex);
                Graphics.CopyTexture (normalRenderTexture, normTex);

                AssetDatabase.CreateAsset (posTex, Utils.Combine (statePath, positionsRenderTexture.name + ".asset"));
                AssetDatabase.CreateAsset (normTex, Utils.Combine (statePath, normalRenderTexture.name + ".asset"));

                var materials = new Material[skinRenderer.sharedMaterials.Length];
                for (int i = 0; i < skinRenderer.sharedMaterials.Length; i++)
                {
                    var mat = new Material (playShader);
                    mat.name = string.Format ("{0}.{1}.{2}.Materials", chosen.name, fileName, i);
                    mat.SetTexture ("_MainTex", skinRenderer.sharedMaterials[i].mainTexture);
                    mat.SetColor ("_Color", skinRenderer.sharedMaterials[i].color);
                    mat.SetTexture ("_PosTex", posTex);
                    mat.SetTexture ("_NmlTex", normTex);
                    mat.SetFloat ("_Length", clip.length);
                    mat.SetFloat ("_Loop", 1f);
                    mat.EnableKeyword ("ANIM_LOOP");
                    AssetDatabase.CreateAsset (mat, statePath + "/" + mat.name + ".mat");
                    materials[i] = mat;
                }
                var go = new GameObject (chosen.name + "." + fileName);
                go.AddComponent<MeshFilter> ().sharedMesh = newMesh;
                var mr = go.AddComponent<MeshRenderer> ();
                mr.sharedMaterials = materials;
                mr.material = materials[0];
                PrefabUtility.CreatePrefab (Utils.Combine (prefabPath, go.name + ".prefab"), go);
                DestroyImmediate (go);
            }
            DestroyImmediate (instance);
        }

        private Vector3 PopulateMesh (Mesh newMesh, Mesh oldMesh, Transform offsetTransform)
        {
            var min = 0f;
            List<Vector3> vertices = new List<Vector3> (oldMesh.vertexCount);
            foreach (var vertex in oldMesh.vertices)
            {
                var point = offsetTransform.TransformPoint (vertex);
                vertices.Add (point);
                if (point.y < min)
                {
                    min = point.y;
                }
            }
            var vertexOffset = new Vector3 (0, -min, 0);
            offsetTransform.position = vertexOffset;
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = vertices[i] + vertexOffset;
            }
            newMesh.SetVertices (vertices);
            for (int i = 0; i < oldMesh.subMeshCount; i++)
            {
                newMesh.SetTriangles (oldMesh.GetTriangles (i).ToArray (), i);
            }
            newMesh.uv = oldMesh.uv.ToArray ();
            newMesh.normals = oldMesh.normals.ToArray ();
            newMesh.tangents = oldMesh.tangents.ToArray ();
            newMesh.colors = oldMesh.colors.ToArray ();
            newMesh.RecalculateBounds ();
            newMesh.MarkDynamic ();
            return vertexOffset;
        }

        private void MoveClip (int current, int desired)
        {
            var clip = clips[current];
            var clipImport = clipsImport[current];
            clips.RemoveAt (current);
            clipsImport.RemoveAt (current);
            if (desired > current)
            {
                desired--;
            }
            clips.Insert (desired, clip);
            clipsImport.Insert (desired, clipImport);
        }

        private SkinnedMeshRenderer FindSkinnedMeshRenderer (Transform parent)
        {
            SkinnedMeshRenderer mr;
            if (
                (mr = parent.GetComponent<SkinnedMeshRenderer> ()) ||
                (mr = parent.GetComponentInChildren<SkinnedMeshRenderer> ())
            )
            {
                return mr;
            }
            return null;
        }

        private Animation FindAnimation (Transform parent)
        {
            Animation animator;
            if (
                (animator = parent.GetComponent<Animation> ()) ||
                (animator = parent.GetComponentInChildren<Animation> ())
            )
            {
                return animator;
            }
            return null;
        }

        public struct VertInfo
        {
            public Vector3 position;
            public Vector3 normal;
        }
    }
}