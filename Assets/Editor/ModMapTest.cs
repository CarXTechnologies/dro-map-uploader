using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class ModMapTestTool
{
    private const int MAX_COUNT_VERTEX_IN_DISREATE = 10000000;
    private const int MAX_COUNT_GAMEOBJECT = 10000;
    private const float BYTES_TO_MEGABYTES = 1048576f;
    private const float MEGABYTES_MAX = 512f;
    private const float MEGABYTES_MAX_META = 24f;
    private const int VERTEX_DISCRETE = 1;
    
    private static readonly List<GameObject> m_gameObjects = new List<GameObject>();
    private static Dictionary<float3, int> m_vertexCountPositionDiscreate = new Dictionary<float3, int>();
    public static Func<string, bool> playCallback;
    public static Action<string, string> errorCallback;

    private string m_currentName;
    private GameObject m_root;
    private List<ValidItem> m_listValid = new List<ValidItem>();

    public static readonly List<ValidItem> Steam = new List<ValidItem>()
    {
        new ValidItem(typeof(Transform), 0, 1000),
        new ValidItem(typeof(MeshCollider), 0, 100),
        new ValidItem(typeof(BoxCollider), 0, 1000),
        new ValidItem(typeof(SphereCollider), 0, 1000),
        new ValidItem(typeof(GameMarkerData), 0, 1000),
        new ValidItem(typeof(MeshRenderer), 0, 1000),
        new ValidItem(typeof(MeshFilter), 0, 1000),
        new ValidItem(typeof(Light), 0, 1000),
        new ValidItem(typeof(HDAdditionalLightData), 0, 1000),
        new ValidItem(typeof(Volume), 0, 1000),
        new ValidItem(typeof(MapConfig), 0, 1000),
        new ValidItem(typeof(CacheData), 0, 1000),
        new ValidItem(typeof(ReflectionProbe), 0, 1000),
        new ValidItem(typeof(LODGroup), 0, 1000),
        new ValidItem(typeof(Minimap), 1, 1),
    };
    
    public static void InitTests(string sceneName)
    {
        m_gameObjects.Clear();
        var scene = SceneManager.GetSceneByName(sceneName);;
        AddTreeGameObjectToList(scene.GetRootGameObjects());
    }
    
#if UNITY_EDITOR
    public static void InitTestsEditor(Scene scene)
    {
        m_gameObjects.Clear();
        AddTreeGameObjectToList(scene.GetRootGameObjects());
    }
#endif  

    public static bool IsNotCorrectMapFileSize(string name, string pathToFile)
    {
        var file = new FileInfo(pathToFile);
        if (!file.Exists)
        {
            TryErrorMessage(name, "Map file is not exist");
            return true;
        }

        var sizeFileInMegabytes = file.Length / BYTES_TO_MEGABYTES;
        var isNoCorrect = MEGABYTES_MAX < sizeFileInMegabytes;
        if (isNoCorrect)
        {
            TryErrorMessage(name, "Map size is " + sizeFileInMegabytes + "/" + MEGABYTES_MAX);
        }

        return isNoCorrect;
    }
    
    public static bool IsNotCorrectMetaFileSize(string pathToFile)
    {
        var file = new FileInfo(pathToFile);
        if (!file.Exists)
        {
            TryErrorMessage(null, "Meta file is not exist");
            return true;
        }

        var sizeFileInMegabytes = file.Length / BYTES_TO_MEGABYTES;
        var isNoCorrect = MEGABYTES_MAX_META < sizeFileInMegabytes;
        if (isNoCorrect)
        {
            TryErrorMessage(null, "Meta size is " + sizeFileInMegabytes + "/" + MEGABYTES_MAX_META);
        }

        return isNoCorrect;
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
                var pos = math.floor(meshFilter.transform.position / VERTEX_DISCRETE) * VERTEX_DISCRETE;
                
                if (m_vertexCountPositionDiscreate.TryGetValue(pos, out var value))
                {
                    m_vertexCountPositionDiscreate[pos] = value + count;
                }
                else
                {
                    m_vertexCountPositionDiscreate.Add(pos, count);
                }
            }
        }
        
        foreach (var val in m_vertexCountPositionDiscreate)
        {
            if (val.Value > MAX_COUNT_VERTEX_IN_DISREATE)
            {
                throw new Exception("Triangle greater than " + MAX_COUNT_VERTEX_IN_DISREATE);
            }
        }
    }
    
    private static void SpawnPointTestExistence()
    {
        const string spawnPoint = "spawnpoint";
        int countSpawnPoint = 0;
        foreach (var gameObject in m_gameObjects)
        {
            var gameMaker = gameObject.GetComponent<GameMarkerData>();
            if (gameMaker != null)
            {
                if (gameMaker.markerData.GetHead() == spawnPoint)
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
    
    public static bool RunTest(string sceneName)
    {
        try
        {
            GameObjectTestCount();
            VertexTestCount();
            SpawnPointTestExistence();
            return false;
        }
        catch (Exception e)
        {
            TryErrorMessage(sceneName, e.Message);
            return true;
        }
    }

    public struct ValidItem
    {
        public Type type;
        public int min;
        public int max;
        public int current;

        public ValidItem(Type type, int min, int max, int current = 0)
        {
            this.type = type;
            this.min = min;
            this.max = max;
            this.current = current;
        }

        public override string ToString()
        {
            if (current < min)
            {
                return $"There are less than {min} {type.Name}";
            }
            
            if (current > max)
            {
                return $"There are more than {max} {type.Name}";
            }

            return string.Empty;
        }
    }
    
    private static void AddTreeGameObjectToList(GameObject[] gameObjects)
    {
        foreach (var gmObject in gameObjects)
        {
            for (int i = 0; i < gmObject.transform.childCount; i++)
            {
                var child = gmObject.transform.GetChild(i);
                m_gameObjects.Add(child.gameObject);
                AddTreeGameObjectToList(new[] { child.gameObject });
            }
        }
    }
    
    private static void TryErrorMessage(string name, string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        errorCallback?.Invoke(name, message);
    }
    
    public static ModMapTestTool Play(string name)
    {
        var continuePlay = playCallback?.Invoke(name);
        if (continuePlay != null && !continuePlay.Value)
        {
            return null;
        }

        var root = GameObject.Find("root");

        if (root == null)
        {
            TryErrorMessage(name, "");
            return null;
        }
        
        var modMapTool = new ModMapTestTool
        {
            m_root = GameObject.Find("root"),
            m_currentName = name
        };
        
        return modMapTool;
    }

    public ModMapTestTool With((Type, int, int) value)
    {
        m_listValid.Add(new ValidItem(value.Item1, value.Item2, value.Item3));
        return this;
    }
    
    public ModMapTestTool WithList(List<ValidItem> value)
    {
        m_listValid.Clear();
        m_listValid.AddRange(value);
        return this;
    }
    
    private void ValidComponents(Transform parent)
    {
        var allComponents = parent.GetComponents(typeof(Component));

        foreach (var component in allComponents)
        {
            if (component != null)
            {
                var compType = component.GetType();

                if (!Try(compType, m_listValid))
                {
                    TryErrorMessage(m_currentName, new ValidItem(compType, Int32.MinValue, Int32.MaxValue, 1).ToString());
                    return;
                }
            }
        }

        for (var i = 0; i < parent.transform.childCount; i++)
        {
            var child = parent.transform.GetChild(i);
            ValidComponents(child);
        }
    }
    
    public void ValidComponents()
    {
        var parent = m_root.transform;
        ValidComponents(parent);

        foreach (var valid in m_listValid)
        {
            if (valid.current < valid.min || valid.current > valid.max)
            {
                TryErrorMessage(m_currentName, valid.ToString());
                return;
            }
        }
    }
        
    private static bool Try(Type type, List<ValidItem> types)
    {
        var tryComp = false;
        for (var index = 0; index < types.Count; index++)
        {
            if (type.Name == types[index].type.Name)
            {
                tryComp = true;
                types[index] = new ValidItem(types[index].type, types[index].min, types[index].max, types[index].current + 1);
                break;
            }
        }
        
        return tryComp;
    }
}