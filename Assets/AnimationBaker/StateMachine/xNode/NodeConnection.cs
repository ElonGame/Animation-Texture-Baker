using System;
using System.Collections.Generic;
using UnityEngine;
using AnimationBaker.StateMachine.Nodes;

namespace AnimationBaker.StateMachine.XNode
{
    [Serializable]
    public class NodeConnection
    {
        [SerializeField] public string name;
        [SerializeField] public Node fromNode;
        [SerializeField] public Node toNode;
        public NodePort ToPort { get { return toPort != null ? toPort : toPort = GetPort(); } }
        public NodePort FromPort { get { return fromPort != null ? fromPort : fromPort = GetPort(); } }
        public bool Cleared { get; set; }

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

        public void Setup(NodePort fromPort, NodePort toPort)
        {
            this.fromPort = fromPort;
            fromNode = fromPort.node;

            this.toPort = toPort;
            toNode = toPort.node;

            name = toPort.fieldName;
        }

        /// <summary> Returns the port that this <see cref="NodeConnection"/> points to </summary>
        private NodePort GetPort()
        {
            if (toNode == null || string.IsNullOrEmpty(name)) return null;
            return toNode.GetPort(name);
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
                var rule = ScriptableObject.CreateInstance<TransitionRule>();
                rule.graph = (StateGraph) fromNode.graph;
                rule.Variable = variable;
                // rule.VariableName = variable.name;
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
                    var rule = rules[i];
                    rules.RemoveAt(i);
                    UnityEngine.Object.DestroyImmediate(rule, true);
                }
            }
        }

        public void RemoveRule(TransitionRule rule)
        {
            rules.Remove(rule);
            UnityEngine.Object.DestroyImmediate(rule, true);
        }
    }
}
