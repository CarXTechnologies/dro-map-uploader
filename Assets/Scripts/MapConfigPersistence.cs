using System;
using UnityEngine;

public static class MapConfigPersistence
{
	public static Action<UnityEngine.Object> markDirty;
	public static Action<UnityEngine.Object> save;
	public static Action<UnityEngine.Object> saveForce;

	public static void MarkDirty(UnityEngine.Object target)
	{
		Dispatch(markDirty, target);
	}

	public static void Save(UnityEngine.Object target)
	{
		Dispatch(save, target);
	}

	public static void SaveForce(UnityEngine.Object target)
	{
		Dispatch(saveForce, target);
	}

	private static void Dispatch(Action<UnityEngine.Object> handler, UnityEngine.Object target)
	{
		if (target == null)
		{
			return;
		}

		if (handler == null)
		{
			Debug.LogWarning($"'{target.name}' was not saved: no persistence handler is installed.");
			return;
		}

		handler(target);
	}
}
