using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnimationBaker.StateMachine.XNode
{
    /// <summary> Base class for all node graphs </summary>
    [Serializable]
    public abstract class NodeGraph : ScriptableObject
    {

        private bool isDirty = false;
        public bool IsDirty
        {
            set
            {
                isDirty = value;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary> All nodes in the graph. <para/>
        /// See: <see cref="AddNode{T}"/> </summary>
        [SerializeField] public List<Node> nodes = new List<Node>();
        [SerializeField] public List<NodeGraphVariable> variables = new List<NodeGraphVariable>();
        [SerializeField] public bool booted = false;
        [SerializeField] public Vector2 dragPosition;

        /// <summary> Add a node to the graph by type </summary>
        public T AddNode<T>() where T : Node
        {
            return AddNode(typeof(T)) as T;
        }

        /// <summary> Add a node to the graph by type </summary>
        public virtual Node AddNode(Type type)
        {
            Node node = ScriptableObject.CreateInstance(type) as Node;
            nodes.Add(node);
            node.graph = this;
            return node;
        }

        /// <summary> Creates a copy of the original node in the graph </summary>
        public virtual Node CopyNode(Node original)
        {
            Node node = ScriptableObject.Instantiate(original);
            node.ClearConnections();
            nodes.Add(node);
            node.graph = this;
            return node;
        }

        /// <summary> Safely remove a node and all its connections </summary>
        /// <param name="node"> The node to remove </param>
        public void RemoveNode(Node node)
        {
            node.ClearConnections();
            nodes.Remove(node);
            if (Application.isPlaying) Destroy(node);
        }

        /// <summary> Remove all nodes and connections from the graph </summary>
        public void Clear()
        {
            if (Application.isPlaying)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    Destroy(nodes[i]);
                }
            }
            nodes.Clear();
        }

        /// <summary> Create a new deep copy of this graph </summary>
        public AnimationBaker.StateMachine.XNode.NodeGraph Copy()
        {
            // Instantiate a new nodegraph instance
            NodeGraph graph = Instantiate(this);
            // Instantiate all nodes inside the graph
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == null) continue;
                Node node = Instantiate(nodes[i]) as Node;
                node.graph = graph;
                graph.nodes[i] = node;
            }

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                if (graph.nodes[i] == null) continue;
                foreach (NodePort port in graph.nodes[i].Ports)
                {
                    port.Redirect(nodes, graph.nodes);
                }
            }

            return graph;
        }

        public virtual void ImportSerialized(object data)
        {
            var node = data as AnimationBaker.StateMachine.XNode.Node;
            if (node != null && !nodes.Contains(node))
            {
                nodes.Add(node);
            }
        }

        private void OnDestroy()
        {
            Clear();
        }

        public virtual NodeGraphVariable AddVariable()
        {
            var variable = new NodeGraphVariable();
            variables.Add(variable);
            return variable;
        }

        public virtual void RemoveVariable(NodeGraphVariable variable)
        {
            variables.Remove(variable);
            foreach (var node in nodes)
            {
                node.VariableRemoved(variable);
            }
        }

        public virtual void Boot()
        {
            booted = true;
        }
    }
}
