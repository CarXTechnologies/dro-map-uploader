using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Plugins.CarX.Modding.Creator.Runtime;
using Plugins.CarX.Modding.Creator.Editor;
using UnityEngine;

namespace MapUploader.Tests
{
    public class BinaryMapContainerTests
    {
        private string directory;
        private string path;
        [SetUp] public void SetUp() { directory = Path.Combine(Path.GetTempPath(), "cxmod-test-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); path = Path.Combine(directory, BinaryModArchive.FileName); }
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        private static KeyValuePair<string, byte[]> Resource(string name, byte[] bytes) => new(name, bytes);

        [Test] public void ModContainerCarriesMapTypeInExistingMetadata()
        {
            Assert.AreEqual("mod.cxmod", BinaryModArchive.FileName);
            var meta = new ModMeta { id = "test-map" };
            Assert.AreEqual("map", meta.contentType);
            Assert.AreEqual("map", JsonUtility.FromJson<ModMeta>(JsonUtility.ToJson(meta)).contentType);
            BinaryModArchive.Write(path, new[] { Resource("test-map.json", BinaryModData.Write(meta)) });
            var provider = new DefaultFileProvider(directory);
            var loaded = BinaryModData.Read<ModMeta>(provider.LoadAsync("test-map", ".json").GetAwaiter().GetResult());
            Assert.AreEqual("map", loaded.contentType);
            Assert.AreEqual("test-map", loaded.id);
            Assert.AreEqual(1, provider.GetAllFilesPath(directory).Length);
        }

        [Test] public void StoresAnimationAsRawBytesAndPreservesTransforms()
        {
            var payload = Enumerable.Range(0, 64).Select(n => (byte)n).ToArray();
            var data = new AnimationMeta { id = "flags", version = "1", assets = new List<VertexAnimationAsset> {
                new() { name = "flag", width = 2, height = 4, vertexCount = 3, positions = Convert.ToBase64String(payload), normals = Convert.ToBase64String(payload),
                    bounds = new Bounds(new Vector3(-2, 3, 4), new Vector3(6, 7, 8)), vertices = new[] { Vector3.one }, uv = new[] { new Vector2(30, -2) },
                    surfaces = new[] { new VertexAnimationSurface { diffusePng = Convert.ToBase64String(payload), textureScale = new Vector2(30, 30), textureOffset = new Vector2(-1, 2) } } }
            }, instances = new List<VertexAnimationInstance> { new() { rigidbodyId = 3, localToWorld = new LToWorld(Vector3.one, Quaternion.identity, new Vector3(-2, 1, 1)) } } };
            var actual = BinaryModData.Read<AnimationMeta>(BinaryModData.Write(data));
            Assert.IsNull(actual.assets[0].positions);
            CollectionAssert.AreEqual(payload, actual.assets[0].positionsBytes);
            CollectionAssert.AreEqual(payload, actual.assets[0].surfaces[0].diffuseBytes);
            Assert.AreEqual(data.assets[0].bounds, actual.assets[0].bounds);
            Assert.AreEqual(data.assets[0].surfaces[0].textureScale, actual.assets[0].surfaces[0].textureScale);
            Assert.AreEqual(data.instances[0].localToWorld.scale, actual.instances[0].localToWorld.scale);
            // A binary document can be repacked without reconstructing Base64 strings.
            CollectionAssert.AreEqual(BinaryModData.Write(data), BinaryModData.Write(actual));
        }
        [Test] public void PreservesThreeLayerMaterialIncludingTinyFloatDifferences()
        {
            var material = new ModPbrMaterial { layers = Enumerable.Range(0, 3).Select(i => new ModPbrLayer {
                diffuse = "Object (" + i + ").png", uv = new Vector4(30, -30, .00000001f * i, 2), color = new Color(4, .2f, .3f, 1),
                metallicRemap = new Vector2(.1f, .9f), normalScale = .7f }).ToArray(), vertexBlend = 1, blendMask = "blend.png", doubleSided = true };
            var actual = BinaryModData.Read<ModPbrMaterial>(BinaryModData.Write(material));
            Assert.AreEqual(JsonUtility.ToJson(material), JsonUtility.ToJson(actual));
        }
        [Test] public void PreservesUnusedLodSentinelsButRejectsNonFiniteGeometry()
        {
            var data = new LodHierarchyMeta("map", "1", new List<LodInstance> { new() { LODDistances0 = new Vector4(10, 20, float.PositiveInfinity, float.PositiveInfinity) } });
            var actual = BinaryModData.Read<LodHierarchyMeta>(BinaryModData.Write(data));
            Assert.AreEqual(data.lodInstances[0].LODDistances0, actual.lodInstances[0].LODDistances0);
            data.lodInstances[0].LocalReferencePoint = new Vector3(float.PositiveInfinity, 0, 0);
            Assert.Throws<InvalidDataException>(() => BinaryModData.Write(data));
        }
        [Test] public void RejectsWrongTypeVersionTruncationAndTrailingData()
        {
            var bytes = BinaryModData.Write(new ModMeta { id = "map" });
            Assert.Throws<InvalidDataException>(() => BinaryModData.Read<AnimationMeta>(bytes));
            var future = (byte[])bytes.Clone(); future[4] = 99;
            Assert.Throws<InvalidDataException>(() => BinaryModData.Read<ModMeta>(future));
            Assert.Catch<IOException>(() => BinaryModData.Read<ModMeta>(bytes.Take(bytes.Length - 1).ToArray()));
            Assert.Throws<InvalidDataException>(() => BinaryModData.Read<ModMeta>(bytes.Concat(new byte[1]).ToArray()));
        }
        [Test] public void DeduplicatesPayloadsAndReadsWithoutExtracting()
        {
            var bytes = new byte[4096]; new System.Random(42).NextBytes(bytes);
            BinaryModArchive.Write(path, new[] { Resource("textures/a.png", bytes), Resource("textures/b.png", bytes), Resource("map.json", BinaryModData.Write(new ModMeta { id = "map" })) });
            Assert.Less(new FileInfo(path).Length, 6000);
            var provider = new DefaultFileProvider(directory);
            CollectionAssert.AreEqual(bytes, provider.LoadAsync("textures/b", ".png").GetAwaiter().GetResult());
            Assert.AreEqual(2, provider.GetAllFilesPath(Path.Combine(directory, "textures")).Length);
            Assert.AreEqual(1, Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length);
        }
        [Test] public void RejectsCorruptionAndTruncatedIndex()
        {
            BinaryModArchive.Write(path, new[] { Resource("a.png", new byte[256]) });
            var bytes = File.ReadAllBytes(path); bytes[16] ^= 1; File.WriteAllBytes(path, bytes);
            Assert.Throws<InvalidDataException>(() => new BinaryModArchive(path).Read("a.png"));
            File.WriteAllBytes(path, bytes.Take(bytes.Length - 1).ToArray());
            var error = Assert.Catch(() => new BinaryModArchive(path));
            Assert.IsTrue(error is InvalidDataException || error is EndOfStreamException);
        }
        [Test] public void RejectsTraversalAndCaseCollisionsWithoutReplacingGoodMap()
        {
            BinaryModArchive.Write(path, new[] { Resource("a.png", new byte[] { 42 }) });
            Assert.Throws<InvalidDataException>(() => BinaryModArchive.Write(path, new[] { Resource("../escape", new byte[1]) }));
            Assert.Throws<InvalidDataException>(() => BinaryModArchive.Write(path, new[] { Resource("a.png", new byte[1]), Resource("A.png", new byte[2]) }));
            CollectionAssert.AreEqual(new byte[] { 42 }, new BinaryModArchive(path).Read("a.png"));
        }
        [Test] public void RejectsExpandedPayloadPastItsDeclaredLimit()
        {
            BinaryModArchive.Write(path, new[] { Resource("compressed", new byte[65536]) });
            using (var file = File.Open(path, FileMode.Open, FileAccess.ReadWrite))
            using (var reader = new BinaryReader(file))
            using (var writer = new BinaryWriter(file))
            {
                file.Position = 8; long index = reader.ReadInt64();
                file.Position = index + 4; int nameLength = reader.ReadUInt16();
                file.Position += nameLength + 8 + 4;
                writer.Write(1); // The Deflate payload still expands to 65536 bytes.
            }
            Assert.Throws<InvalidDataException>(() => new BinaryModArchive(path).Read("compressed"));
        }
        [Test] public void ReadsLatestContainerAfterReplacement()
        {
            BinaryModArchive.Write(path, new[] { Resource("textures/a.png", new byte[] { 1 }) });
            CollectionAssert.AreEqual(new byte[] { 1 }, ModResourceFiles.ReadAllBytes(Path.Combine(directory, "textures/a.png")));
            BinaryModArchive.Write(path, new[] { Resource("textures/a.png", new byte[] { 2, 3 }) });
            CollectionAssert.AreEqual(new byte[] { 2, 3 }, ModResourceFiles.ReadAllBytes(Path.Combine(directory, "textures/a.png")));
        }
        [Test] public void ContainerOverridesStaleLooseFilesAndNeverFallsBackForMissingResources()
        {
            File.WriteAllText(Path.Combine(directory, "map.json"), "stale");
            File.WriteAllText(Path.Combine(directory, "missing.json"), "stale");
            var bytes = BinaryModData.Write(new ModMeta { id = "new" });
            BinaryModArchive.Write(path, new[] { Resource("map.json", bytes) });
            var provider = new DefaultFileProvider(directory);
            CollectionAssert.AreEqual(bytes, provider.LoadAsync("map", ".json").GetAwaiter().GetResult());
            Assert.IsEmpty(provider.LoadAsync("missing", ".json").GetAwaiter().GetResult());
            Assert.AreEqual(1, provider.GetAllFilesPath(directory).Length);
        }
        [Test] public void PackerSharesGeometryAcrossDifferentMaterialsAndPreservesTiling()
        {
            string staging = Path.Combine(directory, "staging"); Directory.CreateDirectory(Path.Combine(staging, "models"));
            File.WriteAllText(Path.Combine(staging, "map.json"), JsonUtility.ToJson(new ModMeta { id = "map" }));
            for (int i = 0; i < 2; i++)
            {
                var mesh = new BinaryModMesh { name = "Object (" + i + ")", vertices = new[] { Vector3.zero, Vector3.right, Vector3.up },
                    normals = Enumerable.Repeat(Vector3.forward, 3).ToArray(), uvs = new[] { Vector2.zero, Vector2.right, Vector2.up }, colors = Enumerable.Repeat(Color.white, 3).ToArray(),
                    subMeshes = new[] { new BinaryModSubMesh { material = "material-" + i, indices = new[] { 0, 1, 2 } } } };
                BinaryModModelCodec.Write(Path.Combine(staging, "models", i + ".cxmesh"), new BinaryModModel { name = "group-" + i, meshes = new[] { mesh } });
                File.WriteAllText(Path.Combine(staging, "models", i + ".mtl"), "newmtl material-" + i + "\nKd 1 0.5 0.2\nmap_Kd -s 30 40 1 -o -2 3 0 texture " + i + ".png\n");
                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                try { texture.SetPixels(Enumerable.Repeat(i == 0 ? Color.red : Color.blue, 16).ToArray()); texture.Apply(); File.WriteAllBytes(Path.Combine(staging, "models", "texture " + i + ".png"), texture.EncodeToPNG()); }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            BinaryMapPacker.Pack(path, BinaryTextureEncoding.Rgba32, staging);
            var archive = new BinaryModArchive(path);
            Assert.AreEqual("map", BinaryModData.Read<ModMeta>(archive.Read("map.json")).contentType);
            Assert.AreEqual(1, archive.Names.Count(n => n.EndsWith(".cxgeom")));
            var first = BinaryModData.Read<BinaryGeometryGroup>(archive.Read("models/0.cxmesh"));
            var second = BinaryModData.Read<BinaryGeometryGroup>(archive.Read("models/1.cxmesh"));
            Assert.AreEqual(first.bindings[0].geometry, second.bindings[0].geometry);
            Assert.AreNotEqual(first.bindings[0].materials[0], second.bindings[0].materials[0]);
            var material = BinaryModData.Read<BinaryMaterialLibrary>(archive.Read("models/1.mtl")).materials[0];
            Assert.AreEqual(new Vector4(30, 40, -2, 3), material.uv);
            string firstTexture = BinaryModData.Read<BinaryMaterialLibrary>(archive.Read("models/0.mtl")).materials[0].properties.Single(p => p.type == BinaryMaterialPropertyType.Texture).texture;
            string secondTexture = material.properties.Single(p => p.type == BinaryMaterialPropertyType.Texture).texture;
            Assert.AreNotEqual(firstTexture, secondTexture);
            Assert.IsFalse(archive.Contains("models/texture 1.png"));
            Assert.IsTrue(BinaryTexture.IsBinary(archive.Read("models/" + secondTexture)));
        }
    }
}
