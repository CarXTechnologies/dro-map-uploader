using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Mods;

namespace Modio.Monetization
{
    /// <summary>
    /// A service interface for handling USD marketplace operations.
    /// </summary>
    public interface IModioUsdMarketplaceService
    {
        /// <summary>
        /// Update the local cache of SKUs for a portal
        /// </summary>
        /// <returns>A tuple containing an error if the operation failed, and an array of PortalSku objects representing the updated SKU cache.</returns>
        public Task<Error> UpdateSkuCache();
        
        /// <summary>
        /// Opens the purchase flow for the specified SKU for a portal.
        /// </summary>
        /// <param name="sku">The SKU to be purchased.</param>
        /// <returns>>A task that represents the asynchronous operation. The task result contains an Error object indicating success or failure of the operation.</returns>
        Task<Error> OpenPurchaseFlow(ModSku sku);
        
        /// <summary>
        /// Gets the localized price for the specified SKU.
        /// </summary>
        /// <param name="sku">The SKU for which to get the local price.</param>
        /// <returns>A string representing the localized price of the SKU.</returns>
        string GetLocalPrice(ModSku sku);
        
        /// <summary>
        /// Attempts to purchase the specified mod.
        /// </summary>
        /// <param name="mod">The mod to be purchased.</param>
        /// <param name="subscribeOnPurchase">Whether to subscribe to the mod upon purchase.</param>
        /// <param name="idempotent">An idempotent key to ensure the purchase request is processed only once.</param>
        /// <returns>>A tuple containing an Error object and a PayObject if the purchase was successful.</returns>
        Task<(Error error, PayObject? payObject)> TryPurchase(Mod mod, bool subscribeOnPurchase, string idempotent);

        /// <summary>
        /// Gets all user entitlements for the authenticated user.
        /// </summary>
        /// <returns>The list of entitlements owned by the user along with any error encountered during the operation.</returns>
        Task<(Error error, List<BrowseEntitlementObject> entitlements)> GetAllUserEntitlements();
    }
}
