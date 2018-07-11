using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace AnimationBaker.StateMachine.XNodeEditor
{
    /// <summary> Base class to derive custom Node Graph editors from. Use this to override how graphs are drawn in the editor. </summary>
    [CustomNodeGraphEditor(typeof(AnimationBaker.StateMachine.XNode.NodeGraph))]
    public class NodeGraphEditor : AnimationBaker.StateMachine.XNodeEditor.Internal.NodeEditorBase<NodeGraphEditor, NodeGraphEditor.CustomNodeGraphEditorAttribute, AnimationBaker.StateMachine.XNode.NodeGraph>
    {
        /// <summary> The position of the window in screen space. </summary>
        public Rect position;
        /// <summary> Are we currently renaming a node? </summary>
        protected bool isRenaming;

        public virtual void OnGUI() { }

        public virtual Texture2D GetGridTexture()
        {
            return NodeEditorPreferences.GetSettings().gridTexture;
        }

        public virtual Texture2D GetSecondaryGridTexture()
        {
            return NodeEditorPreferences.GetSettings().crossTexture;
        }

        /// <summary> Return default settings for this graph type. This is the settings the user will load if no previous settings have been saved. </summary>
        public virtual NodeEditorPreferences.Settings GetDefaultPreferences()
        {
            return new NodeEditorPreferences.Settings();
        }

        /// <summary> Returns context node menu path. Null or empty strings for hidden nodes. </summary>
        public virtual string GetNodeMenuName(Type type)
        {
            //Check if type has the CreateNodeMenuAttribute
            AnimationBaker.StateMachine.XNode.Node.CreateNodeMenuAttribute attrib;
            if (NodeEditorUtilities.GetAttrib(type, out attrib)) // Return custom path
                return attrib.menuName;
            else // Return generated path
                return ObjectNames.NicifyVariableName(type.ToString().Replace('.', '/'));
        }

        public virtual Color GetTypeColor(Type type)
        {
            return NodeEditorPreferences.GetTypeColor(type);
        }

        /// <summary> Creates a copy of the original node in the graph </summary>
        public AnimationBaker.StateMachine.XNode.Node CopyNode(AnimationBaker.StateMachine.XNode.Node original)
        {
            AnimationBaker.StateMachine.XNode.Node node = target.CopyNode(original);
            node.name = original.name;
            AssetDatabase.AddObjectToAsset(node, target);
            if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
            return node;
        }

        /// <summary> Safely remove a node and all its connections. </summary>
        public void RemoveNode(AnimationBaker.StateMachine.XNode.Node node)
        {
            UnityEngine.Object.DestroyImmediate(node, true);
            target.RemoveNode(node);
            if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
        }

        [AttributeUsage(AttributeTargets.Class)]
        public class CustomNodeGraphEditorAttribute : Attribute,
        AnimationBaker.StateMachine.XNodeEditor.Internal.NodeEditorBase<NodeGraphEditor, NodeGraphEditor.CustomNodeGraphEditorAttribute, AnimationBaker.StateMachine.XNode.NodeGraph>.INodeEditorAttrib
        {
            private Type inspectedType;
            public string editorPrefsKey;
            /// <summary> Tells a NodeGraphEditor which Graph type it is an editor for </summary>
            /// <param name="inspectedType">Type that this editor can edit</param>
            /// <param name="uniquePreferencesID">Define unique key for unique layout settings instance</param>
            public CustomNodeGraphEditorAttribute(Type inspectedType, string editorPrefsKey = "xNode.Settings")
            {
                this.inspectedType = inspectedType;
                this.editorPrefsKey = editorPrefsKey;
            }

            public Type GetInspectedType()
            {
                return inspectedType;
            }
        }
    }
}
