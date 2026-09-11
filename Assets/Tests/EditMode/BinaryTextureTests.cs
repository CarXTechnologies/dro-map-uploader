using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Plugins.CarX.Modding.Creator.Runtime;
using Plugins.CarX.Modding.Creator.Editor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MapUploader.Tests
{
    public class BinaryTextureTests
    {
        private string directory;
        [SetUp] public void SetUp() { directory = Path.Combine(Path.GetTempPath(), "cxtex-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); }
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        private static byte[] Png(Color32 color, int width = 4, int height = 4)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            try { texture.SetPixels32(Enumerable.Repeat(color, width * height).ToArray()); texture.Apply(); return texture.EncodeToPNG(); }
            finally { Object.DestroyImmediate(texture); }
        }
        private static Color32 FirstPixel(byte[] bytes) { Assert.AreEqual(BinaryTextureEncoding.Rgba32, BinaryTexture.Inspect(bytes).encoding); return new Color32(bytes[28], bytes[29], bytes[30], bytes[31]); }
        [Test] public void LosslessTexturePreservesPixelsDimensionsColorSpaceAndMips()
        {
            var pixel = new Color32(12, 129, 234, 57);
            var bytes = BinaryTextureBaker.Encode(Png(pixel, 8, 4), true, BinaryTextureEncoding.Rgba32);
            var info = BinaryTexture.Inspect(bytes);
            Assert.AreEqual(8, info.width); Assert.AreEqual(4, info.height); Assert.AreEqual(4, info.mipCount);
            Assert.IsTrue(info.linear); Assert.AreEqual(pixel, FirstPixel(bytes));
            // Inspect every prepared mip: a constant texture must remain constant.
            for (int offset = 28; offset < bytes.Length; offset += 4) CollectionAssert.AreEqual(new byte[] { pixel.r, pixel.g, pixel.b, pixel.a }, bytes.Skip(offset).Take(4));
            var texture = BinaryTexture.Load(bytes, true);
            try { Assert.AreEqual(4, texture.mipmapCount); Assert.IsFalse(texture.isReadable); Assert.IsFalse(texture.isDataSRGB); }
            finally { Object.DestroyImmediate(texture); }
            Assert.Throws<InvalidDataException>(() => BinaryTexture.Load(bytes, false));
        }
        [Test] public void Bc7LoadsPreparedMipChainAndUnalignedImagesRemainLossless()
        {
            var bytes = BinaryTextureBaker.Encode(Png(new Color32(55, 123, 224, 255), 8, 8), false, BinaryTextureEncoding.Bc7);
            var info = BinaryTexture.Inspect(bytes);
            Assert.AreEqual(BinaryTextureEncoding.Bc7, info.encoding); Assert.AreEqual(4, info.mipCount);
            Assert.AreEqual(112, info.byteCount);
            var texture = BinaryTexture.Load(bytes, false);
            try { Assert.AreEqual(TextureFormat.BC7, texture.format); Assert.IsTrue(texture.isDataSRGB); Assert.IsFalse(texture.isReadable); }
            finally { Object.DestroyImmediate(texture); }
            var unaligned = BinaryTextureBaker.Encode(Png(new Color32(1, 2, 3, 4), 7, 3), true, BinaryTextureEncoding.Bc7);
            Assert.AreEqual(BinaryTextureEncoding.Rgba32, BinaryTexture.Inspect(unaligned).encoding);
            Assert.AreEqual(new Color32(1, 2, 3, 4), FirstPixel(unaligned));
        }
        [Test] public void RejectsMalformedTextureBeforeCreatingGpuResource()
        {
            var bytes = BinaryTextureBaker.Encode(Png(Color.white), true, BinaryTextureEncoding.Rgba32);
            foreach (int offset in new[] { 4, 8, 12, 16, 20, 24 })
            {
                var bad = (byte[])bytes.Clone(); Array.Copy(BitConverter.GetBytes(int.MaxValue), 0, bad, offset, 4);
                Assert.Throws<InvalidDataException>(() => BinaryTexture.Inspect(bad), "header offset " + offset);
            }
            Assert.Throws<InvalidDataException>(() => BinaryTexture.Inspect(bytes.Take(bytes.Length - 1).ToArray()));
            Assert.Throws<InvalidDataException>(() => BinaryTexture.Inspect(bytes.Concat(new byte[1]).ToArray()));
            Assert.Throws<InvalidDataException>(() => BinaryTexture.PayloadSize(16384, 16384, 15, BinaryTextureEncoding.Rgba32));
        }
        [Test] public void Bc7GpuSamplingPreservesColorNormalChannelsAndAlphaWithinCompressionTolerance()
        {
            var expected = Enumerable.Range(0, 256).Select(i => new Color32((byte)(i % 16 * 16), (byte)(i / 16 * 16), 128, (byte)(32 + i % 16 * 14))).ToArray();
            foreach (bool linear in new[] { false, true })
            {
                var source = new Texture2D(16, 16, TextureFormat.RGBA32, false, linear);
                Texture2D decoded = null, readback = null; RenderTexture target = null;
                var previous = RenderTexture.active; bool previousSrgb = GL.sRGBWrite;
                try
                {
                    source.SetPixels32(expected); source.Apply();
                    decoded = BinaryTexture.Load(BinaryTextureBaker.Encode(source, linear, BinaryTextureEncoding.Bc7), linear);
                    target = RenderTexture.GetTemporary(16, 16, 0, RenderTextureFormat.ARGB32, linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
                    GL.sRGBWrite = !linear; Graphics.Blit(decoded, target); RenderTexture.active = target;
                    readback = new Texture2D(16, 16, TextureFormat.RGBA32, false, linear);
                    readback.ReadPixels(new Rect(0, 0, 16, 16), 0, 0, false); readback.Apply(false, false);
                    var actual = readback.GetPixels32(); double sum = 0; int maximum = 0;
                    for (int i = 0; i < expected.Length; i++)
                        foreach (int error in new[] { Math.Abs(actual[i].r - expected[i].r), Math.Abs(actual[i].g - expected[i].g), Math.Abs(actual[i].b - expected[i].b), Math.Abs(actual[i].a - expected[i].a) })
                        { sum += error; maximum = Math.Max(maximum, error); }
                    Assert.Less(sum / (expected.Length * 4), 4, "Mean channel error, linear=" + linear);
                    Assert.Less(maximum, 20, "Maximum channel error, linear=" + linear);
                }
                finally
                {
                    GL.sRGBWrite = previousSrgb; RenderTexture.active = previous;
                    if (target != null) RenderTexture.ReleaseTemporary(target);
                    if (decoded != null) Object.DestroyImmediate(decoded); if (readback != null) Object.DestroyImmediate(readback); Object.DestroyImmediate(source);
                }
            }
        }
        [Test] public void TextureProviderLoadsLegacyPngAndPreparedBinary()
        {
            byte[] png = Png(Color.red);
            var provider = new TexturePngProvider(new DefaultFileProvider(directory));
            var legacy = provider.Unpack(png).GetAwaiter().GetResult();
            var prepared = provider.Unpack(BinaryTextureBaker.Encode(png, true, BinaryTextureEncoding.Rgba32)).GetAwaiter().GetResult();
            try { Assert.AreEqual(legacy.width, prepared.width); Assert.AreEqual(legacy.height, prepared.height); Assert.IsTrue(legacy.isReadable); Assert.IsFalse(prepared.isReadable); }
            finally { Object.DestroyImmediate(legacy); Object.DestroyImmediate(prepared); }
        }
        private void Staging()
        {
            Directory.CreateDirectory(Path.Combine(directory, "models"));
            File.WriteAllText(Path.Combine(directory, "map.json"), JsonUtility.ToJson(new ModMeta { id = "map" }));
            BinaryModModelCodec.Write(Path.Combine(directory, "models/a.cxmesh"), new BinaryModModel { name = "a", meshes = new[] { new BinaryModMesh {
                name = "a", vertices = new[] { Vector3.zero, Vector3.up, Vector3.right }, normals = Enumerable.Repeat(Vector3.forward, 3).ToArray(),
                uvs = new Vector2[3], colors = Enumerable.Repeat(Color.white, 3).ToArray(), subMeshes = new[] { new BinaryModSubMesh { material = "a", indices = new[] { 0, 1, 2 } } }
            } } });
        }
        [Test] public void PacksSurfaceChannelsAndSeparatesLinearFromSrgbReferences()
        {
            Staging();
            File.WriteAllBytes(Path.Combine(directory, "models/shared.png"), Png(new Color32(128, 64, 255, 90)));
            File.WriteAllText(Path.Combine(directory, "models/a.mtl"), "newmtl a\nmap_Kd -s 30 40 1 -o 2 3 0 shared.png\nmap_Kn shared.png\nmap_Pr shared.png\nmap_Pm shared.png\nmap_d shared.png\nPr 0.5\nPm 0.25\n");
            string target = Path.Combine(directory, "mod.cxmod"); BinaryMapPacker.Pack(target, BinaryTextureEncoding.Rgba32, directory);
            var archive = new BinaryModArchive(target); Assert.AreEqual(2, archive.FormatVersion);
            var material = BinaryModData.Read<BinaryMaterialLibrary>(archive.Read("models/a.mtl")).materials[0];
            Assert.AreEqual(new Vector4(30, 40, 2, 3), material.uv); Assert.IsTrue(material.alphaTexture);
            Assert.AreEqual(3, material.properties.Length);
            byte[] Read(string semantic) => archive.Read("models/" + material.properties.Single(p => p.semantic == semantic).texture);
            Assert.IsFalse(BinaryTexture.Inspect(Read("map_Kd")).linear); Assert.IsTrue(BinaryTexture.Inspect(Read("map_Kn")).linear);
            var packed = FirstPixel(Read(BinaryTexture.PackedSemantic));
            Assert.That(packed.r, Is.InRange(63, 65)); Assert.That(packed.g, Is.InRange(31, 33)); Assert.AreEqual(128, packed.b); Assert.AreEqual(255, packed.a);
            Assert.IsFalse(archive.Contains("models/shared.png"));
        }
        [Test] public void PreparedLayeredAndVatKeepUvRemapsAndExactAnimationAtlases()
        {
            Staging();
            var png = Png(new Color32(20, 30, 40, 255)); File.WriteAllBytes(Path.Combine(directory, "models/layer.png"), png);
            var pbr = new ModPbrMaterial { blendMask = "layer.png", blendUv = new Vector4(3, 4, 5, 6), layers = Enumerable.Range(0, 3).Select(i => new ModPbrLayer {
                diffuse = "layer.png", normal = "layer.png", mask = "layer.png", uv = new Vector4(30 + i, 40, 2, -3), normalScale = .3f + i }).ToArray() };
            File.WriteAllText(Path.Combine(directory, "models/a.pbr.json"), JsonUtility.ToJson(pbr));
            File.WriteAllText(Path.Combine(directory, "models/a.mtl"), "newmtl a\ncx_pbr a.pbr.json\nmap_Kd layer.png\nmap_Pr layer.png\n");
            Directory.CreateDirectory(Path.Combine(directory, "animations"));
            byte[] positions = Enumerable.Range(0, 128).Select(i => (byte)i).ToArray();
            var animation = new AnimationMeta { assets = new List<VertexAnimationAsset> { new() { positions = Convert.ToBase64String(positions),
                surfaces = new[] { new VertexAnimationSurface { diffusePng = Convert.ToBase64String(png), normalPng = Convert.ToBase64String(png), maskPng = Convert.ToBase64String(png) } } } } };
            File.WriteAllText(Path.Combine(directory, "animations/a.json"), JsonUtility.ToJson(animation));
            string target = Path.Combine(directory, "mod.cxmod"); BinaryMapPacker.Pack(target, BinaryTextureEncoding.Rgba32, directory);
            var archive = new BinaryModArchive(target);
            var actual = BinaryModData.Read<ModPbrMaterial>(archive.Read("models/a.pbr.json"));
            Assert.AreEqual(3, actual.layers.Length); Assert.AreEqual(pbr.blendUv, actual.blendUv);
            for (int i = 0; i < 3; i++) { Assert.AreEqual(pbr.layers[i].uv, actual.layers[i].uv); Assert.AreEqual(pbr.layers[i].normalScale, actual.layers[i].normalScale); }
            Assert.AreEqual(actual.layers[0].normal, actual.blendMask);
            Assert.AreNotEqual(actual.layers[0].diffuse, actual.layers[0].normal);
            Assert.AreEqual(2, archive.Names.Count(n => n.EndsWith(BinaryTexture.Extension)));
            Assert.IsEmpty(BinaryModData.Read<BinaryMaterialLibrary>(archive.Read("models/a.mtl")).materials[0].properties);
            var vat = BinaryModData.Read<AnimationMeta>(archive.Read("animations/a.json")).assets[0];
            CollectionAssert.AreEqual(positions, vat.positionsBytes);
            Assert.IsFalse(BinaryTexture.Inspect(vat.surfaces[0].diffuseBytes).linear);
            Assert.IsTrue(BinaryTexture.Inspect(vat.surfaces[0].normalBytes).linear);
            Assert.IsTrue(BinaryTexture.Inspect(vat.surfaces[0].maskBytes).linear);
        }
        [Test] public void BakesEmissionTintAndRetainsHdrIntensity()
        {
            Staging();
            File.WriteAllBytes(Path.Combine(directory, "models/glow.png"), Png(new Color32(200, 100, 80, 255)));
            File.WriteAllText(Path.Combine(directory, "models/a.mtl"), "newmtl a\nKe 4 1 0\nmap_Ke glow.png\nnewmtl solid\nKe 0.25 0.5 1\n");
            string target = Path.Combine(directory, "mod.cxmod"); BinaryMapPacker.Pack(target, BinaryTextureEncoding.Rgba32, directory);
            var archive = new BinaryModArchive(target);
            var materials = BinaryModData.Read<BinaryMaterialLibrary>(archive.Read("models/a.mtl")).materials;
            Assert.AreEqual(Vector3.one * 4, materials[0].properties.Single(p => p.semantic == "Ke").vector);
            var bytes = archive.Read("models/" + materials[0].properties.Single(p => p.semantic == "map_Ke").texture);
            var pixel = FirstPixel(bytes);
            Assert.AreEqual(200, pixel.r); Assert.AreEqual((byte)(100 * new Color(1, .25f, 0).gamma.g), pixel.g); Assert.AreEqual(0, pixel.b);
            Assert.IsFalse(BinaryTexture.Inspect(bytes).linear);
            Assert.AreEqual(Vector3.one, materials[1].properties.Single(p => p.semantic == "Ke").vector);
            var solid = FirstPixel(archive.Read("models/" + materials[1].properties.Single(p => p.semantic == "map_Ke").texture));
            Assert.AreEqual((Color32)new Color(.25f, .5f, 1).gamma, solid);
            Assert.IsFalse(archive.Contains("models/glow.png"));
        }
    }
}
