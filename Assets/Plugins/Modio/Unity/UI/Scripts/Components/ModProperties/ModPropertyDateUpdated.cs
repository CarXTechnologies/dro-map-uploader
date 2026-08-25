using System;
using Modio.Mods;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyDateUpdated : ModioResourcePropertyDateBase
    {
        [SerializeField] GameObject _disableIfNoUpdate;

        protected override DateTime GetValue(IModioInfo mod) => mod.DateUpdated;

        protected override void OnResourceUpdate(IModioInfo resource)
        {
            base.OnResourceUpdate(resource);
            if (_disableIfNoUpdate != null) _disableIfNoUpdate.SetActive(resource.DateUpdated != resource.DateLive);
        }
    }
}
