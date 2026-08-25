using Modio.Collections;
using Modio.Mods;

namespace Modio.Unity.UI.Components.ModProperties
{
    public abstract class ModioResourceProperty : IModProperty, ICollectionProperty
    {
        protected abstract void OnResourceUpdate(IModioInfo resource); 
        
        public void OnModUpdate(Mod mod)                         => OnResourceUpdate(mod);
        public void OnCollectionUpdate(ModCollection collection) => OnResourceUpdate(collection);
    }
}
