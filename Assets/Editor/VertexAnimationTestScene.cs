using System;
using System.IO;
using System.Linq;
using Plugins.CarX.Modding.Creator.Editor;
using Plugins.CarX.Modding.Creator.Runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor
{
    public static class VertexAnimationTestScene
    {
        private const string ScenePath = "Assets/MapResources/TestMap/TestMap.unity";
        private const string Folder = "Assets/MapResources/TestMap/AnimationTest";
        private const string GroupName = "VAT Animation Test";

        [MenuItem("Map/Add vertex animation test to TestMap")]
        public static void Create()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
                if (!scene.IsValid() || !scene.isLoaded)
                    scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            }
            var group = scene.GetRootGameObjects().FirstOrDefault(g => g.name == GroupName);
            if (group == null)
            {
                if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/MapResources/TestMap", "AnimationTest");
                var mesh = MakeFlag();
                AssetDatabase.CreateAsset(mesh, Folder + "/Flag.asset");
                var material = new Material(Shader.Find("HDRP/Lit")) { name = "Animation Test Orange" };
                material.SetColor("_BaseColor", new Color(1, 0.3f, 0.03f));
                material.SetFloat("_DoubleSidedEnable", 1);
                material.SetFloat("_CullMode", 0);
                AssetDatabase.CreateAsset(material, Folder + "/Flag.mat");
                var controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "/Flag.controller");
                for (int i = 0; i < 2; i++)
                {
                    var duration = i == 0 ? 2f : 0.8f;
                    var clip = new AnimationClip { name = i == 0 ? "Wave" : "Flutter", frameRate = 30 };
                    clip.SetCurve("Flag", typeof(SkinnedMeshRenderer), "blendShape.Wave",
                        new AnimationCurve(new Keyframe(0, 0), new Keyframe(duration / 2, 100), new Keyframe(duration, 0)));
                    var settings = AnimationUtility.GetAnimationClipSettings(clip);
                    settings.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                    AssetDatabase.CreateAsset(clip, Folder + "/" + clip.name + ".anim");
                    controller.AddMotion(clip);
                }
                var prototype = new GameObject("VAT Flag");
                var animator = prototype.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                var flag = new GameObject("Flag");
                flag.transform.SetParent(prototype.transform, false);
                var renderer = flag.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = mesh;
                renderer.sharedMaterial = material;
                renderer.localBounds = new Bounds(new Vector3(1.5f, 2.5f, 0), new Vector3(3, 2, 2));
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Pole";
                pole.transform.SetParent(prototype.transform, false);
                pole.transform.localPosition = new Vector3(0, 1.6f, 0);
                pole.transform.localScale = new Vector3(0.1f, 1.6f, 0.1f);
                UnityEngine.Object.DestroyImmediate(pole.GetComponent<Collider>());
                pole.GetComponent<MeshRenderer>().sharedMaterial = material;
                prototype.AddComponent<GameMarkerData>().markerData = new MarkerData
                {
                    head = "Animation", param = "Animation", index = Array.IndexOf(MarkerData.paramEditor, "Animation"),
                    lastHeadObject = "Animation", value = new AnimationMarkerSettings { animator = animator }
                };
                var prefab = PrefabUtility.SaveAsPrefabAsset(prototype, Folder + "/VAT Flag.prefab");
                UnityEngine.Object.DestroyImmediate(prototype);
                group = new GameObject(GroupName);
                var spawn = UnityEngine.Object.FindObjectsByType<GameMarkerData>(FindObjectsSortMode.None)
                    .FirstOrDefault(m => m.gameObject.scene == scene && m.MarkerHead == "SpawnPoint");
                if (spawn != null)
                {
                    group.transform.position = spawn.transform.position + spawn.transform.right * 7 + spawn.transform.forward * 7;
                    group.transform.rotation = spawn.transform.rotation;
                }
                for (int i = 0; i < 3; i++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
                    instance.name = "VAT Flag " + (i + 1);
                    instance.transform.localPosition = new Vector3(i * 4, 0, 0);
                    var marker = instance.GetComponent<GameMarkerData>();
                    var settings = (AnimationMarkerSettings)marker.markerData.value;
                    settings.clipIndex = i == 2 ? 1 : 0;
                    settings.phase = i / 3f;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(marker);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save TestMap.");
                AssetDatabase.SaveAssets();
            }
            var animation = VertexAnimationBaker.Collect(group.transform, "AnimationTest", "v1.0", _ => false);
            int expectedInstances = group.GetComponentsInChildren<GameMarkerData>()
                .Count(marker => marker.MarkerHead == "Animation");
            if (animation.assets.Count != 1 || expectedInstances == 0 || animation.instances.Count != expectedInstances)
                throw new InvalidOperationException("Test animation instances did not share their baked asset.");
            Directory.CreateDirectory("Temp/AnimationTest");
            File.WriteAllText("Temp/AnimationTest/AnimationTest.json", JsonUtility.ToJson(animation));
            var a = animation.assets[0];
            var pixels = new Texture2D(a.width, a.height, TextureFormat.RGBAHalf, false, true);
            try
            {
                pixels.LoadRawTextureData(Convert.FromBase64String(a.positions));
                pixels.Apply();
                int middle = a.clips[0].frameCount / 2;
                bool changes = Enumerable.Range(0, a.vertexCount).Any(v =>
                    Mathf.Abs(pixels.GetPixel(v, 0).b - pixels.GetPixel(v, middle).b) > 0.01f);
                if (!changes) throw new InvalidOperationException("Flag animation did not deform vertices.");
            }
            finally { UnityEngine.Object.DestroyImmediate(pixels); }
            Selection.activeGameObject = group;
            Debug.Log($"VAT test ready: {ScenePath}; {animation.instances.Count} instances, {a.clips.Length} clips, one shared {a.width}x{a.height} atlas.");
        }

        [MenuItem("Map/Enable physics on test flags")]
        public static void EnableFlagPhysics()
        {
            var path = Folder + "/VAT Flag.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var body = prefab.GetComponent<Rigidbody>();
                if (body == null) body = prefab.AddComponent<Rigidbody>();
                body.mass = 12;
                body.useGravity = true;
                body.isKinematic = false;
                body.linearDamping = 0.05f;
                body.angularDamping = 0.3f;
                body.constraints = RigidbodyConstraints.None;
                body.centerOfMass = new Vector3(0, 0.55f, 0);
                var pole = prefab.GetComponent<CapsuleCollider>();
                if (pole == null) pole = prefab.AddComponent<CapsuleCollider>();
                pole.direction = 1;
                pole.center = new Vector3(0, 1.6f, 0);
                pole.height = 3.2f;
                pole.radius = 0.065f;
                var foot = prefab.GetComponent<BoxCollider>();
                if (foot == null) foot = prefab.AddComponent<BoxCollider>();
                foot.center = new Vector3(0, 0.07f, 0);
                foot.size = new Vector3(0.55f, 0.14f, 0.55f);
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        private static Mesh MakeFlag()
        {
            const int columns = 24, rows = 8;
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var delta = new Vector3[vertices.Length];
            var triangles = new int[columns * rows * 6];
            for (int y = 0; y <= rows; y++) for (int x = 0; x <= columns; x++)
            {
                int v = y * (columns + 1) + x;
                float u = x / (float)columns;
                vertices[v] = new Vector3(u * 3, 1.8f + y / (float)rows * 1.5f, 0);
                uv[v] = new Vector2(u, y / (float)rows);
                normals[v] = Vector3.forward;
                delta[v] = new Vector3(0, 0, Mathf.Sin(u * Mathf.PI * 3 + y * 0.2f) * u * 0.65f);
                if (x == columns || y == rows) continue;
                int t = (y * columns + x) * 6;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + columns + 1;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + columns + 2; triangles[t + 5] = v + columns + 1;
            }
            var mesh = new Mesh { name = "Flag", vertices = vertices, normals = normals, uv = uv, triangles = triangles };
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            AddClothShape(mesh, "Wave", delta);
            for (int y = 0; y <= rows; y++) for (int x = 0; x <= columns; x++)
            {
                int v = y * (columns + 1) + x;
                float u = x / (float)columns;
                delta[v] = new Vector3(0, -0.06f * u, Mathf.Cos(u * Mathf.PI * 3 + y * 0.2f) * u * 0.5f);
            }
            AddClothShape(mesh, "WaveCross", delta);
            return mesh;
        }

        private static void AddClothShape(Mesh mesh, string name, Vector3[] delta)
        {
            var deformed = UnityEngine.Object.Instantiate(mesh);
            try
            {
                deformed.vertices = mesh.vertices.Select((v, i) => v + delta[i]).ToArray();
                deformed.RecalculateNormals();
                deformed.RecalculateTangents();
                var baseNormals = mesh.normals;
                var baseTangents = mesh.tangents;
                var normals = deformed.normals.Select((v, i) => v - baseNormals[i]).ToArray();
                var tangents = deformed.tangents.Select((v, i) => (Vector3)(v - baseTangents[i])).ToArray();
                mesh.AddBlendShapeFrame(name, 100, delta, normals, tangents);
            }
            finally { UnityEngine.Object.DestroyImmediate(deformed); }
        }

        [MenuItem("Map/Upgrade test flags to PBR cloth")]
        public static void UpgradeMaterials()
        {
            var fabric = Folder + "/Fabric030/";
            Texture2D Import(string file, bool normal, bool srgb, bool readable = false)
            {
                var path = fabric + file;
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = srgb;
                importer.isReadable = readable;
                importer.maxTextureSize = 1024;
                importer.anisoLevel = 8;
                importer.mipmapEnabled = true;
                importer.textureCompression = readable ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            var color = Import("Fabric030_1K-PNG_Color.png", false, true);
            var normal = Import("Fabric030_1K-PNG_NormalGL.png", true, false);
            var roughness = Import("Fabric030_1K-PNG_Roughness.png", false, false, true);
            var ao = Import("Fabric030_1K-PNG_AmbientOcclusion.png", false, false, true);
            var mask = new Texture2D(roughness.width, roughness.height, TextureFormat.RGBA32, false, true);
            try
            {
                var r = roughness.GetPixels();
                var a = ao.GetPixels();
                mask.SetPixels(r.Select((pixel, i) => new Color(0, a[i].r, 1, 1 - pixel.r)).ToArray());
                mask.Apply();
                File.WriteAllBytes(fabric + "Fabric030_HDRP_Mask.png", mask.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(mask); }
            var packed = Import("Fabric030_HDRP_Mask.png", false, false);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Flag.mat");
            material.name = "Flag";
            material.SetTexture("_BaseColorMap", color);
            material.SetTexture("_NormalMap", normal);
            material.SetTexture("_MaskMap", packed);
            material.SetColor("_BaseColor", new Color(1, 0.42f, 0.06f));
            material.SetTextureScale("_BaseColorMap", new Vector2(6, 3));
            material.SetFloat("_NormalScale", 0.55f);
            material.SetFloat("_SmoothnessRemapMin", 0.05f);
            material.SetFloat("_SmoothnessRemapMax", 0.35f);
            material.SetFloat("_AORemapMin", 0.5f);
            material.SetFloat("_AORemapMax", 1);
            material.SetFloat("_DoubleSidedEnable", 1);
            material.SetFloat("_DoubleSidedNormalMode", 1);
            material.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
            material.EnableKeyword("_MASKMAP");
            material.EnableKeyword("_DOUBLESIDED_ON");
            UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(material);
            EditorUtility.SetDirty(material);
            string polePath = Folder + "/Pole.mat";
            var metal = AssetDatabase.LoadAssetAtPath<Material>(polePath);
            if (metal == null)
            {
                metal = new Material(Shader.Find("HDRP/Lit")) { name = "Pole" };
                AssetDatabase.CreateAsset(metal, polePath);
            }
            metal.name = "Pole";
            metal.SetColor("_BaseColor", new Color(0.6f, 0.64f, 0.69f));
            metal.SetFloat("_Metallic", 0.92f);
            metal.SetFloat("_Smoothness", 0.62f);
            UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(metal);
            EditorUtility.SetDirty(metal);
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/Flag.asset");
            var improvedMesh = MakeFlag();
            // Mesh setters notify Unity's skinning buffers when the vertex layout changes.
            existingMesh.Clear(false);
            existingMesh.ClearBlendShapes();
            existingMesh.vertices = improvedMesh.vertices;
            existingMesh.normals = improvedMesh.normals;
            existingMesh.uv = improvedMesh.uv;
            existingMesh.tangents = improvedMesh.tangents;
            existingMesh.triangles = improvedMesh.triangles;
            var deltaVertices = new Vector3[improvedMesh.vertexCount];
            var deltaNormals = new Vector3[improvedMesh.vertexCount];
            var deltaTangents = new Vector3[improvedMesh.vertexCount];
            for (int shape = 0; shape < improvedMesh.blendShapeCount; shape++)
            {
                improvedMesh.GetBlendShapeFrameVertices(shape, 0, deltaVertices, deltaNormals, deltaTangents);
                existingMesh.AddBlendShapeFrame(improvedMesh.GetBlendShapeName(shape), 100, deltaVertices, deltaNormals, deltaTangents);
            }
            existingMesh.RecalculateBounds();
            existingMesh.name = "Flag";
            UnityEngine.Object.DestroyImmediate(improvedMesh);
            EditorUtility.SetDirty(existingMesh);
            var prefab = PrefabUtility.LoadPrefabContents(Folder + "/VAT Flag.prefab");
            try
            {
                var flagRenderer = prefab.transform.Find("Flag").GetComponent<SkinnedMeshRenderer>();
                flagRenderer.sharedMesh = null;
                flagRenderer.sharedMesh = existingMesh;
                flagRenderer.SetBlendShapeWeight(0, 50);
                flagRenderer.SetBlendShapeWeight(1, 100);
                prefab.transform.Find("Pole").GetComponent<MeshRenderer>().sharedMaterial = metal;
                foreach (var item in new[] { ("Foot", new Vector3(0, 0.07f, 0), new Vector3(0.55f, 0.07f, 0.55f)),
                    ("Finial", new Vector3(0, 3.3f, 0), new Vector3(0.17f, 0.17f, 0.17f)) })
                {
                    if (prefab.transform.Find(item.Item1) != null) continue;
                    var part = GameObject.CreatePrimitive(item.Item1 == "Foot" ? PrimitiveType.Cylinder : PrimitiveType.Sphere);
                    part.name = item.Item1;
                    part.transform.SetParent(prefab.transform, false);
                    part.transform.localPosition = item.Item2;
                    part.transform.localScale = item.Item3;
                    UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
                    part.GetComponent<MeshRenderer>().sharedMaterial = metal;
                }
                PrefabUtility.SaveAsPrefabAsset(prefab, Folder + "/VAT Flag.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            foreach (var clipName in new[] { "Wave", "Flutter" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/" + clipName + ".anim");
                float duration = clipName == "Wave" ? 2f : 0.8f;
                for (int channel = 0; channel < 2; channel++)
                {
                    var curve = new AnimationCurve();
                    for (int i = 0; i <= 8; i++)
                    {
                        float angle = i / 8f * Mathf.PI * 2 + channel * Mathf.PI / 2;
                        float slope = Mathf.Cos(angle) * 50 * Mathf.PI * 2 / duration;
                        curve.AddKey(new Keyframe(i / 8f * duration, 50 + 50 * Mathf.Sin(angle), slope, slope));
                    }
                    clip.SetCurve("Flag", typeof(SkinnedMeshRenderer), channel == 0 ? "blendShape.Wave" : "blendShape.WaveCross", curve);
                }
                EditorUtility.SetDirty(clip);
            }
            AssetDatabase.SaveAssets();
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.sharedMesh != existingMesh) continue;
                renderer.sharedMesh = null;
                renderer.sharedMesh = existingMesh;
            }
            Create();
        }
    }
}
