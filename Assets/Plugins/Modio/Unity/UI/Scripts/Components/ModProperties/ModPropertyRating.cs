using System;
using Modio.Collections;
using Modio.Mods;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyRating : IModProperty, ICollectionProperty
    {
        [SerializeField] TMP_Text _text;
        [SerializeField, Tooltip("Uses string.Format().\n{0} outputs the rating percentage value.")]
        string _format = "{0}%";
        [SerializeField]
        string _noRating = "N/A";

        public void OnModUpdate(Mod mod)
        {
            if (_text != null) _text.text = string.Format(_format, mod.Stats.RatingsPercent);
        }

        public void OnCollectionUpdate(ModCollection collection)
        {
            if (_text != null)
            {
                _text.text = collection.Stats.RatingsPercent == null
                    ? _noRating
                    : string.Format(_format, collection.Stats.RatingsPercent);
            }
        }
    }
}
