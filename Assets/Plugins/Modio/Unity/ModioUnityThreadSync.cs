using System.Threading;

namespace Modio.Unity
{
    public static class ModioUnityThreadSync
    {
        public static SynchronizationContext SynchronizationContext
        {
            get
            {
                if (_syncContext is null)
                    ModioLog.Error?.Log(
                        $"Synchronization context is null! Please call {nameof(InitializeThreadSync)} from the Unity main thread before getting the context!"
                    );
                
                return _syncContext;
            }
        }
        static SynchronizationContext _syncContext;

        public static void InitializeThreadSync() => _syncContext ??= SynchronizationContext.Current;
    }
}
