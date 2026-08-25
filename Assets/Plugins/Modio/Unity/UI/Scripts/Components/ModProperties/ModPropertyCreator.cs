using System;
using Modio.Mods;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyCreator : ModioResourceProperty
    {
        [SerializeField] ModioUIUser _user;

        protected override void OnResourceUpdate(IModioInfo resource) 
            => _user.SetUser(resource.Creator);
    }
}
