using System;
using System.Threading.Tasks;

namespace Modio.FileIO
{
    public interface IModioRootPathProvider
    {
        /// <summary>
        /// Path of Mod Installs
        /// </summary>
        public string Path
        {
            get;
        }

        /// <summary>
        /// Retrieve the path for User Data
        /// </summary>
        Task<string> GetUserPath();
    }
}
