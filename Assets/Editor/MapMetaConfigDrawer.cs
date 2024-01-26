using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(MapMetaConfigValue))]
    public class MetaConfigDrawer : PropertyDrawer
    {
        private float m_height;
        private MapMetaConfig m_target;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_target = property.serializedObject.targetObject as MapMetaConfig;
            if (m_target == null)
            {
                base.OnGUI(position, property, label);
                return;
            }
            
            EditorGUI.BeginProperty(position, label, property);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            var rectView = new Rect(position.position, new Vector2(position.width / 2, 18));
            var rectName = new Rect(position.position, new Vector2(position.width, 20));
            var rectLabelDesc = new Rect(position.position, new Vector2(position.width / 2, 24));
            var rectInfo = new Rect(position.position, new Vector2(position.width, 40));
            
            GUI.Box(rectName, "Workshop Name(only letters, 128 char)");
            rectName.y += 22;
            m_target.mapMetaConfigValue.mapName = EditorGUI.TextField(rectName, m_target.mapMetaConfigValue.mapName);
            
            rectLabelDesc.y += rectLabelDesc.height * 2;
            GUI.Box(rectLabelDesc, "Workshop Description");
            rectLabelDesc.x += rectLabelDesc.width;
            GUI.Box(rectLabelDesc, " Preview, Icon (16:9)");
            rectView.y += rectLabelDesc.height * 3 + 4;
            
            TextArea(ref rectView, ref m_target.mapMetaConfigValue.mapDescription, new Vector2(position.width / 2, 128), Vector2.right);
            TextureProp(ref rectView, ref m_target.mapMetaConfigValue.largeIcon, new Vector2(128, 128), Vector2.up);
            TextureProp(ref rectView, ref m_target.mapMetaConfigValue.icon, new Vector2(96, 96), Vector2.up);

            var build = MapManagerConfig.GetBuildOrEmpty(m_target);
            m_height = rectView.height + 64;
            
            if (!build.lastMeta.Equals(m_target.mapMetaConfigValue))
            {
                rectInfo.y += m_height;
                EditorGUI.HelpBox(rectInfo, "Meta is changed. Please rebuild meta in \"Tool/MapBuilder\"", MessageType.Warning);
                m_height += 54;
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private void TextureProp(ref Rect position, ref Texture2D property, Vector2 size, Vector2 space)
        {
            if (property == null)
            {
                property = Texture2D.grayTexture;
            }

            var rectTexture = new Rect(position.position, new Vector2(size.x, size.y * ((float)9 / 16)));
            property = (Texture2D)EditorGUI.ObjectField(rectTexture, property, typeof(Texture2D), false);
            position.position += rectTexture.size * space;
            position.size += rectTexture.size * space;
        }
        
        private void TextArea(ref Rect position, ref string property, Vector2 size, Vector2 space)
        {
            var rectTexture = new Rect(position.position, size);
            property = EditorGUI.TextArea(rectTexture, property);
            position.position += rectTexture.size * space;
            position.size += rectTexture.size * space;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return m_target != null ? m_height : EditorGUI.GetPropertyHeight(property, label);
        }
    }
}
