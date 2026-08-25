using System;
using Modio.Collections;
using Modio.Mods;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyFileSize : IModProperty, ICollectionProperty
    {
        [SerializeField] TMP_Text _text;
        [SerializeField, Tooltip(StringFormat.BYTES_FORMAT_TOOLTIP)]
        StringFormatBytes _format = StringFormatBytes.Suffix;
        [SerializeField, ShowIf(nameof(IsCustomFormat))]
        string _customFormat;

        public void OnModUpdate(Mod mod) => _text.text = mod?.File == null ? "NULL" :
            StringFormat.Bytes(_format, mod.File.FileSize, _customFormat);

        bool IsCustomFormat() => _format == StringFormatBytes.Custom;

        public void OnCollectionUpdate(ModCollection collection)
        {
            _text.text = StringFormat.Bytes(_format, collection.Filesize, _customFormat);
        }
    }
}
