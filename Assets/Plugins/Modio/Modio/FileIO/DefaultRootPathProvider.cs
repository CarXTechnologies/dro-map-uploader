using System;
using System.Threading.Tasks;

namespace Modio.FileIO
{
    public class DefaultRootPathProvider : IModioRootPathProvider
    {
        public virtual string Path => $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}";
        public Task<string> GetUserPath() => Task.FromResult(Path);
    }
}
