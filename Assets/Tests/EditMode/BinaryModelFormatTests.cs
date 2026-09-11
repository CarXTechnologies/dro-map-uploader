using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Plugins.CarX.Modding.Creator.Runtime;
using Plugins.CarX.Modding.Creator.Editor;
using UnityEngine;

namespace MapUploader.Tests
{
    public class BinaryModelFormatTests
    {
        private string path;
        [SetUp] public void SetUp() => path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cxmesh");
        [TearDown] public void TearDown() { if (File.Exists(path)) File.Delete(path); }

        private static BinaryModModel Fixture() => new BinaryModModel {
            name = "material-group", meshes = new[] { new BinaryModMesh {
                name = "Object (1)", castShadows = false,
                vertices = new[] { Vector3.zero, Vector3.right, Vector3.up },
                normals = Enumerable.Repeat(Vector3.forward, 3).ToArray(),
                uvs = new[] { new Vector2(-2, 30), Vector2.right, Vector2.up },
                colors = new[] { Color.red, Color.green, Color.blue },
                subMeshes = new[] {
                    new BinaryModSubMesh { material = "material-a", indices = new[] { 0, 1, 2 } },
                    new BinaryModSubMesh { material = "material-b", indices = new[] { 2, 1, 0 } }
                }
            } }
        };

        [Test] public void PreservesGeometryUvColorsAndDistinctMaterials()
        {
            var source = Fixture();
            BinaryModModelCodec.Write(path, source);
            var actual = BinaryModModelCodec.Read(path);
            Assert.AreEqual(source.name, actual.name);
            var a = actual.meshes.Single(); var s = source.meshes.Single();
            Assert.AreEqual(s.name, a.name); Assert.IsFalse(a.castShadows);
            CollectionAssert.AreEqual(s.vertices, a.vertices);
            CollectionAssert.AreEqual(s.normals, a.normals);
            CollectionAssert.AreEqual(s.uvs, a.uvs);
            CollectionAssert.AreEqual(s.colors, a.colors);
            for (int i = 0; i < 2; i++) {
                Assert.AreEqual(s.subMeshes[i].material, a.subMeshes[i].material);
                CollectionAssert.AreEqual(s.subMeshes[i].indices, a.subMeshes[i].indices);
            }
        }
        [Test] public void RejectsUnknownVersion()
        {
            BinaryModModelCodec.Write(path, Fixture());
            var bytes = File.ReadAllBytes(path); bytes[4] = 99; File.WriteAllBytes(path, bytes);
            Assert.Throws<InvalidDataException>(() => BinaryModModelCodec.Read(path));
        }
        [Test] public void RejectsTruncatedData()
        {
            BinaryModModelCodec.Write(path, Fixture());
            var bytes = File.ReadAllBytes(path); File.WriteAllBytes(path, bytes.Take(bytes.Length - 2).ToArray());
            Assert.Throws<InvalidDataException>(() => BinaryModModelCodec.Read(path));
        }
        [Test] public void RejectsInvalidIndices()
        {
            var model = Fixture(); model.meshes[0].subMeshes[0].indices[0] = 3;
            Assert.Throws<InvalidDataException>(() => BinaryModModelCodec.Write(path, model));
            WriteV1(model);
            Assert.Throws<InvalidDataException>(() => BinaryModModelCodec.Read(path));
        }
        [Test] public void RejectsNonFiniteVertices()
        {
            var model = Fixture(); model.meshes[0].vertices[0] = new Vector3(float.NaN, 0, 0);
            BinaryModModelCodec.Write(path, model);
            Assert.Throws<InvalidDataException>(() => BinaryModModelCodec.Read(path));
        }
        [Test] public void PreservesSerializedFormatNumbers()
        {
            Assert.AreEqual(2, Enum.GetValues(typeof(FormatBuild)).Length);
            Assert.AreEqual(1, (int)FormatBuild.Wavefront);
            Assert.AreEqual(2, (int)FormatBuild.Binary);
        }

        [Test] public void ReadsExistingVersionOneFiles()
        {
            var model = Fixture(); WriteV1(model);
            var actual = BinaryModModelCodec.Read(path).meshes[0];
            CollectionAssert.AreEqual(model.meshes[0].vertices, actual.vertices);
            CollectionAssert.AreEqual(model.meshes[0].colors, actual.colors);
            CollectionAssert.AreEqual(model.meshes[0].uvs, actual.uvs);
            Assert.AreEqual("material-b", actual.subMeshes[1].material);
        }

