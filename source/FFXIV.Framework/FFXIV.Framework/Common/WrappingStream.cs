using System;
using System.IO;
using System.Threading.Tasks;

namespace FFXIV.Framework.Common
{
    /// <summary>
    /// ストリームのWrapperクラス
    /// </summary>
    /// <remarks>
    /// Dispose 時に、内部ストリームの参照を外します
    /// </remarks>
    public class WrappingStream : Stream
    {
        private Stream m_streamBase;

        public WrappingStream(Stream streamBase)
        {
            if (streamBase == null)
            {
                throw new ArgumentNullException("streamBase");
            }

            m_streamBase = streamBase;
        }

        public override bool CanRead => this.m_streamBase.CanRead;

        public override bool CanSeek => this.m_streamBase.CanSeek;

        public override bool CanWrite => this.m_streamBase.CanWrite;

        public override long Length => this.m_streamBase.Length;

        public override long Position
        {
            get => this.m_streamBase.Position;
            set => this.m_streamBase.Position = value;
        }

        public override void Flush() => this.m_streamBase?.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            this.m_streamBase.Read(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return m_streamBase.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public new Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            return m_streamBase.ReadAsync(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            this.m_streamBase.Seek(offset, origin);

        public override void SetLength(long value) =>
            this.m_streamBase.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) =>
            this.m_streamBase.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_streamBase.Dispose();
                m_streamBase = null;
            }

            base.Dispose(disposing);
        }

        public byte[] ToArray()
        {
            if (this.m_streamBase is MemoryStream ms)
            {
                return ms.ToArray();
            }

            return null;
        }

        private void ThrowIfDisposed()
        {
            if (m_streamBase == null)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
