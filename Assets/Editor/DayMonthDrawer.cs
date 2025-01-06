using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomPropertyDrawer(typeof(DayMonth))]
	public class DayMonthDrawer : PropertyDrawer
	{
		private float m_height;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			var propHeight = base.GetPropertyHeight(property, label);
			var propMount = property.FindPropertyRelative("mount");
			var propDay = property.FindPropertyRelative("day");

			Vector2 size = GUI.skin.label.CalcSize(label);

			var labelRect = new Rect(position.x + 15, position.y, size.x, propHeight);
			var monthRect = new Rect(position.x + 23 + size.x, position.y, 96, propHeight);
			var dayRect = new Rect(position.x + 123 + size.x, position.y, 40, propHeight);
			m_height = 0;
			EditorGUI.LabelField(labelRect, label);
			propMount.enumValueIndex = (int)(Month)EditorGUI.EnumPopup(monthRect, (Month)propMount.enumValueIndex);

			int countValues = DayMonth.maxDays[(Month)propMount.enumValueIndex];
			string[] intValues = new string[countValues];
			for (int i = 0; i < countValues; i++)
			{
				intValues[i] = i.ToString();
			}

			if (propDay.intValue >= countValues)
			{
				propDay.intValue = 0;
			}

			propDay.intValue = (int)(Month)EditorGUI.Popup(dayRect, propDay.intValue, intValues);
			m_height += 18;
			EditorGUI.indentLevel = indent;
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return m_height;
		}
	}
}