using System.Threading.Tasks;
using Modio.Extensions;
using Modio.Monetization;
using Modio.Unity.UI.Components.Localization;
using UnityEngine;

namespace Modio.Unity.UI.Scripts.Components
{
    public class ModioUICurrencyName : MonoBehaviour
    {
        ModioUILocalizedText _locText;

        void Awake()
        {
            _locText = GetComponent<ModioUILocalizedText>();
        }

        void Start()
        {
            ModioClient.OnInitialized += OnPluginReady;
        }

        void OnDestroy()
        {
            ModioClient.OnInitialized -= OnPluginReady;
        }

        void OnPluginReady()
        {
            SetTextToCurrencyName().ForgetTaskSafely();
        }

        async Task SetTextToCurrencyName()
        {
            (Error error, string currencyName) = await ModioVirtualCurrencyName.GetVirtualCurrencyName();

            if (error) 
                ModioLog.Warning?.Log($"Error getting the currency name from the mod.io API: {error}");
            
            // The default currency name of Cogs will be used if we fail to get the currency name
            _locText.SetFormatArgs(currencyName);
        }
    }
}
