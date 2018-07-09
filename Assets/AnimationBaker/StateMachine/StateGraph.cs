using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using AnimationBaker.StateMachine.Nodes;
using AnimationBaker.StateMachine.Variables;
using XNode;

namespace AnimationBaker.StateMachine
{
	[CreateAssetMenu(fileName = "Animation Graph", menuName = "Animation Baker/State Machine")]
	public class StateGraph : NodeGraph, ISerializationCallbackReceiver
	{
		public GameObject Prefab;
		public List<ClipData> AnimationClips = new List<ClipData>();
		public List<MachineVariable> MachineVariables = new List<MachineVariable>();
		public ShadowCastingMode ShadowCastingMode = ShadowCastingMode.On;
		public bool ReceivesShadows = true;
		public int Vertices = 0;
		public int PrefabHashCode = 0;
		public Animation PrefabAnimation = null;
		public bool HasAnimation
		{
			get
			{
				return PrefabAnimation != null;
			}
		}

		public BaseNode AddNewClip(System.Type Type, AnimationClip clip = null, string name = "", int ClipIndex = 0)
		{
			var node = AddNode(Type);
			if (name == "")
			{
				if (clip != null)
				{
					name = clip.name;
				}
				else
				{
					name = "Unnamed " + UnityEngine.Random.Range(0, 999);
				}
			}
			node.name = name;
			if (Type == typeof(StateNode))
			{
				node.AddInstanceInput(typeof(BaseNode.Empty), Node.ConnectionType.Multiple, "Input");
				node.AddInstanceOutput(typeof(BaseNode.Empty), Node.ConnectionType.Multiple, "Output");
				if (clip != null)
				{
					AnimationClips.Add(new ClipData { Node = node as StateNode, Index = ClipIndex, Name = clip.name, WrapMode = clip.wrapMode, FrameRate = clip.frameRate, Duration = clip.length });
				}
			}
			return node as BaseNode;
		}

		public void SetPrefab(GameObject prefab)
		{
			Prefab = prefab;
			PrefabHashCode = prefab.GetHashCode();
			PrefabAnimation = prefab.GetComponent<Animation>();
		}

		public void RemoveClip(ClipData clip)
		{
#if UNITY_EDITOR
			if (clip.Node)
			{
				UnityEngine.Object.DestroyImmediate(clip.Node, true);
				RemoveNode(clip.Node);
				AssetDatabase.SaveAssets();
			}
#endif
			AnimationClips.Remove(clip);
			Selection.activeObject = this;
		}

		public void OnBeforeSerialize() { }

		public void OnAfterDeserialize()
		{
			foreach (BaseNode node in nodes)
			{
				node.graph = this;
			}
			foreach (var variable in MachineVariables)
			{
				variable.graph = this;
			}
		}
	}

	[System.Serializable]
	public class ClipData
	{
		public int Index;
		public float Duration;
		public float FrameRate;
		public StateNode Node;
		public string Name;
		public WrapMode WrapMode;
	}
}
