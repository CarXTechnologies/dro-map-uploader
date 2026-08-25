using System;
using Modio.Mods;
using Modio.Monetization;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyPrice : ModioPropertyNumberBase, IModProperty
    {
        [SerializeField] GameObject _disableIfFree;
        [SerializeField] bool _alsoDisableIfPurchased;
        [SerializeField] GameObject _enableIfPurchased;
        [SerializeField] GameObject _enableIfUsdMarketplace;
        [SerializeField] GameObject _enableIfVirtualCurrency;
        [SerializeField] GameObject _enableIfUnavailable;
        [SerializeField] TMP_Text _fiatPriceText;
        public void OnModUpdate(Mod mod)
        {
            var settings = ModioServices.Resolve<ModioSettings>();

            bool platformIsMonetized = settings.TryGetPlatformSettings(out MonetizationSettings monetizationSettings);

            bool modIsMonetized = platformIsMonetized && mod.IsMonetized && !(_alsoDisableIfPurchased && mod.IsPurchased);

            if (_disableIfFree != null)
                _disableIfFree.SetActive(modIsMonetized);

            if (_enableIfPurchased != null) _enableIfPurchased.SetActive(mod.IsPurchased);

            
            // If we can't get a matching SKU for the portal, PortalSku will be null
            var disablePurchase = modIsMonetized &&
                                  monetizationSettings.MonetizationType == ModioMonetizationType.UsdMarketplace &&
                                  mod.PortalSku == null;
                
            if (_enableIfUsdMarketplace != null)
                _enableIfUsdMarketplace.SetActive(
                    modIsMonetized && 
                    monetizationSettings.MonetizationType == ModioMonetizationType.UsdMarketplace
                    && !disablePurchase
                );

            if (_enableIfVirtualCurrency != null)
                _enableIfVirtualCurrency.SetActive(
                    modIsMonetized &&
                    monetizationSettings.MonetizationType == ModioMonetizationType.VirtualCurrency
                );

            if (_enableIfUnavailable != null)
            {
                _enableIfUnavailable.SetActive(disablePurchase);
            }

            SetValue(mod.Price);
            
            if (_fiatPriceText != null) _fiatPriceText.text = mod.FiatPrice ?? string.Empty;
        }
    }
}
