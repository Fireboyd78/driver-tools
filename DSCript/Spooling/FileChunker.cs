using System;
using System.Collections.ObjectModel;
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

namespace DSCript.Spooling
{
    /// <summary>
    /// Represents a delegate for handling spooler events.
    /// </summary>
    /// <param name="sender">The spooler who triggered this event.</param>
    /// <param name="e">The event arguments accompanying the spooler.</param>
    public delegate void SpoolerEventHandler(Spooler sender, EventArgs e);

    public class FileChunker : IDisposable
    {
        private SpoolablePackage _content;
        private FileStream _stream;

        /// <summary>
        /// Gets the filename of the chunker, if applicable. A null value means nothing was loaded.
        /// </summary>
        public string FileName
        {
            get { return (_stream != null ? _stream.Name : null); }
        }

        /// <summary>
        /// The state of the chunker, whether any files have been loaded or not.
        /// </summary>
        public bool IsLoaded { get; protected set; }

        /// <summary>
        /// Determines whether or not the chunker can load a file.
        /// </summary>
        public virtual bool CanLoad
        {
            get { return true; }
        }

        /// <summary>
        /// Determines whether or not the chunker can save a file.
        /// </summary>
        public virtual bool CanSave
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the content of this chunker.
        /// </summary>
        public SpoolablePackage Content
        {
            get
            {
                if (_content == null)
                    _content = new SpoolablePackage();

                return _content;
            }
            protected set
            {
                _content = value;
            }
        }

        #region Event Handlers
        /// <summary>
        /// An event that is called when a spooler is loaded from a file. The event args are unused.
        /// </summary>
        public event SpoolerEventHandler SpoolerLoaded;

        /// <summary>
        /// An event that is called prior to the chunker loading a file. The event args are unused.
        /// </summary>
        public event EventHandler FileLoadBegin;

        /// <summary>
        /// An event that is called when the chunker finishes loading a file. The event args are unused.
        /// </summary>
        public event EventHandler FileLoadEnd;

        /// <summary>
        /// An event that is called prior to the chunker saving a file. The event args are unused.
        /// </summary>
        public event EventHandler FileSaveBegin;

        /// <summary>
        /// An event that is called when the chunker finishes saving a file. The event args are unused.
        /// </summary>
        public event EventHandler FileSaveEnd;
        #endregion

        /// <summary>
        /// Represents a method that is called when the chunker loads a spooler from a file.
        /// The default behavior fires the <see cref="SpoolerLoaded"/> event.
        /// </summary>
        /// <param name="sender">The spooler that was loaded.</param>
        /// <param name="e">Unused.</param>
        protected virtual void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            if (SpoolerLoaded != null)
                SpoolerLoaded(sender, e);
        }

        /// <summary>
        /// Represents a method that is called prior to the chunker loading a file.
        /// Ideally, this method should be used to set up any uninitialized variables.
        /// <para>The default behavior fires the <see cref="FileLoadBegin"/> event.</para>
        /// </summary>
        protected virtual void OnFileLoadBegin()
        {
            if (FileLoadBegin != null)
                FileLoadBegin(this, EventArgs.Empty);
        }

        /// <summary>
        /// Represents a method that is called when the chunker finishes loading a file.
        /// The default behavior fires the <see cref="FileLoadEnd"/> event.
        /// </summary>
        protected virtual void OnFileLoadEnd()
        {
            if (FileLoadEnd != null)
                FileLoadEnd(this, EventArgs.Empty);
        }

        /// <summary>
        /// Represents a method that is called prior to the chunker saving a file.
        /// Ideally, this method should be used to ensure all content is ready to be saved.
        /// <para>The default behavior fires the <see cref="FileSaveBegin"/> event.</para>
        /// </summary>
        protected virtual void OnFileSaveBegin()
        {
            if (FileSaveBegin != null)
                FileSaveBegin(this, EventArgs.Empty);
        }

        /// <summary>
        /// Represents a method that is called when the chunker finishes saving a file.
        /// The default behavior fires the <see cref="FileSaveEnd"/> event.
        /// </summary>
        protected virtual void OnFileSaveEnd()
        {
            if (FileSaveEnd != null)
                FileSaveEnd(this, EventArgs.Empty);
        }

        public virtual void Dispose()
        {
            if (_stream != null)
                _stream.Dispose();

            Content.Dispose();
        }

        /// <summary>
        /// Retrieves a buffer using data provided from the specified spooler. Intended for internal use only.
        /// </summary>
        /// <param name="spooler">The spooler</param>
        /// <returns>The buffer from the file for the spooler.</returns>
        internal byte[] GetBuffer(SpoolableBuffer spooler)
        {
            // these errors should never occur
            if (spooler.FileOffset <= 0)
                throw new Exception("FATAL ERROR: Cannot retrieve the buffer from a spooler that has not been loaded properly.");
            if (_stream == null || !_stream.CanRead)
                throw new Exception("FATAL ERROR: Cannot retrieve the buffer from a file that has been closed.");

            _stream.Position = spooler.FileOffset;

            var buffer = new byte[spooler.Size];

            _stream.Read(buffer, 0, buffer.Length);

            return buffer;
        }

