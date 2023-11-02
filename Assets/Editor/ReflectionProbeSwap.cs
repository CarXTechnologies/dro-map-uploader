using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

public class ReflectionProbeSwap : MonoBehaviour
{
#if UNITY_EDITOR
    [ContextMenu("Bake")]
    private void Bake()
    {
        var hdrpAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
        var hdrpSettings = hdrpAsset.currentPlatformRenderPipelineSettings;

        var savedCubemapSize = hdrpSettings.lightLoopSettings.reflectionCubemapSize;
        hdrpSettings.lightLoopSettings.reflectionCubemapSize = CubeReflectionResolution.CubeReflectionResolution512;
        var reflectProbe = GetComponent<ReflectionProbe>();
        var additionalData = reflectProbe.GetComponent<HDAdditionalReflectionData>();
        
        RenderTexture reflectionCube     = additionalData.realtimeTexture;
        RenderTexture reflectionEquirect = new RenderTexture(1024, 512, 0, reflectionCube.graphicsFormat);

        reflectionCube.ConvertToEquirect(reflectionEquirect, Camera.MonoOrStereoscopicEye.Mono);

        RenderTexture.active = reflectionEquirect;

        Texture2D row = new Texture2D(reflectionEquirect.width, reflectionEquirect.height, reflectionCube.graphicsFormat, 0, TextureCreationFlags.None);
        row.ReadPixels(new Rect(0, 0, reflectionEquirect.width, reflectionEquirect.height), 0, 0);

        RenderTexture.active = null;

        string sceneName = SceneManager.GetActiveScene().name;
        var filepath = EditorUtility.SaveFilePanel("Save Reflection Probe", null, sceneName, "exr");
        if (!string.IsNullOrEmpty(filepath))
        {
            File.WriteAllBytes(filepath, row.EncodeToEXR(Texture2D.EXRFlags.CompressZIP));
        }

        hdrpSettings.lightLoopSettings.reflectionCubemapSize = savedCubemapSize;
    }
#endif
}