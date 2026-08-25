using System;

namespace Modio.Monetization
{
    [Serializable]
    public class MonetizationSettings : IModioServiceSettings
    {
        // So long as this object is included in the platform settings, Monetization
        // is considered to be active for the C# SDK
        public string CurrencyFallbackName = "Cogs";
        public ModioMonetizationType MonetizationType = ModioMonetizationType.VirtualCurrency; 
    }
}
