using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DSCript.Spoolers
{
    public abstract class SpoolableResource
    {
        public static implicit operator Spooler(SpoolableResource resource)
        {
            return resource.ToSpooler();
        }

        public abstract Spooler ToSpooler();
    }

    public abstract class Spooler : IDisposable
    {
        private static int hashKey = 0;

        private int hash = 0, alignment = 4096;
        private string description = String.Empty;

        private int Hash
        {
            get { return hash; }
            set { hash = value * (hashKey += 1); }
        }

        public override bool Equals(object obj)
        {
            return (obj.GetHashCode() == this.GetHashCode());
        }

        public abstract void Dispose();

        /// <summary>
        /// Returns the hash code for this spooler.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return Hash;
        }

        /// <summary>
        /// Gets or sets the byte-alignment of this spooler.
        /// </summary>
        public int Alignment
        {
            get { return alignment; }
            set { alignment = value; }
        }

        /// <summary>
        /// Gets or sets the description of this spooler.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        /// <summary>
        /// Gets the magic number of this spooler.
        /// </summary>
        public virtual int Magic { get; private set; }

        /// <summary>
        /// Gets the reserved field of this spooler.
        /// </summary>
        public virtual byte Reserved { get; private set; }

        /// <summary>
        /// Gets the size of this spooler.
        /// </summary>
        public abstract int Size { get; }

        /// <summary>
        /// Loads a spooler from the specified stream.
        /// </summary>
        /// <param name="stream">The stream to load the spooler from.</param>
        public abstract void Load(Stream stream);

        /// <summary>
        /// Writes the contents of the spooler to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to write the contents of the spooler to.</param>
        public abstract void WriteTo(Stream stream);

        /// <summary>
        /// Saves the contents of the spooler to the specified file.
        /// </summary>
        /// <param name="filename">The file to write the contents of the spooler to.</param>
        public virtual void Save(string filename)
        {
            using (FileStream fs = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                WriteTo(fs);
            }
        }

        protected Spooler() : this(0, 0) { }
        protected Spooler(int magic, byte reserved)
        {
            ++Hash;

            Magic = magic;
            Reserved = reserved;
        }
    }
}
