using System.Linq;
using Plugins.CarX.Modding.Creator.Runtime;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(AnimationMarkerSettings))]
    public sealed class AnimationMarkerSettingsDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            6 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            void Field(string name, string title)
            {
                EditorGUI.PropertyField(line, property.FindPropertyRelative(name), new GUIContent(title));
                line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            Field("animator", "Animator");
            Field("samplesPerSecond", "Bake frames per second");
            var animator = property.FindPropertyRelative("animator").objectReferenceValue as Animator;
            if (animator == null && property.serializedObject.targetObject is GameMarkerData marker)
                animator = marker.GetComponent<Animator>();
            var clips = animator?.runtimeAnimatorController?.animationClips.Distinct().ToArray();
            var index = property.FindPropertyRelative("clipIndex");
            if (clips != null && clips.Length > 0)
                index.intValue = EditorGUI.Popup(line, "Initial clip", index.intValue, clips.Select((c, i) => $"{i}: {c.name}").ToArray());
            else
                EditorGUI.LabelField(line, "Initial clip", "Assign an Animator with clips");
            line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            Field("speed", "Playback speed");
            Field("phase", "Initial phase (0..1)");
            Field("loop", "Loop");
            EditorGUI.EndProperty();
        }
    }
}
