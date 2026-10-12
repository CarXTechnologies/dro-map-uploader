using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Modio.FileIO
{
    // CARX PATCH: upload progress of ModfileBuilder.PublishModfile, see CARX_PATCHES.md section 2.
    /// <summary>
    /// Read only pass-through stream counting the bytes read from the wrapped stream. <see cref="BytesRead"/> may be
    /// read from another thread than the one reading. Does not own the wrapped stream.
    /// </summary>
    internal sealed class ReadCountingStreamWrapper : Stream
    {
        readonly Stream _baseStream;
        long _bytesRead;

        public ReadCountingStreamWrapper(Stream baseStream) => _baseStream = baseStream;

        public long BytesRead => Interlocked.Read(ref _bytesRead);

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _baseStream.Read(buffer, offset, count);

            if (bytesRead > 0) Interlocked.Add(ref _bytesRead, bytesRead);

            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // Not resuming on the caller's context: callers such as Stream.CopyToAsync do not either.
            int bytesRead = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);

            if (bytesRead > 0) Interlocked.Add(ref _bytesRead, bytesRead);

            return bytesRead;
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _baseStream.Length;
        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }
    }
}
