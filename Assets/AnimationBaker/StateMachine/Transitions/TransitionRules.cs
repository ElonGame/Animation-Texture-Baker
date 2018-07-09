using System.Collections.Generic;
using UnityEngine;
using AnimationBaker.StateMachine.Nodes;
using AnimationBaker.StateMachine.Variables;

namespace AnimationBaker.StateMachine.Transitions
{
    [System.Serializable]
    public class TransitionRules : ScriptableObject
    {
        public BaseNode node;
        [SerializeField]
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
}
