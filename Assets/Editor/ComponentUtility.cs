using System.Collections.Generic;
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
                if (childChild != null && childChild.Length > 0)
                {
                    components.AddRange(childChild);
                }

                var component = child.GetComponent<T>();
                if (component != null)
                {
                    bool validName = false;
                    foreach (var name in validNames)
                    {
                        if (component.transform.name == name.transform.name)
                        {
                            validName = true;
                            break;
                        }
                    }

                    if (validName)
                    {
                        components.Add(component);
                    }
                }
            }

            return components.ToArray();
        }
    }
}