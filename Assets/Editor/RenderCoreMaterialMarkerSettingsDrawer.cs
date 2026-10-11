using System;
using System.Collections.Generic;
using System.Linq;
using Plugins.CarX.Modding.Creator.Editor;
using Plugins.CarX.Modding.Creator.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor
{
	/// <summary>
	/// Инспектор маркера Material: переопределения материалов рендерера вариантами rendercore. Маркер на объекте с LODGroup
	/// действует на рендереры всех LOD. Export from material заполняет свойства варианта из исходного материала HDRP.
	/// </summary>
	[CustomPropertyDrawer(typeof(RenderCoreMaterialMarkerSettings))]
	public sealed class RenderCoreMaterialMarkerSettingsDrawer : PropertyDrawer
	{
		private const string HelpText = "Binary only. Each entry replaces a material of this renderer (on a LODGroup object - of all its LODs) " +
			"with a render core variant. Wavefront ignores the marker.";

		private static readonly string[] s_variantNames = ModRenderCoreVariants.All.Select(variant => variant.name).ToArray();

		private static float Line => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

		/// <summary>Новые настройки маркера: по записи на каждый материал рендерера с вариантом по его настройкам HDRP.</summary>
		public static RenderCoreMaterialMarkerSettings CreateSettings(Component marker)
		{
			var settings = new RenderCoreMaterialMarkerSettings();

			if (marker != null)
			{
				AddMissingMaterials(settings, marker.gameObject);
			}

			return settings;
		}

		/// <summary>Материалы рендерера объекта и, если на объекте LODGroup, рендереров всех его LOD.</summary>
		public static IReadOnlyList<Material> GetRendererMaterials(GameObject gameObject)
		{
			var renderers = new List<Renderer>();
			var lodGroup = gameObject.GetComponent<LODGroup>();

			if (lodGroup != null)
			{
				renderers.AddRange(lodGroup.GetLODs().SelectMany(lod => lod.renderers).Where(renderer => renderer != null));
			}

			var ownRenderer = gameObject.GetComponent<MeshRenderer>();

			if (ownRenderer != null)
			{
				renderers.Add(ownRenderer);
			}

			return renderers.SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).Distinct().ToArray();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return Layout(new Rect(0, 0, EditorGUIUtility.currentViewWidth, 0), property, draw: false);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			Layout(position, property, draw: true);
			EditorGUI.EndProperty();
		}

		private static float Layout(Rect position, SerializedProperty property, bool draw)
		{
			var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			float top = position.y;

			Rect Next(float lines = 1)
			{
				var rect = new Rect(line.x, line.y, line.width, line.height + (lines - 1) * Line);
				line.y += lines * Line;
				return rect;
			}

			Rect help = Next(lines: 2);

			if (draw)
			{
				EditorGUI.HelpBox(help, HelpText, MessageType.Info);
			}

			SerializedProperty overrides = property.FindPropertyRelative(nameof(RenderCoreMaterialMarkerSettings.overrides));
			Rect addRect = Next();

			if (draw && GUI.Button(addRect, "Add renderer materials") && property.serializedObject.targetObject is Component component)
			{
				AddMissingMaterials(overrides, component.gameObject);
			}

			for (int i = 0; i < overrides.arraySize; i++)
			{
				SerializedProperty element = overrides.GetArrayElementAtIndex(i);

				if (DrawOverride(element, i, property, Next, draw))
				{
					overrides.DeleteArrayElementAtIndex(i);
					break;
				}
			}

			return line.y - top;
		}

		/// <summary>Одна запись. Возвращает true, если запись удалили.</summary>
		private static bool DrawOverride(SerializedProperty element, int index, SerializedProperty settings, Func<float, Rect> next, bool draw)
		{
			SerializedProperty source = element.FindPropertyRelative(nameof(RenderCoreMaterialOverride.source));
			SerializedProperty variant = element.FindPropertyRelative(nameof(RenderCoreMaterialOverride.variant));
			Rect header = next(1);
			string title = (source.objectReferenceValue != null ? source.objectReferenceValue.name : "No material") + " -> " +
				(string.IsNullOrEmpty(variant.stringValue) ? "not set" : variant.stringValue);

			if (draw)
			{
				element.isExpanded = EditorGUI.Foldout(new Rect(header.x, header.y, header.width - 70, header.height), element.isExpanded, title, toggleOnLabelClick: true);

				if (GUI.Button(new Rect(header.xMax - 66, header.y, 66, header.height), "Remove"))
				{
					return true;
				}
			}

			if (!element.isExpanded)
			{
				return false;
			}

			EditorGUI.indentLevel++;

			try
			{
				Rect sourceRect = next(1);
				Rect variantRect = next(1);
				Rect exportRect = next(1);

				if (draw)
				{
					EditorGUI.PropertyField(sourceRect, source, new GUIContent("Source material", "Renderer material that this entry replaces."));
					int selected = Array.IndexOf(s_variantNames, variant.stringValue);
					int chosen = EditorGUI.Popup(variantRect, "Render core variant", selected, s_variantNames);

					if (chosen != selected && chosen >= 0)
					{
						variant.stringValue = s_variantNames[chosen];
					}

					using (new EditorGUI.DisabledScope(source.objectReferenceValue == null))
					{
						if (GUI.Button(EditorGUI.IndentedRect(exportRect), new GUIContent("Export from material",
							"Fill the variant properties from the source HDRP material. Generated textures are saved to a RenderCore folder next to it.")))
						{
							ScheduleExport(settings, index);
						}
					}
				}

				if (ModRenderCoreVariants.TryGet(variant.stringValue, out ModRenderCoreVariant renderCoreVariant))
				{
					SerializedProperty values = element.FindPropertyRelative(nameof(RenderCoreMaterialOverride.values));

					foreach (ModRenderCoreVariantProperty property in renderCoreVariant.properties)
					{
						DrawValue(values, property, next, draw);
					}
				}
			}
			finally
			{
				EditorGUI.indentLevel--;
			}

			return false;
		}

		private static void DrawValue(SerializedProperty values, ModRenderCoreVariantProperty property, Func<float, Rect> next, bool draw)
		{
			var content = new GUIContent(property.label, property.name + ": " + property.tooltip);
			bool isVector = property.type == ModRenderCorePropertyType.Vector;
			Rect rect = next(isVector ? 5 : 1);

			if (!draw)
			{
				return;
			}

			SerializedProperty value = FindOrAddValue(values, property);
			SerializedProperty vector = value.FindPropertyRelative(nameof(RenderCoreMaterialValue.vector));

			switch (property.type)
			{
				case ModRenderCorePropertyType.Texture:
					EditorGUI.PropertyField(rect, value.FindPropertyRelative(nameof(RenderCoreMaterialValue.texture)), content);
					break;
				case ModRenderCorePropertyType.Color:
					vector.vector4Value = EditorGUI.ColorField(rect, content, vector.vector4Value, showEyedropper: true, showAlpha: true, hdr: false);
					break;
				case ModRenderCorePropertyType.Float:
					vector.vector4Value = new Vector4(EditorGUI.FloatField(rect, content, vector.vector4Value.x), 0, 0, 0);
					break;
				default:
					DrawVector(rect, content, vector, property.vectorLabels);
					break;
			}
		}

		private static void DrawVector(Rect rect, GUIContent content, SerializedProperty vector, IReadOnlyList<string> labels)
		{
			var line = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.LabelField(line, content);
			Vector4 value = vector.vector4Value;
			EditorGUI.indentLevel++;

			for (int axis = 0; axis < 4; axis++)
			{
				line.y += Line;
				string axisLabel = labels != null && axis < labels.Count ? labels[axis] : "XYZW"[axis].ToString();
				value[axis] = EditorGUI.FloatField(line, axisLabel, value[axis]);
			}

			EditorGUI.indentLevel--;
			vector.vector4Value = value;
		}

		private static SerializedProperty FindOrAddValue(SerializedProperty values, ModRenderCoreVariantProperty property)
		{
			for (int i = 0; i < values.arraySize; i++)
			{
				SerializedProperty candidate = values.GetArrayElementAtIndex(i);

				if (candidate.FindPropertyRelative(nameof(RenderCoreMaterialValue.name)).stringValue == property.name)
				{
					return candidate;
				}
			}

			values.InsertArrayElementAtIndex(values.arraySize);
			SerializedProperty added = values.GetArrayElementAtIndex(values.arraySize - 1);
			added.FindPropertyRelative(nameof(RenderCoreMaterialValue.name)).stringValue = property.name;
			added.FindPropertyRelative(nameof(RenderCoreMaterialValue.vector)).vector4Value = property.defaultValue;
			added.FindPropertyRelative(nameof(RenderCoreMaterialValue.texture)).objectReferenceValue = null;
			return added;
		}

		/// <summary>Export from material вне OnGUI: запекание импортирует ассеты, что нельзя делать посреди отрисовки инспектора.</summary>
		private static void ScheduleExport(SerializedProperty settings, int index)
		{
			if (settings.serializedObject.targetObject is not Component component)
			{
				return;
			}

			settings.serializedObject.ApplyModifiedProperties();
			EditorApplication.delayCall += () => ModifySettings(component, "Export render core material", markerSettings =>
			{
				if (index >= markerSettings.overrides.Count)
				{
					return;
				}

				RenderCoreMaterialOverride materialOverride = markerSettings.overrides[index];

				try
				{
					IReadOnlyList<string> manual = RenderCoreMaterialBaker.Bake(materialOverride);
					string message = $"'{materialOverride.source.name}' exported to {materialOverride.variant}.";
					Debug.Log(manual.Count == 0 ? message : message + " Set manually: " + string.Join(", ", manual) + ".", component);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception, component);
					EditorUtility.DisplayDialog("Export from material", exception.Message, "OK");
				}
			});
		}

		private static void ModifySettings(Component component, string undoName, Action<RenderCoreMaterialMarkerSettings> modify)
		{
			if (component == null || component is not IMarkerDataSource { MarkerData: RenderCoreMaterialMarkerSettings settings })
			{
				return;
			}

			Undo.RecordObject(component, undoName);
			modify(settings);
			PrefabUtility.RecordPrefabInstancePropertyModifications(component);
			EditorUtility.SetDirty(component);
		}

		private static void AddMissingMaterials(SerializedProperty overrides, GameObject gameObject)
		{
			var listed = new HashSet<Object>();

			for (int i = 0; i < overrides.arraySize; i++)
			{
				listed.Add(overrides.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(RenderCoreMaterialOverride.source)).objectReferenceValue);
			}

			foreach (Material material in GetRendererMaterials(gameObject).Where(material => !listed.Contains(material)))
			{
				overrides.InsertArrayElementAtIndex(overrides.arraySize);
				SerializedProperty added = overrides.GetArrayElementAtIndex(overrides.arraySize - 1);
				added.FindPropertyRelative(nameof(RenderCoreMaterialOverride.source)).objectReferenceValue = material;
				added.FindPropertyRelative(nameof(RenderCoreMaterialOverride.variant)).stringValue = RenderCoreMaterialBaker.SuggestVariant(material);
				added.FindPropertyRelative(nameof(RenderCoreMaterialOverride.values)).ClearArray();
			}
		}

		private static void AddMissingMaterials(RenderCoreMaterialMarkerSettings settings, GameObject gameObject)
		{
			foreach (Material material in GetRendererMaterials(gameObject))
			{
				if (settings.Find(material) == null)
				{
					settings.overrides.Add(new RenderCoreMaterialOverride { source = material, variant = RenderCoreMaterialBaker.SuggestVariant(material) });
				}
			}
		}
	}
}
