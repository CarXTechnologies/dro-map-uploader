using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor.Validation
{
	public enum MapValidationSeverity
	{
		Info = 0,
		Warning = 1,
		Error = 2,
	}

	public sealed class MapValidationIssue
	{
		public MapValidationSeverity severity;
		public string category;
		public string message;
		public int instanceId;
		public string objectPath;

		public bool HasObject => instanceId != 0 || !string.IsNullOrEmpty(objectPath);

		public Object ResolveObject()
		{
			if (instanceId != 0)
			{
				var byId = EditorUtility.EntityIdToObject(instanceId);

				if (byId != null)
				{
					return byId;
				}
			}

			return string.IsNullOrEmpty(objectPath) ? null : FindByPath(objectPath);
		}

		private static GameObject FindByPath(string path)
		{
			var segments = path.Split('/');
			var scene = SceneManager.GetActiveScene();

			if (!scene.IsValid() || segments.Length == 0)
			{
				return null;
			}

			GameObject current = null;

			foreach (var root in scene.GetRootGameObjects())
			{
				if (root.name == segments[0])
				{
					current = root;
					break;
				}
			}

			for (var i = 1; current != null && i < segments.Length; i++)
			{
				var child = current.transform.Find(segments[i]);
				current = child != null ? child.gameObject : null;
			}

			return current;
		}
	}
}
