using System;
using System.IO;
using System.Text.RegularExpressions;
using Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MapUploader.Tests
{
    public class MapBuildStagingTests
    {
        [TestCase(0)]
        [TestCase(99)]
        public void RemovedAndUnknownFormatsCannotBeBuilt(int format)
        {
            Assert.IsTrue(MapBuilder.IsFormatBlocked((FormatBuild)format, out var reason));
            StringAssert.Contains("Wavefront or Binary", reason);
        }

        [TestCase((FormatBuild)0, 3)]
        [TestCase(FormatBuild.Binary, 1)]
        [TestCase(FormatBuild.Wavefront, 2)]
        public void InvalidBuildCannotWriteAnExport(FormatBuild format, int complete)
        {
            var config = ScriptableObject.CreateInstance<MapMetaConfig>();
            var destination = Path.Combine(Path.GetTempPath(), "MapStagingTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                var build = new MapManagerConfig.BuildData { config = config, format = format, buildSuccess = complete };
                LogAssert.Expect(LogType.Error, new Regex("Build Map and Meta in Wavefront or Binary first"));
                Assert.IsFalse(MapBuilder.BuildDataTransitionToDirectory(build, destination));
                Assert.IsFalse(Directory.Exists(destination));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
