using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		private const string LayerBlendText = "layer blend (RGB mask, A tint)";

		private const string LayerBlendTooltip = "The variant blends its layers by mesh vertex colors: RGB is the layer mask, A tints the albedo. " +
			"A mesh without vertex colors is exported white and shows only the fourth layer.";

		private const string UnusedValuesTooltip = "Stored values of properties this variant does not have (left from another variant or " +
			"removed from render core). They are not exported.";

		// Отступ HelpBox от ширины инспектора: поля, отступ записи и иконка.
		private const float HelpBoxMargin = 72;

		private const int MinHelpBoxLines = 2;
		private const float SideButtonWidth = 66;

		private static readonly string[] s_variantNames = ModRenderCoreVariants.Offered.Select(variant => variant.name).ToArray();
		private static readonly GUIContent s_channelsLabel = new GUIContent("Channels");
		private static readonly GUIContent s_vertexColorsLabel = new GUIContent("Vertex colors", LayerBlendTooltip);
		private static readonly GUIContent s_layerBlendContent = new GUIContent(LayerBlendText, LayerBlendTooltip);
		private static readonly GUIContent s_unusedValuesLabel = new GUIContent("Unused values", UnusedValuesTooltip);
		private static readonly GUIContent s_removeUnusedContent = new GUIContent("Remove", "Remove the unused values of this entry.");

		// Варианты таблицы статичны, поэтому кэш по ссылке на свойство конечен.
		private static readonly Dictionary<ModRenderCoreVariantProperty, GUIContent> s_channelLayouts = new();

		// Список popup с лишним пунктом для сохранённого варианта вне Offered: чтобы значение не стёрлось и было видно.
		private static readonly Dictionary<string, string[]> s_optionsWithStored = new();

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
			SerializedProperty revision = element.FindPropertyRelative(nameof(RenderCoreMaterialOverride.revision));
			string variantName = variant.stringValue;
			bool available = ModRenderCoreVariants.TryGet(variantName, out ModRenderCoreVariant renderCoreVariant);
			Rect header = next(1);

			if (draw)
			{
				string title = (source.objectReferenceValue != null ? source.objectReferenceValue.name : "No material") + " -> " +
					(string.IsNullOrEmpty(variantName) ? "not set" : variantName + GetStatusSuffix(variantName, available));

				element.isExpanded = EditorGUI.Foldout(new Rect(header.x, header.y, header.width - SideButtonWidth - 4, header.height), element.isExpanded, title,
					toggleOnLabelClick: true);

				if (GUI.Button(new Rect(header.xMax - SideButtonWidth, header.y, SideButtonWidth, header.height), "Remove"))
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

				// Новый вариант из popup рисуется сразу, как и раньше; высота догонит его в следующем кадре.
				if (draw)
				{
					EditorGUI.PropertyField(sourceRect, source, new GUIContent("Source material", "Renderer material that this entry replaces."));

					if (DrawVariantPopup(variantRect, variant))
					{
						variantName = variant.stringValue;
						available = ModRenderCoreVariants.TryGet(variantName, out renderCoreVariant);
						// Автор выбрал вариант сейчас: значения он запечёт или задаст для текущей раскладки, то есть для текущей ревизии.
						revision.intValue = GetRevision(variantName);
					}
				}

				// Вариант вне Offered: значение не стираем, а объясняем, почему его нельзя использовать.
				if (!available && !string.IsNullOrEmpty(variantName))
				{
					string message = GetUnusableMessage(variantName);
					Rect helpRect = next(GetHelpBoxLines(message));

					if (draw)
					{
						EditorGUI.HelpBox(EditorGUI.IndentedRect(helpRect), message, MessageType.Error);
					}
				}

				// Значения запечены для старой ревизии варианта: экспорт запишет её, и игра возьмёт запасной материал (0 считается ревизией 1).
				if (available && Math.Max(1, revision.intValue) < renderCoreVariant.revision)
				{
					string message = $"Exported from material for revision {Math.Max(1, revision.intValue)} of {variantName}, the current revision is " +
						$"{renderCoreVariant.revision}. The game uses the source material until you re-run Export from material.";
					Rect staleRect = next(GetHelpBoxLines(message));

					if (draw)
					{
						EditorGUI.HelpBox(EditorGUI.IndentedRect(staleRect), message, MessageType.Warning);
					}
				}

				if (available && renderCoreVariant.vertexColors == ModRenderCoreVertexColors.LayerBlend)
				{
					Rect vertexColorsRect = next(1);

					if (draw)
					{
						EditorGUI.LabelField(vertexColorsRect, s_vertexColorsLabel, s_layerBlendContent, EditorStyles.miniLabel);
					}
				}

				Rect exportRect = next(1);

				if (draw)
				{
					using (new EditorGUI.DisabledScope(source.objectReferenceValue == null || !available))
					{
						if (GUI.Button(EditorGUI.IndentedRect(exportRect), new GUIContent("Export from material",
							"Fill the variant properties from the source HDRP material. Generated textures are saved to a RenderCore folder next to it.")))
						{
							ScheduleExport(settings, index);
						}
					}
				}

				if (available)
				{
					SerializedProperty values = element.FindPropertyRelative(nameof(RenderCoreMaterialOverride.values));
					DrawUnusedValues(values, renderCoreVariant, next, draw);

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

		/// <summary>
		/// Popup вариантов из Offered; сохранённый вариант вне списка показывается отдельным пунктом, а не пустым полем.
		/// Возвращает true, если выбрали другой вариант.
		/// </summary>
		private static bool DrawVariantPopup(Rect rect, SerializedProperty variant)
		{
			string stored = variant.stringValue;
			string[] options = s_variantNames;
			int selected = Array.IndexOf(s_variantNames, stored);

			if (selected < 0 && !string.IsNullOrEmpty(stored))
			{
				options = GetOptionsWithStored(stored);
				selected = options.Length - 1;
			}

			int chosen = EditorGUI.Popup(rect, "Render core variant", selected, options);

			// Пункт сохранённого варианта не выбирается заново: он лишь показывает текущее значение.
			if (chosen == selected || chosen < 0 || chosen >= s_variantNames.Length)
			{
				return false;
			}

			variant.stringValue = s_variantNames[chosen];
			return true;
		}

		private static string[] GetOptionsWithStored(string stored)
		{
			if (!s_optionsWithStored.TryGetValue(stored, out string[] options))
			{
				string status = ModRenderCoreVariants.TryFind(stored, out _) ? "unavailable" : "unknown";
				options = s_variantNames.Append($"<{stored}: {status}>").ToArray();
				s_optionsWithStored.Add(stored, options);
			}

			return options;
		}

		private static string GetStatusSuffix(string variantName, bool available)
		{
			if (available)
			{
				return string.Empty;
			}

			return ModRenderCoreVariants.TryFind(variantName, out _) ? " (unavailable)" : " (unknown)";
		}

		private static string GetUnusableMessage(string variantName)
		{
			if (ModRenderCoreVariants.TryFind(variantName, out ModRenderCoreVariant variant))
			{
				return $"{variantName} cannot be used in mods: {variant.unavailableReason}. Choose another variant; until then the source material is exported.";
			}

			return $"{variantName} is unknown in the render core mirror of this SDK. Choose another variant; until then the source material is exported.";
		}

		/// <summary>Строки HelpBox по ширине инспектора: одна и та же ширина при расчёте высоты и отрисовке, иначе высоты разойдутся.</summary>
		private static float GetHelpBoxLines(string message)
		{
			float width = Mathf.Max(1, EditorGUIUtility.currentViewWidth - HelpBoxMargin - EditorGUI.indentLevel * 15);
			float height = EditorStyles.helpBox.CalcHeight(new GUIContent(message), width);
			return Mathf.Max(MinHelpBoxLines, Mathf.Ceil(height / Line));
		}

		/// <summary>Значения свойств, которых у варианта нет: остались от другого варианта или удалены из rendercore; не экспортируются.</summary>
		private static void DrawUnusedValues(SerializedProperty values, ModRenderCoreVariant variant, Func<float, Rect> next, bool draw)
		{
			List<string> unused = null;

			for (int i = 0; i < values.arraySize; i++)
			{
				string name = values.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(RenderCoreMaterialValue.name)).stringValue;

				if (!variant.TryGetProperty(name, out _))
				{
					unused ??= new List<string>();
					unused.Add(string.IsNullOrEmpty(name) ? "<unnamed>" : name);
				}
			}

			if (unused == null)
			{
				return;
			}

			Rect rect = next(1);

			if (!draw)
			{
				return;
			}

			EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width - SideButtonWidth - 4, rect.height), s_unusedValuesLabel,
				new GUIContent(string.Join(", ", unused), UnusedValuesTooltip), EditorStyles.miniLabel);

			if (!GUI.Button(new Rect(rect.xMax - SideButtonWidth, rect.y, SideButtonWidth, rect.height), s_removeUnusedContent))
			{
				return;
			}

			for (int i = values.arraySize - 1; i >= 0; i--)
			{
				string name = values.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(RenderCoreMaterialValue.name)).stringValue;

				if (!variant.TryGetProperty(name, out _))
				{
					values.DeleteArrayElementAtIndex(i);
				}
			}
		}

		private static void DrawValue(SerializedProperty values, ModRenderCoreVariantProperty property, Func<float, Rect> next, bool draw)
		{
			bool isVector = property.type == ModRenderCorePropertyType.Vector;
			Rect rect = next(isVector ? 5 : 1);
			Rect channelsRect = property.channels != null ? next(1) : default;

			if (!draw)
			{
				return;
			}

			GUIContent channels = property.channels != null ? GetChannelLayout(property) : null;

			// У packed map основная подсказка — раскладка каналов зеркала; tooltip rendercore вторичен: он бывает неточным.
			var content = new GUIContent(property.label, channels != null
				? property.name + ": " + channels.text + "\nRender core: " + property.tooltip
				: property.name + ": " + property.tooltip);

			if (channels != null)
			{
				EditorGUI.indentLevel++;
				EditorGUI.LabelField(channelsRect, s_channelsLabel, channels, EditorStyles.miniLabel);
				EditorGUI.indentLevel--;
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

		/// <summary>Раскладка packed map по каналам зеркала, например «R Roughness · G — · B Alpha · A —».</summary>
		private static GUIContent GetChannelLayout(ModRenderCoreVariantProperty property)
		{
			if (s_channelLayouts.TryGetValue(property, out GUIContent layout))
			{
				return layout;
			}

			var builder = new StringBuilder();

			for (int i = 0; i < property.channels.Count && i < 4; i++)
			{
				if (i > 0)
				{
					builder.Append(" · ");
				}

				builder.Append("RGBA"[i]).Append(' ').Append(GetChannelName(property.channels[i]));
			}

			layout = new GUIContent(builder.ToString(), "Packed map channels from the render core mirror. Render core: " + property.tooltip);
			s_channelLayouts.Add(property, layout);
			return layout;
		}

		private static string GetChannelName(ModRenderCoreChannel channel)
		{
			switch (channel)
			{
				case ModRenderCoreChannel.Zero:
					return "—";
				case ModRenderCoreChannel.AmbientOcclusion:
					return "AO";
				case ModRenderCoreChannel.AmbientOcclusionAlpha:
					return "AO × Alpha";
				case ModRenderCoreChannel.NormalX:
					return "Normal X";
				case ModRenderCoreChannel.NormalY:
					return "Normal Y";
				case ModRenderCoreChannel.ColorMask:
					return "Color mask";
				default:
					// Roughness, Metalness, Alpha и будущие значения — по имени enum.
					return channel.ToString();
			}
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
				string suggested = RenderCoreMaterialBaker.SuggestVariant(material);

				// InsertArrayElementAtIndex копирует предыдущую запись: все поля новой задаются явно, включая ревизию.
				overrides.InsertArrayElementAtIndex(overrides.arraySize);
				SerializedProperty added = overrides.GetArrayElementAtIndex(overrides.arraySize - 1);
				added.FindPropertyRelative(nameof(RenderCoreMaterialOverride.source)).objectReferenceValue = material;
				added.FindPropertyRelative(nameof(RenderCoreMaterialOverride.variant)).stringValue = suggested;
				added.FindPropertyRelative(nameof(RenderCoreMaterialOverride.revision)).intValue = GetRevision(suggested);
				added.FindPropertyRelative(nameof(RenderCoreMaterialOverride.values)).ClearArray();
			}
		}

		private static void AddMissingMaterials(RenderCoreMaterialMarkerSettings settings, GameObject gameObject)
		{
			foreach (Material material in GetRendererMaterials(gameObject))
			{
				if (settings.Find(material) == null)
				{
					string suggested = RenderCoreMaterialBaker.SuggestVariant(material);
					settings.overrides.Add(new RenderCoreMaterialOverride { source = material, variant = suggested, revision = GetRevision(suggested) });
				}
			}
		}

		/// <summary>
		/// Ревизия записи, в которой вариант выбран сейчас: значения ещё не запечены, автор запечёт или задаст их для текущей раскладки.
		/// Для варианта вне таблицы — 0 (как у записи без ревизии): экспорт его всё равно пропустит.
		/// </summary>
		private static int GetRevision(string variantName)
		{
			return ModRenderCoreVariants.TryGet(variantName, out ModRenderCoreVariant variant) ? variant.revision : 0;
		}
	}
}
