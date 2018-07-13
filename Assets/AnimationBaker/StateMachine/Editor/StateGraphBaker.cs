using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using AnimationBaker.StateMachine.Nodes;
using AnimationBaker.StateMachine.XNode;
using AnimationBaker.StateMachine.XNodeEditor;
using AnimationBaker.Utils;

namespace AnimationBaker.StateMachine.Editor
{
    public partial class StateGraphEditor : UnityEditor.Editor
    {

        SkinnedMeshRenderer _meshRenderer;
        SkinnedMeshRenderer meshRenderer
        {
            get
            {
                if (!graph || !graph.Prefab) return null;
                if (!_meshRenderer)
                {
                    _meshRenderer = graph.Prefab.GetComponentInChildren<SkinnedMeshRenderer>();
                }
                return _meshRenderer;
            }
        }

        Transform _rootBone;
        Transform rootBone
        {
            get
            {
                if (_rootBone == null)
                {
                    var renderer = meshRenderer;
                    if (renderer != null)
                    {
                        _rootBone = meshRenderer.rootBone;
                    }
                }
                return _rootBone;
            }
        }

        public int TextureWidth
        {
            get
            {
                return Mathf.NextPowerOfTwo(graph.Vertices);
            }
        }

        public int TextureHeight
        {
            get
            {
                int frames = 1;
                foreach (BaseNode node in graph.nodes)
                {
                    if (!node.Clip) continue;
                    var delta = node.FrameRate * 0.001f;
                    var frame = Mathf.CeilToInt(node.Duration / delta);
                    frames += frame;
                }
                return Mathf.NextPowerOfTwo(frames);
            }
        }

        string outputPath
        {
            get
            {
                return PlayerPrefs.GetString(graph.Prefab.name + "AnimationOutputDir", Application.dataPath);
            }
            set
            {
                PlayerPrefs.SetString(graph.Prefab.name + "AnimationOutputDir", value);
            }
        }

        ComputeShader infoTexGen;
        Shader playShader;

        public void DrawBaking()
        {
            if (!graph.animationLoaded) return;
            GUILayout.Space(4);
            var horizontalRect = EditorGUILayout.BeginHorizontal();
            var changeButtonRect = horizontalRect;
            changeButtonRect.x = horizontalRect.width - 35;
            changeButtonRect.width = 50;
            if (GUI.Button(changeButtonRect, "Change", EditorStyles.miniButton))
            {
                var path = EditorUtility.OpenFolderPanel("Choose Output Path", outputPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    outputPath = path;
                }
            }
            EditorGUILayout.LabelField("Output: " + StringUtils.ClampPath(outputPath, horizontalRect.width - 110));

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);

            if (string.IsNullOrEmpty(outputPath)) return;
            if (!Directory.Exists(outputPath)) return;

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
                EditorGUILayout.HelpBox("✔ The Root Bone is not found on the GameObject's Skinned Mesh Renderer", MessageType.Error);
                GUILayout.Space(8);
                return;
            }
            else
            {
                EditorGUILayout.HelpBox("✔ Found Root Bone: " + rootBone.name, MessageType.None);
            }
            if (graph.PrefabAnimation == null)
            {
                EditorGUILayout.HelpBox("The Animation Component was not found on the GameObject's children. We need a reference to it 's animation clips in order to bake them.", MessageType.Error);
                EditorGUILayout.HelpBox("Importing your model with Legacy Animations will automatically add this component.", MessageType.Info);
                GUILayout.Space(8);
                return;
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
            if (graph.meshes.Count > 0)
            {
                EditorGUILayout.HelpBox($"✔ Found {graph.meshes.Count} meshes on the bones.", MessageType.None);
            }
            EditorGUILayout.HelpBox($"✔ Position and Normal Texture Size: {TextureWidth} x {TextureHeight} ({(TextureWidth * TextureHeight / 131072f).ToString("N0")}MB)", MessageType.None);

            if (GUILayout.Button("Bake Prefab"))
            {
                Bake(GetClips());
            }
            if (GUILayout.Button("Generate ECS Component"))
            {
                GenerateFiles(GetClips());
            }
        }

        private void GenerateFiles(AnimationClip[] animationClip)
        {

        }

