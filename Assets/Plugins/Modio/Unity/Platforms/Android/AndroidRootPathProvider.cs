using System.Threading.Tasks;
using Modio.FileIO;
using UnityEngine;

namespace Modio.Unity.Platforms.Android
{
    public class AndroidRootPathProvider : IModioRootPathProvider
    {
        public string Path => $"{Application.persistentDataPath}/UnityCache/";
        public Task<string> GetUserPath() => Task.FromResult($"{Application.persistentDataPath}");
    }
}