        private void ReadChunk(SpoolablePackage parent)
        {
            var baseOffset = _stream.Position;
            var entriesOffset = baseOffset + 0x10;

            var magic = _stream.ReadInt32();

            if (magic != (int)ChunkType.Chunk)
                throw new Exception("Bad magic - cannot not load chunk file!");

            var size = _stream.ReadInt32();
            var count = _stream.ReadInt32();
            var flags = _stream.ReadInt32();

            if (flags != 3)
                throw new Exception("Unknown flags - cannot load chunk file!");

            for (int i = 0; i < count; i++)
            {
                _stream.Seek((i * 0x10), entriesOffset);

                var entry = new {
                    Magic     = _stream.ReadInt32(),
                    Offset    = _stream.ReadInt32(),
                    Reserved  = (byte)_stream.ReadByte(),
                    StrLen    = (byte)_stream.ReadByte(),
                    Align     = (SpoolerAlignment)((byte)_stream.ReadByte()),
                    PadByte   = (byte)_stream.ReadByte(),
                    Size      = _stream.ReadInt32()
                };

                string description  = null;
                
                if (entry.StrLen > 0)
                {
                    _stream.Seek((entry.Offset + entry.Size), baseOffset);
                    description = _stream.ReadString(entry.StrLen);
                }

                _stream.Seek(entry.Offset, baseOffset);

                Spooler spooler = null;

                if (_stream.PeekInt32() == (int)ChunkType.Chunk)
                {
                    spooler = new SpoolablePackage(entry.Size) {
                        Alignment   = entry.Align,
                        Description = description,
                        Magic       = entry.Magic,
                        Offset      = entry.Offset,
                        Reserved    = entry.Reserved,
                    };

                    ReadChunk((SpoolablePackage)spooler);
                }
                else
                {
                    spooler = new SpoolableBuffer(entry.Size) {
                        Alignment   = entry.Align,
                        Description = description,
                        Magic       = entry.Magic,
                        Offset      = entry.Offset,
                        Reserved    = entry.Reserved,

                        FileOffset  = ((int)baseOffset + entry.Offset),
                        FileChunker = this
                    };
                }

                parent.Children.Add(spooler);
                parent.IsDirty = false;

                OnSpoolerLoaded(spooler, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Writes the specified block of chunked data to the current position in a given <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="chunk">The chunked data.</param>
        /// <exception cref="T:System.EndOfStreamException">Thrown when the amount of chunked data exceeds the length of the stream.</exception>
        public static void WriteChunk(Stream stream, SpoolablePackage chunk)
        {
            var baseOffset = stream.Position;

            stream.Write((int)ChunkType.Chunk);
            stream.Write(chunk.Size);
            stream.Write(chunk.Children.Count);
            stream.Write(0x3); // TODO: handle this properly

            var entryStart = (baseOffset + 0x10);

            // write chunk entries
            for (int i = 0; i < chunk.Children.Count; i++)
            {
                stream.Seek(i * 0x10, entryStart);

                var entry = chunk.Children[i];

                stream.Write(entry.Magic);
                stream.Write(entry.Offset);
                stream.Write(entry.Reserved);
                stream.Write(entry.StrLen);
                stream.Write((byte)entry.Alignment);
                stream.Write(0xFB); // ;)
                stream.Write(entry.Size);

                stream.Seek(entry.Offset, baseOffset);

                if (entry is SpoolablePackage)
                {
                    WriteChunk(stream, (SpoolablePackage)entry);
                }
                else if (entry is SpoolableBuffer)
                {
                    stream.Write(((SpoolableBuffer)entry).GetBufferInternal());
                }
                else
                {
                    throw new Exception("FATAL ERROR: Unsupported spooler type, cannot export data!");
                }

                // write description where applicable
                if (entry.StrLen > 0)
                {
                    stream.Seek(entry.Offset + entry.Size, baseOffset);
                    stream.Write(entry.Description);
                }
            }
        }

        /// <summary>
        /// Creates a new file with the specified name and writes a block of chunked data to it.
        /// </summary>
        /// <param name="filename">The file to write to.</param>
        /// <param name="chunk">The chunked data.</param>
        public static void WriteChunk(string filename, SpoolablePackage chunk)
        {
            using (var fs = File.Create(filename))
            {
                fs.SetLength(chunk.Size);

                fs.Fill(Chunk.PaddingBytes, chunk.Size);
                fs.Position = 0;

                WriteChunk(fs, chunk);
            }
        }

        /// <summary>
        /// Loads the contents of the file into the file chunker, and overwrites any existing content.
        /// </summary>
        /// <param name="filename">The file to load.</param>
        /// <returns>True if the chunker loaded the file; otherwise, false./</returns>
        public bool Load(string filename)
        {
            if (CanLoad)
            {
                if (!File.Exists(filename))
                    throw new FileNotFoundException("The specified chunk file could not be found.", "filename");

                // check for existing content
                if (_stream != null || Content.Children.Count > 0)
                {
                    // make sure we clean up existing data
                    _stream = null;
                    Content.Dispose();

                    // force creation of new content
                    _content = null;
                }

                _stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

                OnFileLoadBegin();

                ReadChunk(Content);
                IsLoaded = true;

                OnFileLoadEnd();

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Saves the contents of the chunker into the specified file.
        /// </summary>
        /// <param name="filename">The file to save.</param>
        /// <returns>True if the chunker saved the file; otherwise, false.</returns>
        public bool Save(string filename)
        {
            if (CanSave)
            {
                OnFileSaveBegin();
                WriteChunk(filename, Content);
                OnFileLoadEnd();

                return true;
            }
            else
            {
                return false;
            }
        }

        public FileChunker() { }
        public FileChunker(string filename)
        {
            Load(filename);
        }
    }
}
