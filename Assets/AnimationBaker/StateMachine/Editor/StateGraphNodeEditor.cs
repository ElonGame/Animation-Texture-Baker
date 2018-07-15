using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimationBaker.StateMachine.XNodeEditor;

namespace AnimationBaker.StateMachine.Editor
{
	[CustomNodeGraphEditor(typeof(StateGraph))]
	public class StateGraphNodeEditor : NodeGraphEditor
	{
		public override string GetNodeMenuName(System.Type type)
		{
			if (type.Namespace == "AnimationBaker.StateMachine.Nodes")
			{
				return base.GetNodeMenuName(type).Replace("Animation Baker/State Machine/Nodes/", "");
			}
			else return null;
		}
	}
}
