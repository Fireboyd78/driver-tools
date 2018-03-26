using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public struct SpoolerContext : IEquatable<int>, IEquatable<string>, IEquatable<ChunkType>, IEquatable<SpoolerContext>, IComparable<string>
    {
        private int m_value;

        public static implicit operator ChunkType(SpoolerContext context)
        {
            return (ChunkType)context.m_value;
        }

        public static implicit operator int (SpoolerContext context)
        {
            return context.m_value;
        }

        public static implicit operator SpoolerContext(int context)
        {
            return new SpoolerContext(context);
        }

        public static implicit operator SpoolerContext(ChunkType chunkType)
        {
            return new SpoolerContext((int)chunkType);
        }

        public static implicit operator SpoolerContext(string context)
        {
            if (context == null || context.Length != 4)
                throw new ArgumentException("Context string must be 4 characters long and cannot be null.", nameof(context));

            var c1 = context[0];
            var c2 = context[1];
            var c3 = context[2];
            var c4 = context[3];

            return new SpoolerContext((c4 << 24) | (c3 << 16) | (c2 << 8) | (c1 << 0));
        }

        private unsafe int FastCompare(string value)
        {
            fixed (int* pValue = &m_value)
            fixed (char* pOther = value)
            {
                var ptr = pOther;

                var len = value.Length;
                var idx = 0;
                
                for (idx = 0; idx < len; idx++)
                {
                    var a = *((byte*)pValue + idx);
                    var b = *(ptr++);

                    if ((a != b) && (b != '*'))
                        return -1;
                }

                return ((idx + 1) == len) ? 0 : idx;
            }
        }

        public override int GetHashCode()
        {
            return m_value;
        }

        public int CompareTo(string value)
        {
            if (String.IsNullOrEmpty(value) || value.Length > 4)
                return -1;

            return FastCompare(value);
        }
        
        public bool Equals(int value)
        {
            return m_value == value;
        }

        public bool Equals(string value)
        {
            return CompareTo(value) == 0;
        }

        public bool Equals(ChunkType chunkType)
        {
            return m_value == (int)chunkType;
        }

        public bool Equals(SpoolerContext other)
        {
            return m_value == other.m_value;
        }

        public override bool Equals(object obj)
        {
            if (obj is int)
                return m_value == (int)obj;
            if (obj is string)
                return Equals((string)obj);
            if (obj is SpoolerContext)
                return Equals((SpoolerContext)obj);
            if (obj is ChunkType)
                return Equals((ChunkType)obj);

            return false;
        }

        public override string ToString()
        {
            var str = "";

            if (m_value > 0)
            {
                for (int b = 0; b < 4; b++)
                {
                    var c = (char)((m_value >> (b * 8)) & 0xFF);

                    if (c != '\0')
                        str += c;
                }
            }

            return str;
        }

        private SpoolerContext(int context)
        {
            m_value = context;
        }
    }

    public enum SpoolerAlignment : byte
    {
        /// <summary>
        /// 4-byte alignment (e.g. 0x3 -> 0x4)
        /// </summary>
        Align4      = 0x2,

        /// <summary>
        /// 16-byte alignment (e.g. 0x3 -> 0x10)
        /// </summary>
        Align16     = 0x4,

        /// <summary>
        /// 256-byte alignment (e.g. 0x3 -> 0x100)
        /// </summary>
        Align256    = 0x8,
        
        /// <summary>
        /// 2048-byte alignment (e.g. 0x3 -> 0x800)
        /// </summary>
        Align2048   = 0xB,

        /// <summary>
        /// 4096-byte alignment (e.g. 0x3 -> 0x1000)
        /// </summary>
        Align4096   = 0xC
    }
    
    /// <summary>
    /// Represents an abstract class for data spoolers.
    /// </summary>
    public abstract class Spooler : IDisposable
    {
        private SpoolerAlignment _alignment = SpoolerAlignment.Align4096;
        
        private int _context;
        private byte _version;

        private string _description;

        public static explicit operator ChunkEntry(Spooler spooler)
        {
            return new ChunkEntry() {
                Context     = spooler.Context,
                Offset      = spooler.Offset,
                Version     = spooler.Version,
                StrLen      = spooler.StrLen,
                Alignment   = spooler.Alignment,
                Reserved    = 0xFB, // ;)
                Size        = spooler.Size,
            };
        }
        
        /// <summary>
        /// Disposes of the resources used by this spooler.
        /// </summary>
        public virtual void Dispose()
        {

        }

        /// <summary>
        /// Gets or sets the description for this spooler. The length should not exceed 255 characters.
        /// <para>Setting this value to 'null' will force an empty string to be used instead.</para>
        /// </summary>
        public string Description
        {
            get { return (_description != null) ? _description : String.Empty; }
            set
            {
                var str = value;

                if (_description != null && _description != str)
                    IsDirty = true;

                if (str != null)
                {
                    // string too long? no worries, we'll just force it to be 255 characters long :)
                    if (str.Length > 255)
                        str = str.Substring(0, 255);

                    _description = str;
                }
                else
                {
                    _description = String.Empty;
                }

                if (IsDirty)
                    IsModified = true;
            }
        }

        /// <summary>
        /// Gets or sets the context of this spooler.
        /// </summary>
        public int Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                    IsModified = true;

                _context = value;
            }

        }

        /// <summary>
        /// Gets or sets the magic number for this spooler.
        /// </summary>
        [Obsolete("Use the 'Context' property instead -- this is the original, incorrect name.")]
        public int Magic
        {
            get { return Context; }
            set { Context = value; }
        }
        
        /// <summary>
        /// Gets the absolute position of this spooler, relative to its parent.
        /// If this spooler is not attached to a parent, this will return zero.
        /// </summary>
        public int BaseOffset
        {
            get
            {
                if (Parent != null)
                {
                    int offset = Offset;

                    var parent = Parent;

                    while (parent != null)
                    {
                        offset += parent.Offset;
                        parent = parent.Parent;
                    }

                    return offset;
                }

                // if there's no parent, there's no "offset"
                return 0;
            }
        }

        /// <summary>
        /// Gets or sets the offset of this spooler. Intended for internal use only.
        /// </summary>
        internal int Offset { get; set; }

        /// <summary>
        /// Gets or sets the version of this spooler.
        /// </summary>
        public byte Version
        {
            get { return _version; }
            set
            {
                if (_version != value)
                    IsModified = true;

                _version = value;
            }
        }

        /// <summary>
        /// Gets or sets the reserved byte for this spooler.
        /// </summary>
        [Obsolete("Use the 'Version' property instead -- this is the original, incorrect name.")]
        public byte Reserved
        {
            get { return Version; }
            set { Version = value; }
        }
        
        /// <summary>
        /// Gets the length of the description for this spooler.
        /// </summary>
        public byte StrLen
        {
            get { return (byte)Description.Length; }
        }

        /// <summary>
        /// Gets or sets the byte-alignment of this spooler.
        /// </summary>
        public SpoolerAlignment Alignment
        {
            get { return _alignment; }
            set
            {
                if (_alignment != value)
                    IsModified = true;

                _alignment = value;
            }
        }

        /// <summary>
        /// Gets the size of this spooler. If an inherited class does not implement this property, a value of zero is always returned.
        /// </summary>
        public virtual int Size
        {
            get { return 0; }
        }

        public virtual bool AreChangesPending
        {
            get { return IsModified; }
        }

        public virtual void CommitChanges()
        {
            IsModified = false;
        }

        public virtual void NotifyChanges()
        {
            IsModified = true;
        }
        
        /// <summary>
        /// Gets the <see cref="SpoolablePackage"/> containing this spooler. If no parent is attached, the value is null.
        /// </summary>
        public SpoolablePackage Parent { get; internal set; }

        /// <summary>
        /// Gets or sets the dirty status of this spooler.
        /// </summary>
        protected internal bool IsDirty { get; set; }

        protected internal bool IsModified { get; set; }

        /// <summary>
        /// Ensures the spooler has been detached from its parent, if applicable.
        /// </summary>
        protected virtual void EnsureDetach()
        {
            // make sure we detach from our parent
            if (Parent != null)
                Parent.Children.Remove(this);
        }
    }
}
