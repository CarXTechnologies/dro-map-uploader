using System.Threading.Tasks;
using Modio.Mods;

namespace Modio.Monetization
{
    public class ModioVirtualCurrencyName
    {
        const string FALLBACK_NAME = "Cogs";
        
        public static async Task<(Error error, string name)> GetVirtualCurrencyName()
        {
            (Error readError, GameData data) = await GameData.GetGameData();

            if (!readError && !string.IsNullOrEmpty(data.CurrencyName))
                return (Error.None, data.CurrencyName);

            return (readError, ModioClient.Settings.TryGetPlatformSettings(out MonetizationSettings monetizationSettings)
                        ? monetizationSettings.CurrencyFallbackName
                        : FALLBACK_NAME);
        }
    }
}
