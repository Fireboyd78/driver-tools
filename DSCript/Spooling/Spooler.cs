using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public enum SpoolerAlignment : byte
    {
        /// <summary>
        /// 4-byte alignment (e.g. 0x7 -> 0x8)
        /// </summary>
        Align4      = 0x2,
        
        /// <summary>
        /// 16-byte alignment (e.g. 0x7 -> 0x10)
        /// </summary>
        Align16     = 0x4,

        /// <summary>
        /// 2048-byte alignment (e.g. 0x7 -> 0x800)
        /// </summary>
        Align2048   = 0xB,

        /// <summary>
        /// 4096-byte alignment (e.g. 0x7 -> 0x1000)
        /// </summary>
        Align4096   = 0xC
    }

    /// <summary>
    /// Represents an abstract class for data spoolers.
    /// </summary>
    public abstract class Spooler : IDisposable
    {
        private SpoolerAlignment _alignment = SpoolerAlignment.Align4096;
        
        // when computing sizes, this cannot be null
        private string _description;
        private bool _isDescriptionDirty;
        
        /// <summary>
        /// Disposes of the resources used by this spooler.
        /// </summary>
        public virtual void Dispose()
        {

        }

        /// <summary>
        /// Gets or sets the description for this spooler. The length should not exceed 255 characters.
        /// </summary>
        public string Description
        {
            get { return (_description != null) ? _description : String.Empty; }
            set
            {
                var str = value;

                // string too long? no worries, we'll just force it to be 255 characters long :)
                if (str != null && str.Length > 255)
                    str = str.Substring(0, 255);

                _description = str;

                // don't flag as dirty if this is the first time we're setting the description
                if (_isDescriptionDirty)
                {
                    IsDirty = true;
                }
                else
                {
                    _isDescriptionDirty = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the magic number for this spooler.
        /// </summary>
        public int Magic { get; set; }

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
        /// Gets or sets the reserved byte for this spooler.
        /// </summary>
        public byte Reserved { get; set; }

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
            set { _alignment = value; }
        }

        /// <summary>
        /// Gets the size of this spooler. If an inherited class does not implement this property, a value of zero is always returned.
        /// </summary>
        public virtual int Size
        {
            get { return 0; }
        }

        /// <summary>
        /// Gets the <see cref="SpoolablePackage"/> containing this spooler. If no parent is attached, the value is null.
        /// </summary>
        public SpoolablePackage Parent { get; internal set; }

        /// <summary>
        /// Gets or sets the dirty status of this spooler. Intended for internal use only.
        /// </summary>
        internal bool IsDirty { get; set; }

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
