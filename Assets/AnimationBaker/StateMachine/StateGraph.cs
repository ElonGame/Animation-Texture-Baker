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
		public List<Mesh> meshes { get; set; }
		public StartNode startNode;
		public EndNode endNode;
		public AnyNode anyNode;
		public bool animationLoaded { get; set; }

		public bool HasAnimation
		{
			get
			{
				return PrefabAnimation != null;
			}
		}

		public BaseNode AddNewAnimation(System.Type Type, AnimationClip clip = null, string name = "")
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
				var baseNode = (BaseNode) node;
				if (clip != null)
				{
					baseNode.Clip = clip;
					baseNode.Duration = clip.length;
					baseNode.WrapMode = clip.wrapMode;
					baseNode.FrameRate = clip.frameRate;
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
			RecalculateMesh();
		}

		private void RecalculateMesh()
		{
			if (!Prefab) return;
			Vertices = 0;
			meshes = new List<Mesh>();
			foreach (var mesh in GetMeshes(meshes, Prefab.transform))
			{
				Vertices += mesh.vertexCount;
			}
		}

		public void LoadAnimationStates()
		{
			if (!Prefab) return;
			PrefabAnimation = Prefab.GetComponent<Animation>();
#if UNITY_EDITOR
			foreach (AnimationClip clip in AnimationUtility.GetAnimationClips(Prefab))
			{
				foreach (BaseNode node in nodes)
				{
					if (node.name == clip.name)
					{
						node.Clip = clip;
					}
				}
			}
#endif
			animationLoaded = true;
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
			SyncVariables();
			RecalculateMesh();
		}

		private void SyncVariables()
		{
			foreach (BaseNode node in nodes)
			{
				foreach (var port in node.Ports)
				{
					foreach (var connection in port.Connections)
					{
						foreach (var rule in connection.rules)
						{
							if (rule.Variable != null) continue;
							foreach (var variable in variables)
							{
								if (variable.name == rule.VariableName)
								{
									rule.Variable = variable;
								}
							}
						}
					}
				}
			}
		}

		public NodeGraphVariable GetVariable(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				if (variables.Count > 0)
				{
					return variables[0];
				}
				return null;
			}
			foreach (var variable in variables)
			{
				if (variable.name == name)
				{
					return variable;
				}
			}
			return null;
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
		AnimationClip lastClip = null;
		bool isAnyClip = false;

		public AnimationClip Evaluate(float dt)
		{
			if (anyNode == null) return null;
			if (startNode == null) return null;
			runtime = dt;
			AnimationClip qualifiedState = anyNode.Evaluate(null);
			if (qualifiedState != null)
			{
				isAnyClip = true;
				if (lastClip != qualifiedState)
				{
					internalCounter = 0;
				}
				lastClip = qualifiedState;
				UpdateConnections(qualifiedState);
				return qualifiedState;
			}
			else if (isAnyClip)
			{
				if (
					internalCounter < lastClip.length
				)
				{
					UpdateConnections(lastClip);
					return lastClip;
				}
				else
				{
					isAnyClip = false;
				}
			}
			qualifiedState = startNode.Evaluate(null);
			if (qualifiedState != null)
			{
				if (lastClip != qualifiedState)
				{
					internalCounter = 0;
				}
				lastClip = qualifiedState;
				UpdateConnections(qualifiedState);
				return qualifiedState;
			}
			return null;
		}

		private void UpdateConnections(AnimationClip clip)
		{
			BaseNode activeNode = null;
			foreach (BaseNode node in nodes)
			{
				if (node.Clip == clip)
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