        [Test] public void CollidersOmitRenderAttributesAndPreserveGeometry()
        {
            var model = Fixture(); model.meshes[0].isCollider = true;
            model.meshes[0].normals = null; model.meshes[0].uvs = null; model.meshes[0].colors = null;
            BinaryModModelCodec.Write(path, model);
            var actual = BinaryModModelCodec.Read(path).meshes[0];
            Assert.IsTrue(actual.isCollider);
            Assert.IsEmpty(actual.normals); Assert.IsEmpty(actual.uvs); Assert.IsEmpty(actual.colors);
            CollectionAssert.AreEqual(model.meshes[0].vertices, actual.vertices);
            CollectionAssert.AreEqual(model.meshes[0].subMeshes[1].indices, actual.subMeshes[1].indices);
        }

        [Test] public void PreservesSmallAttributeDifferencesAndSignedZero()
        {
            var model = Fixture(); var mesh = model.meshes[0];
            mesh.normals[1].z = 1.0000001f;
            mesh.uvs = new[] { Vector2.zero, new Vector2(BitConverter.Int32BitsToSingle(int.MinValue), 0), new Vector2(0.00000001f, 0) };
            mesh.colors = Enumerable.Repeat(new Color(2.5f, 0.1234567f, 0, 1), 3).ToArray();
            BinaryModModelCodec.Write(path, model);
            var actual = BinaryModModelCodec.Read(path).meshes[0];
            for (int i = 0; i < 3; i++) {
                Assert.AreEqual(BitConverter.SingleToInt32Bits(mesh.uvs[i].x), BitConverter.SingleToInt32Bits(actual.uvs[i].x));
                Assert.AreEqual(BitConverter.SingleToInt32Bits(mesh.normals[i].z), BitConverter.SingleToInt32Bits(actual.normals[i].z));
                Assert.AreEqual(mesh.colors[i], actual.colors[i]);
            }
        }

        [Test] public void SupportsBothIndexWidthsAtBoundary()
        {
            foreach (int count in new[] { 65536, 65537 }) {
                var model = Fixture(); var mesh = model.meshes[0];
                mesh.vertices = new Vector3[count]; mesh.normals = new Vector3[count];
                mesh.uvs = new Vector2[count]; mesh.colors = Enumerable.Repeat(Color.white, count).ToArray();
                mesh.subMeshes = new[] { new BinaryModSubMesh { material = "a", indices = new[] { 0, count - 1, 1 } } };
                BinaryModModelCodec.Write(path, model);
                var actual = BinaryModModelCodec.Read(path).meshes[0];
                Assert.AreEqual(count - 1, actual.subMeshes[0].indices[1]);
                Assert.AreEqual(count, actual.vertices.Length);
                Assert.Less(new FileInfo(path).Length, count * 4L, "Repeated attributes and positions should compress");
            }
        }

        [Test] public void RejectsChecksumMismatch()
        {
            var model = Fixture(); BinaryModModelCodec.Write(path, model);
            var bytes = File.ReadAllBytes(path);
            int block = 8 + 4 + Encoding.UTF8.GetByteCount(model.name) + 4;
            bytes[block + 9] ^= 1; // CRC of the uncompressed mesh, not the payload.
            File.WriteAllBytes(path, bytes);
            Assert.Throws<InvalidDataException>(() => BinaryModModelCodec.Read(path));
        }

        [Test] public void RejectsDecompressionBeyondDeclaredSize()
        {
            var model = Fixture(); BinaryModModelCodec.Write(path, model);
            var bytes = File.ReadAllBytes(path);
            int block = 8 + 4 + Encoding.UTF8.GetByteCount(model.name) + 4;
            Assert.AreEqual(1, bytes[block], "Fixture should use Deflate");
            Array.Copy(BitConverter.GetBytes(1), 0, bytes, block + 1, 4);
            File.WriteAllBytes(path, bytes);
            Assert.Throws<InvalidDataException>(() => BinaryModModelCodec.Read(path));
        }

