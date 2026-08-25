using System;
using Modio.Mods;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyDateReleased : ModioResourcePropertyDateBase
    {
        protected override DateTime GetValue(IModioInfo mod) => mod.DateLive;
    }
}
