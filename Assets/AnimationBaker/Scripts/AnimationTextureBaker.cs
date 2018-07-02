using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

public class AnimationTextureBaker : MonoBehaviour
{
    public ComputeShader infoTexGen;
    public Shader playShader;

    public struct VertInfo
    {
        public Vector3 position;
        public Vector3 normal;
    }

    // Use this for initialization
    void Start ()
    {
        var name = this.name;
        var basePath = "BakedAnimationTex";
        var subFolderPath = Path.Combine ("Assets", basePath);
        var folderName = Path.Combine (subFolderPath, name);
        var animation = GetComponent<Animation> ();
        var skin = GetComponentInChildren<SkinnedMeshRenderer> ();
        var vCount = skin.sharedMesh.vertexCount;
        var skinTrans = skin.transform;
        var texWidth = Mathf.NextPowerOfTwo (vCount);
        var mesh = new Mesh ();

        Mesh newMesh = new Mesh ();
        newMesh.vertices = skin.sharedMesh.vertices;
        newMesh.subMeshCount = skin.sharedMesh.subMeshCount;
        for (int i = 0; i < skin.sharedMesh.subMeshCount; i++)
        {
            newMesh.SetTriangles (skin.sharedMesh.GetTriangles (i), i);
        }
        newMesh.uv = skin.sharedMesh.uv;
        newMesh.normals = skin.sharedMesh.normals;
        newMesh.colors = skin.sharedMesh.colors;
        newMesh.tangents = skin.sharedMesh.tangents;

        var scale = Vector3.one;
        scale.x = scale.x / skinTrans.localScale.x;
        scale.y = scale.y / skinTrans.localScale.y;
        scale.z = scale.z / skinTrans.localScale.z;

        var rotation = Quaternion.Euler (Quaternion.identity.eulerAngles + skin.transform.rotation.eulerAngles);
        var vertices = new Vector3[newMesh.vertexCount];
        for (int j = 0; j < newMesh.vertexCount; j++)
        {
            vertices[j] = skinTrans.TransformPoint (newMesh.vertices[j]);
        }
        newMesh.vertices = vertices;

        newMesh.RecalculateBounds ();

        if (!AssetDatabase.IsValidFolder (folderName))
        {
            if (!AssetDatabase.IsValidFolder (subFolderPath))
            {
                AssetDatabase.CreateFolder ("Assets", basePath);
            }
            if (!AssetDatabase.IsValidFolder (folderName))
            {
                AssetDatabase.CreateFolder (subFolderPath, name);
            }
        }

        AssetDatabase.CreateAsset (newMesh, Path.Combine (folderName, name + "Mesh.asset"));

        foreach (AnimationState state in animation)
        {
            var fileName = StringUtils.CreateFileName (state.name);
            animation.Play (state.name);
            var frames = Mathf.NextPowerOfTwo ((int) (state.length / 0.05f));
            var dt = state.length / frames;
            var time = 0f;
            var infoList = new List<VertInfo> ();

            var pRt = new RenderTexture (texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
            pRt.name = string.Format ("{0}.{1}.posTex", name, fileName);
            var nRt = new RenderTexture (texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
            nRt.name = string.Format ("{0}.{1}.normTex", name, fileName);;
            foreach (var rt in new [] { pRt, nRt })
            {
                rt.enableRandomWrite = true;
                rt.Create ();
                RenderTexture.active = rt;
                GL.Clear (true, true, Color.clear);
            }

            for (var i = 0; i < frames; i++)
            {
                state.time = time;
                animation.Sample ();
                skin.BakeMesh (mesh);
                var animVertices = new Vector3[mesh.vertices.Length];
                for (int j = 0; j < mesh.vertexCount; j++)
                {
                    animVertices[j] = Vector3.Scale (skinTrans.TransformPoint (mesh.vertices[j]), scale);
                }

                infoList.AddRange (Enumerable.Range (0, vCount)
                    .Select (idx => new VertInfo ()
                    {
                        position = animVertices[idx],
                            normal = mesh.normals[idx]
                    })
                );

                time += dt;
            }
            var buffer = new ComputeBuffer (infoList.Count, System.Runtime.InteropServices.Marshal.SizeOf (typeof (VertInfo)));
            buffer.SetData (infoList.ToArray ());

            var kernel = infoTexGen.FindKernel ("CSMain");
            uint x, y, z;
            infoTexGen.GetKernelThreadGroupSizes (kernel, out x, out y, out z);

            infoTexGen.SetInt ("VertCount", vCount);
            infoTexGen.SetBuffer (kernel, "Info", buffer);
            infoTexGen.SetTexture (kernel, "OutPosition", pRt);
            infoTexGen.SetTexture (kernel, "OutNormal", nRt);
            infoTexGen.Dispatch (kernel, vCount / (int) x + 1, frames / (int) y + 1, 1);

            buffer.Release ();

#if UNITY_EDITOR
            var posTex = RenderTextureToTexture2D.Convert (pRt);
            var normTex = RenderTextureToTexture2D.Convert (nRt);
            Graphics.CopyTexture (pRt, posTex);
            Graphics.CopyTexture (nRt, normTex);
            AssetDatabase.CreateAsset (posTex, Path.Combine (folderName, pRt.name + ".asset"));
            AssetDatabase.CreateAsset (normTex, Path.Combine (folderName, nRt.name + ".asset"));

            var materials = new Material[skin.materials.Length];
            var go = new GameObject (name + "." + fileName);
            for (int i = 0; i < skin.materials.Length; i++)
            {
                var mat = new Material (playShader);
                mat.SetTexture ("_MainTex", skin.materials[i].mainTexture);
                mat.SetColor ("_Color", skin.materials[i].color);
                mat.SetTexture ("_PosTex", posTex);
                mat.SetTexture ("_NmlTex", normTex);
                mat.SetFloat ("_Length", state.length);
                if (state.wrapMode == WrapMode.Loop)
                {
                    mat.SetFloat ("_Loop", 1f);
                    mat.EnableKeyword ("ANIM_LOOP");
                }
                var path = Path.Combine (folderName, string.Format ("{0}.{1}.{2}.animTex.asset", name, fileName, i));
                AssetDatabase.CreateAsset (mat, path);
                materials[i] = AssetDatabase.LoadMainAssetAtPath (path) as Material;
            }
            go.AddComponent<MeshFilter> ().sharedMesh = newMesh;
            var meshRenderer = go.AddComponent<MeshRenderer> ();
            meshRenderer.sharedMaterials = materials;
            PrefabUtility.CreatePrefab (Path.Combine (subFolderPath, go.name + ".prefab").Replace ("\\", "/"), go);
#endif
        }
        AssetDatabase.SaveAssets ();
    }
}