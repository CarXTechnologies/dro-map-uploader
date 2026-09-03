using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
using UnityEngine.VFX;
using UnityEngine.Video;

namespace Editor
{
	public static class ModMapTestTool
	{
		public const float BYTES_TO_MEGABYTES = 1048576f;

		public static ValidItemData Target = default;

		/// <summary>
		/// Component budget every map has to fit into, regardless of where it is published.
		/// The two size numbers are only defaults; the uploader replaces them with the limits of the active vendor
		/// through <see cref="ValidItemData.CloneWithLimits"/> before validating a build.
		/// </summary>
		public static readonly ValidItemData ComponentRules = new(4096, 24f,
			new ValidItem(nameof(Transform), 1, 20000),
			//Physics
			new ValidItem(nameof(MeshCollider), 1, 10000),
			new ValidItem(nameof(BoxCollider), 0, 10000),
			new ValidItem(nameof(SphereCollider), 0, 1000),
			new ValidItem(nameof(CapsuleCollider), 0, 1000),
			new ValidItem(nameof(Rigidbody), 0, 1000),
			new ValidItem(nameof(FixedJoint), 0, 100),
			new ValidItem(nameof(SpringJoint), 0, 100),
			new ValidItem(nameof(HingeJoint), 0, 100),
			//Hdrp
			new ValidItem(nameof(ReflectionProbe), 1, 1),
			new ValidItem(nameof(HDAdditionalLightData), 0, 500),
			new ValidItem(nameof(HDAdditionalReflectionData), 0, 200),
			new ValidItem(nameof(Volume), 1, 1),
			//Render
			new ValidItem(nameof(MeshRenderer), 0, 10000),
			new ValidItem(nameof(MeshFilter), 0, 10000),
			new ValidItem(nameof(Light), 0, 500),
			new ValidItem(nameof(LODGroup), 0, 10000),
			new ValidItem(nameof(Animator), 0, 100),
			// UI
			new ValidItem(nameof(Canvas), 0, 10),
			new ValidItem(nameof(CanvasScaler), 0, 10),
			new ValidItem(nameof(CanvasRenderer), 0, 50),
			new ValidItem(nameof(RectTransform), 0, 100),
			new ValidItem(nameof(TextMeshProUGUI), 0, 50),
			new ValidItem(nameof(RawImage), 0, 20),
			new ValidItem(nameof(VideoPlayer), 0, 5, new ValidVideoPlayer()),
			//Particle
			new ValidItem(nameof(ParticleSystem), 0, 200),
			new ValidItem(nameof(ParticleSystemRenderer), 0, 200),
			new ValidItem(nameof(VisualEffect), 0, 200),
			new ValidItem("VFXRenderer", 0, 200),
			//Other
			new ValidItem(nameof(GameMarkerData), 1, 10000),
			new ValidItem(nameof(CacheData), 0, 1),
			new ValidItem(nameof(Minimap), 1, 1),
			new ValidItem("SceneObjectIDMapSceneAsset", 0, 1)
		);

		public static bool ValidType(Component component, List<ValidItem> types, bool addToValidList = true)
		{
			if (component == null || types == null)
			{
				return false;
			}

			var type = component.GetType();

			for (var index = 0; index < types.Count; index++)
			{
				if (type.Name != types[index].type)
				{
					continue;
				}

				if (addToValidList)
				{
					types[index] = new ValidItem(types[index].type, types[index].min, types[index].max,
						types[index].validComponentProcess, types[index].current + 1, types[index].components);
				}

				types[index].components.Add(component);
				return true;
			}

			return false;
		}

		public static bool ValidType(Type type, List<string> types)
		{
			if (type == null || types == null)
			{
				return false;
			}

			for (var index = 0; index < types.Count; index++)
			{
				if (type.Name == types[index])
				{
					return true;
				}
			}

			return false;
		}
	}
}
