using System;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(InspectorSettingAttribute))]
    public class InspectorSettingEditor : PropertyDrawer
    {
        private InspectorSettingAttribute inspectAttr;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            inspectAttr = (InspectorSettingAttribute)attribute;

            if (inspectAttr == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            
            var name = inspectAttr.newName == string.Empty ? label : new GUIContent(inspectAttr.newName);

            EditorGUI.BeginDisabledGroup(inspectAttr.isLock);
            if (inspectAttr.isExpand)
            {
                EditorGUI.PropertyField(position, property, name, true);
                return;
            }

            if (inspectAttr.isExpand)
            {
                position.y += base.GetPropertyHeight(property, label);
                var current = new Rect(position.x,
                    position.y - base.GetPropertyHeight(property, label), position.width,
                    base.GetPropertyHeight(property, label));
                position.x += 4;
                if (!EditorGUI.PropertyField(current, property, name))
                {
                    return;
                }
            }

            EditorGUI.indentLevel += 1;
            while (property.NextVisible(property.hasVisibleChildren))
            {
                position.height = EditorGUI.GetPropertyHeight(property);
                EditorGUI.PropertyField(position, property);
                position.y += position.height + 2;
            }
            
            EditorGUI.EndDisabledGroup();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (inspectAttr == null || inspectAttr.isExpand)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            
            return EditorGUI.GetPropertyHeight(property);
        }
    }
}