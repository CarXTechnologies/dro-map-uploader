using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Camera))]
public class CaptureCamera : MonoBehaviour
{
    [SerializeField] private int m_width = 1024;
    [SerializeField] private int m_height = 1024;
    
    [SerializeField] private int m_samples = 1;
    
    private int m_inWidth;
    private int m_inHeight;

#if UNITY_EDITOR
    [ContextMenu("Capture")]
    private void Capture()
    {
        var camera = GetComponent<Camera>();
        m_inWidth = m_width * m_samples;
        m_inHeight = m_height * m_samples;

        var lastRenderTarget = RenderTexture.active;

        camera.targetTexture = RenderTexture.GetTemporary(
            m_inWidth,
            m_inHeight,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.sRGB);

        camera.Render();
        RenderTexture.active = FilteredDownscale(camera.targetTexture, m_width, m_height);
        
        var texture2D = new Texture2D(m_width, m_height, TextureFormat.ARGB32, false, false);
        texture2D.ReadPixels(new Rect(0, 0, m_width, m_height), 0, 0);
        
        var filepath = EditorUtility.SaveFilePanel("Save Capture", Application.dataPath, "", "png");
        
        RenderTexture.active.Release();
        RenderTexture.active = lastRenderTarget;
        camera.targetTexture = null;
        
        if (!string.IsNullOrEmpty(filepath))
        {
            File.WriteAllBytes(filepath, texture2D.EncodeToPNG());
            AssetDatabase.Refresh();
            filepath = "Assets" + filepath.Replace(Application.dataPath, string.Empty);
            var asset = (TextureImporter)AssetImporter.GetAtPath(filepath);
            asset.isReadable = true;
            AssetDatabase.ImportAsset(filepath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
        }
    }
    
    private static RenderTexture FilteredDownscale(RenderTexture source, int width, int height)
    {
        RenderTexture activeRT = RenderTexture.active;

        int w = source.width / 2;
        int h = source.height / 2;

        if (w < width || h < height)
        {
            w = width;
            h = height;
        }

        var temp1 = RenderTexture.GetTemporary (w, h, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit (source, temp1);
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

            RenderTexture temp2 = RenderTexture.GetTemporary (w, h, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(temp1, temp2);

            (temp1, temp2) = (temp2, temp1);
            RenderTexture.ReleaseTemporary (temp2);
        }
        
        RenderTexture.active = activeRT;

        return temp1;
    }
#endif
}