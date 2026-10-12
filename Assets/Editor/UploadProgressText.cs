using System;
using System.Globalization;
using Plugins.CarX.Modding.Creator.Runtime.Publishing;

namespace Editor
{
	/// <summary>
	/// Turns a publisher's <see cref="ModUploadProgress"/> into the text the MapBuilder window shows while a map is
	/// being published. Pure on purpose: the window decides when to redraw, this only decides what the line says.
	/// </summary>
	/// <remarks>
	/// Numbers are written with the invariant culture so the line reads the same on every machine - a Russian locale
	/// would otherwise turn "12.3" into "12,3" in the middle of an otherwise fixed layout.
	/// </remarks>
	public static class UploadProgressText
	{
		private const string Separator = " · ";
		private const double BytesPerMegabyte = 1024d * 1024d;

		/// <summary>
		/// Absorbs float noise before flooring a fraction to a percentage, so 0.45f reads as 45% and not 44%.
		/// </summary>
		private const double PercentEpsilon = 1e-4;

		/// <summary>
		/// Short name of a stage. Used on its own as the progress bar title and as the start of the status line.
		/// </summary>
		/// <param name="vendorName">Where the files are processed, e.g. "mod.io". Empty falls back to a neutral word.</param>
		public static string StageTitle(ModUploadStage stage, string vendorName, bool english)
		{
			switch (stage)
			{
				case ModUploadStage.Preparing:
					return L(english, "Подготовка…", "Preparing…");
				case ModUploadStage.Packing:
					return L(english, "Упаковка файлов…", "Packing files…");
				case ModUploadStage.Uploading:
					return L(english, "Отправка…", "Uploading…");
				case ModUploadStage.Processing:
					return L(english, "Обработка на ", "Processing on ") + VendorOrFallback(vendorName, english) + "…";
				default:
					return L(english, "Публикация…", "Publishing…");
			}
		}

		/// <summary>
		/// The full status line: the stage, its percentage and sizes where the stage has them, and how long the
		/// operation has been running - e.g. "Uploading… 63% · 120 / 190 MB · 1:05".
		/// </summary>
		/// <remarks>
		/// The percentage is the stage's own when its bytes are known, and the overall one otherwise (Steam does not
		/// report bytes). The sizes are left out when the stage has no byte total.
		/// </remarks>
		public static string Format(ModUploadProgress progress, TimeSpan elapsed, string vendorName, bool english)
		{
			var text = StageTitle(progress.Stage, vendorName, english);

			if (ShowsPercent(progress.Stage))
			{
				text += " " + StagePercent(progress).ToString(CultureInfo.InvariantCulture) + "%";

				if (progress.TotalBytes > 0)
				{
					text += Separator + FormatMegabytes(progress.DoneBytes, progress.TotalBytes, english);
				}
			}

			return WithElapsed(text, elapsed);
		}

		/// <summary>
		/// Appends the elapsed time to <paramref name="text"/>, e.g. "Connecting to mod.io… · 0:07".
		/// </summary>
		public static string WithElapsed(string text, TimeSpan elapsed)
		{
			var time = FormatElapsed(elapsed);
			return string.IsNullOrEmpty(text) ? time : text + Separator + time;
		}

		/// <summary>
		/// Elapsed time as m:ss. Minutes are not wrapped into hours, so an hour and five minutes reads 65:00.
		/// </summary>
		public static string FormatElapsed(TimeSpan elapsed)
		{
			var seconds = elapsed > TimeSpan.Zero ? (long)elapsed.TotalSeconds : 0L;
			return (seconds / 60).ToString(CultureInfo.InvariantCulture) + ":" +
			       (seconds % 60).ToString("00", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Whole percent shown next to the stage, 0..100, rounded down so 100% only appears once a stage is complete.
		/// Taken from the stage's bytes when they are known, from the overall fraction otherwise.
		/// </summary>
		public static int StagePercent(ModUploadProgress progress)
		{
			if (progress.TotalBytes > 0)
			{
				var done = Math.Min(Math.Max(progress.DoneBytes, 0L), progress.TotalBytes);
				return (int)(done * 100L / progress.TotalBytes);
			}

			return (int)Math.Floor(Clamp01(progress.Fraction) * 100d + PercentEpsilon);
		}

		/// <summary>
		/// Value for a 0..100 progress bar: the overall progress of the whole operation.
		/// </summary>
		public static float BarValue(ModUploadProgress progress)
		{
			return (float)(Clamp01(progress.Fraction) * 100d);
		}

		/// <summary>
		/// "12.3 / 45.6 MB". Precision follows the total, so both numbers keep the same width while they change:
		/// no decimals from 100 MB up, one below that, two below a megabyte.
		/// </summary>
		public static string FormatMegabytes(long doneBytes, long totalBytes, bool english)
		{
			var total = Math.Max(totalBytes, 0L) / BytesPerMegabyte;
			var done = Math.Min(Math.Max(doneBytes, 0L), Math.Max(totalBytes, 0L)) / BytesPerMegabyte;
			var format = total >= 100d ? "0" : total >= 1d ? "0.0" : "0.00";

			return done.ToString(format, CultureInfo.InvariantCulture) + " / " +
			       total.ToString(format, CultureInfo.InvariantCulture) + L(english, " МБ", " MB");
		}

		private static bool ShowsPercent(ModUploadStage stage)
		{
			// Preparing is too short to measure and Processing has nothing to count - the elapsed time is what
			// shows those two are alive.
			return stage != ModUploadStage.Preparing && stage != ModUploadStage.Processing;
		}

		private static string VendorOrFallback(string vendorName, bool english)
		{
			if (!string.IsNullOrWhiteSpace(vendorName))
			{
				return vendorName.Trim();
			}

			return L(english, "площадке", "the platform");
		}

		/// <summary>
		/// Picks the string for the window language. Russian first, like the window's own L(ru, en), so every string
		/// here reads in the same order as the rest of the UI.
		/// </summary>
		private static string L(bool english, string russian, string englishText)
		{
			return english ? englishText : russian;
		}

		private static double Clamp01(float value)
		{
			if (float.IsNaN(value) || value <= 0f)
			{
				return 0d;
			}

			return value >= 1f ? 1d : value;
		}
	}
}
