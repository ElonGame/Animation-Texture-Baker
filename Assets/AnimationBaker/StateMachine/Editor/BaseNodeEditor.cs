using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AnimationBaker.StateMachine.Nodes;
using XNodeEditor;

namespace AnimationBaker.StateMachine.Editor
{
	[CustomNodeEditor(typeof(BaseNode))]
	public class BaseNodeEditor : NodeEditor
	{

		public override void OnHeaderGUI()
		{
			GUI.color = Color.white;
			BaseNode node = target as BaseNode;
			StateGraph graph = node.graph as StateGraph;
			base.OnHeaderGUI();

		}

		public override void OnBodyGUI()
		{
			BaseNode node = target as BaseNode;
			StateGraph graph = node.graph as StateGraph;

			base.OnBodyGUI();

			if (node.HasState)
			{
				EditorGUILayout.BeginVertical();
				foreach (var port in node.Ports)
				{
					if (port.IsDynamic && port.IsOutput)
					{
						NodeEditorGUILayout.PortField(port);
					}
				}
				EditorGUILayout.EndVertical();
			}
		}
	}
}
