using System.Collections.Generic;
using UnityEditor;

namespace Editor
{
    public class BuildSceneProcessor : UnityEditor.AssetModificationProcessor
    {
        private const string DIALOG_TITLE = "Add to Build Settings?";
        private const string DIALOG_MSG = "Add to build settings for inclusion in future builds?";
        private const string DIALOG_OK = "Yes";
        private const string DIALOG_NO = "Not now";

        private static List<string> ignorePaths = new List<string>();

        public static void OnWillCreateAsset(string path)
        {
            if (path.EndsWith(".unity.meta"))
            {
                path = path.Substring(0, path.Length - 5);
            }

            ProcessAssetsForScenes(new [] { path });
        }

        public static string[] OnWillSaveAssets(string[] paths)
        {
            return ProcessAssetsForScenes(paths);
        }

        private static string[] ProcessAssetsForScenes(string[] paths)
        {
            var scenePath = string.Empty;

            foreach (var path in paths)
            {
                if (path.Contains(".unity"))
                {
                    scenePath = path;
                }
            }
        
            if (!string.IsNullOrEmpty(scenePath) && !ignorePaths.Contains(scenePath))
            {
                AddSceneToBuildSettings(scenePath);
            }

            return paths;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (var scene in scenes)
            {
                if (scene.path == scenePath)
                {
                    return;
                }
            }
        
            var newScene = new EditorBuildSettingsScene
            {
                path = scenePath,
                enabled = true
            };

            scenes.Add(newScene);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}