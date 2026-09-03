using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Editor
{
	[CustomEditor(typeof(CaptureCamera))]
	public class CaptureCameraEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			EditorGUILayout.Space(6);

			if (GUILayout.Button("Capture", GUILayout.Height(24)))
			{
				Capture((CaptureCamera)target);
			}
		}

		private static void Capture(CaptureCamera capture)
		{
			var camera = capture.GetComponent<Camera>();
			camera.useOcclusionCulling = false;

			var inWidth = capture.Width * capture.Samples;
			var inHeight = capture.Height * capture.Samples;

			var lastRenderTarget = RenderTexture.active;
			var hdCameraData = camera.GetComponent<HDAdditionalCameraData>();
			var lastClearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
			var lastBackgroundColorHDR = Color.black;

			if (hdCameraData != null)
			{
				lastClearColorMode = hdCameraData.clearColorMode;
				lastBackgroundColorHDR = hdCameraData.backgroundColorHDR;
			}

			Texture2D texture2D;

			if (capture.ClipBackground && hdCameraData != null)
			{
				var lastCustomRenderingSettings = hdCameraData.customRenderingSettings;
				var lastCustomFrameSettings = hdCameraData.renderingPathCustomFrameSettings;
				var lastCustomFrameSettingsMask = hdCameraData.renderingPathCustomFrameSettingsOverrideMask;
				var lastAntialiasing = hdCameraData.antialiasing;

				// Tonemapping/bloom/vignette/color grading are non-linear w.r.t. the background color,
				// which breaks the black/white difference-matte math, so they're disabled for the two
				// capture passes. Exposure is left untouched - it's a separate linear scale, and
				// disabling it entirely skips HDR compression and blows out highlights.
				var frameSettings = hdCameraData.renderingPathCustomFrameSettings;
				frameSettings.SetEnabled(FrameSettingsField.Postprocess, false);
				hdCameraData.renderingPathCustomFrameSettings = frameSettings;

				var overrideMask = hdCameraData.renderingPathCustomFrameSettingsOverrideMask;
				overrideMask.mask[(uint)FrameSettingsField.Postprocess] = true;
				hdCameraData.renderingPathCustomFrameSettingsOverrideMask = overrideMask;

				hdCameraData.customRenderingSettings = true;
				// TAA history from the previous pass would otherwise bleed into the next background pass.
				hdCameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;

				var onBlack = RenderWithBackground(capture, camera, hdCameraData, Color.black, inWidth, inHeight);
				var onWhite = RenderWithBackground(capture, camera, hdCameraData, Color.white, inWidth, inHeight);
				texture2D = ComposeTransparent(onBlack, onWhite);
				Object.DestroyImmediate(onBlack);
				Object.DestroyImmediate(onWhite);

				hdCameraData.customRenderingSettings = lastCustomRenderingSettings;
				hdCameraData.renderingPathCustomFrameSettings = lastCustomFrameSettings;
				hdCameraData.renderingPathCustomFrameSettingsOverrideMask = lastCustomFrameSettingsMask;
				hdCameraData.antialiasing = lastAntialiasing;
			}
			else
			{
				texture2D = RenderWithBackground(capture, camera, hdCameraData,
					hdCameraData != null ? lastBackgroundColorHDR : (Color?)null, inWidth, inHeight);
			}

			var filepath = EditorUtility.SaveFilePanel("Save Capture", Application.dataPath, "", "png");

			RenderTexture.active = lastRenderTarget;
			camera.targetTexture = null;

			if (hdCameraData != null)
			{
				hdCameraData.clearColorMode = lastClearColorMode;
				hdCameraData.backgroundColorHDR = lastBackgroundColorHDR;
			}

			if (!string.IsNullOrEmpty(filepath))
			{
				File.WriteAllBytes(filepath, texture2D.EncodeToPNG());
				AssetDatabase.Refresh();
				filepath = "Assets" + filepath.Replace(Application.dataPath, string.Empty);
				var asset = (TextureImporter)AssetImporter.GetAtPath(filepath);
				asset.isReadable = true;
				asset.alphaIsTransparency = capture.ClipBackground;
				AssetDatabase.ImportAsset(filepath, ImportAssetOptions.ForceUpdate);
				AssetDatabase.Refresh();
			}

			Object.DestroyImmediate(texture2D);
		}

		private static Texture2D RenderWithBackground(CaptureCamera capture, Camera camera,
			HDAdditionalCameraData hdCameraData, Color? background, int inWidth, int inHeight)
		{
			if (hdCameraData != null && background.HasValue)
			{
				hdCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
				hdCameraData.backgroundColorHDR = background.Value;
			}

			camera.targetTexture = RenderTexture.GetTemporary(inWidth, inHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

			camera.Render();
			RenderTexture.active = FilteredDownscale(camera.targetTexture, capture.Width, capture.Height);

			var texture2D = new Texture2D(capture.Width, capture.Height, TextureFormat.RGBA32, false, false);
			texture2D.ReadPixels(new Rect(0, 0, capture.Width, capture.Height), 0, 0);
			texture2D.Apply();

			RenderTexture.active.Release();
			camera.targetTexture = null;

			return texture2D;
		}

		private static Texture2D ComposeTransparent(Texture2D onBlack, Texture2D onWhite)
		{
			var width = onBlack.width;
			var height = onBlack.height;

			var blackPixels = onBlack.GetPixels();
			var whitePixels = onWhite.GetPixels();
			var result = new Color[blackPixels.Length];

			for (var i = 0; i < result.Length; i++)
			{
				var black = blackPixels[i];
				var white = whitePixels[i];

				var alpha = 1f - ((white.r - black.r) + (white.g - black.g) + (white.b - black.b)) / 3f;
				alpha = Mathf.Clamp01(alpha);

				var color = alpha > 0.05f ? black / alpha : (black + white) * 0.5f;
				result[i] = new Color(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b), alpha);
			}

			var texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
			texture2D.SetPixels(result);
			texture2D.Apply();

			return texture2D;
		}

		private static RenderTexture FilteredDownscale(RenderTexture source,
			int width,
			int height)
		{
			RenderTexture activeRT = RenderTexture.active;

			int w = source.width / 2;
			int h = source.height / 2;

			if (w < width || h < height)
			{
				w = width;
				h = height;
			}

			var temp1 = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
			Graphics.Blit(source, temp1);
			source.Release();

			while (w > width && h > height)
			{
				w /= 2;
				h /= 2;

				if (w < width || h < height)
				{
					w = width;
					h = height;
				}

				RenderTexture temp2 = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
				Graphics.Blit(temp1, temp2);

				(temp1, temp2) = (temp2, temp1);
				RenderTexture.ReleaseTemporary(temp2);
			}

			RenderTexture.active = activeRT;

			return temp1;
		}
	}
}
