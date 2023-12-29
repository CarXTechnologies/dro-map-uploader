using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Editor
{
    [CustomPropertyDrawer(typeof(MarkerData))]
    public class MarkerDataDrawer : PropertyDrawer
    {
        private float m_height;
        
        [Obsolete("Obsolete")]
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            var propHeight = base.GetPropertyHeight(property, label);
            var amountRect = new Rect(position.x + 16, position.y, position.width - 16, propHeight);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            void Space(float power = 1)
            {
                amountRect.y += propHeight * power;
                m_height += propHeight * power;
            }
            m_height = 0;
            
            var propHead = property.FindPropertyRelative("head");
            var propIndex = property.FindPropertyRelative("index");
            var propTemplateIndex = property.FindPropertyRelative("templateIndex");
            var propTemplateConfig = property.FindPropertyRelative("templateConfig");
            var propTemplateName = property.FindPropertyRelative("templateName");
            var propParam = property.FindPropertyRelative("param");
            var propValue = property.FindPropertyRelative("value");
            var propLastHead = property.FindPropertyRelative("lastHeadObject");

            propIndex.intValue = EditorGUI.Popup(amountRect, propIndex.intValue, MarkerData.paramEditor);
            propParam.stringValue = MarkerData.paramEditor[propIndex.intValue];
            propHead.stringValue = MarkerData.GetHeadTarget(propParam.stringValue);
            bool drawTemplatePopup = false;
            bool drawTemplate = false;
            if (MarkerData.paramObjectsEditor.ContainsKey(propHead.stringValue))
            {
                Space();
                EditorGUI.ObjectField(amountRect, propTemplateConfig);
                var obj = propTemplateConfig.objectReferenceValue;
                
                if (obj != null && obj is GameMarkerTemplateConfig gameMarkerTemplateConfig)
                {
                    var templates = gameMarkerTemplateConfig.presets.presets.Select(i => i.templateName).ToArray();
                    templates[templates.Length - 1] = "Custom";
                    Space();
                    propTemplateIndex.intValue = EditorGUI.Popup(amountRect, propTemplateIndex.intValue, templates);
                    propTemplateName.stringValue = templates[propTemplateIndex.intValue];
                    drawTemplate = true;
                    if (templates[propTemplateIndex.intValue] != "Custom")
                    {
                        propValue.managedReferenceValue = gameMarkerTemplateConfig.presets.presets[propTemplateIndex.intValue].value;
                        drawTemplatePopup = true;
                    }
                }
            }

            if (propLastHead.stringValue != propHead.stringValue)
            {
                if (MarkerData.paramObjectsEditor.TryGetValue(propHead.stringValue, out var getValue))
                {
                    propValue.managedReferenceValue = getValue?.Invoke();
                }
                else
                {
                    propValue.managedReferenceValue = null;
                }

                propLastHead.stringValue = propHead.stringValue;
            }

            if (drawTemplate)
            {
                EditorGUI.BeginDisabledGroup(drawTemplatePopup);
                EditorGUI.PropertyField(amountRect, propValue, GUIContent.none, true);
                EditorGUI.EndDisabledGroup();

                amountRect.y += EditorGUI.GetPropertyHeight(propValue, true); 
                m_height += EditorGUI.GetPropertyHeight(propValue, true); 
            }
            else
            {
                Space();
            }
            
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return m_height;
        }
    }
}