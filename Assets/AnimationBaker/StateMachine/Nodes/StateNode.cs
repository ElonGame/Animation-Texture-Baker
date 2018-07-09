using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using XNode;

namespace AnimationBaker.StateMachine.Nodes
{
    [System.Serializable]
    [NodeTint("#767d80")]
    public class StateNode : BaseNode
    {
        public override bool HasState { get => true; }

        public override NodeType NodeType
        {
            get => NodeType.State;
            set { }
        }

        public void Rename(string newName)
        {
#if UNITY_EDITOR
            name = newName;
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
#endif
        }
    }
}
