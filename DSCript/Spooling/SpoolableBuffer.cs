using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public sealed class SpoolableBuffer : Spooler, IDisposable, ICopySpooler<SpoolableBuffer>
    {
        bool ICopyCat<SpoolableBuffer>.CanCopy(CopyClassType copyType)                          => true;
        bool ICopyCat<SpoolableBuffer>.CanCopyTo(SpoolableBuffer obj, CopyClassType copyType)   => true;

        bool ICopyCat<SpoolableBuffer>.IsCopyOf(SpoolableBuffer obj, CopyClassType copyType)
        {
            throw new NotImplementedException();
        }

        SpoolableBuffer ICopyClass<SpoolableBuffer>.Copy(CopyClassType copyType)
        {
            return GetCopy(copyType);
        }

        void ICopyClassTo<SpoolableBuffer>.CopyTo(SpoolableBuffer obj, CopyClassType copyType)
        {
            CopyTo(obj, copyType);
        }

        protected override bool CanCopy(CopyClassType copyType)
        {
            return true;
        }

        protected override bool CanCopyTo(Spooler obj, CopyClassType copyType)
        {
            return (obj is SpoolableBuffer);
        }

        protected override bool IsCopyOf(Spooler obj, CopyClassType copyType)
        {
            throw new NotImplementedException();
        }

        protected override Spooler Copy(CopyClassType copyType)
        {
            return GetCopy(copyType);
        }

        protected override void CopyTo(Spooler obj, CopyClassType copyType)
        {
            var spooler = obj as SpoolableBuffer;

            if (spooler == null)
                throw new Exception("Cannot copy spoolable buffer; type mismatch!");

            CopyTo(spooler, copyType);
        }

        private SpoolableBuffer GetCopy(CopyClassType copyType)
        {
            var spooler = new SpoolableBuffer();

            CopyTo(spooler, copyType);

            return spooler;
        }

        private void CopyTo(SpoolableBuffer obj, CopyClassType copyType)
        {
            obj.IsDirty = true;

            CopyParamsTo(obj);

            if (copyType == CopyClassType.DeepCopy)
            {
                // copy the buffer as well
                var buffer = GetBuffer();

                obj.SetBuffer(buffer);
            }

            obj.CommitChanges();
        }

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

        public override bool AreChangesPending
        {
            get
            {
                if (IsDirty)
                {
                    Debug.WriteLine("**** AreChangesPending called on a dirty buffer!");
                    return true;
                }

                return base.AreChangesPending;
            }
        }

        internal void SetRawBuffer(byte[] buffer)
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
        }

        internal byte[] GetRawBuffer(bool detach = false)
        {
            if (m_buffer != null)
                return m_buffer;

            var buffer = new byte[m_size];

            if (FileChunker != null)
            {
                buffer = FileChunker.GetBuffer(this);

                if (detach)
                    SetRawBuffer(buffer);
            }
            else if (m_tempFile != null)
            {
                // retrieve buffer from temp file
                buffer = m_tempFile.GetBuffer();
            }

            return buffer;
        }

        /// <summary>
        /// Returns a <see cref="MemoryStream"/> based on a local copy of the buffer. Changes made to the local copy will not reflect upon the buffer attached to the spooler.
        /// </summary>
        /// <returns>A <see cref="MemoryStream"/> based on a local copy of the buffer. local copy of the buffer.</returns>
        /// <remarks>This method is a shortcut for creating a new <see cref="MemoryStream"/> using the <see cref="GetBuffer()"/> method.</remarks>
        public MemoryStream GetMemoryStream()
        {
            var buffer = GetBuffer();

            return (FileChunker.HACK_BigEndian)
                ? new BigEndianMemoryStream(buffer)
                : new MemoryStream(buffer);
        }

        /// <summary>
        /// Returns a local copy of the buffer. Changes made to the local copy will not reflect upon the buffer attached to the spooler.
        /// </summary>
        /// <returns>A local copy of the buffer.</returns>
        public byte[] GetBuffer()
        {
            var buffer = GetRawBuffer(true);

            if (Object.ReferenceEquals(buffer, m_buffer))
            {
                buffer = new byte[m_size];

                // copy from existing buffer
                Buffer.BlockCopy(m_buffer, 0, buffer, 0, m_size);
            }

            return buffer;
        }

        /// <summary>
        /// Sets the content of the buffer. If this spooler is attached to a parent, the parent will adjust its size accordingly.
        /// </summary>
        /// <param name="buffer">The buffer containing the new data.</param>
        public void SetBuffer(byte[] buffer)
        {
            var dirty = false;
            var oldSize = m_size;

            SetRawBuffer(buffer);

            // if sizes are different, have our parents recalculate
            if (buffer.Length != oldSize)
                dirty = true;

            NotifyChanges(dirty);
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
            EnsureDetach();

            // don't notify of buffer changes
            SetRawBuffer(null);
            Offset = 0;

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

        public SpoolableBuffer(ref ChunkEntry entry)
            : base(ref entry)
        {
            m_size = entry.Size;
        }
    }
}
