#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(IdentifierAttribute))]
public class IdentifierDrawer : PropertyDrawer 
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) 
    {
        EditorGUI.BeginDisabledGroup(true);
        if (string.IsNullOrEmpty(property.stringValue)) 
        {
            property.stringValue = Guid.NewGuid().ToString();
            property.serializedObject.ApplyModifiedProperties();
        }
        EditorGUI.PropertyField(position, property, label, true);
        EditorGUI.EndDisabledGroup();
    }
}
#endif