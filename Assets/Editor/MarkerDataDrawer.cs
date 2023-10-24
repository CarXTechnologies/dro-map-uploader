using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(MarkerData))]
    public class MarkerDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var propHead = property.FindPropertyRelative("head");
            var propParam = property.FindPropertyRelative("param");
            var propIndex = property.FindPropertyRelative("index");
            var amountRect = new Rect(position.x, position.y, position.width - 16, position.height);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            amountRect.x += 16;
        
            propIndex.intValue = EditorGUI.Popup(amountRect, propIndex.intValue, MarkerData.paramEditor);
            propParam.stringValue = MarkerData.paramEditor[propIndex.intValue];

            var group = propParam.stringValue.IndexOf('/');
            propHead.stringValue = group != -1 ? propParam.stringValue.Substring(0, group) : propParam.stringValue;

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}