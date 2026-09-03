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
		var result = string.Empty;

		if (data == null)
		{
			return result;
		}

		for (var index = 0; index < data.Count; index++)
		{
			var item = data[index];
			if (item.current != 0)
			{
				result += item.ToStat() + (index < data.Count - 2 ? "\n" : String.Empty);
			}
		}

		return result;
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

public interface IValidComponentProcess
{
	public bool isSuccess { get; }
	public string processMessage { get; }
	public void ValidProcess(Component comp);

	public void Reset();
}

[Serializable]
public struct ValidItem : IValidComponentProcess
{
	public string type;
	public int min;
	public int max;
	public int current;
	public IValidComponentProcess validComponentProcess;
	public List<Component> components;
	private bool m_isSuccess;

	public ValidItem(
		string type,
		int min,
		int max,
		IValidComponentProcess validComponentProcess = null,
		int current = 0,
		List<Component> components = null)
	{
		this.type = type;
		this.min = min;
		this.max = max;
		this.current = current;
		this.validComponentProcess = validComponentProcess;
		this.components = components ?? new List<Component>();
		m_isSuccess = true;
	}

	public string ToStat()
	{
		return $"{type} : {current} count ({min} min - {max} max)";
	}

	public override string ToString()
	{
		var result = string.Empty;
		if (validComponentProcess is { isSuccess: false })
		{
			result += validComponentProcess.processMessage + "\n";
		}

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

	public void ValidProcess()
	{
		foreach (var component in components)
		{
			ValidProcess(component);
		}

		m_isSuccess = m_isSuccess && (current >= min && current <= max);
	}

	public void ValidProcess(Component comp)
	{
		validComponentProcess?.ValidProcess(comp);
		m_isSuccess = m_isSuccess && (validComponentProcess?.isSuccess ?? true);
	}

	public void Reset()
	{
		validComponentProcess?.Reset();
		components.Clear();
		m_isSuccess = true;
	}

	public ValidItem CloneStats()
	{
		return new ValidItem(type, min, max, validComponentProcess, current);
	}

	public ValidItem CloneRules()
	{
		return new ValidItem(type, min, max, validComponentProcess);
	}

	public bool isSuccess => m_isSuccess;

	public string processMessage => ToString();
}