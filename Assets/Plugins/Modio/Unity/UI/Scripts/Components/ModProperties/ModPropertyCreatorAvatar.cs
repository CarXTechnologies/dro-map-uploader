using System;
using Modio.Images;
using Modio.Mods;
using Modio.Users;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyCreatorAvatar : ModioResourceProperty
    {
        [SerializeField] RawImage _image;
        [SerializeField] UserProfile.AvatarResolution _resolution = UserProfile.AvatarResolution.X50_Y50;
        LazyImage<Texture2D> _lazyImage;

        protected override void OnResourceUpdate(IModioInfo resource)
        {
            _lazyImage ??= new LazyImage<Texture2D>(
                ImageCacheTexture2D.Instance,
                texture2D =>
                {
                    if (_image != null) _image.texture = texture2D;
                }
            );
            _lazyImage.SetImage(resource.Creator.Avatar, _resolution);
        }
    }
}
