using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Editor
{
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

    public class ModMapTestTool
    {
        public const float BYTES_TO_MEGABYTES = 1048576f;

        private static readonly List<GameObject> m_gameObjects = new List<GameObject>();
        private static Dictionary<Vector3, int> m_vertexCountPositionDiscreate = new Dictionary<Vector3, int>();
        public static Func<string, bool> playCallback;
        public static Action<string, string> errorCallback;

        private string m_currentName;

        public static readonly ValidItemData Target = new ValidItemData
        (4096, 24f, 100f, 30000000,
            new ValidItem(typeof(Transform), 1, 10000),
            //Physics
            new ValidItem(typeof(MeshCollider), 1, 2000),
            new ValidItem(typeof(BoxCollider), 0, 2000),
            new ValidItem(typeof(SphereCollider), 0, 1000),
            new ValidItem(typeof(CapsuleCollider), 0, 1000),
            new ValidItem(typeof(Rigidbody), 0, 1000),
            //Hdrp 
            new ValidItem(typeof(ReflectionProbe), 1, 1),
            new ValidItem(typeof(HDAdditionalLightData), 0, 200),
            new ValidItem(typeof(HDAdditionalReflectionData), 0, 200),
            new ValidItem(typeof(Volume), 1, 1),
            //Render
            new ValidItem(typeof(MeshRenderer), 0, 1000),
            new ValidItem(typeof(MeshFilter), 0, 1000),
            new ValidItem(typeof(Light), 0, 200),
            new ValidItem(typeof(LODGroup), 0, 1000),
            // UI
            new ValidItem(typeof(Canvas), 0, 10),
            new ValidItem(typeof(CanvasScaler), 0, 10),
            new ValidItem(typeof(GraphicRaycaster), 0, 10),
            new ValidItem(typeof(CanvasRenderer), 0, 100),
            new ValidItem(typeof(RectTransform), 0, 100),
            new ValidItem(typeof(TextMeshProUGUI), 0, 100),
            //Particle
            new ValidItem(typeof(ParticleSystem), 0, 100),
            new ValidItem(typeof(ParticleSystemRenderer), 0, 100),
            //Other
            new ValidItem(typeof(GameMarkerData), 1, 1000),
            new ValidItem(typeof(CacheData), 0, 1),
            new ValidItem(typeof(Minimap), 1, 1)
        );

        private GameObject m_root;
        private List<ValidItem> m_listValid = new List<ValidItem>();

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
            var isNoCorrect = Target.maxSizeInMb < sizeFileInMegabytes;
            if (isNoCorrect)
            {
                TryErrorMessage(name, "Map size is " + sizeFileInMegabytes + "/" + Target.maxSizeInMb);
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
            var isNoCorrect = Target.maxSizeInMbMeta < sizeFileInMegabytes;
            if (isNoCorrect)
            {
                TryErrorMessage(null, "Meta size is " + sizeFileInMegabytes + "/" + Target.maxSizeInMbMeta);
            }

            return isNoCorrect;
        }

        private static void VertexTestCount()
        {
            m_vertexCountPositionDiscreate.Clear();
            foreach (var gameObject in m_gameObjects)
            {
                var meshFilter = gameObject.GetComponent<MeshFilter>();
            
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    var count = meshFilter.sharedMesh.vertexCount;

                    var pos = meshFilter.transform.position / Target.vertexDistanceForMaxCount;
                
                    var posDisc = new Vector3(Mathf.Floor(pos.x), Mathf.Floor(pos.y), Mathf.Floor(pos.z))
                                  * Target.vertexDistanceForMaxCount;
                
                    if (m_vertexCountPositionDiscreate.TryGetValue(posDisc, out var value))
                    {
                        m_vertexCountPositionDiscreate[posDisc] = value + count;
                    }
                    else
                    {
                        m_vertexCountPositionDiscreate.Add(posDisc, count);
                    }
                }
            }
        
            foreach (var val in m_vertexCountPositionDiscreate)
            {
                if (val.Value > Target.vertexCountForDistance)
                {
                    throw new Exception("Triangle greater than " + Target.vertexCountForDistance);
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

        public struct ValidItemData
        {
            public List<ValidItem> data;
            public readonly int vertexCountForDistance;
            public readonly float vertexDistanceForMaxCount;
            public readonly float maxSizeInMb;
            public readonly float maxSizeInMbMeta;

            public ValidItemData(float maxSizeInMb = 512f, float maxSizeInMbMeta = 24f, float vertexDistanceForMaxCount = 1f, 
                int vertexCountForDistance = 10000000, params ValidItem[] data)
            {
                this.vertexCountForDistance = vertexCountForDistance;
                this.vertexDistanceForMaxCount = vertexDistanceForMaxCount;
                this.maxSizeInMb = maxSizeInMb;
                this.maxSizeInMbMeta = maxSizeInMbMeta;
                this.data = new List<ValidItem>(data);
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
    
        public static void TryErrorMessage(string name, string message)
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

                    if (!ValidType(compType, m_listValid))
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
    
        public static bool ValidType(Type type, List<ValidItem> types, bool addToValidList = true)
        {
            var tryComp = false;
            for (var index = 0; index < types.Count; index++)
            {
                if (type.Name == types[index].type.Name)
                {
                    tryComp = true;
                    if (addToValidList)
                    {
                        types[index] = new ValidItem(types[index].type, types[index].min, types[index].max, types[index].current + 1);
                    }

                    break;
                }
            }
        
            return tryComp;
        }
    
        public static bool ValidType(Type type, List<string> types)
        {
            var tryComp = false;
            for (var index = 0; index < types.Count; index++)
            {
                if (type.Name == types[index])
                {
                    tryComp = true;
                
                    break;
                }
            }
        
            return tryComp;
        }
    }
}