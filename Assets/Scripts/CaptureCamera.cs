using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(Camera))]
public class CaptureCamera : MonoBehaviour
{
	[SerializeField] private int m_width = 1024;
	[SerializeField] private int m_height = 1024;

	[SerializeField] private int m_samples = 1;

	[SerializeField] private bool m_clipBackground;

	private int m_inWidth;
	private int m_inHeight;

#if UNITY_EDITOR
	[ContextMenu("Capture")]
	private void Capture()
	{
		var camera = GetComponent<Camera>();
		camera.useOcclusionCulling = false;
		m_inWidth = m_width * m_samples;
		m_inHeight = m_height * m_samples;

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

		if (m_clipBackground && hdCameraData != null)
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

			var onBlack = RenderWithBackground(camera, hdCameraData, Color.black);
			var onWhite = RenderWithBackground(camera, hdCameraData, Color.white);
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
			texture2D = RenderWithBackground(camera, hdCameraData, hdCameraData != null ? lastBackgroundColorHDR : (Color?)null);
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
			asset.alphaIsTransparency = m_clipBackground;
			AssetDatabase.ImportAsset(filepath, ImportAssetOptions.ForceUpdate);
			AssetDatabase.Refresh();
		}

		Object.DestroyImmediate(texture2D);
	}

	private Texture2D RenderWithBackground(Camera camera, HDAdditionalCameraData hdCameraData, Color? background)
	{
		if (hdCameraData != null && background.HasValue)
		{
			hdCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
			hdCameraData.backgroundColorHDR = background.Value;
		}

		camera.targetTexture = RenderTexture.GetTemporary(m_inWidth, m_inHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

		camera.Render();
		RenderTexture.active = FilteredDownscale(camera.targetTexture, m_width, m_height);

		var texture2D = new Texture2D(m_width, m_height, TextureFormat.RGBA32, false, false);
		texture2D.ReadPixels(new Rect(0, 0, m_width, m_height), 0, 0);
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
#endif
}
