using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using AnimationBaker.StateMachine.Nodes;
using XNode;

namespace AnimationBaker.StateMachine
{
	[System.Serializable]
	public class StateMachineVariables
	{
		public string Name;
		public VariableType VariableType = VariableType.Boolean;
	}

	[CreateAssetMenu(fileName = "Animation Graph", menuName = "Animation Baker/State Machine")]
	public class StateGraph : NodeGraph
	{
		public GameObject Prefab;
		public List<ClipData> AnimationClips = new List<ClipData>();
		public List<StateMachineVariables> MachineVariables = new List<StateMachineVariables>();

		public void AddNewClip()
		{
			var node = AddNode<StateNode>();
			node.name = "Unnamed " + UnityEngine.Random.Range(0, 999);
			AnimationClips.Add(new ClipData { Node = node });
#if UNITY_EDITOR
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			RefreshWindow();
#endif
			Selection.activeObject = this;
		}

		public void RemoveClip(ClipData clip)
		{
#if UNITY_EDITOR
			if (clip.Node)
			{
				RemoveNode(clip.Node);
				DestroyImmediate(clip.Node);
			}
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			RefreshWindow();
			// AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			// EditorUtility.SetDirty(this);
#endif
			AnimationClips.Remove(clip);
			Selection.activeObject = this;
		}

		private void RefreshWindow()
		{
#if UNITY_EDITOR
			Type win = typeof(EditorWindow).Assembly.GetType("XNodeEditor.NodeEditorWindow");
			Debug.Log(win);
			if (win != null)
				EditorWindow.GetWindow(win.GetType()).Repaint();
#endif
		}
	}

	[System.Serializable]
	public class ClipData
	{
		public StateNode Node;
		public AnimationClip Clip;
		public WrapMode WrapMode;
	}

	[System.Serializable]
	public class NodeVariable
	{
		public VariableType VariableType;
		public Qualifier Qualifiers;
	}

	public enum Qualifier
	{
		Equal,
		LessThanAndEqual,
		LessThan,
		MoreThanAndEqual,
		MoreThan
	}

	[System.Serializable]
	public enum VariableType
	{
		Integer,
		Float,
		Boolean,
		Trigger
	}
}
