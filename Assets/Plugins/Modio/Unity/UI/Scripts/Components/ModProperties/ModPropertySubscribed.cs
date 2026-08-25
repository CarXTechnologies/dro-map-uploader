using System;
using Modio.Mods;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertySubscribed : ModioResourceProperty
    {
        [SerializeField] GameObject _notSubscribedActive;
        [SerializeField] GameObject _subscribedActive;

        protected override void OnResourceUpdate(IModioInfo resource)
        {
            if (_notSubscribedActive != null) _notSubscribedActive.SetActive(!resource.IsSubscribed);
            if (_subscribedActive != null) _subscribedActive.SetActive(resource.IsSubscribed);
        }
    }
}
