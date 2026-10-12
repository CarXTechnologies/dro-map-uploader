using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class UIUtils
{
	private static readonly TimeSpan s_downloadTimeout = TimeSpan.FromSeconds(10);

	/// <summary>
	/// Downloads an image and hands it to <paramref name="callback"/>, which is invoked exactly once: with the texture on
	/// success, with nulls on a failed request, a timeout or an image that cannot be decoded.
	/// </summary>
	/// <remarks>
	/// Callers keep "downloading" state until the callback runs (the map window locks Build and Validate on it), so a
	/// path that skips the callback would leave that state stuck for the rest of the session.
	/// </remarks>
	public static async Task DownloadSprite(string url, Action<Sprite, Texture2D> callback)
	{
		Texture2D texture = null;

		try
		{
			using UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
			var operation = webRequest.SendWebRequest();

			// Real time, not a count of delays: Task.Delay in the editor resumes on editor ticks and runs long.
			var elapsed = Stopwatch.StartNew();

			while (!operation.isDone && elapsed.Elapsed < s_downloadTimeout)
			{
				await Task.Delay(10);
			}

			if (operation.isDone && webRequest.result == UnityWebRequest.Result.Success)
			{
				texture = DownloadHandlerTexture.GetContent(webRequest);
			}
		}
		catch (Exception exception)
		{
			Debug.LogWarning($"Could not load the preview '{url}': {exception.Message}");
			texture = null;
		}

		callback(texture != null ? CreateSprite(texture) : null, texture);
	}

	private static Sprite CreateSprite(Texture2D texture)
	{
		var sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
		sprite.hideFlags = HideFlags.HideAndDontSave;
		return sprite;
	}
}
