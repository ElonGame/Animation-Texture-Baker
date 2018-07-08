using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Unity.Collections;
using UnityEditor;

namespace AnimationBaker
{
    public class AnimationBakerWindow : EditorWindow
    {
        public ComputeShader infoTexGen;
        public Shader playShader;
        // public RenderTexture randomWrite;

        // const float animationFps = 60;

        GameObject chosen;
        Transform chosenTransform;
        GameObject lastChosen;
        SkinnedMeshRenderer meshRenderer;
        List<AnimationClip> clips = new List<AnimationClip>();
        List<bool> clipsImport = new List<bool>();
        List<MeshFilter> boneMeshes = new List<MeshFilter>();
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
                return clipsImport.Where(x => x).Count();
            }
        }
        string outputDir
        {
            get
            {
                return PlayerPrefs.GetString("animationbakeroutput", "");
            }
        }
        string outputPath
        {
            get
            {
                return outputDir.Substring(Application.dataPath.Length - 6);
            }
        }
        Transform rootBone;
        int totalClips = 0;

        [MenuItem("Assets/Create/Animation Baker/Prefab", false, 0)]
        private static void ShowWindow()
        {
            var window = GetWindow<AnimationBakerWindow>("Baked Animation Character", true);
            window.minSize = new Vector3(320, 320);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(4);
            var horizontalRect = EditorGUILayout.BeginHorizontal();
            var changeButtonRect = horizontalRect;
            changeButtonRect.x = horizontalRect.width - 50;
            changeButtonRect.width = 50;
            if (GUI.Button(changeButtonRect, "Change", EditorStyles.miniButton))
            {
                var path = EditorUtility.OpenFolderPanel("Choose Output Path",
                    outputDir, "");
                if (!string.IsNullOrEmpty(path))
                {
                    PlayerPrefs.SetString("animationbakeroutput", path);
                }
            }
            EditorGUILayout.LabelField("Output: " + Utils.ClampPath(outputDir, horizontalRect.width - 110));

            if (string.IsNullOrEmpty(outputDir)) return;
            if (!Directory.Exists(outputDir)) return;

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);
            chosen = EditorGUILayout.ObjectField("Choose Character", chosen, typeof(GameObject), false) as GameObject;
            if (!chosen) return;
            GUILayout.Space(4);
            chosenTransform = chosen.transform;
            if (chosen != lastChosen)
            {
                totalClips = 0;
                clipsImport.Clear();
                clips.Clear();
                lastChosen = chosen;
                rootBone = null;
                boneMeshes.Clear();
                if (meshRenderer = FindSkinnedMeshRenderer(chosen.transform))
                {
                    rootBone = meshRenderer.rootBone;
                    FindMeshInBones(boneMeshes, rootBone);
                }
                animation = FindAnimation(chosen.transform);
            }
            if (meshRenderer == null)
            {
                EditorGUILayout.HelpBox("A SkinnedMeshRenderer Component was not found on the GameObject's children ", MessageType.Error);
                GUILayout.Space(8);
                return;
            }
            else
            {
                EditorGUILayout.HelpBox("✔ Found Skinned Mesh Renderer ", MessageType.None);
            }
            if (rootBone == null)
            {
                EditorGUILayout.HelpBox("✔ The Root Bone is not found on the GameObject 's Skinned Mesh Renderer", MessageType.Error);
                GUILayout.Space(8);
                return;
            }
            else
            {
                EditorGUILayout.HelpBox("✔ Found Root Bone: " + rootBone.name, MessageType.None);
            }
            if (animation == null)
            {
                EditorGUILayout.HelpBox("The Animation Component was not found on the GameObject's children.We need a reference to it 's animation clips in order to bake them.", MessageType.Error);
                EditorGUILayout.HelpBox("Importing your model with Legacy Animations will automatically add this component.", MessageType.Info);
                GUILayout.Space(8);
                return;
            }
            if ((totalClips = animation.GetClipCount()) == 0)
            {
                EditorGUILayout.HelpBox("The Animation Component does not have any animation clips.", MessageType.Error);
                GUILayout.Space(8);
                return;
            }
            else
            {
                EditorGUILayout.HelpBox($"✔ Found {totalClips} animation clips", MessageType.None);
                if (clips.Count == 0)
                {
                    for (int i = 0; i < totalClips; i++)
                    {
                        clipsImport.Add(false);
                    }
                    clips.AddRange(AnimationUtility.GetAnimationClips(chosen));
                }
            }
            if (rootBone.rotation != Quaternion.identity)
            {
                EditorGUILayout.HelpBox("✔ Root Bone rotation will be adjusted to identity", MessageType.None);
            }
            if (rootBone.localScale != Vector3.one)
            {
                EditorGUILayout.HelpBox("✔ Root Bone scale will be adjusted to world space", MessageType.None);
            }
            if (meshRenderer.transform.rotation != Quaternion.identity)
            {
                EditorGUILayout.HelpBox("✔ Mesh rotation will be adjusted to identity", MessageType.None);
            }
            if (meshRenderer.transform.localScale != Vector3.one)
            {
                EditorGUILayout.HelpBox("✔ Mesh scale will be adjusted to world space", MessageType.None);
            }
            if (boneMeshes.Count > 0)
            {
                EditorGUILayout.HelpBox($"✔ Found {boneMeshes.Count} meshes on the bones.", MessageType.None);
            }

            EditorGUILayout.LabelField("Bake (" + bakeClipsCount + ") Animations: Default " + idleAnimation?.name);

            for (int i = 0; i < totalClips; i++)
            {
                EditorGUILayout.BeginHorizontal();
                clipsImport[i] = EditorGUILayout.ToggleLeft(clips[i].name, clipsImport[i]);
                if (i == 0)
                {
                    GUI.enabled = false;
                }
                if (GUILayout.Button("↑", EditorStyles.miniButton)) MoveClip(i, i - 1);
                if (i == 0)
                {
                    GUI.enabled = true;
                }
                if (i == totalClips - 1)
                {
                    GUI.enabled = false;
                }
                if (GUILayout.Button("↓", EditorStyles.miniButton)) MoveClip(i, i + 1);
                if (i == totalClips - 1)
                {
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(4);

            if (bakeClipsCount > 0 && GUILayout.Button("Generate"))
            {
                Generate();
            }
        }

        private void Generate()
        {
            if (!infoTexGen)
            {
                infoTexGen = (ComputeShader) Resources.Load("MeshInfoTextureGen", typeof(ComputeShader));
            }
            if (!playShader)
            {
                playShader = (Shader) Resources.Load("LitBakedAnimPlayer", typeof(Shader));
            }
            var instance = Instantiate(chosen, Vector3.zero, Quaternion.identity);
            var instanceTransform = instance.transform;
            var skinRenderer = FindSkinnedMeshRenderer(instanceTransform);
            var newAnimation = FindAnimation(instanceTransform);
            var boneMeshes = new List<MeshFilter>();
            FindMeshInBones(boneMeshes, instanceTransform);
            var skinTransform = skinRenderer.transform;
            var oldMesh = skinRenderer.sharedMesh;
            Mesh newMesh = new Mesh();
            var offset = PopulateMesh(newMesh, oldMesh, skinRenderer.transform);
            var verticesCount = newMesh.vertexCount;
            var prefabPath = Utils.Combine(outputPath, chosen.name);
            var prefabDir = Utils.Combine(outputDir, chosen.name);
            if (!Directory.Exists(prefabDir))
                Directory.CreateDirectory(prefabDir);

            // Save mesh
            AssetDatabase.CreateAsset(newMesh, prefabPath + "/" + chosen.name + "Mesh.asset");

            var scale = Vector3.one;
            scale.x = 1 / rootBone.localScale.x;
            scale.y = 1 / rootBone.localScale.y;
            scale.z = 1 / rootBone.localScale.z;

            var totalClips = 0;
            var frames = new List<int>();
            var frameDeltas = new List<float>();
            var totalFrames = 0;
            var finalClips = new List<AnimationClip>();
            for (int i = 0; i < clipsImport.Count; i++)
            {
                if (clipsImport[i] == false) continue;
                var clip = clips[i];
                var delta = clip.frameRate * 0.001f;
                var frame = Mathf.CeilToInt(clip.length / delta);
                frames.Add(frame);
                frameDeltas.Add(delta);
                totalFrames += frame;
                totalClips++;
                finalClips.Add(clip);
            }

            // to store metadata
            totalFrames += 1;

            var texHeight = Mathf.NextPowerOfTwo(totalFrames);
            var texWidth = Mathf.NextPowerOfTwo(verticesCount);

            // store first cell:
            // total clips

            // second cell and beyond:
            // x: frame count
            // y: 1 / framerate
            // z: offset
            // w: wrap mode:
            //    0 play once the revert to default
            //    1 play once the revert to default
            //    2 loop
            //    4 not implemented
            //    8 play once then hold

            var infoList = new List<VertInfo>();

            int texOffset = 1;

            // infoList.Add(new VertInfo { position = new Vector3(totalClips, 0, 0), normal = Vector3.zero });

            for (int i = 0; i < frames.Count; i++)
            {
                infoList.Add(new VertInfo
                {
                    position = new Vector3(frames[i], finalClips[i].length, texOffset),
                        normal = Vector3.zero,
                        extra = (int) finalClips[i].wrapMode
                });
                texOffset += (int) frames[i];
            }
            for (int i = totalClips; i < verticesCount; i++)
            {
                infoList.Add(new VertInfo
                {
                    position = Vector3.zero, normal = Vector3.zero
                });
            }

            var boneOffset = Vector3.zero;
            var boneScale = 0f;
            var animMesh = new Mesh();
            for (int i = 0; i < finalClips.Count; i++)
            {
                var clip = finalClips[i];
                var dt = 0f;
                var len = 0;
                while (dt < clip.length)
                {
                    clip.SampleAnimation(instance, Mathf.Clamp(dt, 0, clip.length));
                    skinRenderer.BakeMesh(animMesh);
                    if (boneScale == 0)
                    {
                        var bounds = new Bounds();
                        for (int j = 0; j < animMesh.vertexCount; j++)
                        {
                            var point = skinTransform.TransformPoint(animMesh.vertices[j]);
                            if (j == 0)
                            {
                                bounds.center = point;
                            }
                            bounds.Encapsulate(point);
                        }
                        foreach (var filter in boneMeshes)
                        {
                            var boneMesh = filter.sharedMesh;
                            for (int j = 0; j < boneMesh.vertexCount; j++)
                            {
                                var point = filter.transform.TransformPoint(boneMesh.vertices[j]);
                                bounds.Encapsulate(point);
                            }
                        }
                        boneScale = newMesh.bounds.size.y / bounds.size.y;
                        boneOffset.y = 0 - bounds.min.y;
                    }

                    for (int j = 0; j < animMesh.vertexCount; j++)
                    {
                        var vert = (skinTransform.TransformPoint(animMesh.vertices[j]) + boneOffset) * boneScale;
                        infoList.Add(new VertInfo { position = vert, normal = animMesh.normals[j] });
                    }
                    foreach (var filter in boneMeshes)
                    {
                        var mesh = filter.sharedMesh;
                        for (int k = 0; k < mesh.vertexCount; k++)
                        {
                            var vert = (filter.transform.TransformPoint(mesh.vertices[k]) + boneOffset) * boneScale;
                            infoList.Add(new VertInfo { position = vert, normal = mesh.normals[k], extra = 1 });
                        }
                    }
                    len++;
                    dt += frameDeltas[i];
                }
            }

            var positionsRenderTexture = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGBHalf);
            var normalRenderTexture = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGBHalf);
            var positionTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBAHalf, false, false);
            positionTexture.wrapMode = TextureWrapMode.Clamp;
            positionTexture.filterMode = FilterMode.Point;
            var normalTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBAHalf, false, false);
            normalTexture.wrapMode = TextureWrapMode.Clamp;
            normalTexture.filterMode = FilterMode.Point;

            foreach (var rt in new [] { positionsRenderTexture, normalRenderTexture })
            {
                rt.enableRandomWrite = true;
                rt.Create();
                RenderTexture.active = rt;
                GL.Clear(true, true, Color.clear);
            }

            var buffer = new ComputeBuffer(infoList.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertInfo)));
            buffer.SetData(infoList.ToArray());

            var kernel = infoTexGen.FindKernel("CSMain");
            uint x, y, z;
            infoTexGen.GetKernelThreadGroupSizes(kernel, out x, out y, out z);

            infoTexGen.SetInt("VertCount", verticesCount);
            infoTexGen.SetBuffer(kernel, "Info", buffer);
            infoTexGen.SetTexture(kernel, "OutPosition", positionsRenderTexture);
            infoTexGen.SetTexture(kernel, "OutNormal", normalRenderTexture);
            infoTexGen.Dispatch(kernel, verticesCount / (int) x + 1, texHeight / (int) y + 1, 1);

            var posTex = RenderTextureToTexture2D.Convert(positionsRenderTexture);
            var normTex = RenderTextureToTexture2D.Convert(normalRenderTexture);

            Graphics.CopyTexture(posTex, positionTexture);
            Graphics.CopyTexture(normTex, normalTexture);

            positionsRenderTexture.Release();
            normalRenderTexture.Release();

            buffer.Release();

            AssetDatabase.CreateAsset(positionTexture, Utils.Combine(prefabPath, "Positions.asset"));
            AssetDatabase.CreateAsset(normalTexture, Utils.Combine(prefabPath, "Normals.asset"));

            // var pngData = positionTexture.EncodeToPNG();
            // System.IO.File.WriteAllBytes(Utils.Combine(prefabPath, "DebugPositions.png"), pngData);

            AssetDatabase.Refresh();

            var materials = new Material[skinRenderer.sharedMaterials.Length];
            for (int i = 0; i < skinRenderer.sharedMaterials.Length; i++)
            {
                var mat = new Material(playShader);
                mat.name = string.Format("{0}.{1}.Material", chosen.name, i);
                mat.SetTexture("_MainTex", skinRenderer.sharedMaterials[i].mainTexture);
                mat.SetColor("_Color", skinRenderer.sharedMaterials[i].color);
                mat.SetTexture("_PosTex", positionTexture);
                mat.SetTexture("_NmlTex", normalTexture);
                mat.SetFloat("_TexHeight", texHeight);
                mat.SetFloat("_TotalAnimations", totalClips);
                // mat.SetTexture("_RandomWrite", randomWrite);
                // Graphics.SetRandomWriteTarget(1, randomWrite);
                mat.enableInstancing = true;
                AssetDatabase.CreateAsset(mat, prefabPath + "/" + mat.name + ".mat");
                materials[i] = mat;
            }
            var go = new GameObject(chosen.name);
            go.AddComponent<MeshFilter>().sharedMesh = newMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = materials;
            mr.material = materials[0];
            PrefabUtility.CreatePrefab(Utils.Combine(prefabPath, go.name + ".prefab"), go);
            DestroyImmediate(go);
            DestroyImmediate(instance);
        }

        private Vector3 PopulateMesh(Mesh newMesh, Mesh oldMesh, Transform offsetTransform)
        {
            var min = 0f;
            List<Vector3> vertices = new List<Vector3>(oldMesh.vertexCount);
            foreach (var vertex in oldMesh.vertices)
            {
                var point = offsetTransform.TransformPoint(vertex);
                vertices.Add(point);
                if (point.y < min)
                {
                    min = point.y;
                }
            }
            var vertexOffset = new Vector3(0, -min, 0);
            offsetTransform.position = vertexOffset;
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = vertices[i] + vertexOffset;
            }
            newMesh.subMeshCount = oldMesh.subMeshCount;
            newMesh.SetVertices(vertices);
            for (int i = 0; i < oldMesh.subMeshCount; i++)
            {
                newMesh.SetTriangles(oldMesh.GetTriangles(i).ToArray(), i);
            }
            var offset = vertices.Count;
            newMesh.uv = oldMesh.uv.ToArray();
            newMesh.normals = oldMesh.normals.ToArray();
            newMesh.tangents = oldMesh.tangents.ToArray();
            newMesh.colors = oldMesh.colors.ToArray();
            foreach (var filter in boneMeshes)
            {
                var boneMesh = filter.sharedMesh;
                var newVerts = newMesh.vertices.ToList();
                var newUv = newMesh.uv.ToList();
                var newNormals = newMesh.normals.ToList();
                var newTangents = newMesh.tangents.ToList();
                var newColors = newMesh.colors.ToList();
                var newTris = newMesh.triangles.ToList();

                for (int i = 0; i < boneMesh.vertexCount; i++)
                {
                    newVerts.Add(filter.transform.TransformPoint(boneMesh.vertices[i]));
                }
                newMesh.vertices = newVerts.ToArray();

                var boneTris = boneMesh.triangles.ToList();
                for (int i = 0; i < boneTris.Count; i++)
                {
                    boneTris[i] = boneTris[i] + offset;
                }
                newTris.AddRange(boneTris);
                newMesh.SetTriangles(newTris, 0);

                newUv.AddRange(boneMesh.uv);
                newNormals.AddRange(boneMesh.normals);
                newTangents.AddRange(boneMesh.tangents);
                newColors.AddRange(boneMesh.colors);

                newMesh.uv = newUv.ToArray();
                newMesh.normals = newNormals.ToArray();
                newMesh.tangents = newTangents.ToArray();
                if (oldMesh.colors.Length > 0)
                    newMesh.colors = newColors.ToArray();

                offset += boneMesh.vertexCount;
            }
            newMesh.RecalculateBounds();
            newMesh.MarkDynamic();
            return vertexOffset;
        }

        private void MoveClip(int current, int desired)
        {
            var clip = clips[current];
            var clipImport = clipsImport[current];
            clips.RemoveAt(current);
            clipsImport.RemoveAt(current);
            clips.Insert(desired, clip);
            clipsImport.Insert(desired, clipImport);
        }

        private SkinnedMeshRenderer FindSkinnedMeshRenderer(Transform parent)
        {
            SkinnedMeshRenderer mr;
            if (
                (mr = parent.GetComponent<SkinnedMeshRenderer>()) ||
                (mr = parent.GetComponentInChildren<SkinnedMeshRenderer>())
            )
            {
                return mr;
            }
            return null;
        }

        private Animation FindAnimation(Transform parent)
        {
            Animation animator;
            if (
                (animator = parent.GetComponent<Animation>()) ||
                (animator = parent.GetComponentInChildren<Animation>())
            )
            {
                return animator;
            }
            return null;
        }

        private void FindMeshInBones(List<MeshFilter> filters, Transform bone)
        {
            foreach (Transform child in bone)
            {
                FindMeshInBones(filters, child);
            }
            var filter = bone.GetComponent<MeshFilter>();
            if (filter != null)
            {
                filters.Add(filter);
            }
        }

        public struct VertInfo
        {
            public Vector3 position;
            public Vector3 normal;
            public float extra;
        }
    }
}
