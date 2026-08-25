using System;
using Modio.Collections;
using Modio.Mods;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyRatingsNegative : ModioPropertyNumberBase, IModProperty, ICollectionProperty
    {
        public void OnModUpdate(Mod mod) => SetValue(mod.Stats.RatingsNegative);
        
        public void OnCollectionUpdate(ModCollection collection)
            => SetValue(collection.Stats.RatingsNegative);
    }
}
