using System;
using UnityEngine;

[Serializable]
public struct MapMetaConfigValue
{
	public string mapName;
	[TextArea] public string mapDescription;
	public Texture2D icon;
	public Texture2D largeIcon;
	public string[] authors;
	public string url;

	public PlatformBuild platform;
	public CompressBuild compress;

	public override bool Equals(object obj)
	{
		if (obj is MapMetaConfigValue value)
		{
			return value.icon == icon &&
			       value.largeIcon == largeIcon &&
			       value.mapDescription == mapDescription &&
			       value.mapName == mapName &&
			       value.platform == platform &&
			       value.compress == compress;
		}

		return false;
	}
}