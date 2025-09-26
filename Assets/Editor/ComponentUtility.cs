using System.Collections.Generic;
using UnityEngine;

namespace Editor
{
	public static class ComponentUtility
	{ 
		public static T[] FindAllComponent<T>(this Transform parent, params Component[] validNames) where T : Component
		{
			if (parent == null)
				return System.Array.Empty<T>();

			var components = new List<T>(parent.childCount);
			
			bool acceptAll = validNames == null || validNames.Length == 0;

			for (int i = 0; i < parent.childCount; i++)
			{
				var child = parent.GetChild(i);
				var childChild = child.FindAllComponent<T>(validNames);

				if (childChild != null && childChild.Length > 0)
				{
					components.AddRange(childChild);
				}
				
				var component = child.GetComponent<T>();
				if (component == null)
				{
					continue;
				}

				bool validName = false;
				if (acceptAll)
				{
					validName = true;
				}
				else
				{
					foreach (var name in validNames)
					{
						if (name == null || name.transform == null || component.transform == null)
							continue;
						
						if (component.transform.name == name.transform.name)
						{
							validName = true;
							break;
						}
					}
				}

				if (validName)
				{
					components.Add(component);
				}
			}

			return components.ToArray();
		}
	}
}
