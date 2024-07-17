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
        private string m_oldTemplate;
        private int m_msPropIndex;
        private string m_msPropParam;
        private string m_msPropHead;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var gameMarkerData = (property.serializedObject.targetObject as GameMarkerData);
            var propHeight = base.GetPropertyHeight(property, label);
            var amountRect = new Rect(position.x + 16, position.y, position.width - 16, propHeight);
            
            void Space(float power = 1)
            {
                amountRect.y += propHeight * power;
                m_height += propHeight * power;
            }
            
            var indent = EditorGUI.indentLevel;
            
            m_height = 0;
            EditorGUI.indentLevel = 0;
            
            EditorGUI.BeginProperty(position, label, property);
            
            var propHead = property.FindPropertyRelative("head");
            var propIndex = property.FindPropertyRelative("index");
            var propTemplateIndex = property.FindPropertyRelative("templateIndex");
            var propTemplateConfig = property.FindPropertyRelative("templateConfig");
            var propTemplateName = property.FindPropertyRelative("templateName");
            var propParam = property.FindPropertyRelative("param");
            var propValue = property.FindPropertyRelative("value");
            var propCustomValue = property.FindPropertyRelative("customValue");
            var propLastHead = property.FindPropertyRelative("lastHeadObject");
            
            var popup = new Rect(position.x, propHeight, position.width, 18);
            
            if (property.serializedObject.isEditingMultipleObjects)
            {
                m_msPropIndex = EditorGUI.Popup(popup, m_msPropIndex, MarkerData.paramEditor);
                m_msPropParam = MarkerData.paramEditor[m_msPropIndex];
                m_msPropHead = MarkerData.GetHeadTarget(m_msPropParam);
                
                var selectPopup = new Rect(position.x, m_height, position.width, 18);
                
                if (GUI.Button(selectPopup, "Select : " + m_msPropParam))
                {
                    propIndex.intValue = m_msPropIndex;
                    propParam.stringValue = m_msPropParam;
                    propHead.stringValue = m_msPropHead;
                }
                Space();
            }
            else
            {
                propIndex.intValue = EditorGUI.Popup(amountRect, propIndex.intValue, MarkerData.paramEditor);
                propParam.stringValue = MarkerData.paramEditor[propIndex.intValue];
                propHead.stringValue = MarkerData.GetHeadTarget(propParam.stringValue);
                popup.y -= 18;
            }
            
            EditorGUI.DrawRect(popup, new Color(0.3f, 0.3f, 0.3f, 1.0f));
            GUI.Label(popup, "...");
            
            bool drawTemplatePopup = false;
            bool drawTemplate = false;
            
            if (property.serializedObject.isEditingMultipleObjects)
            {
                Space();
                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
                return;
            }
            
            string paramPath = MarkerData.paramObjectsEditor.ContainsKey(propParam.stringValue) ?
                propParam.stringValue : propHead.stringValue;
            
            if (MarkerData.paramObjectsEditor.ContainsKey(paramPath))
            {
                Space();
                EditorGUI.ObjectField(amountRect, propTemplateConfig);
                var obj = propTemplateConfig.objectReferenceValue;
                
                if (obj != null && obj is GameMarkerTemplateConfig gameMarkerTemplateConfig)
                {
                    var templates = gameMarkerTemplateConfig.presets.presets.Select(i => i.templateName).ToArray();
                    bool find = false;
                    for (var index = 0; index < templates.Length; index++)
                    {
                        if (templates[index] == propTemplateName.stringValue)
                        {
                            propTemplateIndex.intValue = index;
                            find = true;
                            break;
                        }
                    }

                    if (!find)
                    {
                        propTemplateIndex.intValue = templates.Length - 1;
                        propValue.managedReferenceValue = gameMarkerData.markerData.customValue;
                    }
                    
                    m_oldTemplate = templates[propTemplateIndex.intValue];
                    
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
                    else if (m_oldTemplate != "Custom")
                    {
                        propValue.managedReferenceValue = gameMarkerData.markerData.customValue;
                    }

                    m_oldTemplate = templates[propTemplateIndex.intValue];
                }
            }

            if (propLastHead.stringValue != propParam.stringValue)
            {
                if (MarkerData.paramObjectsEditor.TryGetValue(paramPath, out var getValue))
                {
                    propValue.managedReferenceValue = getValue?.Invoke(propParam.stringValue);
                    propCustomValue.managedReferenceValue = getValue?.Invoke(propParam.stringValue);
                }
                else
                {
                    propValue.managedReferenceValue = null;
                }

                propLastHead.stringValue = propParam.stringValue;
            }

            if (drawTemplate)
            {
                EditorGUI.BeginDisabledGroup(drawTemplatePopup);
                amountRect.x -= 16;
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