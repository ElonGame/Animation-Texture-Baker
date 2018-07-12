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

namespace AnimationBaker.StateMachine
{
	[CreateAssetMenu(fileName = "Animation Graph", menuName = "Animation Baker/State Machine")]
	public class StateGraph : NodeGraph, ISerializationCallbackReceiver
	{
		public GameObject Prefab;
		public ShadowCastingMode ShadowCastingMode = ShadowCastingMode.On;
		public bool ReceivesShadows = true;
		public int Vertices = 0;
		public int PrefabHashCode = 0;
		public Animation PrefabAnimation { get; set; }

		List<Mesh> meshes = new List<Mesh>();
		public StartNode startNode;
		public EndNode endNode;
		public AnyNode anyNode;
		bool animationLoaded = false;

		public bool HasAnimation
		{
			get
			{
				return PrefabAnimation != null;
			}
		}

		public BaseNode AddNewAnimation(System.Type Type, AnimationState state = null, string name = "")
		{
			var node = AddNode(Type);
			if (name == "")
			{
				if (state != null)
				{
					name = state.name;
				}
				else
				{
					name = "Unnamed " + UnityEngine.Random.Range(0, 999);
				}
			}
			node.name = name;
			if (Type == typeof(StateNode))
			{
				var baseNode = (BaseNode) node;
				if (state != null)
				{
					baseNode.AnimationState = state;
					baseNode.Duration = state.clip.length;
					baseNode.WrapMode = state.wrapMode;
					baseNode.FrameRate = state.clip.frameRate;
				}
				baseNode.AddInstanceInput(typeof(BaseNode.Empty), Node.ConnectionType.Multiple, "Input");
				baseNode.AddInstanceOutput(typeof(BaseNode.Empty), Node.ConnectionType.Multiple, "Output");
			}
			return node as BaseNode;
		}

		public void SetPrefab(GameObject prefab)
		{
			Prefab = prefab;
			PrefabHashCode = prefab.GetHashCode();
			PrefabAnimation = prefab.GetComponent<Animation>();
			foreach (var mesh in GetMeshes(meshes, prefab.transform))
			{
				Vertices += mesh.vertexCount;
			}
		}

		public void LoadAnimationStates()
		{
			if (!Prefab) return;
			PrefabAnimation = Prefab.GetComponent<Animation>();
			if (PrefabAnimation)
			{
				foreach (AnimationState state in PrefabAnimation)
				{
					foreach (BaseNode node in nodes)
					{
						if (node.name == state.clip.name)
						{
							node.AnimationState = state;
						}
					}
				}
				animationLoaded = true;
			}
		}

		private void OnValidate()
		{
			if (startNode == null || endNode == null || anyNode == null)
				FindKeyNodes();
			if (!animationLoaded)
				LoadAnimationStates();
		}

		private void OnEnable()
		{
			LoadAnimationStates();
			FindKeyNodes();
		}

		private void FindKeyNodes()
		{
			for (int i = nodes.Count - 1; i > -1; i--)
			{
				if (nodes[i] == null)
				{
					nodes.RemoveAt(i);
				}
			}
			foreach (BaseNode node in nodes)
			{
				node.graph = this;
				if (node.NodeType == NodeType.Start)
				{
					startNode = (StartNode) node;
				}
				if (node.NodeType == NodeType.End)
				{
					endNode = (EndNode) node;
				}
				if (node.NodeType == NodeType.Any)
				{
					anyNode = (AnyNode) node;
				}
			}
		}

		List<Mesh> GetMeshes(List<Mesh> meshes, Transform source)
		{
			var filter = source.GetComponent<MeshFilter>();
			var skinRenderer = source.GetComponent<SkinnedMeshRenderer>();
			if (filter && filter.sharedMesh)
			{
				meshes.Add(filter.sharedMesh);
			}
			if (skinRenderer && skinRenderer.sharedMesh)
			{
				meshes.Add(skinRenderer.sharedMesh);
			}
			foreach (Transform child in source)
			{
				GetMeshes(meshes, child);
			}
			return meshes;
		}

		public void OnBeforeSerialize() { }

		public void OnAfterDeserialize()
		{
			foreach (BaseNode node in nodes)
			{
				if (node == null) DestroyImmediate(node);
				node.graph = this;
			}
		}

		public override void Boot()
		{
			if (!HasNode("End"))
			{
				var endNode = AddNewAnimation(typeof(EndNode), null, "End");
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
				var startNode = AddNewAnimation(typeof(StartNode), null, "Start");
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
				var anyNode = AddNewAnimation(typeof(AnyNode), null, "Any State");
				anyNode.AddInstanceOutput(typeof(BaseNode.Empty), Node.ConnectionType.Multiple, "Output");
#if UNITY_EDITOR
				AssetDatabase.AddObjectToAsset(anyNode, this);
				AssetDatabase.SaveAssets();
#endif
			}
			booted = true;

			FindKeyNodes();
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

		float _lastRuntime = 0;

		float runtime = 0;
		public float internalCounter
		{
			get
			{
				return runtime - _lastRuntime;
			}
			set
			{
				_lastRuntime = runtime;
			}
		}
		AnimationState lastState = null;
		bool isAnyState = false;

		public AnimationState Evaluate(float dt)
		{
			if (anyNode == null) return null;
			if (startNode == null) return null;
			runtime = dt;
			AnimationState qualifiedState = anyNode.Evaluate(null);
			if (qualifiedState != null)
			{
				isAnyState = true;
				if (lastState != qualifiedState)
				{
					internalCounter = 0;
				}
				lastState = qualifiedState;
				UpdateConnections(qualifiedState);
				return qualifiedState;
			}
			else if (isAnyState)
			{
				if (
					internalCounter < lastState.length
				)
				{
					UpdateConnections(lastState);
					return lastState;
				}
				else
				{
					isAnyState = false;
				}
			}
			qualifiedState = startNode.Evaluate(null);
			if (qualifiedState != null)
			{
				if (lastState != qualifiedState)
				{
					internalCounter = 0;
				}
				lastState = qualifiedState;
				UpdateConnections(qualifiedState);
				return qualifiedState;
			}
			return null;
		}

		private void UpdateConnections(AnimationState state)
		{
			BaseNode activeNode = null;
			foreach (BaseNode node in nodes)
			{
				if (node.AnimationState == state)
				{
					activeNode = node;
				}
				foreach (var port in node.Inputs)
				{
					foreach (var connection in port.Connections)
					{
						connection.Cleared = false;
					}
				}
			}
			if (activeNode != null)
			{
				SetActive(activeNode);
			}
		}

		private static void SetActive(BaseNode node)
		{
			foreach (var port in node.Inputs)
			{
				foreach (var connection in port.Connections)
				{
					connection.Cleared = true;
				}
			}
		}
	}

}
