using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public static class ModMapTestTool
{
    private static readonly List<GameObject> m_gameObjects = new List<GameObject>();
    private static int vertexDiscreate = 1;
    private static Dictionary<float3, int> vertexCountPositionDiscreate = new Dictionary<float3, int>();
    private const int MAX_COUNT_VERTEX_IN_DISREATE = 100000;
    private const int MAX_COUNT_GAMEOBJECT = 10000;
    private const float BYTES_TO_MEGABYTES = 1048576f;
    private const float MEGABYTES_MAX = 512f;
    private const float MEGABYTES_MAX_META = 24f;
    
    private static void InitTests(string sceneName)
    {
        m_gameObjects.Clear();
        SceneManager.LoadScene(sceneName);
        var scene = SceneManager.GetActiveScene();
        AddTreeGameObjectToList(scene.GetRootGameObjects());
    }

    public static bool IsNotCorrectFileSize(string pathToFile)
    {
        return MEGABYTES_MAX < new FileInfo(pathToFile).Length / BYTES_TO_MEGABYTES;
    }
    
    public static bool IsNotCorrectMetaFileSize(string pathToFile)
    {
        return MEGABYTES_MAX_META < new FileInfo(pathToFile).Length / BYTES_TO_MEGABYTES;
    }
    
#if UNITY_EDITOR
    private static void InitTestsEditor(string pathToScene)
    {
        m_gameObjects.Clear();
        Scene scene = EditorSceneManager.OpenScene(pathToScene);
        AddTreeGameObjectToList(scene.GetRootGameObjects());
    }
#endif  
    
    private static void SizeTest(string path, string metaBundle, string mapBundle)
    {
        if (IsNotCorrectFileSize(path + "/" + mapBundle) && IsNotCorrectFileSize(path + "/" + metaBundle))
        {
            throw new Exception("Size is Big");
        }
    }

    private static void GameObjectTestCount()
    {
        if (m_gameObjects.Count < 1 && m_gameObjects.Count > MAX_COUNT_GAMEOBJECT)
        {
            throw new Exception("The number of objects cannot be 0 or greater than " + MAX_COUNT_GAMEOBJECT);
        }
    }

    private static void VertexTestCount()
    {
        foreach (var gameObject in m_gameObjects)
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var count = meshFilter.sharedMesh.vertexCount;
                var pos = math.floor(meshFilter.transform.position / vertexDiscreate) * vertexDiscreate;
                
                if (vertexCountPositionDiscreate.TryGetValue(pos, out var value))
                {
                    vertexCountPositionDiscreate[pos] = value + count;
                }
                else
                {
                    vertexCountPositionDiscreate.Add(pos, count);
                }
            }
        }
        
        foreach (var val in vertexCountPositionDiscreate)
        {
            if (val.Value > MAX_COUNT_VERTEX_IN_DISREATE)
            {
                throw new Exception("Triangle greater than " + MAX_COUNT_VERTEX_IN_DISREATE);
            }
        }
    }
    
    private static void SpawnPointTestExistence()
    {
        int countSpawnPoint = 0;
        foreach (var gameObject in m_gameObjects)
        {
            var gameMaker = gameObject.GetComponent<GameMarkerData>();
            if (gameMaker != null)
            {
                if (string.Equals(gameMaker.markerData.head, "SpawnPoint", StringComparison.CurrentCultureIgnoreCase))
                {
                    countSpawnPoint++;
                }
            }
        }

        if (countSpawnPoint == 0)
        {
            throw new Exception("No one spawn point in map");
        }

        if (countSpawnPoint > 1)
        {
            throw new Exception("Multiple spawn point in map");
        }
    }

    public static bool RunTest(string pathToManifest, string metaBundle, string mapBundle, string editorTargetScene = "")
    {
        try
        {
            SizeTest(pathToManifest, metaBundle, mapBundle);
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                InitTests(mapBundle);
            }
            else
            {
                InitTestsEditor(editorTargetScene);
            }
#else
            InitTests(MapMetaConfig.Value.mapName);
#endif
            GameObjectTestCount();
            VertexTestCount();
            SpawnPointTestExistence();
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError("Test Mod Map Error : " + e.Message);
            return true;
        }
    }

    private static void AddTreeGameObjectToList(GameObject[] gameObjects)
    {
        foreach (var gmObject in gameObjects)
        {
            m_gameObjects.Add(gmObject);
            for (int i = 0; i < gmObject.transform.childCount; i++)
            {
                var child = gmObject.transform.GetChild(i);
                m_gameObjects.Add(child.gameObject);
                AddTreeGameObjectToList(new[] { child.gameObject });
            }
        }
    }
}