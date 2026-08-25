using System;
using Modio.Collections;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class CollectionPropertyModCount : ModioPropertyNumberBase, ICollectionProperty
    {
        public void OnCollectionUpdate(ModCollection collection) => SetValue(collection.Stats.ModsTotal);
    }
}
