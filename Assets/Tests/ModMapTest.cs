using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ModMapTest
{
    private readonly List<GameObject> m_gameObjects = new List<GameObject>();
    private int vertexDiscreate;
    private int vertexCount;
    private Dictionary<Vector3, int> vertexCountPositionDiscreate = new Dictionary<Vector3, int>();
    
    [Test, Order(1)]
    public void Init()
    {
        m_gameObjects.Clear();
        SceneManager.LoadScene(MapObjectConfig.Value.mapName, LoadSceneMode.Additive);
        Add(SceneManager.GetSceneByName(MapObjectConfig.Value.mapName).GetRootGameObjects());
        Assert.True(true);
    }
    
    [Test, Order(2)]
    public void GameObjectCount()
    {
        if (m_gameObjects.Count < 1 && m_gameObjects.Count > 10000)
        {
            Assert.Fail("The number of objects cannot be zero or greater than 10000!");
            return;
        }
        
        Debug.Log(m_gameObjects.Count);
    }
    
    [Test, Order(2)]
    public void MeshCount()
    {
        vertexCount = 0;
        foreach (var gameObject in m_gameObjects)
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();

            if (meshFilter != null && meshFilter.mesh != null)
            {
                var count = meshFilter.mesh.vertexBufferCount;
                vertexCount += count;
                
                var pos = math.floor(meshFilter.transform.position / vertexDiscreate) * vertexDiscreate;
                if (!vertexCountPositionDiscreate.ContainsKey(pos))
                {
                    vertexCountPositionDiscreate.Add(pos, 0);
                }
                    
                vertexCountPositionDiscreate[pos] += count;
            }
        }

        foreach (var val in vertexCountPositionDiscreate)
        {
            if (val.Value > 100000)
            {
                Assert.Fail("Count triangle greater than 100000");
            }
            Debug.Log(val.Value);
        }

        Debug.Log(vertexCount);
    }
    
    private void Add(GameObject[] gameObjects)
    {
        foreach (var gmObject in gameObjects)
        {
            m_gameObjects.Add(gmObject);
            for (int i = 0; i < gmObject.transform.childCount; i++)
            {
                var child = gmObject.transform.GetChild(i);
                m_gameObjects.Add(child.gameObject);
                Add(new[] { child.gameObject });
            }
        }
    }

    [UnityTest]
    public IEnumerator NewTestScriptWithEnumeratorPasses()
    {
        SceneManager.GetActiveScene().GetRootGameObjects();
        yield return null;
    }
}