using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
// using AnimationBaker.StateMachine.Transitions;
using AnimationBaker.StateMachine.Variables;
using AnimationBaker.Utils.XNode;

namespace AnimationBaker.StateMachine.Nodes
{

    [System.Serializable]
    public enum NodeType
    {
        Start,
        State,
        End
    }

    // [System.Serializable]
    // public class BaseNodeListTransitionRulesDictionary : SerializableDictionary<BaseNode, TransitionRules> { }

    [System.Serializable]
    public class RulesToggleDictionary : SerializableDictionary<int, bool> { }

    [System.Serializable]
    public abstract class BaseNode : Node
    {
        public abstract NodeType NodeType { get; set; }
        public virtual bool HasState { get => false; }

        public float Duration = 0;
        public RulesToggleDictionary RulesToggles = new RulesToggleDictionary();
        // public BaseNodeListTransitionRulesDictionary Rules = new BaseNodeListTransitionRulesDictionary();

        public override object GetValue(NodePort port)
        {
            return null;
        }

        public void AddOutput()
        {
            var portName = "Output";
            RemoveInstancePort(portName);
            AddInstanceOutput(typeof(BaseNode.Empty), ConnectionType.Override, portName);
            UpdateStaticPorts();
        }

        public void OnEnter()
        {
            StateGraph fmGraph = graph as StateGraph;
        }

        [Serializable]
        public class Empty { }

        public void RemoveVariables(BaseNode node)
        {
            // Rules.Remove(node);
        }

        // private void OnValidate()
        // {
        // }

        //         public void AddRule(BaseNode node)
        //         {
        //             TransitionRules rules = TransitionRules.CreateInstance<TransitionRules>();
        //             rules.name = "TR_" + name + node.name;
        //             rules.fromNode = this;
        //             rules.toNode = node;
        //             var rule = new TransitionRule();
        //             // rules.Rules.Add(rule);
        //             // Rules[node] = rules;
        // #if UNITY_EDITOR
        //             AssetDatabase.AddObjectToAsset(rules, this);
        //             AssetDatabase.SaveAssets();
        // #endif
        //         }

        //         public void RemoveRule(BaseNode node, TransitionRules rules)
        //         {
        //             // Rules.Remove(node);
        // #if UNITY_EDITOR
        //             // UnityEngine.Object.DestroyImmediate(rules, true);
        //             // AssetDatabase.SaveAssets();
        // #endif
        //         }

        //         protected override void OnEnable()
        //         {
        //             base.OnEnable();
        // #if UNITY_EDITOR
        //             var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(this));
        //             foreach (var asset in assets)
        //             {
        //                 var transitionRules = asset as TransitionRules;
        //                 if (transitionRules != null)
        //                 {
        //                     if (transitionRules.fromNode == this)
        //                     {
        //                         Rules[transitionRules.toNode] = transitionRules;
        //                     }
        //                 }
        //             }
        //             foreach (var port in Outputs)
        //             {
        //                 for (int i = 0; i < port.ConnectionCount; i++)
        //                 {
        //                     var connection = port.GetConnection(i);
        //                     var node = connection.node as BaseNode;
        //                     if (!Rules.ContainsKey(node))
        //                     {
        //                         AddRule(node);
        //                     }
        //                 }
        //             }
        // #endif
        //         }
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
