using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class MarkerData
{
	public static readonly string[] paramEditor =
	{
		"SpawnPoint",
		"Road/default",
		"Road/asphalt",
		"Road/brick",
		"Road/cobbles",
		"Road/cobbles_big",
		"Road/concrete",
		"Road/dirt",
		"Road/earth",
		"Road/grass",
		"Road/gravel",
		"Road/ice_snowy",
		"Road/kerb_racetrack",
		"Road/liquid_water",
		"Road/metal",
		"Road/metal_grille",
		"Road/pavement",
		"Road/paving_slab",
		"Road/plastic",
		"Road/road_icy",
		"Road/sand",
		"Road/snow",
		"Road/snow_asphalt",
		"Road/snow_cobbles",
		"Road/snow_gravel",
		"Road/wood",
		"Road/wood_plank",
		"Animation"
	};

	public static bool IsSupportedHead(string head) => head == "SpawnPoint" || head == "Road" || head == "Animation";

	public static int FindEditorIndex(string head, string param)
	{
		if (!IsSupportedHead(head)) return -1;
		return Array.FindIndex(paramEditor, item => item == param || (item == head && item.IndexOf('/') < 0));
	}

	public string head;
	public string param;
	[SerializeReference] public object value;
	[SerializeReference] public object customValue;
	public int index;
	public string lastHeadObject;
	public string templateName;
	public int templateIndex;
	public GameMarkerTemplateConfig templateConfig;
	private static string[] m_paramEditorOnlyParameters;

	public static readonly Dictionary<string, Func<string, object>> paramObjectsEditor =
		new()
		{
			{ "Road", name => AssetUtils.GetDBConfig<SurfaceTemplate>(name.Replace("Road/", string.Empty)).physicMaterial },
			{ "Animation", name => new Plugins.CarX.Modding.Creator.Runtime.AnimationMarkerSettings() },
		};

	public static string[] paramEditorOnlyParameters
	{
		get
		{
			m_paramEditorOnlyParameters ??= paramEditor.Where(s =>
				{
					var split = s.Split('/');
					return split.Length > 0 && paramObjectsEditor.ContainsKey(split[0]);
				})
				.ToArray();

			return m_paramEditorOnlyParameters;
		}
	}

	public void Update()
	{
		if (templateConfig == null && head != "Animation")
		{
			value = null;
		}
	}

	public string GetHead() => string.IsNullOrEmpty(head) ? string.Empty : head.ToLowerInvariant();

	public static string GetHeadTarget(string param)
	{
		var group = param.IndexOf('/');
		return group != -1 ? param.Substring(0, group) : param;
	}
}