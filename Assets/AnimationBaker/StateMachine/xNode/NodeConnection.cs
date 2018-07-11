using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimationBaker.StateMachine.XNode
{
    [Serializable]
    public class NodeConnection
    {
        [SerializeField] public string fieldName;
        [SerializeField] public Node fromNode;
        [SerializeField] public Node toNode;
        public NodePort ToPort { get { return toPort != null ? toPort : toPort = GetPort(); } }
        public NodePort FromPort { get { return fromPort != null ? fromPort : fromPort = GetPort(); } }

        [NonSerialized] private NodePort toPort;
        [NonSerialized] private NodePort fromPort;
        [SerializeField] public List<Vector2> reroutePoints = new List<Vector2>();
        [SerializeField] public List<TransitionRule> rules = new List<TransitionRule>();

        public bool HasRules
        {
            get
            {
                return rules.Count > 0;
            }
        }

        public NodeConnection(NodePort fromPort, NodePort toPort)
        {
            this.fromPort = fromPort;
            fromNode = fromPort.node;

            this.toPort = toPort;
            toNode = toPort.node;

            fieldName = toPort.fieldName;
        }

        /// <summary> Returns the port that this <see cref="NodeConnection"/> points to </summary>
        private NodePort GetPort()
        {
            if (toNode == null || string.IsNullOrEmpty(fieldName)) return null;
            return toNode.GetPort(fieldName);
        }

        public bool CanAddRule()
        {
            return fromNode.graph.variables.Count > 0;
        }

        public TransitionRule AddRule()
        {
            var variable = fromNode.graph.variables[0];
            if (variable != null)
            {
                var rule = new TransitionRule();
                rule.Variable = variable;
                rules.Add(rule);
                return rule;
            }
            return null;
        }

        public void RemoveRuleWithVariable(NodeGraphVariable variable)
        {
            for (int i = rules.Count - 1; i > -1; i++)
            {
                if (rules[i].Variable == variable)
                {
                    rules.RemoveAt(i);
                }
            }
        }

        public void RemoveRule(TransitionRule item)
        {
            rules.Remove(item);
        }
    }

    [System.Serializable]
    public class TransitionRule
    {
        public NodeGraphVariable Variable;
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
        MoreThan
    }
}
