using System;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(ListGameMarkerTemplate))]
    public class GameMarkerTemplateConfigDrawer : PropertyDrawer
    {
        private float m_height;
        
        [Obsolete]
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            var propPresets = property.FindPropertyRelative("presets");
            var propSelectHead = property.FindPropertyRelative("selectHead");
            var propTemplateName = property.FindPropertyRelative("templateName");
            var propHeight = base.GetPropertyHeight(property, label);
            var amountRect = new Rect(position.x + 16, position.y, position.width - 16, propHeight);
            
            m_height = 0;
            
            void Space(float power = 1)
            {
                amountRect.y += propHeight * power;
                m_height += propHeight * power;
            }
            
            for (int i = 0; i < propPresets.arraySize - 1; i++)
            {
                var propChild = propPresets.GetArrayElementAtIndex(i);
                var propFind = propChild.FindPropertyRelative("value");
                if (propFind != null)
                {
                    var propTemplateNameChild = propChild.FindPropertyRelative("templateName");
                    amountRect.width -= 64;
                    EditorGUI.PropertyField(amountRect, propFind, new GUIContent(propTemplateNameChild.stringValue) ,true);
                    amountRect.width += 64;
                    var removeRect = new Rect(amountRect.x + amountRect.width - 64, amountRect.y, 64, amountRect.height);
                    if (GUI.Button(removeRect, "Remove"))
                    {
                        propPresets.DeleteArrayElementAtIndex(i);
                    }
                    
                    var childHeight = EditorGUI.GetPropertyHeight(propFind);
                    amountRect.y += childHeight;
                    m_height += childHeight;
                }
            }

            Space();
            amountRect.width /= 3;
            propSelectHead.intValue = EditorGUI.Popup(amountRect, propSelectHead.intValue, MarkerData.paramEditor);
            amountRect.x += amountRect.width;
            propTemplateName.stringValue = EditorGUI.TextField(amountRect, propTemplateName.stringValue);

            if (propTemplateName.stringValue == string.Empty)
            {
                var style = new GUIStyle();
                style.fontStyle = FontStyle.Italic;
                style.normal.textColor = Color.grey;

                amountRect.x += amountRect.width / 4;
                amountRect.y += 2;
                GUI.Label(amountRect, "TEMPLATE NAME", style);
                amountRect.x -= amountRect.width / 4;
                amountRect.y -= 2;
            }
            else
            {
                amountRect.x += amountRect.width;

                if (GetIndexTemplate(propTemplateName.stringValue, propPresets) == -1 && 
                    GUI.Button(amountRect, "Add Template"))
                {
                    propPresets.InsertArrayElementAtIndex(propPresets.arraySize - 1);
                    var propChild = propPresets.GetArrayElementAtIndex(propPresets.arraySize - 2);
                    var propChildTemplateName = propChild.FindPropertyRelative("templateName");
                    var propHead = propChild.FindPropertyRelative("head");
                    var propParam = propChild.FindPropertyRelative("param");
                    var propLastHead = propChild.FindPropertyRelative("lastHeadObject");
                    var propValue = propChild.FindPropertyRelative("value");
                    
                    propParam.stringValue = MarkerData.paramEditor[propSelectHead.intValue];
                    propHead.stringValue = MarkerData.GetHeadTarget(propParam.stringValue);

                    if (MarkerData.paramObjectsEditor.TryGetValue(propHead.stringValue, out var getValue))
                    {
                        propValue.managedReferenceValue = getValue?.Invoke();
                    }
                    else 
                    {
                        propValue.managedReferenceValue = null;
                    }

                    propLastHead.stringValue = propHead.stringValue;
                        
                    propChildTemplateName.stringValue = propTemplateName.stringValue;
                }
            }

            Space();
            
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public int GetIndexTemplate(string template, SerializedProperty property)
        {
            for (int i = 0; i < property.arraySize - 1; i++)
            {
                var propChild = property.GetArrayElementAtIndex(i);
                var propFind = propChild.FindPropertyRelative("templateName");
                if (template == propFind.stringValue)
                {
                    return i;
                }
            }

            return -1;
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return m_height;
        }
    }
}