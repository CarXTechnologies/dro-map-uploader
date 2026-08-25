using System;
using Modio.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class CollectionPropertyRating : ICollectionProperty
    {
        [SerializeField] TMP_Text _text;
        [SerializeField, Tooltip("Uses string.Format().\n{0} outputs the rating percentage value.")]
        string _format = "{0}%";
        [SerializeField]
        string _noRating = "N/A";
        [SerializeField] Image _image;

        [SerializeField] Color _mostlyPositiveColor;
        [SerializeField] Color _mixedRatingsColor;
        [SerializeField] Color _mostlyNegativeColor;

        public void OnCollectionUpdate(ModCollection collection)
        {
            if (_text != null)
            {
                _text.text = collection.Stats.RatingsPercent == null
                    ? _noRating
                    : string.Format(_format, collection.Stats.RatingsPercent);

                switch (collection.Stats.RatingsPercent)
                {
                    case >= 90:
                        SetColor(_mostlyPositiveColor);
                        break;

                    case < 90 and >= 50:
                        SetColor(_mixedRatingsColor);
                        break;

                    case < 50:
                        SetColor(_mostlyNegativeColor);
                        break;
                }
            }
        }

        void SetColor(Color color)
        {
            _text.color = color;
            _image.color = color;
        }
    }
}
