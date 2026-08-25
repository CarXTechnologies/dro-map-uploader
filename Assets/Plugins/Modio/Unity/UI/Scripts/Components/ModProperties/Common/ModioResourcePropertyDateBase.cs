using System;
using Modio.Mods;
using Modio.Unity.UI.Components.Localization;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public abstract class ModioResourcePropertyDateBase : ModioResourceProperty
    {
        [SerializeField] TMP_Text _text;
        [SerializeField] string _format = "dd MMM, yy";

        protected override void OnResourceUpdate(IModioInfo resource) =>
            _text.text = GetValue(resource).ToString(_format, ModioUILocalizationManager.CultureInfo);

        protected abstract DateTime GetValue(IModioInfo mod);
    }
}
