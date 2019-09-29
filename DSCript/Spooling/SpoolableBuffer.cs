using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public sealed class SpoolableBuffer : Spooler, IDisposable
    {
        public static readonly int MaxBufferSize = 1024 * 384; //~384Kb

        private byte[] m_buffer;
        private int m_size;
        
        private DSCTempFile m_tempFile;

        private void GetTempFileReady()
        {
            if (m_tempFile == null)
                m_tempFile = new DSCTempFile();
        }

        private void ReleaseTempFile()
        {
            if (m_tempFile != null)
            {
                m_tempFile.Dispose();
                m_tempFile = null;
            }
        }

        private void DetachChunker()
        {
            if (FileChunker != null)
            {
                FileChunker = null;
                FileOffset = 0;
            }
        }

        /// <summary>
        /// Gets or sets the file offset of this spooler. Intended for internal use only.
        /// </summary>
        internal int FileOffset { get; set; }

        /// <summary>
        /// Gets or sets the file chunker for this spooler. Intended for internal use only.
        /// </summary>
        internal FileChunker FileChunker { get; set; }

        /// <summary>
        /// Returns the buffer directly. Intended for internal use only.
        /// </summary>
        /// <returns>A direct copy of the buffer.</returns>
        internal byte[] GetBufferInternal()
        {
            if (m_buffer == null)
            {
                if (FileChunker != null)
                {
                    var buffer = FileChunker.GetBuffer(this);

                    m_size = (buffer != null) ? buffer.Length : 0;
                    
                    if (m_size > MaxBufferSize)
                    {
                        GetTempFileReady();

                        m_tempFile.SetBuffer(buffer);
                        m_buffer = null;
                    }
                    else
                    {
                        m_buffer = buffer;
                    }

                    // once you extract, you can't go back (lol)
                    DetachChunker();

                    // return the local copy?
                    if (m_buffer == null)
                        return buffer;
                }
                else if (m_tempFile != null)
                {
                    // retrieve buffer from temp file
                    return m_tempFile.GetBuffer();
                }
                else
                {
                    // return an empty buffer of '_size' length - what could POSSIBLY go wrong?
                    return new byte[m_size];
                }
            }

            return m_buffer;
        }

        /// <summary>
        /// Returns a <see cref="MemoryStream"/> based on a local copy of the buffer. Changes made to the local copy will not reflect upon the buffer attached to the spooler.
        /// </summary>
        /// <returns>A <see cref="MemoryStream"/> based on a local copy of the buffer. local copy of the buffer.</returns>
        /// <remarks>This method is a shortcut for creating a new <see cref="MemoryStream"/> using the <see cref="GetBuffer()"/> method.</remarks>
        public MemoryStream GetMemoryStream()
        {
            return new MemoryStream(GetBuffer());
        }

        /// <summary>
        /// Returns a local copy of the buffer. Changes made to the local copy will not reflect upon the buffer attached to the spooler.
        /// </summary>
        /// <returns>A local copy of the buffer.</returns>
        public byte[] GetBuffer()
        {
            var buffer = GetBufferInternal();

            // check if we're receiving an unattached buffer (temp file buffer, empty buffer, etc.)
            if (m_buffer == null)
                return buffer;

            var localBuf = new byte[m_size];

            // copy from existing buffer
            Buffer.BlockCopy(m_buffer, 0, localBuf, 0, m_size);

            return localBuf;
        }

        /// <summary>
        /// Sets the content of the buffer. If this spooler is attached to a parent, the parent will adjust its size accordingly.
        /// </summary>
        /// <param name="buffer">The buffer containing the new data.</param>
        public void SetBuffer(byte[] buffer)
        {
            m_size = (buffer != null) ? buffer.Length : 0;

            // for large chunks of data, use a temp file
            if (m_size > MaxBufferSize)
            {
                GetTempFileReady();

                m_tempFile.SetBuffer(buffer);

                // free up the buffer
                // this will also let GetBufferInternal() know we're using a temp file
                m_buffer = null;
            }
            else
            {
                // use the buffer in memory
                m_buffer = buffer;
            }

            // Don't forget to detach from the chunker
            DetachChunker();

            // set our dirty flag
            IsDirty = true;
            IsModified = true;
        }

        /// <summary>
        /// Clears the contents of the buffer.
        /// </summary>
        public void ClearBuffer()
        {
            // SetBuffer handles everything nicely, we'll let it handle everything for us
            SetBuffer(null);
        }

        public override void Dispose()
        {
            ClearBuffer();
            EnsureDetach();

            // lastly, release the temp file (if applicable)
            ReleaseTempFile();
        }

        /// <summary>
        /// Returns the size of the buffer.
        /// </summary>
        public override int Size
        {
            get { return m_size; }
        }

        public SpoolableBuffer()
        {
            m_size = 0;
        }

        public SpoolableBuffer(int size)
        {
            m_size = size;
        }
    }
}
