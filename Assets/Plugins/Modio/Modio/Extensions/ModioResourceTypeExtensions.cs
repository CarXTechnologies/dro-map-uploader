using System;

namespace Modio.Extensions
{
    public static class ModioResourceTypeExtensions
    {
        public static string GetStringCode(this ModioResourceType resourceType) => resourceType switch
        {
            ModioResourceType.Mod        => "mods",
            ModioResourceType.Collection => "collections",
            _                            => throw new ArgumentException(),
        };
    }
}
