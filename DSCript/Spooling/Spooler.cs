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

        public static implicit operator int(SpoolerContext context)
        {
            return context.m_value;
        }

        public static implicit operator SpoolerContext(int context)
        {
            return new SpoolerContext(context);
        }

        public static implicit operator SpoolerContext(ChunkType chunkType)
        {
            return new SpoolerContext(chunkType);
        }

        public static implicit operator SpoolerContext(string context)
        {
            return new SpoolerContext(context);
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
            // TODO: make a proper formatter
            //
            //var str = "";
            //
            //if (m_value > 0)
            //{
            //    for (int b = 0; b < 4; b++)
            //    {
            //        var c = (char)((m_value >> (b * 8)) & 0xFF);
            //
            //        if (c != '\0')
            //            str += c;
            //    }
            //}
            //
            //return str;

            return m_value.ToString();
        }
        
        public SpoolerContext(int context)
        {
            m_value = context;
        }

        public SpoolerContext(ChunkType chunkType)
        {
            m_value = (int)chunkType;
        }

        public SpoolerContext(string context)
        {
            if (context == null || context.Length != 4)
                throw new ArgumentException("Context string must be 4 characters long and cannot be null.", nameof(context));

            var c1 = context[0];
            var c2 = context[1];
            var c3 = context[2];
            var c4 = context[3];

            m_value = ((c4 << 24) | (c3 << 16) | (c2 << 8) | (c1 << 0));
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
        /// 1024-byte alignment (e.g. 0x3 -> 0x400)
        /// </summary>
        Align1024   = 0xA,

        /// <summary>
        /// 2048-byte alignment (e.g. 0x3 -> 0x800)
        /// </summary>
        Align2048   = 0xB,

        /// <summary>
        /// 4096-byte alignment (e.g. 0x3 -> 0x1000)
        /// </summary>
        Align4096   = 0xC,
    }

    public interface ICopySpooler<T> : ICopyCat<T>
        where T : Spooler { }

    /// <summary>
    /// Represents an abstract class for data spoolers.
    /// </summary>
    public abstract class Spooler : IDisposable, ICopySpooler<Spooler>
    {
        bool ICopyCat<Spooler>.CanCopy(CopyClassType copyType)                  => CanCopy(copyType);
        bool ICopyCat<Spooler>.CanCopyTo(Spooler obj, CopyClassType copyType)   => CanCopyTo(obj, copyType);

        bool ICopyCat<Spooler>.IsCopyOf(Spooler obj, CopyClassType copyType)    => IsCopyOf(obj, copyType);

        Spooler ICopyClass<Spooler>.Copy(CopyClassType copyType)                => Copy(copyType);
        void ICopyClassTo<Spooler>.CopyTo(Spooler obj, CopyClassType copyType)  => CopyTo(obj, copyType);

        protected abstract bool CanCopy(CopyClassType copyType);
        protected abstract bool CanCopyTo(Spooler obj, CopyClassType copyType);

        protected abstract bool IsCopyOf(Spooler obj, CopyClassType copyType);

        protected abstract Spooler Copy(CopyClassType copyType);
        protected abstract void CopyTo(Spooler obj, CopyClassType copyType);

        private SpoolerAlignment _alignment = SpoolerAlignment.Align4096;
        
        private SpoolerContext _context;
        private int _version;

        private string _description;

        public static explicit operator ChunkEntry(Spooler spooler)
        {
            return new ChunkEntry() {
                Context     = spooler.Context,
                Offset      = spooler.Offset,
                Version     = (byte)spooler.Version,
                StrLen      = (byte)spooler.Description.Length,
                Alignment   = (byte)spooler.Alignment,
                Reserved    = 0xFB, // ;)
                Size        = spooler.Size,
            };
        }

        protected void CopyParamsTo(Spooler other)
        {
            other.Alignment = Alignment;
            other.Context = Context;
            other.Description = Description;
            other.Version = Version;
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
            get { return _description ?? String.Empty; }
            set
            {
                var str = value;
                
                if (str != null)
                {
                    // string too long? no worries, we'll just force it to be 255 characters long :)
                    if (str.Length > 255)
                        str = str.Substring(0, 255);
                }
                else
                {
                    str = String.Empty;
                }

                var dirty = false;

                // if we're clean, see if we need to report size changes
                if (!IsDirty)
                {
                    if (Description != str)
                    {
                        // if they're the same length, we don't need to recalculate anything
                        if (str.Length == Description.Length)
                            dirty = false;
                        else
                            dirty = true;
                    }
                }

                // set the new description
                _description = str;

                NotifyChanges(dirty);
            }
        }

        /// <summary>
        /// Gets or sets the context of this spooler.
        /// </summary>
        public SpoolerContext Context
        {
            get { return _context; }
            set
            {
                if (!_context.Equals(value))
                    NotifyChanges(false);

                _context = value;
            }

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

                    if (offset > 0)
                    {
                        var parent = Parent;

                        while (parent != null)
                        {
                            offset += parent.Offset;
                            parent = parent.Parent;
                        }
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
        public int Version
        {
            get { return _version; }
            set
            {
                if (_version != value)
                    NotifyChanges(false);

                _version = value;
            }
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
                    NotifyChanges(true);

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
            get
            {
                // flagged for an update?
                if (IsModified)
                    return true;

                // if we were previously orphaned,
                // verify our offset is correct
                if (Parent != null)
                {
                    // we need our offset updated by our parent
                    if (Offset <= 0)
                        return true;
                }

                // no changes are pending
                return false;
            }
        }

        protected bool NotifyAllParents(bool dirty)
        {
            var parent = Parent;

            if (parent != null)
            {
                do
                {
                    parent.NotifyChanges();

                    if (dirty)
                        parent.IsDirty = true;

                    parent = parent.Parent;
                } while (parent != null);

                return true;
            }

            return false;
        }

        public virtual void CommitChanges()
        {
            IsModified = false;
            IsDirty = false;
        }

        public virtual void NotifyChanges(bool dirty = false)
        {
            // only notify of changes if we're clean;
            // always ensure our parents get flagged as dirty if needed
            if (!IsDirty)
            {
                // flag our changes
                IsModified = true;

                // notify all parents of our change;
                // if this fails, we're completely detached
                if (NotifyAllParents(dirty))
                {
                    // flag ourselves as dirty?
                    if (dirty)
                        IsDirty = true;
                }
                else if (dirty)
                {
                    // reset our offset because we're orphaned;
                    // when we do eventually get attached,
                    // our parent will update it for us
                    Offset = 0;

                    // ignore future changes until we're cleaned
                    IsDirty = true;
                }
            }
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

        protected internal virtual void SetCommon(ref ChunkEntry entry)
        {
            _alignment = (SpoolerAlignment)entry.Alignment;
            _context = entry.Context;
            _version = entry.Version;

            Offset = entry.Offset;
        }

        /// <summary>
        /// Ensures the spooler has been detached from its parent, if applicable.
        /// </summary>
        protected virtual void EnsureDetach()
        {
            // make sure we detach from our parent
            if (Parent != null)
                Parent.Children.Remove(this);
        }

        protected Spooler() { }
        protected Spooler(ref ChunkEntry entry)
        {
            SetCommon(ref entry);
        }
    }
}
