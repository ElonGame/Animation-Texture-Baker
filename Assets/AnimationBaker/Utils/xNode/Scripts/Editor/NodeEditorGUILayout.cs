using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace AnimationBaker.Utils.XNodeEditor
{
    /// <summary> xNode-specific version of <see cref="EditorGUILayout"/> </summary>
    public static class NodeEditorGUILayout
    {

        /// <summary> Make a field for a serialized property. Automatically displays relevant node port. </summary>
        public static void PropertyField(SerializedProperty property, bool includeChildren = true, params GUILayoutOption[] options)
        {
            PropertyField(property, (GUIContent) null, includeChildren, options);
        }

        /// <summary> Make a field for a serialized property. Automatically displays relevant node port. </summary>
        public static void PropertyField(SerializedProperty property, GUIContent label, bool includeChildren = true, params GUILayoutOption[] options)
        {
            if (property == null) throw new NullReferenceException();
            AnimationBaker.Utils.XNode.Node node = property.serializedObject.targetObject as AnimationBaker.Utils.XNode.Node;
            AnimationBaker.Utils.XNode.NodePort port = node.GetPort(property.name);
            PropertyField(property, label, port, includeChildren);
        }

        /// <summary> Make a field for a serialized property. Manual node port override. </summary>
        public static void PropertyField(SerializedProperty property, AnimationBaker.Utils.XNode.NodePort port, bool includeChildren = true, params GUILayoutOption[] options)
        {
            PropertyField(property, null, port, includeChildren, options);
        }

        /// <summary> Make a field for a serialized property. Manual node port override. </summary>
        public static void PropertyField(SerializedProperty property, GUIContent label, AnimationBaker.Utils.XNode.NodePort port, bool includeChildren = true, params GUILayoutOption[] options)
        {
            if (property == null) throw new NullReferenceException();

            // If property is not a port, display a regular property field
            if (port == null) EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
            else
            {
                Rect rect = new Rect();

                // If property is an input, display a regular property field and put a port handle on the left side
                if (port.direction == AnimationBaker.Utils.XNode.NodePort.IO.Input)
                {
                    // Get data from [Input] attribute
                    AnimationBaker.Utils.XNode.Node.ShowBackingValue showBacking = AnimationBaker.Utils.XNode.Node.ShowBackingValue.Unconnected;
                    AnimationBaker.Utils.XNode.Node.InputAttribute inputAttribute;
                    if (NodeEditorUtilities.GetAttrib(port.node.GetType(), property.name, out inputAttribute)) showBacking = inputAttribute.backingValue;

                    switch (showBacking)
                    {
                        case AnimationBaker.Utils.XNode.Node.ShowBackingValue.Unconnected:
                            // Display a label if port is connected
                            if (port.IsConnected) EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName));
                            // Display an editable property field if port is not connected
                            else EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
                            break;
                        case AnimationBaker.Utils.XNode.Node.ShowBackingValue.Never:
                            // Display a label
                            EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName));
                            break;
                        case AnimationBaker.Utils.XNode.Node.ShowBackingValue.Always:
                            // Display an editable property field
                            EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
                            break;
                    }

                    rect = GUILayoutUtility.GetLastRect();
                    rect.position = rect.position - new Vector2(16, 0);
                    // If property is an output, display a text label and put a port handle on the right side
                }
                else if (port.direction == AnimationBaker.Utils.XNode.NodePort.IO.Output)
                {
                    // Get data from [Output] attribute
                    AnimationBaker.Utils.XNode.Node.ShowBackingValue showBacking = AnimationBaker.Utils.XNode.Node.ShowBackingValue.Unconnected;
                    AnimationBaker.Utils.XNode.Node.OutputAttribute outputAttribute;
                    if (NodeEditorUtilities.GetAttrib(port.node.GetType(), property.name, out outputAttribute)) showBacking = outputAttribute.backingValue;

                    switch (showBacking)
                    {
                        case AnimationBaker.Utils.XNode.Node.ShowBackingValue.Unconnected:
                            // Display a label if port is connected
                            if (port.IsConnected) EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName), NodeEditorResources.OutputPort, GUILayout.MinWidth(30));
                            // Display an editable property field if port is not connected
                            else EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
                            break;
                        case AnimationBaker.Utils.XNode.Node.ShowBackingValue.Never:
                            // Display a label
                            EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName), NodeEditorResources.OutputPort, GUILayout.MinWidth(30));
                            break;
                        case AnimationBaker.Utils.XNode.Node.ShowBackingValue.Always:
                            // Display an editable property field
                            EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
                            break;
                    }

                    rect = GUILayoutUtility.GetLastRect();
                    rect.position = rect.position + new Vector2(rect.width, 0);
                }

                rect.size = new Vector2(16, 16);

                Color backgroundColor = new Color32(90, 97, 105, 255);
                if (NodeEditorWindow.nodeTint.ContainsKey(port.node.GetType())) backgroundColor *= NodeEditorWindow.nodeTint[port.node.GetType()];
                Color col = NodeEditorWindow.current.graphEditor.GetTypeColor(port.ValueType);
                DrawPortHandle(rect, backgroundColor, col);

                // Register the handle position
                Vector2 portPos = rect.center;
                if (NodeEditor.portPositions.ContainsKey(port)) NodeEditor.portPositions[port] = portPos;
                else NodeEditor.portPositions.Add(port, portPos);
            }
        }

        /// <summary> Make a simple port field. </summary>
        public static void PortField(AnimationBaker.Utils.XNode.NodePort port, params GUILayoutOption[] options)
        {
            PortField(null, port, options);
        }

        /// <summary> Make a simple port field. </summary>
        public static void PortField(GUIContent label, AnimationBaker.Utils.XNode.NodePort port, params GUILayoutOption[] options)
        {
            if (port == null) return;
            if (options == null) options = new GUILayoutOption[] { GUILayout.MinWidth(30) };
            Rect rect = new Rect();
            GUIContent content = label != null ? label : new GUIContent(ObjectNames.NicifyVariableName(port.fieldName));

            // If property is an input, display a regular property field and put a port handle on the left side
            if (port.direction == AnimationBaker.Utils.XNode.NodePort.IO.Input)
            {
                // Display a label
                EditorGUILayout.LabelField(content, options);

                rect = GUILayoutUtility.GetLastRect();
                rect.position = rect.position - new Vector2(16, 0);
                // If property is an output, display a text label and put a port handle on the right side
            }
            else if (port.direction == AnimationBaker.Utils.XNode.NodePort.IO.Output)
            {
                // Display a label
                EditorGUILayout.LabelField(content, NodeEditorResources.OutputPort, options);

                rect = GUILayoutUtility.GetLastRect();
                rect.position = rect.position + new Vector2(rect.width, 0);
            }

            rect.size = new Vector2(16, 16);

            Color backgroundColor = new Color32(90, 97, 105, 255);
            if (NodeEditorWindow.nodeTint.ContainsKey(port.node.GetType())) backgroundColor *= NodeEditorWindow.nodeTint[port.node.GetType()];
            Color col = NodeEditorWindow.current.graphEditor.GetTypeColor(port.ValueType);
            DrawPortHandle(rect, backgroundColor, col);

            // Register the handle position
            Vector2 portPos = rect.center;
            if (NodeEditor.portPositions.ContainsKey(port)) NodeEditor.portPositions[port] = portPos;
            else NodeEditor.portPositions.Add(port, portPos);
        }

        /// <summary> Draws an input and an output port on the same line </summary>
        public static void PortPair(AnimationBaker.Utils.XNode.NodePort input, AnimationBaker.Utils.XNode.NodePort output)
        {
            GUILayout.BeginHorizontal();
            NodeEditorGUILayout.PortField(input, GUILayout.MinWidth(0));
            NodeEditorGUILayout.PortField(output, GUILayout.MinWidth(0));
            GUILayout.EndHorizontal();
        }

        public static void DrawPortHandle(Rect rect, Color backgroundColor, Color typeColor)
        {
            Color col = GUI.color;
            GUI.color = backgroundColor;
            GUI.DrawTexture(rect, NodeEditorResources.dotOuter);
            GUI.color = typeColor;
            GUI.DrawTexture(rect, NodeEditorResources.dot);
            GUI.color = col;
        }

        /// <summary> Draw an editable list of instance ports. Port names are named as "[fieldName] [index]" </summary>
        /// <param name="fieldName">Supply a list for editable values</param>
        /// <param name="type">Value type of added instance ports</param>
        /// <param name="serializedObject">The serializedObject of the node</param>
        /// <param name="connectionType">Connection type of added instance ports</param>
        public static void InstancePortList(string fieldName, Type type, SerializedObject serializedObject, AnimationBaker.Utils.XNode.Node.ConnectionType connectionType = AnimationBaker.Utils.XNode.Node.ConnectionType.Multiple)
        {
            AnimationBaker.Utils.XNode.Node node = serializedObject.targetObject as AnimationBaker.Utils.XNode.Node;
            SerializedProperty arrayData = serializedObject.FindProperty(fieldName);
            bool hasArrayData = arrayData != null && arrayData.isArray;
            int arraySize = hasArrayData ? arrayData.arraySize : 0;

            List<AnimationBaker.Utils.XNode.NodePort> instancePorts = node.InstancePorts.Where(x => x.fieldName.StartsWith(fieldName)).OrderBy(x => x.fieldName).ToList();

            for (int i = 0; i < instancePorts.Count(); i++)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("-", GUILayout.Width(30)))
                {
                    // Clear the removed ports connections
                    instancePorts[i].ClearConnections();
                    // Move following connections one step up to replace the missing connection
                    for (int k = i + 1; k < instancePorts.Count(); k++)
                    {
                        for (int j = 0; j < instancePorts[k].ConnectionCount; j++)
                        {
                            AnimationBaker.Utils.XNode.NodePort other = instancePorts[k].GetConnection(j);
                            instancePorts[k].Disconnect(other);
                            instancePorts[k - 1].Connect(other);
                        }
                    }
                    // Remove the last instance port, to avoid messing up the indexing
                    node.RemoveInstancePort(instancePorts[instancePorts.Count() - 1].fieldName);
                    serializedObject.Update();
                    EditorUtility.SetDirty(node);
                    if (hasArrayData)
                    {
                        arrayData.DeleteArrayElementAtIndex(i);
                        arraySize--;
                    }
                    i--;
                }
                else
                {
                    if (hasArrayData)
                    {
                        if (i < arraySize)
                        {
                            SerializedProperty itemData = arrayData.GetArrayElementAtIndex(i);
                            if (itemData != null) EditorGUILayout.PropertyField(itemData, new GUIContent(ObjectNames.NicifyVariableName(fieldName) + " " + i));
                            else EditorGUILayout.LabelField("[Missing array data]");
                        }
                        else EditorGUILayout.LabelField("[Out of bounds]");

                    }
                    else
                    {
                        EditorGUILayout.LabelField(instancePorts[i].fieldName);
                    }
                    NodeEditorGUILayout.PortField(new GUIContent(), node.GetPort(instancePorts[i].fieldName), GUILayout.Width(-4));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {

                string newName = fieldName + " 0";
                int i = 0;
                while (node.HasPort(newName)) newName = fieldName + " " + (++i);

                instancePorts.Add(node.AddInstanceOutput(type, connectionType, newName));
                serializedObject.Update();
                EditorUtility.SetDirty(node);
                if (hasArrayData) arrayData.InsertArrayElementAtIndex(arraySize);
            }
            GUILayout.EndHorizontal();
        }
    }
}
