using System;
using UnityEngine;
using UnityEngine.Video;

public class ValidVideoPlayer : IValidComponentProcess
{
    public bool isSuccess { get; private set; } = true;

    public string processMessage { get; private set; }

    private int m_videoMaxWidth;
    private int m_videoMaxHeight;
    private int m_maxFramerate;
    private int m_maxTimeInSecond;
    
    public ValidVideoPlayer(int videoMaxWidth = 1280, int videoMaxHeight = 720, int maxFramerate = 30, int maxTimeInSecond = 15)
    {
        m_videoMaxWidth = videoMaxWidth;
        m_videoMaxHeight = videoMaxHeight;
        m_maxFramerate = maxFramerate;
        m_maxTimeInSecond = maxTimeInSecond;
    }

    public void Reset()
    {
        processMessage = String.Empty;
        isSuccess = true;
    }

    public void ValidProcess(Component comp)
    {
        const float videoMaxWidth = 1280;
        const float videoMaxHeight = 720;
        const int maxFramerate = 30;
        const int maxTimeInSecond = 15;
        
        var compType = comp.GetType();
        if (compType.Name == nameof(VideoPlayer))
        {
            var videoPlayer = comp.gameObject.GetComponent<VideoPlayer>();
            var message = string.Empty;

            if (videoPlayer.clip != null)
            {
                if (videoPlayer.clip.length > maxTimeInSecond)
                {
                    message += $"{comp.gameObject.name} | Video max duration is {maxTimeInSecond} sec\n";
                }

                if (videoPlayer.clip.frameRate > maxFramerate)
                {
                    message += $"{comp.gameObject.name} | Video max framerate is {maxFramerate}\n";
                }

                if (videoPlayer.clip.width > videoMaxWidth || videoPlayer.clip.height > videoMaxHeight)
                {
                    message += $"{comp.gameObject.name} | Video max size is {videoMaxWidth} / {videoMaxHeight}\n";
                }
            }
            
            if (videoPlayer != null && videoPlayer.source == VideoSource.Url)
            {
                message += $"{comp.gameObject.name} | No support : {videoPlayer.source}\n";
            }

            if (message.Length > 2)
            {
                message = message.Substring(0, message.LastIndexOf("\n", StringComparison.Ordinal));
            }
            
            if (!string.IsNullOrEmpty(message))
            {
                isSuccess = false;
                processMessage = message;
            }
        }
    }
}