        private void Bake(AnimationClip[] clips)
        {
            if (!infoTexGen)
            {
                infoTexGen = (ComputeShader) Resources.Load("AnimationBaker/Shaders/MeshInfoTextureGen", typeof(ComputeShader));
            }
            if (!playShader)
            {
                playShader = (Shader) Resources.Load("AnimationBaker/Shaders/LitBakedStatePlayer", typeof(Shader));
            }

            AssetDatabase.SaveAssets();

            var instance = Instantiate(graph.Prefab, Vector3.zero, Quaternion.identity);
            var instanceTransform = instance.transform;
            var skinRenderer = FindSkinnedMeshRenderer(instanceTransform);

            for (int i = 0; i < skinRenderer.sharedMaterials.Length; i++)
            {
                var mat = skinRenderer.sharedMaterials[i];
                var materialPath = outputPath + "/" + mat.name + ".mat";
                if (File.Exists(materialPath))
                {
                    File.Delete(materialPath);
                }
            }

            var dataPathLength = Application.dataPath.Length - 6;

            var meshPath = outputPath + "/" + graph.Prefab.name + "Mesh.asset";
            if (File.Exists(meshPath))
            {
                File.Delete(meshPath);
            }
            meshPath = meshPath.Substring(dataPathLength);

            var positionTexturePath = StringUtils.Combine(outputPath, "Positions.asset");
            if (File.Exists(positionTexturePath))
            {
                File.Delete(positionTexturePath);
            }
            positionTexturePath = positionTexturePath.Substring(dataPathLength);

            var normalTexturePath = StringUtils.Combine(outputPath, "Normals.asset");
            if (File.Exists(normalTexturePath))
            {
                File.Delete(normalTexturePath);
            }
            normalTexturePath = normalTexturePath.Substring(dataPathLength);

            var prefabPath = StringUtils.Combine(outputPath, graph.Prefab.name + ".prefab");
            if (File.Exists(prefabPath))
            {
                File.Delete(prefabPath);
            }
            prefabPath = prefabPath.Substring(dataPathLength);

            AssetDatabase.Refresh();

            var newAnimation = FindAnimation(instanceTransform);
            var boneMeshes = new List<MeshFilter>();
            FindMeshInBones(boneMeshes, instanceTransform);
            var skinTransform = skinRenderer.transform;
            var oldMesh = skinRenderer.sharedMesh;
            Mesh newMesh = new Mesh();
            var offset = PopulateMesh(newMesh, oldMesh, boneMeshes, skinRenderer.transform);
            var verticesCount = newMesh.vertexCount;

            // Save mesh

            AssetDatabase.CreateAsset(newMesh, meshPath);

            var scale = Vector3.one;
            scale.x = 1 / rootBone.localScale.x;
            scale.y = 1 / rootBone.localScale.y;
            scale.z = 1 / rootBone.localScale.z;

            var totalClips = 0;
            var frames = new List<int>();
            var frameDeltas = new List<float>();
            var totalFrames = 0;
            var finalClips = new List<AnimationClip>();
            for (int i = 0; i < clips.Length; i++)
            {
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
                        infoList.Add(new VertInfo { position = vert, normal = animMesh.normals[j], extra = 1 });
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

            AssetDatabase.CreateAsset(positionTexture, positionTexturePath);
            AssetDatabase.CreateAsset(normalTexture, normalTexturePath);

            // var pngData = positionTexture.EncodeToPNG();
            // System.IO.File.WriteAllBytes(Utils.Combine(outputPath, "DebugPositions.png"), pngData);

            AssetDatabase.Refresh();

            var materials = new Material[skinRenderer.sharedMaterials.Length];
            for (int i = 0; i < skinRenderer.sharedMaterials.Length; i++)
            {
                var mat = new Material(playShader);
                mat.name = string.Format("{0}.{1}.Material", graph.Prefab.name, i);
                mat.SetTexture("_MainTex", skinRenderer.sharedMaterials[i].mainTexture);
                mat.SetColor("_Color", skinRenderer.sharedMaterials[i].color);
                mat.SetTexture("_PosTex", positionTexture);
                mat.SetTexture("_NmlTex", normalTexture);
                mat.enableInstancing = true;
                var materialPath = outputPath + "/" + mat.name + ".mat";
                materialPath = materialPath.Substring(dataPathLength);
                AssetDatabase.CreateAsset(mat, materialPath);
                materials[i] = mat;
            }
            var go = new GameObject(graph.Prefab.name);
            go.AddComponent<MeshFilter>().sharedMesh = newMesh;
            var assembly = Assembly.GetAssembly(typeof(StateGraph));
            var componentType = assembly.GetType("AnimationBaker.Baked.Baked" + graph.Prefab.name + "Component");
            go.AddComponent(componentType);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = materials;
            mr.material = materials[0];

            PrefabUtility.CreatePrefab(prefabPath, go);
            DestroyImmediate(go);
            DestroyImmediate(instance);
        }

        private AnimationClip[] GetClips()
        {
            return graph.nodes.Where(x =>((BaseNode) x).Clip != null).Select(x =>((BaseNode) x).Clip).ToArray();
        }

        private Vector3 PopulateMesh(Mesh newMesh, Mesh oldMesh, List<MeshFilter> boneMeshes, Transform offsetTransform)
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
    }

    public struct VertInfo
    {
        public Vector3 position;
        public Vector3 normal;
        public float extra;
    }
}
