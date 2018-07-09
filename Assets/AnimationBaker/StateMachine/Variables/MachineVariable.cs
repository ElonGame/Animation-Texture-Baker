using System.Collections.Generic;
using UnityEngine;
using AnimationBaker.StateMachine.Nodes;
namespace AnimationBaker.StateMachine.Variables
{

    [System.Serializable]
    public class MachineVariable : ScriptableObject
    {
        [HideInInspector]
        public StateGraph graph;
        public string Name;
        public VariableType VariableType = VariableType.Boolean;
        [HideInInspector]
        public float DefaultFloatVal;
        [HideInInspector]
        public int DefaultIntVal;
        [HideInInspector]
        public bool DefaultBoolVal;
        [HideInInspector]
        public float RuntimeFloatVal;
        [HideInInspector]
        public int RuntimeIntVal;
        [HideInInspector]
        public bool RuntimeBoolVal;
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
