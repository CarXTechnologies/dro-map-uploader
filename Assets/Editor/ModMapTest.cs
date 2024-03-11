using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.VFX;
using UnityEngine.Video;

namespace Editor
{
    public class ModMapTestTool
    {
        public const float BYTES_TO_MEGABYTES = 1048576f;

        private static readonly List<GameObject> m_gameObjects = new List<GameObject>();
        private static Dictionary<Vector3, int> m_vertexCountPositionDiscreate = new Dictionary<Vector3, int>();
        public static Func<string, bool> playCallback;
        public static Action<string, string> errorCallback;

        private string m_currentName;

        public static ValidItemData Target = default;
        
        public static readonly ValidItemData Steam = new ValidItemData
        (4096, 24f, 100f, 30000000,
            new ValidItem(nameof(Transform), 1, 20000),
            //Physics
            new ValidItem(nameof(MeshCollider), 1, 10000),
            new ValidItem(nameof(BoxCollider), 0, 10000),
            new ValidItem(nameof(SphereCollider), 0, 1000),
            new ValidItem(nameof(CapsuleCollider), 0, 1000),
            new ValidItem(nameof(Rigidbody), 0, 1000),
            new ValidItem(nameof(FixedJoint), 0, 100),
            new ValidItem(nameof(SpringJoint), 0, 100),
            new ValidItem(nameof(HingeJoint), 0, 100),
            //Hdrp 
            new ValidItem(nameof(ReflectionProbe), 1, 1),
            new ValidItem(nameof(HDAdditionalLightData), 0, 200),
            new ValidItem(nameof(HDAdditionalReflectionData), 0, 200),
            new ValidItem(nameof(Volume), 1, 1),
            //Render
            new ValidItem(nameof(MeshRenderer), 0, 10000),
            new ValidItem(nameof(MeshFilter), 0, 10000),
            new ValidItem(nameof(Light), 0, 500),
            new ValidItem(nameof(LODGroup), 0, 10000),
            new ValidItem(nameof(Animator), 0, 100),
            // UI
            new ValidItem(nameof(Canvas), 0, 10),
            new ValidItem(nameof(CanvasScaler), 0, 10),
            new ValidItem(nameof(CanvasRenderer), 0, 100),
            new ValidItem(nameof(RectTransform), 0, 100),
            new ValidItem(nameof(TextMeshProUGUI), 0, 50),
            new ValidItem(nameof(RawImage), 0, 20),
            new ValidItem(nameof(VideoPlayer), 0, 5, 
                new ValidVideoPlayer(1280, 720, 30, 15)),
            //Particle
            new ValidItem(nameof(ParticleSystem), 0, 200),
            new ValidItem(nameof(ParticleSystemRenderer), 0, 200),
            new ValidItem(nameof(VisualEffect), 0, 200),
            new ValidItem("VFXRenderer", 0, 200),
            //Other
            new ValidItem(nameof(GameMarkerData), 1, 1000),
            new ValidItem(nameof(CacheData), 0, 1),
            new ValidItem(nameof(Minimap), 1, 1),
            new ValidItem("SceneObjectIDMapSceneAsset", 0, 1)
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

        public ModMapTestTool With((string, int, int) value)
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
                    if (!ValidType(component, m_listValid))
                    {
                        var compType = component.GetType();
                        TryErrorMessage(m_currentName, new ValidItem(compType.Name, Int32.MinValue, Int32.MaxValue, null, 1).ToString());
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
            foreach (var valid in m_listValid)
            {
                valid.Reset();
            }

            var parent = m_root.transform;
            ValidComponents(parent);

            foreach (var valid in m_listValid)
            {
                valid.ValidProcess();
                if (!valid.isSuccess)
                {
                    TryErrorMessage(m_currentName, valid.ToString());
                    return;
                }
            }
        }
    
        public static bool ValidType(Component component, List<ValidItem> types, bool addToValidList = true)
        {
            var tryComp = false;
            var type = component.GetType();
            for (var index = 0; index < types.Count; index++)
            {
                if (type.Name == types[index].type)
                {
                    tryComp = true;
                    if (addToValidList)
                    {
                        types[index] = new ValidItem(types[index].type, types[index].min, types[index].max, 
                            types[index].validComponentProcess, types[index].current + 1, types[index].components);
                    }

                    types[index].components.Add(component);
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