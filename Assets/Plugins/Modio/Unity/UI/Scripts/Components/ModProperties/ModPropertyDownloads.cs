using System;
using Modio.Collections;
using Modio.Mods;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyDownloads : ModioPropertyNumberBase, IModProperty, ICollectionProperty
    {
        public void OnModUpdate(Mod mod) => SetValue(mod.Stats.Downloads);

        public void OnCollectionUpdate(ModCollection collection) => SetValue(collection.Stats.Downloads);
    }
}
