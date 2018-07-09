using System.Collections.Generic;
using UnityEngine;
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
    public class TransitionRules : ScriptableObject
    {
        public List<TransitionRule> Rules = new List<TransitionRule>();
    }

    [System.Serializable]
    public class TransitionRule
    {
        public MachineVariable Variable;
        public Qualifier Qualifiers;
        public float QualifierValueFloatVal;
        public int QualifierValueIntVal;

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
