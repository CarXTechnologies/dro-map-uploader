using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Editor
{
    public class MapBuilderProcessor : UnityEditor.AssetModificationProcessor
    {
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
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                
            foreach (var path in paths)
            {
                if (path.Contains(".unity") && path.Contains("Assets/MapResources"))
                {
                    AddSceneToBuildSettings(ref scenes, path);
                }
            }
            
            var scenesAcc = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (var index = 0; index < scenes.Count; index++)
            {
                if (paths.FirstOrDefault(val => scenes[index].path == val) != null)
                {
                    scenesAcc.Add(scenes[index]);
                }
            }

            EditorBuildSettings.scenes = scenesAcc.Distinct(SceneEqualityComparer.Default).ToArray();
            return paths;
        }

        private static void AddSceneToBuildSettings(ref List<EditorBuildSettingsScene> scenes, string scenePath)
        {
            var newScene = new EditorBuildSettingsScene
            {
                path = scenePath,
                enabled = true
            };

            scenes.Add(newScene);
        }
    }
    
    public class SceneEqualityComparer : IEqualityComparer<EditorBuildSettingsScene>
    {
        public static readonly SceneEqualityComparer Default = new SceneEqualityComparer();
 
        public bool Equals(EditorBuildSettingsScene x, EditorBuildSettingsScene y)
        {
            return x.path == y.path;
        }
 
        public int GetHashCode(EditorBuildSettingsScene obj)
        {
            return obj.path != null ? obj.path.GetHashCode() : 0;
        }
    }
}