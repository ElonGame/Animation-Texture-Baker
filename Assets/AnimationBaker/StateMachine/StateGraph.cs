using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using AnimationBaker.StateMachine.Nodes;
using AnimationBaker.StateMachine.XNode;
// using static AnimationBaker.StateMachine.XNode.Node;

namespace AnimationBaker.StateMachine
{
	[CreateAssetMenu(fileName = "Animation Graph", menuName = "Animation Baker/State Machine")]
	public class StateGraph : NodeGraph, ISerializationCallbackReceiver
	{
		public GameObject Prefab;
		public List<ClipData> AnimationClips = new List<ClipData>();
		public ShadowCastingMode ShadowCastingMode = ShadowCastingMode.On;
		public bool ReceivesShadows = true;
		public int Vertices = 0;
		public int PrefabHashCode = 0;
		public Animation PrefabAnimation { get; set; }
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
			if (clip.Node)
				RemoveNode(clip.Node);
			AnimationClips.Remove(clip);
			// Selection.activeObject = this;
		}

		public void OnBeforeSerialize() { }

		public void OnAfterDeserialize()
		{
			foreach (BaseNode node in nodes)
			{
				node.graph = this;
			}
		}

		public override void Boot()
		{
			if (!HasNode("End"))
			{
				var endNode = AddNewClip(typeof(EndNode), null, "End");
				var endPos = endNode.position;
				endPos.x += 300;
				endNode.position = endPos;
				endNode.AddInstanceInput(typeof(BaseNode.Empty), Node.ConnectionType.Multiple, "Input");
#if UNITY_EDITOR
				AssetDatabase.AddObjectToAsset(endNode, this);
				AssetDatabase.SaveAssets();
#endif
			}
			if (!HasNode("Start"))
			{
				var startNode = AddNewClip(typeof(StartNode), null, "Start");
				var startPos = startNode.position;
				startPos.x -= 300;
				startNode.position = startPos;
				startNode.AddInstanceOutput(typeof(BaseNode.Empty), Node.ConnectionType.Override, "Output");
#if UNITY_EDITOR
				AssetDatabase.AddObjectToAsset(startNode, this);
				AssetDatabase.SaveAssets();
#endif
			}
			if (!HasNode("Any"))
			{
				var anyNode = AddNewClip(typeof(AnyNode), null, "Any State");
				anyNode.AddInstanceOutput(typeof(BaseNode.Empty), Node.ConnectionType.Multiple, "Output");
#if UNITY_EDITOR
				AssetDatabase.AddObjectToAsset(anyNode, this);
				AssetDatabase.SaveAssets();
#endif
			}
			booted = true;
		}

		public bool HasNode(string name)
		{
			foreach (var node in nodes)
			{
				if (node.name == name)
				{
					return true;
				}
			}
			return false;
		}
	}
}
