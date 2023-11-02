using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InspectorNameAttribute))]
public class InspectorNameEditor : PropertyDrawer
{
    public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
    {
        EditorGUI.PropertyField(position, property, new GUIContent( (attribute as InspectorNameAttribute)?.newName ));
    }
}