using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class UIUtils
{
    public static async Task DownloadSprite(string url, Action<Sprite, Texture2D> callback)
    {
        using UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
        var webRequestSend = webRequest.SendWebRequest();
        while (!webRequestSend.webRequest.isDone)
        {
            await Task.Delay(10);
        }

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            callback.Invoke(null, null);
            return;
        }

        var texture = DownloadHandlerTexture.GetContent(webRequest);
        callback(CreateSprite(texture), texture);
    }
    
    public static Sprite CreateSprite(Texture2D texture)
    {
        var sprite = texture != null ? Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f)) : Sprite.Create(null, new Rect(0.0f, 0.0f, 1.0f, 1.0f), new Vector2(0.5f, 0.5f));
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}