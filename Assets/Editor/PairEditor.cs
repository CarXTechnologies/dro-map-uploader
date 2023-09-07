using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(MapPair))]
    public class PairEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.BeginProperty(position, label, property);
           
            var propValueKey = property.FindPropertyRelative("value");
            var propKey = property.FindPropertyRelative("index");
            var propPopupKey = property.FindPropertyRelative("popupValues");

            var values = new string[propPopupKey.arraySize];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = propPopupKey.GetArrayElementAtIndex(i).stringValue;
            }
            
            propKey.intValue = EditorGUI.Popup(position, propKey.intValue, values);
            propValueKey.stringValue = values[propKey.intValue];
            
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}