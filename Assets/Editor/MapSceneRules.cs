using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Editor
{
	/// <summary>Scene budgets shared by Wavefront and Binary; both use SceneFormatCollector.</summary>
	public static class MapSceneRules
	{
		public const float BytesPerMegabyte = 1048576f;
		public static readonly ValidItemData ComponentRules = new(4096, 24f,
			new ValidItem(nameof(Transform), 1, 20000),
			new ValidItem(nameof(MeshCollider), 0, 10000),
			new ValidItem(nameof(BoxCollider), 0, 10000),
			new ValidItem(nameof(SphereCollider), 0, 1000),
			new ValidItem(nameof(CapsuleCollider), 0, 1000),
			new ValidItem(nameof(Rigidbody), 0, 1000),
			new ValidItem(nameof(MeshRenderer), 0, 10000),
			new ValidItem(nameof(MeshFilter), 0, 10000),
			new ValidItem(nameof(Light), 0, 500),
			new ValidItem(nameof(HDAdditionalLightData), 0, 500),
			new ValidItem(nameof(LODGroup), 0, 10000),
			new ValidItem(nameof(GameMarkerData), 1, 10000),
			new ValidItem(nameof(Minimap), 1, 1));

		public static bool CountExportedComponent(Component component, List<ValidItem> rules)
		{
			if (component == null || rules == null) return false;
			for (int i = 0; i < rules.Count; i++)
			{
				var item = rules[i];
				if (item.type != component.GetType().Name) continue;
				item.current++;
				item.components.Add(component);
				rules[i] = item;
				return true;
			}
			return false;
		}
	}
}
