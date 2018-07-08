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
    public class StateNode : BaseNode
    {
        [Input(ShowBackingValue.Never, ConnectionType.Multiple)] public Empty enter;
        public override bool CanAddOutput { get => true; }
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
