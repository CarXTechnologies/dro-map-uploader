using System.Collections.Generic;
using NUnit.Framework;
using Plugins.CarX.Modding.Creator.Runtime;
using UnityEngine;

namespace MapUploader.Tests
{
	public class RoadMarkerValuesTests
	{
		private readonly List<Object> m_created = new();

		[TearDown]
		public void DestroyCreatedObjects()
		{
			foreach (Object created in m_created)
			{
				if (created != null)
				{
					Object.DestroyImmediate(created);
				}
			}

			m_created.Clear();
		}

		[Test]
		public void RoadWithoutTemplateConfigExportsNoValues()
		{
			GameMarkerData marker = CreateRoad(Values(0.5f));

			Assert.IsNull(marker.MarkerData);
		}

		[Test]
		public void RoadWithTemplateExportsTheTemplateValuesWithoutBeingShown()
		{
			GameMarkerData marker = CreateRoad(Values(0.5f));
			marker.markerData.templateConfig = CreateConfig("Wet", Values(0.7f));
			marker.markerData.templateName = "Wet";

			Assert.AreEqual(Values(0.7f), marker.MarkerData);
		}

		[Test]
		public void RoadWithCustomExportsItsOwnValues()
		{
			GameMarkerData marker = CreateRoad(Values(0.5f));
			marker.markerData.templateConfig = CreateConfig("Wet", Values(0.7f));
			marker.markerData.templateName = "Custom";

			Assert.AreEqual(Values(0.5f), marker.MarkerData);
		}

		[Test]
		public void RoadWithRemovedTemplateExportsItsCustomValues()
		{
			GameMarkerData marker = CreateRoad(Values(0.5f));
			marker.markerData.customValue = Values(0.3f);
			marker.markerData.templateConfig = CreateConfig("Wet", Values(0.7f));
			marker.markerData.templateName = "Dry";

			Assert.AreEqual(Values(0.3f), marker.MarkerData);
		}

		[Test]
		public void NewTemplateSlotIsNotATemplate()
		{
			GameMarkerData marker = CreateRoad(Values(0.5f));
			marker.markerData.customValue = Values(0.3f);
			marker.markerData.templateConfig = CreateConfig("Wet", Values(0.7f));
			marker.markerData.templateName = PendingTemplateName;

			Assert.AreEqual(Values(0.3f), marker.MarkerData);
		}

		[Test]
		public void OtherMarkersExportTheirValue()
		{
			GameObject gameObject = new GameObject("Animated");
			m_created.Add(gameObject);
			GameMarkerData marker = gameObject.AddComponent<GameMarkerData>();
			AnimationMarkerSettings settings = new AnimationMarkerSettings();
			marker.markerData = new MarkerData { head = "Animation", param = "Animation", value = settings };

			Assert.AreSame(settings, marker.MarkerData);
		}

		[TestCase("grass", SurfaceType.Grass)]
		[TestCase("sand", SurfaceType.Sand)]
		[TestCase("road_icy", SurfaceType.Ice)]
		[TestCase("asphalt", SurfaceType.Asphalt)]
		public void SurfaceDefaultsCarryTheGameSurfaceType(string surface, SurfaceType expected)
		{
			SurfaceTemplate defaults = AssetUtils.GetDBConfig<SurfaceTemplate>(surface);

			Assert.IsNotNull(defaults);
			Assert.AreEqual(expected, defaults.physicMaterial.surfaceType);
		}

		private const string PendingTemplateName = "Pending";

		private static PhysicMaterialProperties Values(float friction)
		{
			return new PhysicMaterialProperties { friction = friction, bumpScale = 25, surfaceType = SurfaceType.Grass };
		}

		private GameMarkerData CreateRoad(PhysicMaterialProperties values)
		{
			GameObject gameObject = new GameObject("Road", typeof(BoxCollider));
			m_created.Add(gameObject);
			GameMarkerData marker = gameObject.AddComponent<GameMarkerData>();
			marker.markerData = new MarkerData { head = "Road", param = "Road/grass", value = values };
			return marker;
		}

		private GameMarkerTemplateConfig CreateConfig(string templateName, PhysicMaterialProperties values)
		{
			GameMarkerTemplateConfig config = ScriptableObject.CreateInstance<GameMarkerTemplateConfig>();
			m_created.Add(config);
			config.presets.presets = new[]
			{
				new MarkerData { head = "Road", param = "Road/asphalt", templateName = templateName, value = values },
				// The last slot, which the config drawer keeps for adding templates; it is never a template.
				new MarkerData { head = "Road", param = "Road/asphalt", templateName = PendingTemplateName, value = Values(0.9f) }
			};
			return config;
		}
	}
}
