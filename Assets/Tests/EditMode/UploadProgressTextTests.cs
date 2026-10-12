using System;
using System.Globalization;
using System.Threading;
using Editor;
using NUnit.Framework;
using Plugins.CarX.Modding.Creator.Runtime.Publishing;

namespace MapUploader.Tests
{
    public class UploadProgressTextTests
    {
        private const long MiB = 1024L * 1024L;

        [Test]
        public void UploadingShowsStagePercentSizesAndElapsedTime()
        {
            var progress = new ModUploadProgress(ModUploadStage.Uploading, 0.66f, 120 * MiB, 190 * MiB);

            Assert.AreEqual("Uploading… 63% · 120 / 190 MB · 1:05",
                UploadProgressText.Format(progress, TimeSpan.FromSeconds(65), "mod.io", true));
            Assert.AreEqual("Отправка… 63% · 120 / 190 МБ · 1:05",
                UploadProgressText.Format(progress, TimeSpan.FromSeconds(65), "mod.io", false));
        }

        [Test]
        public void PackingShowsOneDecimalBelowHundredMegabytes()
        {
            var progress = new ModUploadProgress(ModUploadStage.Packing, 0.05f, 16 * MiB + MiB / 5, 64 * MiB);

            Assert.AreEqual("Packing files… 25% · 16.2 / 64.0 MB · 0:07",
                UploadProgressText.Format(progress, TimeSpan.FromSeconds(7), "mod.io", true));
            Assert.AreEqual("Упаковка файлов… 25% · 16.2 / 64.0 МБ · 0:07",
                UploadProgressText.Format(progress, TimeSpan.FromSeconds(7), "mod.io", false));
        }

        [Test]
        public void StagePercentComesFromBytesNotFromOverallFraction()
        {
            var progress = new ModUploadProgress(ModUploadStage.Uploading, 0.5f, 10 * MiB, 100 * MiB);

            Assert.AreEqual(10, UploadProgressText.StagePercent(progress));
            Assert.AreEqual(50f, UploadProgressText.BarValue(progress), 0.001f);
        }

        [Test]
        public void UnknownSizeShowsOverallPercentWithoutMegabytes()
        {
            // Steam reports a fraction only.
            var progress = new ModUploadProgress(ModUploadStage.Uploading, 0.45f, 0, 0);

            Assert.AreEqual("Uploading… 45% · 0:30",
                UploadProgressText.Format(progress, TimeSpan.FromSeconds(30), "Steam Workshop", true));
            Assert.AreEqual("Отправка… 45% · 0:30",
                UploadProgressText.Format(progress, TimeSpan.FromSeconds(30), "Steam Workshop", false));
        }

        [Test]
        public void PreparingShowsNoPercent()
        {
            var progress = new ModUploadProgress(ModUploadStage.Preparing, 0.01f, 0, 0);

            Assert.AreEqual("Preparing… · 0:03", UploadProgressText.Format(progress, TimeSpan.FromSeconds(3), "mod.io", true));
            Assert.AreEqual("Подготовка… · 0:03", UploadProgressText.Format(progress, TimeSpan.FromSeconds(3), "mod.io", false));
        }

        [Test]
        public void ProcessingNamesTheVendorAndOnlyTicksTime()
        {
            var progress = new ModUploadProgress(ModUploadStage.Processing, 0.97f, 190 * MiB, 190 * MiB);

            Assert.AreEqual("Processing on mod.io… · 2:00",
                UploadProgressText.Format(progress, TimeSpan.FromMinutes(2), "mod.io", true));
            Assert.AreEqual("Обработка на mod.io… · 2:00",
                UploadProgressText.Format(progress, TimeSpan.FromMinutes(2), "mod.io", false));
        }

        [TestCase((string)null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ProcessingFallsBackToANeutralWordWithoutVendor(string vendor)
        {
            Assert.AreEqual("Processing on the platform…", UploadProgressText.StageTitle(ModUploadStage.Processing, vendor, true));
            Assert.AreEqual("Обработка на площадке…", UploadProgressText.StageTitle(ModUploadStage.Processing, vendor, false));
        }

        [TestCase(0d, "0:00")]
        [TestCase(-5d, "0:00")]
        [TestCase(59.9d, "0:59")]
        [TestCase(61d, "1:01")]
        [TestCase(3725d, "62:05")]
        public void ElapsedIsMinutesAndSeconds(double seconds, string expected)
        {
            Assert.AreEqual(expected, UploadProgressText.FormatElapsed(TimeSpan.FromSeconds(seconds)));
        }

        [Test]
        public void WithElapsedAppendsTimeToAStep()
        {
            Assert.AreEqual("Connecting to mod.io… · 0:04",
                UploadProgressText.WithElapsed("Connecting to mod.io…", TimeSpan.FromSeconds(4)));
            Assert.AreEqual("0:04", UploadProgressText.WithElapsed(null, TimeSpan.FromSeconds(4)));
        }

        [Test]
        public void SizesBelowAMegabyteKeepTwoDecimals()
        {
            Assert.AreEqual("0.50 / 0.75 MB", UploadProgressText.FormatMegabytes(MiB / 2, MiB * 3 / 4, true));
        }

        [Test]
        public void PercentNeverReachesHundredBeforeTheLastByte()
        {
            var almost = new ModUploadProgress(ModUploadStage.Uploading, 0.96f, 190 * MiB - 1, 190 * MiB);
            var done = new ModUploadProgress(ModUploadStage.Uploading, 0.97f, 190 * MiB, 190 * MiB);

            Assert.AreEqual(99, UploadProgressText.StagePercent(almost));
            Assert.AreEqual(100, UploadProgressText.StagePercent(done));
        }

        [Test]
        public void OutOfRangeValuesAreClamped()
        {
            var over = new ModUploadProgress(ModUploadStage.Uploading, 1.5f, 300 * MiB, 200 * MiB);
            var under = new ModUploadProgress(ModUploadStage.Packing, -0.5f, -10, 0);
            var notANumber = new ModUploadProgress(ModUploadStage.Packing, float.NaN, 0, 0);

            Assert.AreEqual(100, UploadProgressText.StagePercent(over));
            Assert.AreEqual(100f, UploadProgressText.BarValue(over), 0.001f);
            Assert.AreEqual("Uploading… 100% · 200 / 200 MB · 0:00", UploadProgressText.Format(over, TimeSpan.Zero, "mod.io", true));
            Assert.AreEqual(0, UploadProgressText.StagePercent(under));
            Assert.AreEqual(0f, UploadProgressText.BarValue(under), 0.001f);
            Assert.AreEqual(0f, UploadProgressText.BarValue(notANumber), 0.001f);
        }

        [Test]
        public void NumbersDoNotFollowTheMachineCulture()
        {
            var previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");
                var progress = new ModUploadProgress(ModUploadStage.Packing, 0.1f, 12 * MiB + MiB / 2, 45 * MiB);

                Assert.AreEqual("Упаковка файлов… 27% · 12.5 / 45.0 МБ · 0:01",
                    UploadProgressText.Format(progress, TimeSpan.FromSeconds(1), "mod.io", false));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }
    }
}
