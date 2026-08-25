using System;
using Modio.Images;
using Modio.Mods;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyLogo : ModioResourceProperty
    {
        [SerializeField] RawImage _image;
        [SerializeField] Mod.LogoResolution _resolution = Mod.LogoResolution.X320_Y180;
        [SerializeField] bool _useHighestAvailableResolutionAsFallback = true;
        [Space]
        [Tooltip("(Optional) Active while loading, inactive once loaded.")]
        [SerializeField]
        GameObject _loadingActive;
        [Tooltip("(Optional) Inactive while loading, active once loaded.")]
        [SerializeField]
        GameObject _loadedActive;
        LazyImage<Texture2D> _lazyImage;

        protected override void OnResourceUpdate(IModioInfo resource)
        {
            _lazyImage ??= new LazyImage<Texture2D>(
                ImageCacheTexture2D.Instance,
                texture2D =>
                {
                    if (_image != null) _image.texture = texture2D;
                },
                isLoading =>
                {
                    if (_loadingActive) _loadingActive.SetActive(isLoading);
                    if (_loadedActive) _loadedActive.SetActive(!isLoading);
                }
            );
            
            _lazyImage.SetImage(resource.Logo, _resolution);
        }
    }
}
