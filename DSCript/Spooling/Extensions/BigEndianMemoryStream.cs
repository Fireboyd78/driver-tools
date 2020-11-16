using System.IO;

namespace DSCript.Spooling
{
    public sealed class BigEndianMemoryStream : MemoryStream
    {
        bool m_oldEndianStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (StreamExtensions.UseBigEndian != m_oldEndianStatus))
                StreamExtensions.UseBigEndian = m_oldEndianStatus;

            base.Dispose(disposing);
        }

        private void InitEndianStatus()
        {
            m_oldEndianStatus = StreamExtensions.UseBigEndian;
            StreamExtensions.UseBigEndian = true;
        }

        public BigEndianMemoryStream()
        {
            InitEndianStatus();
        }

        public BigEndianMemoryStream(int capacity) : base(capacity)
        {
            InitEndianStatus();
        }

        public BigEndianMemoryStream(byte[] buffer) : base(buffer)
        {
            InitEndianStatus();
        }

        public BigEndianMemoryStream(byte[] buffer, bool writable) : base(buffer, writable)
        {
            InitEndianStatus();
        }

        public BigEndianMemoryStream(byte[] buffer, int index, int count) : base(buffer, index, count)
        {
            InitEndianStatus();
        }

        public BigEndianMemoryStream(byte[] buffer, int index, int count, bool writable) : base(buffer, index, count, writable)
        {
            InitEndianStatus();
        }

        public BigEndianMemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible) : base(buffer, index, count, writable, publiclyVisible)
        {
            InitEndianStatus();
        }
    }
}
