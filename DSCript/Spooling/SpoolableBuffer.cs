using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public sealed class SpoolableBuffer : Spooler, IDisposable
    {
        private static readonly int maxBufferSize = 1024 * 384; //~384Kb

        private byte[] _buffer;
        private int _size;
        private string _tempFileName;

        private string TempFileName
        {
            get
            {
                if (String.IsNullOrEmpty(_tempFileName))
                {
                    var tmpFile = Path.GetTempFileName();

                    // GetTempFileName() creates a temp file, delete it
                    File.Delete(tmpFile);

                    _tempFileName = Path.Combine(DSC.TempDirectory, Path.GetFileName(tmpFile));
                }

                return _tempFileName;
            }
        }

        private FileStream TempFile;

        private void CleanupTempFile()
        {
            if (TempFile != null)
            {
                TempFile.Dispose();
                TempFile = null;

                File.Delete(TempFileName);
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
            if (_buffer == null)
            {
                if (FileChunker != null)
                {
                    _buffer = FileChunker.GetBuffer(this);

                    // once you extract, you can't go back (lol)
                    DetachChunker();
                }
                else if (TempFile != null)
                {
                    var buf = new byte[_size];

                    TempFile.Position = 0;
                    TempFile.Read(buf, 0, _size);

                    return buf;
                }
                else
                {
                    // return an empty buffer of '_size' length - what could POSSIBLY go wrong?
                    return new byte[_size];
                }
            }

            return _buffer;
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
            // intBuf = 'internal buffer'
            // locBuf = 'local buffer'

            var intBuf = GetBufferInternal();

            // check if we're receiving an unattached buffer (temp file buffer, empty buffer, etc.)
            if (_buffer == null)
                return intBuf;

            var locBuf = new byte[_size];

            // copy from existing buffer
            Array.Copy(_buffer, locBuf, _size);

            return locBuf;
        }

        /// <summary>
        /// Sets the content of the buffer. If this spooler is attached to a parent, the parent will adjust its size accordingly.
        /// </summary>
        /// <param name="buffer">The buffer containing the new data.</param>
        public void SetBuffer(byte[] buffer)
        {
            _size = (buffer != null) ? buffer.Length : 0;

            if (_size > maxBufferSize)
            {
                if (TempFile == null)
                    TempFile = File.Create(TempFileName);

                TempFile.SetLength(_size);

                TempFile.Position = 0;
                TempFile.Write(buffer, 0, _size);

                _buffer = null;
            }
            else
            {
                CleanupTempFile();
                _buffer = buffer;
            }

            // Don't forget to detach from the chunker
            DetachChunker();

            // set our dirty flag
            IsDirty = true;
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
        }

        /// <summary>
        /// Returns the size of the buffer.
        /// </summary>
        public override int Size
        {
            get { return _size; }
        }

        public SpoolableBuffer()
        {
            _size = 0;
        }

        public SpoolableBuffer(int size)
        {
            _size = size;
        }

        ~SpoolableBuffer()
        {
            Dispose();
        }
    }
}
