using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Editor
{
	public static class ComponentUtility
	{
		public static T[] FindAllComponent<T>(this Transform parent, params Component[] validNames) where T : Component
		{
			var components = new List<T>(parent.childCount);
			for (int i = 0; i < parent.childCount; i++)
			{
				var child = parent.GetChild(i);
				var childChild = child.FindAllComponent<T>();

				if (childChild is { Length: > 0 })
				{
					components.AddRange(childChild);
				}

				var component = child.GetComponent<T>();

				if (component == null)
				{
					continue;
				}

				var validName = validNames.Any(name => component.transform.name == name.transform.name);

				if (validName)
				{
					components.Add(component);
				}
			}

			return components.ToArray();
		}
	}
}