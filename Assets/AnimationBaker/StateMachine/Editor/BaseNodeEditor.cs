using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AnimationBaker.StateMachine.Nodes;
using AnimationBaker.StateMachine.XNode;
using AnimationBaker.StateMachine.XNodeEditor;

namespace AnimationBaker.StateMachine.Editor
{
	[CustomNodeEditor(typeof(BaseNode))]
	public class BaseNodeEditor : NodeEditor
	{

		public override void OnHeaderGUI()
		{
			GUI.color = Color.white;
			base.OnHeaderGUI();
		}

		public override void OnBodyGUI()
		{
			BaseNode node = target as BaseNode;
			StateGraph graph = node.graph as StateGraph;

			base.OnBodyGUI();

			NodePort input = null;
			if (node.InstanceInputs.Count() > 0)
			{
				input = node.InstanceInputs.First();
			}
			NodePort output = null;
			if (node.InstanceOutputs.Count() > 0)
			{
				output = node.InstanceOutputs.First();
			}
			if (input == null)
			{
				NodeEditorGUILayout.VerticalPortField(null, output);
			}
			else if (output == null)
			{
				NodeEditorGUILayout.VerticalPortField(null, input);
			}
			else
			{
				NodeEditorGUILayout.VerticalPortPair(input, output);
			}
		}
	}
}
