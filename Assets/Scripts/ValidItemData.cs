using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ValidItemData : ICloneable
{
	public List<ValidItem> data;
	public readonly float maxSizeInMb;
	public readonly float maxSizeInMbMeta;

	public ValidItemData(float maxSizeInMb = 512f, float maxSizeInMbMeta = 24f, params ValidItem[] data)
	{
		this.maxSizeInMb = maxSizeInMb;
		this.maxSizeInMbMeta = maxSizeInMbMeta;
		this.data = data == null ? new List<ValidItem>() : new List<ValidItem>(data);
	}

	public override string ToString()
	{
		if (data == null)
		{
			return string.Empty;
		}

		var lines = new List<string>();

		foreach (var item in data)
		{
			if (item.current != 0)
			{
				lines.Add(item.ToStat());
			}
		}

		return string.Join("\n", lines);
	}

	public object Clone()
	{
		return new ValidItemData(maxSizeInMb, maxSizeInMbMeta, CopyData(item => item.CloneStats()));
	}

	/// <summary>
	/// Copy of these rules with the size caps replaced by the ones the active mod vendor enforces.
	/// The component whitelist is a property of the game and the same everywhere, while the payload limits differ
	/// per vendor, so only the latter are substituted.
	/// </summary>
	public ValidItemData CloneWithLimits(float payloadSizeInMb, float metaSizeInMb)
	{
		return new ValidItemData(payloadSizeInMb, metaSizeInMb, CopyData(item => item.CloneRules()));
	}

	private ValidItem[] CopyData(Func<ValidItem, ValidItem> copy)
	{
		if (data == null)
		{
			return null;
		}

		var result = new ValidItem[data.Count];

		for (var index = 0; index < data.Count; index++)
		{
			result[index] = copy(data[index]);
		}

		return result;
	}
}

[Serializable]
public struct ValidItem
{
	public string type;
	public int min;
	public int max;
	public int current;
	public List<Component> components;

	public ValidItem(
		string type,
		int min,
		int max,
		int current = 0,
		List<Component> components = null)
	{
		this.type = type;
		this.min = min;
		this.max = max;
		this.current = current;
		this.components = components ?? new List<Component>();
	}

	public string ToStat()
	{
		return $"{type} : {current} count ({min} min - {max} max)";
	}

	public override string ToString()
	{
		var result = string.Empty;

		if (current < min)
		{
			result += $"There are less than {min} {type}\n";
		}

		if (current > max)
		{
			result += $"There are more than {max} {type}\n";
		}

		return result;
	}

	public void Reset()
	{
		components?.Clear();
		current = 0;
	}

	public ValidItem CloneStats()
	{
		return new ValidItem(type, min, max, current);
	}

	public ValidItem CloneRules()
	{
		return new ValidItem(type, min, max);
	}

}