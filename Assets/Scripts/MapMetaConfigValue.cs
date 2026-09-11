using System;
using System.Linq;
using UnityEngine;

[Serializable]
public struct MapMetaConfigValue
{
	public string mapName;
	public string mapVersion;
	[TextArea] public string summary;
	[TextArea] public string mapDescription;
	public Texture2D icon;
	public Texture2D largeIcon;
	public string[] authors;
	public string url;


	public override bool Equals(object obj)
	{
		if (obj is MapMetaConfigValue value)
		{
			return value.icon == icon &&
			       value.largeIcon == largeIcon &&
			       value.summary == summary &&
			       value.mapDescription == mapDescription &&
			       value.mapName == mapName &&
			       value.mapVersion == mapVersion &&
			       value.url == url &&
			       (value.authors ?? Array.Empty<string>()).SequenceEqual(authors ?? Array.Empty<string>());
		}

		return false;
	}

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(mapName); hash.Add(mapVersion); hash.Add(summary); hash.Add(mapDescription); hash.Add(icon); hash.Add(largeIcon); hash.Add(url);
		foreach (var author in authors ?? Array.Empty<string>()) hash.Add(author);
		return hash.ToHashCode();
	}
}
