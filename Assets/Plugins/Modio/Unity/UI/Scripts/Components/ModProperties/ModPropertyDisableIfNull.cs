using System;
using Modio.Collections;
using Modio.Mods;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertySetGameobjectsActive : IModProperty, ICollectionProperty
    {
        [SerializeField]
        GameObject[] _targetsActive;
        [SerializeField]
        GameObject[] _targetsNotActive;
        
        public void OnModUpdate(Mod mod)
        {
            foreach(GameObject target in _targetsActive)
                if (target != null)
                    target.SetActive(true);
            foreach(GameObject target in _targetsNotActive)
                if (target != null)
                    target.SetActive(false);
        }

        public void OnCollectionUpdate(ModCollection collection)
        {
            foreach(GameObject target in _targetsActive)
                if (target != null)
                    target.SetActive(true);
            foreach(GameObject target in _targetsNotActive)
                if (target != null)
                    target.SetActive(false);
        }
    }
}
