using System;
using System.Threading;

namespace Modio.API
{
    // CARX PATCH: upload progress of ModfileBuilder.PublishModfile, see CARX_PATCHES.md section 2.
    /// <summary>
    /// Ambient progress of the transfer running in the current async flow, so code deep in the call chain
    /// (<see cref="Modio.FileIO.BaseDataStorage.CompressToZip"/>, the API client sending a request) can report it
    /// without upstream signatures changing.
    /// </summary>
    /// <remarks>
    /// The listener is held in an <see cref="AsyncLocal{T}"/>: a scope opened by <see cref="BeginScope"/> is seen by
    /// everything that async flow awaits, and by nothing else. Reports are made on the thread of the reporting code,
    /// and are a cheap no-op while no scope is open.
    /// </remarks>
    public static class ModioTransferProgress
    {
        public interface IListener
        {
            /// <summary>Source bytes read so far while zipping, of <paramref name="totalBytes"/> (0 when unknown).</summary>
            void OnCompress(long doneBytes, long totalBytes);

            /// <summary>Fraction (0..1) of the body of the request being sent that has left the machine.</summary>
            void OnRequestUpload(float fraction);
        }

        static readonly AsyncLocal<IListener> _listener = new AsyncLocal<IListener>();

        /// <summary>True while a scope is open in the current async flow.</summary>
        public static bool IsListening => _listener.Value != null;

        /// <summary>Makes <paramref name="listener"/> receive the reports of the current async flow until the
        /// returned scope is disposed; disposing restores the listener that was there before.</summary>
        public static IDisposable BeginScope(IListener listener) => new Scope(listener);

        public static void ReportCompress(long doneBytes, long totalBytes)
            => _listener.Value?.OnCompress(doneBytes, totalBytes);

        public static void ReportRequestUpload(float fraction) => _listener.Value?.OnRequestUpload(fraction);

        sealed class Scope : IDisposable
        {
            readonly IListener _previous;
            bool _disposed;

            public Scope(IListener listener)
            {
                _previous = _listener.Value;
                _listener.Value = listener;
            }

            public void Dispose()
            {
                if (_disposed) return;

                _disposed = true;
                _listener.Value = _previous;
            }
        }
    }
}
