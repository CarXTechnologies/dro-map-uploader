using Plugins.CarX.Modding.Creator.Runtime;
using UnityEngine;

public class GameMarkerData : MonoBehaviour, IMarkerDataSource
{
	public MarkerData markerData;

	public string MarkerHead => markerData?.head;
	public string MarkerParam => markerData?.param;
	public object MarkerData => markerData != null && markerData.head == "Road" ? GetRoadValues(markerData) : markerData?.value;

	// Road values are resolved at export, not taken from what the Inspector last wrote, so they don't depend on which markers were selected.
	// Without a Template Config the marker exports none and the game uses its own surface template, Surface Type and Water Rate included.
	private static object GetRoadValues(MarkerData data)
	{
		if (data.templateConfig == null)
		{
			return null;
		}

		if (data.templateName == "Custom")
		{
			return data.value;
		}

		// The last preset is the config's "new template" slot, not a template.
		MarkerData[] presets = data.templateConfig.presets.presets;
		for (int i = 0; i < presets.Length - 1; i++)
		{
			if (presets[i].templateName == data.templateName && presets[i].value is PhysicMaterialProperties)
			{
				return presets[i].value;
			}
		}

		// The template was removed: the Inspector switches such a marker to Custom with its own values.
		return data.customValue is PhysicMaterialProperties ? data.customValue : data.value;
	}
}