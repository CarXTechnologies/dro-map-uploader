using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ModMapTest
{
    private readonly List<GameObject> m_gameObjects = new List<GameObject>();
    private int vertexDiscreate = 1;
    private int vertexCount;
    private Dictionary<float3, int> vertexCountPositionDiscreate = new Dictionary<float3, int>();
    private const int MAX_COUNT_VERTEX_IN_DISREATE = 100000;
    private const int MAX_COUNT_VERTEX = 100000000;
    private const int MAX_COUNT_GAMEOBJECT = 10000;

    [UnityTest, Order(1)]
    public IEnumerator InitTests()
    {
        m_gameObjects.Clear();
        var scenePath = MapMetaConfig.Value.GetTargetScenePath();
        var scene = EditorSceneManager.OpenScene(scenePath);
        
        AddTreeGameObjectToList(scene.GetRootGameObjects());
        yield break;
    }
    
    [Test, Order(2)]
    public void GameObjectTestCount()
    {
        if (m_gameObjects.Count < 1 && m_gameObjects.Count > MAX_COUNT_GAMEOBJECT)
        {
            Assert.Fail("The number of objects cannot be 0 or greater than " + MAX_COUNT_GAMEOBJECT);
            return;
        }
        
        Debug.Log("GameObject count : " + m_gameObjects.Count);
    }
    
    [Test, Order(2)]
    public void VertexTestCount()
    {
        vertexCount = 0;
        foreach (var gameObject in m_gameObjects)
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var count = meshFilter.sharedMesh.vertexCount;
                vertexCount += count;
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
                Assert.Fail("Triangle greater than " + MAX_COUNT_VERTEX_IN_DISREATE);
            }
        }

        if (vertexCount > MAX_COUNT_VERTEX)
        {
            Assert.Fail("Triangle greater than " + MAX_COUNT_VERTEX);
        }
        
        Debug.Log("Vertex count : " + vertexCount);
    }
    
    [Test, Order(2)]
    public void SpawnPointTestExistence()
    {
        int countSpawnPoint = 0;
        foreach (var gameObject in m_gameObjects)
        {
            var gameMaker = gameObject.GetComponent<GameMarkerData>();
            if (gameMaker != null)
            {
                if (gameMaker.markerData.head.ToLower() == "spawnpoint")
                {
                    countSpawnPoint++;
                }
            }
        }

        if (countSpawnPoint == 0)
        {
            Assert.Fail("No one spawn point in map");
        }
        else if (countSpawnPoint > 1)
        {
            Assert.Fail("Multiple spawn point in map");
        }
    }

    private void AddTreeGameObjectToList(GameObject[] gameObjects)
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