        [Test] public void RejectsOversizedDecompressionAllocation()
        {
            var model = Fixture(); BinaryModModelCodec.Write(path, model);
            var bytes = File.ReadAllBytes(path);
            int block = 8 + 4 + Encoding.UTF8.GetByteCount(model.name) + 4;
            Array.Copy(BitConverter.GetBytes(int.MaxValue), 0, bytes, block + 1, 4);
            File.WriteAllBytes(path, bytes);
            Assert.Throws<InvalidDataException>(() => BinaryModModelCodec.Read(path));
        }

        [Test] public void ReadsMultipleIndependentMeshBlocks()
        {
            var model = Fixture(); var second = Fixture().meshes[0]; second.name = "second";
            second.isCollider = true; model.meshes = new[] { model.meshes[0], second };
            BinaryModModelCodec.Write(path, model);
            var actual = BinaryModModelCodec.Read(path);
            Assert.AreEqual(2, actual.meshes.Length);
            Assert.AreEqual("second", actual.meshes[1].name);
            Assert.IsTrue(actual.meshes[1].isCollider);
            Assert.IsFalse(actual.meshes[0].isCollider);
        }

        [Test] public void DeduplicatesExactPbrCopiesWithoutMergingDifferentTextures()
        {
            string directory = path + "-textures";
            Directory.CreateDirectory(directory);
            try {
                File.WriteAllBytes(Path.Combine(directory, "a_base.png"), new byte[] { 1, 2, 3 });
                File.WriteAllBytes(Path.Combine(directory, "a_pbr.png"), new byte[] { 1, 2, 3 });
                File.WriteAllBytes(Path.Combine(directory, "a_1_pbr.png"), new byte[] { 1, 2, 4 });
                File.WriteAllBytes(Path.Combine(directory, "protected_pbr.png"), new byte[] { 1, 2, 3 });
                File.WriteAllText(Path.Combine(directory, "a.mtl"), "map_Kd protected_pbr.png\n");
                var material = new ModPbrMaterial { layers = new[] {
                    new ModPbrLayer { diffuse = "a_pbr.png", uv = new Vector4(30, 20, 1, 2) },
                    new ModPbrLayer { diffuse = "a_1_pbr.png" }
                } };
                string json = Path.Combine(directory, "a.pbr.json");
                File.WriteAllText(json, JsonUtility.ToJson(material));
                Assert.AreEqual(3, UnityGoObjExporter.DeduplicatePbrTextures(directory));
                var actual = JsonUtility.FromJson<ModPbrMaterial>(File.ReadAllText(json));
                Assert.AreEqual("a_base.png", actual.layers[0].diffuse);
                Assert.AreEqual(material.layers[0].uv, actual.layers[0].uv);
                Assert.AreEqual("a_1_pbr.png", actual.layers[1].diffuse);
                Assert.IsFalse(File.Exists(Path.Combine(directory, "a_pbr.png")));
                Assert.IsTrue(File.Exists(Path.Combine(directory, "protected_pbr.png")));
                Assert.AreEqual(0, UnityGoObjExporter.DeduplicatePbrTextures(directory), "Second pass must be idempotent");
            }
            finally { foreach (string file in Directory.GetFiles(directory)) File.Delete(file); Directory.Delete(directory); }
        }

        private void WriteV1(BinaryModModel model)
        {
            using var writer = new BinaryWriter(File.Create(path));
            void Text(string text) { var bytes = Encoding.UTF8.GetBytes(text); writer.Write(bytes.Length); writer.Write(bytes); }
            writer.Write(0x4D425843u); writer.Write(1); Text(model.name); writer.Write(model.meshes.Length);
            foreach (var mesh in model.meshes) {
                Text(mesh.name); writer.Write(mesh.castShadows); writer.Write(mesh.vertices.Length);
                for (int i = 0; i < mesh.vertices.Length; i++) {
                    var v = mesh.vertices[i]; var n = mesh.normals[i]; var uv = mesh.uvs[i]; var c = mesh.colors[i];
                    foreach (float value in new[] { v.x, v.y, v.z, n.x, n.y, n.z, uv.x, uv.y, c.r, c.g, c.b, c.a }) writer.Write(value);
                }
                writer.Write(mesh.subMeshes.Length);
                foreach (var sub in mesh.subMeshes) { Text(sub.material); writer.Write(sub.indices.Length); foreach (int index in sub.indices) writer.Write(index); }
            }
        }
    }
}
