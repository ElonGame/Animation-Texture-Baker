using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNodeEditor;

namespace AnimationBaker.StateMachine.Editor
{
	[CustomNodeGraphEditor(typeof(StateGraph))]
	public class StateGraphNodeEditor : NodeGraphEditor
	{
		/// <summary> 
		/// Overriding GetNodePath lets you control if and how nodes are categorized.
		/// In this example we are sorting out all node types that are not in the XNode.Examples namespace.
		/// </summary>
		public override string GetNodeMenuName(System.Type type)
		{
			// return null;
			if (type.Namespace == "AnimationBaker.StateMachine.Nodes")
			{
				return base.GetNodeMenuName(type).Replace("Animation Baker/State Machine/Nodes/", "");
			}
			else return null;
		}
	}
}
