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

    [StructLayout(LayoutKind.Sequential, Size = 0xC)]
    public struct ChunkHeader
    {
        public static readonly int SizeOf = Marshal.SizeOf(typeof(ChunkHeader));
        public static readonly int Magic = (int)ChunkType.Chunk;

        public int Size;
        public int Count;
        public int Version;

        public static void WriteTo(Stream stream, SpoolablePackage chunk)
        {
            var header = new ChunkHeader(chunk);

            stream.Write(Magic);
            stream.Write(header, SizeOf);
        }

        public ChunkHeader(SpoolablePackage chunk)
        {
            Size = chunk.Size;
            Count = chunk.Children.Count;
            Version = Chunk.Version;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x10)]
    public struct ChunkEntry
    {
        public static readonly int SizeOf = Marshal.SizeOf(typeof(ChunkEntry));

        public int Context;
        public int Offset;
        public byte Version;
        public byte StrLen;
        public byte Alignment;
        public byte Reserved;
        public int Size;
    }

    public interface IFileChunker : IDisposable
    {
        string FileName { get; }

        bool IsCompressed { get; set; }
        bool IsLoaded { get; }

        bool AreChangesPending { get; }

        bool CanLoad { get; }
        bool CanSave { get; }

        SpoolerCollection Children { get; }
        SpoolablePackage Content { get; }

        event EventHandler FileLoadBegin;
        event EventHandler FileLoadEnd;
        event EventHandler FileSaveBegin;
        event EventHandler FileSaveEnd;

        event SpoolerEventHandler SpoolerLoaded;

        void CommitChanges();

        bool Load(string filename);

        bool Save();
        bool Save(string filename, bool updateStream = true);
    }

    public class FileChunker : IFileChunker
    {
        private SpoolablePackage _content;
        private FileStream _stream;

        public static bool HACK_BigEndian { get; set; }

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
        /// Gets or sets if the chunker uses LZW-compression.
        /// </summary>
        public bool IsCompressed { get; set; }

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
            get
            {
                return !IsCompressed;
            }
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

        public SpoolerCollection Children
        {
            get { return Content.Children; }
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

        public bool AreChangesPending
        {
            get { return Content.AreChangesPending; }
        }

        public void CommitChanges()
        {
            Content.CommitChanges();
        }

        public virtual void Dispose()
        {
            if (_stream != null)
            {
                _stream.Close();
                _stream.Dispose();
            }

            Content.Dispose();

            IsLoaded = false;
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

            if (IsCompressed)
            {
                if (HACK_BigEndian)
                    throw new InvalidDataException("Big-Endian file chunkers with compression are unsupported!");

                var comType = _stream.ReadInt32();
                var bufferSize = _stream.ReadInt32(); // aligned to 2048-bytes
                var dataSize = _stream.ReadInt32(); // size of compressed data (incl. header)

                if (comType == 1)
                {
                    // uncompressed data
                    // this is actually never used, but it doesn't hurt to support it!
                    _stream.Read(buffer, 0, (dataSize - 0xC));
                }
                else
                {
                    /* TODO: decompress the LZW data */
                    DSC.Log("WARNING: LZW decompression is not yet implemented.");

                    // return ALL of the compressed data (including the header)
                    // this way, we can use the data for research purposes if needed
                    _stream.Position -= 0xC;
                    _stream.Read(buffer, 0, dataSize);
                }
            }
            else
            {
                _stream.Read(buffer, 0, buffer.Length);
            }

            return buffer;
        }

        public static bool ReadChunkHeader(Stream stream, out ChunkHeader header)
        {
            var magic = stream.ReadInt32();

            if (magic != ChunkHeader.Magic)
            {
                header = new ChunkHeader();
                return false;
            }

            header = stream.Read<ChunkHeader>();
            return true;
        }

        private IEnumerable<Spooler> ReadChunkEntries(Stream stream, int offset, int count)
        {
            var entries = new List<ChunkEntry>(count);

            DSC.Update($"Processing {count} chunks...");

            for (int i = 0; i < count; i++)
            {
                var entry = stream.Read<ChunkEntry>();

                entries.Add(entry);
            }

            for (int idx = 1; idx <= count; idx++)
            {
                DSC.Update($"Loading chunk {idx} / {count}", (idx / (double)count) * 100.0);

                var data = entries[idx - 1];
                var dataOffset = (offset + data.Offset);

                if (Enum.IsDefined(typeof(ChunkType), data.Context))
                    DSC.Update($" - {(ChunkType)data.Context}");

                string description = null;

                // get description if present
                if (data.StrLen > 0)
                {
                    stream.Position = (dataOffset + data.Size);
                    description = stream.ReadString(data.StrLen);

                    DSC.Update($" - '{description}'");
                }

                // read chunk data
                stream.Position = dataOffset;

                Spooler spooler = null;

                if (stream.PeekInt32() == ChunkHeader.Magic)
                {
                    // process chunk
                    spooler = ReadChunk_NEW(stream, data);
                }
                else
                {
                    // process data
                    spooler = new SpoolableBuffer(ref data)
                    {
                        FileOffset = (offset + data.Offset),
                        FileChunker = this,
                    };
                }

                // ignore changes until attached to our parent
                spooler.IsDirty = true;
                spooler.Description = description;

                yield return spooler;
            }
        }

        public SpoolablePackage ReadChunk_NEW(Stream stream, ChunkEntry? data)
        {
            var offset = (int)stream.Position;
            ChunkHeader header;

            if (!ReadChunkHeader(stream, out header))
                throw new Exception("Invalid chunk data -- bad magic!");

            // does chunk contain LZW-compressed data?
            if ((header.Version & 0x80000000) != 0)
            {
                IsCompressed = true;
                header.Version &= 0x7FFFFFFF;
            }

            if (header.Version != Chunk.Version)
                throw new Exception("Unsupported version - cannot load chunk file!");

            var count = header.Count;

            var entries = ReadChunkEntries(stream, offset, count);
            var chunk = new SpoolablePackage(entries);

            // set the size explicitly
            chunk.SetSizeInternal(header.Size);

            if (data.HasValue)
            {
                var info = data.Value;

                chunk.SetCommon(ref info);
            }

            // fire off events
            foreach (var spooler in chunk.Children)
                OnSpoolerLoaded(spooler, EventArgs.Empty);

            return chunk;
        }

        private void ReadChunk(SpoolablePackage parent)
        {
            var offset = _stream.Position;
            var magic = _stream.ReadInt32();

            if (magic != ChunkHeader.Magic)
                throw new Exception("Invalid chunk header -- bad magic!");

            var info = _stream.Read<ChunkHeader>();

            // does chunk contain LZW-compressed data?
            if ((info.Version & 0x80000000) != 0)
            {
                IsCompressed = true;
                info.Version &= 0x7FFFFFFF;
            }

            if (info.Version != Chunk.Version)
                throw new Exception("Unsupported version - cannot load chunk file!");

            parent.SetSizeInternal(info.Size);

            var count = info.Count;
            var entries = new List<ChunkEntry>(count);

            DSC.Update($"Processing {count} chunks...");

            for (int i = 0; i < count; i++)
            {
                var entry = _stream.Read<ChunkEntry>();

                entries.Add(entry);
            }

            for (int i = 0, idx = 1; i < count; i++, idx++)
            {
                DSC.Update($"Loading chunk {idx} / {count}", (idx / (double)info.Count) * 100.0);

                var data = entries[i];
                var dataOffset = (int)(offset + data.Offset);

                if (Enum.IsDefined(typeof(ChunkType), data.Context))
                    DSC.Update($" - {(ChunkType)data.Context}");

                string description = null;

                if (data.StrLen > 0)
                {
                    _stream.Position = (dataOffset + data.Size);
                    description = _stream.ReadString(data.StrLen);

                    DSC.Update($" - '{description}'");
                }

                _stream.Position = dataOffset;

                Spooler spooler = null;

                if (_stream.PeekInt32() == ChunkHeader.Magic)
                {
                    spooler = new SpoolablePackage(ref data)
                    {
                        Description = description,
                    };

                    ReadChunk((SpoolablePackage)spooler);
                }
                else
                {
                    spooler = new SpoolableBuffer(ref data)
                    {
                        Description = description,

                        FileOffset = ((int)offset + data.Offset),
                        FileChunker = this,
                    };
                }

                spooler.IsModified = false;

                parent.Children.Add(spooler);
                parent.IsModified = false;

                OnSpoolerLoaded(spooler, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Writes the specified block of chunked data to the current position in a given <see cref="Stream"/>.
        /// The length of the stream should be set prior to calling this method.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="chunk">The chunked data.</param>
        /// <exception cref="T:System.EndOfStreamException">Thrown when the amount of chunked data exceeds the length of the stream.</exception>
        public static void WriteChunk(Stream stream, SpoolablePackage chunk)
        {
            var offset = stream.Position;

            if (chunk.AreChangesPending)
                chunk.CommitChanges();

            var spoolers = chunk.Children;
            var count = spoolers.Count;

            ChunkHeader.WriteTo(stream, chunk);

            var entries = new List<ChunkEntry>(count);

            DSC.Update($"Processing {count} chunks...");

            for (int i = 0; i < count; i++)
            {
                var entry = (ChunkEntry)spoolers[i];

                stream.Write(entry);
                entries.Add(entry);
            }

            // write chunk entries
            for (int i = 0, idx = 1; i < count; i++, idx++)
            {
                DSC.Update($"Writing chunk {idx} / {count}", (idx / (double)count) * 100.0);

                var spooler = spoolers[i];

                var data = entries[i];
                var dataOffset = (offset + data.Offset);

                stream.Position = dataOffset;

                if (spooler is SpoolablePackage)
                {
                    WriteChunk(stream, (SpoolablePackage)spooler);
                }
                else if (spooler is SpoolableBuffer)
                {
                    var buffer = ((SpoolableBuffer)spooler).GetRawBuffer();

                    stream.Write(buffer);
                }
                else
                {
                    throw new Exception("FATAL ERROR: Unsupported spooler type, cannot export data!");
                }

                // write description where applicable
                if (!String.IsNullOrEmpty(spooler.Description))
                {
                    stream.Position = (dataOffset + data.Size);
                    stream.Write(spooler.Description);
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
                DSC.Update($"Writing chunk file: '{filename}'");

                if (chunk.AreChangesPending)
                    chunk.CommitChanges();

                fs.SetLength(chunk.Size);

                fs.Fill(Chunk.PaddingBytes, chunk.Size);
                fs.Position = 0;

                WriteChunk(fs, chunk);

                DSC.Update($"Finished writing chunk file.");
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
                    throw new FileNotFoundException("The specified chunk file could not be found.", filename);

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

                ChunkHeader header;

                if (!ReadChunkHeader(_stream, out header))
                {
                    // not a chunk file; terminate
                    _stream.Dispose();
                    _stream = null;

                    return false;
                }

                _stream.Position = 0;

                OnFileLoadBegin();

                DSC.Update($"Loading chunk file: '{filename}'");

                Content = ReadChunk_NEW(_stream, null);
                IsLoaded = true;

                DSC.Update($"Finished loading chunk file.");

                OnFileLoadEnd();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Saves the contents of the chunker into the original file, overwriting any existing data.
        /// </summary>
        /// <returns>True if the chunker saved the file; otherwise, false.</returns>
        public bool Save()
        {
            if (CanSave)
            {
                // write chunk data to a temp file
                var tmpFilename = FileName + ".tmp";

                OnFileSaveBegin();
                WriteChunk(tmpFilename, Content);
                OnFileSaveEnd();

                // dispose of the old stream
                _stream.Close();
                _stream.Dispose();

                // delete old file & rename our temp file
                File.Delete(FileName);
                File.Move(tmpFilename, FileName);

                // set up new stream
                _stream = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Saves the contents of the chunker into the specified file, overwriting any existing data.
        /// </summary>
        /// <param name="filename">The file to save.</param>
        /// <param name="updateStream">Determines if the stream should be updated. The default is value 'true'.</param>
        /// <returns>True if the chunker saved the file; otherwise, false.</returns>
        public bool Save(string filename, bool updateStream = true)
        {
            // user requested an overwrite?
            if (filename == FileName)
                return Save();

            if (CanSave)
            {
                OnFileSaveBegin();
                WriteChunk(filename, Content);
                OnFileSaveEnd();

                if (updateStream)
                {
                    if (_stream != null)
                    {
                        // close old stream
                        _stream.Close();
                        _stream.Dispose();
                    }

                    // set up our new one
                    _stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                }

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
