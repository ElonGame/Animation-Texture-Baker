using UnityEngine;

namespace AnimationBaker.StateMachine.XNode
{
    [System.Serializable]
    public class TransitionRule : ScriptableObject
    {
        NodeGraphVariable _variable;
        public NodeGraphVariable Variable
        {
            get
            {
                if (_variable == null)
                {
                    _variable = graph.GetVariable(VariableName);
                }
                return _variable;
            }
            set
            {
                _variable = value;
            }
        }

        [HideInInspector]
        public StateGraph graph;
        [HideInInspector]
        public string VariableName = "";
        public Qualifier Qualifier;
        public bool QualifierBoolVal;
        public TrueFalse QualifierBoolEnum
        {
            get
            {
                return (TrueFalse) (QualifierBoolVal ? 1 : 0);
            }
            set
            {
                QualifierBoolVal = ((int) value == 1 ? true : false);
            }
        }
        public float QualifierFloatVal;
        public int QualifierIntVal;

        internal bool Evaluate()
        {
            var results = false;
            switch (Variable.VariableType)
            {
                case VariableType.Boolean:
                    results = Variable.RuntimeBoolVal;
                    break;
                case VariableType.Trigger:
                    if (Variable.RuntimeTriggerVal)
                    {
                        Variable.RuntimeTriggerVal = false;
                        results = true;
                    }
                    else
                    {
                        results = false;
                    }
                    break;
                case VariableType.Float:
                    results = QualifyFloat();
                    break;
                case VariableType.Integer:
                    results = QualifyInteger();
                    break;
            }
            return results;
        }

        private bool QualifyFloat()
        {
            switch (Qualifier)
            {
                case Qualifier.Equal:
                    return Variable.RuntimeFloatVal == QualifierFloatVal;
                case Qualifier.NotEqual:
                    return Variable.RuntimeFloatVal != QualifierFloatVal;
                case Qualifier.LessThanAndEqual:
                    return Variable.RuntimeFloatVal <= QualifierFloatVal;
                case Qualifier.LessThan:
                    return Variable.RuntimeFloatVal < QualifierFloatVal;
                case Qualifier.MoreThan:
                    return Variable.RuntimeFloatVal > QualifierFloatVal;
                case Qualifier.MoreThanAndEqual:
                    return Variable.RuntimeFloatVal >= QualifierFloatVal;
            }
            return false;
        }

        private bool QualifyInteger()
        {
            switch (Qualifier)
            {
                case Qualifier.Equal:
                    return Variable.RuntimeIntVal == QualifierIntVal;
                case Qualifier.NotEqual:
                    return Variable.RuntimeIntVal != QualifierIntVal;
                case Qualifier.LessThanAndEqual:
                    return Variable.RuntimeIntVal <= QualifierIntVal;
                case Qualifier.LessThan:
                    return Variable.RuntimeIntVal < QualifierIntVal;
                case Qualifier.MoreThan:
                    return Variable.RuntimeIntVal > QualifierIntVal;
                case Qualifier.MoreThanAndEqual:
                    return Variable.RuntimeIntVal >= QualifierIntVal;
            }
            return false;
        }
    }

    public enum TrueFalse
    {
        True = 1,
        False = 0
    }

    public enum Qualifier
    {
        Equal,
        LessThanAndEqual,
        LessThan,
        MoreThanAndEqual,
        MoreThan,
        NotEqual
    }
}
