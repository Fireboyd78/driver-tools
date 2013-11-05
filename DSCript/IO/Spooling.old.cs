using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.SpoolableFiles
{
    /*
     * THIS BLOWS BIG TIME
     * I HATE SPOOLABLE FILES
     * THEY SUCK
     * BLAH
     */

    /// <summary>Represents a basic spooler.</summary>
    public interface ISpooler
    {
        int ID { get; }

        SpoolableFile FileStream { get; }
        SpoolerData Parent { get; }

        SpoolerData GetSpooler(int id);
        SpoolerData AddSpooler(int id, SpoolerData spooler);
        bool RemoveSpooler(int id);

        bool HasParent { get; }

        uint BaseOffset { get; set; }
        uint Size { get; set; }
    }

    public sealed class SpoolerData
    {
        private int _id = -1;

        private ISpooler _baseSpooler = null;

        private string _description = "";

        private uint _type, _offset, _size = 0;
        private int _unk1, _unk2, _unk3 = 0;

        public int ID
        {
            get { return _id; }
        }

        public ISpooler BaseSpooler
        {
            get 
            {
                if (_baseSpooler == null) throw new Exception("FATAL ERROR :: Spooler data not connected to base spooler!");

                return _baseSpooler;
            }
        }

        public uint Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public uint Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        public uint Size
        {
            get { return _size; }
            set { _size = value; }
        }

        public int Unk1
        {
            get { return _unk1; }
            set { _unk1 = value; }
        }

        public int Unk2
        {
            get { return _unk2; }
            set { _unk2 = value; }
        }

        public int Unk3
        {
            get { return _unk3; }
            set { _unk3 = value; }
        }

        public SpoolerData(ISpooler spooler)
        {

        }
    }

    public sealed class Spooler : ISpooler, IDisposable
    {
        private int _id = -1;

        private uint _baseOffset, _size = 0;

        private SpoolableFile _file = null;
        private SpoolerData _parent = null;

        private Dictionary<int, SpoolerData> _spoolers;

        public void Dispose()
        {
            var i = _id;

            _id = -1;
            
            _baseOffset = 0;
            _size = 0;

            _file = null;
            _parent = null;

            _spoolers = null;

            Console.WriteLine("Spooler {0} was destroyed.", i);
        }

        public int ID
        {
            get { return _id; }
        }

        public SpoolableFile FileStream
        {
            get
            {
                if (_file == null) throw new Exception(SpoolableFile.e_ReturnNullStream);

                return _file;
            }
        }

        public SpoolerData Parent
        {
            get
            {
                if (!HasParent) throw new Exception("The root node doesn't have a parent..try implementing some error checking to avoid this problem!");

                return _parent;
            }
        }

        public SpoolerData GetSpooler(int id)
        {
            if (_spoolers.ContainsKey(id))
                return _spoolers[id];
            else
                throw new Exception("Cannot return a non-existent spooler.");
        }

        public SpoolerData AddSpooler(int id, SpoolerData spooler)
        {
            _spoolers.Add(id, spooler);

            if (_spoolers.ContainsKey(id))
            {
                Console.WriteLine("Creating spooler {0} ...", id);
                return _spoolers[id];
            }
            else
                throw new Exception("Failed to create spooler; I don't know what happened!");
        }

        public bool RemoveSpooler(int id)
        {
            if (_spoolers.ContainsKey(id))
            {
                Console.WriteLine("Removing spooler {0} ...", id);
                return (_spoolers.Remove(id)) ? true : false;
            }
            else
            {
                Console.WriteLine("Tried to remove non-existent spooler {0} ...", id);
                return false;
            }
        }

        public uint BaseOffset
        {
            get { return _baseOffset; }
            set { _baseOffset = value; }
        }

        public uint Size
        {
            get { return _size; }
            set { _size = value; }
        }

        public bool IsAlive
        {
            get { return ((ID == -1) || (Size == 0)) ? false : true; }
        }

        public bool HasParent
        {
            get { return (_parent != null) ? true : false; }
        }

        public SpoolerData this[int id]
        {
            get
            {
                return GetSpooler(id);
            }
            set
            {
                if (_spoolers.ContainsKey(id))
                {
                    Console.WriteLine("Spooler {0} already exists, overwriting...", id);
                    _spoolers[id] = value;
                }
                else
                {
                    Console.WriteLine("Creating spooler {0} ...", id);
                    _spoolers.Add(id, value);
                }
            }
        }

        public Spooler(SpoolableFile file, SpoolerData parent, uint offset, uint size)
        {
            _file = file;

            _parent = parent ?? null;

            _baseOffset = offset;
            _size = size;

            _id = FileStream.GetNumSpoolers() + 1;

            FileStream.AddChunk(this);

            Console.WriteLine("Spooler {0} created!", ID);
        }

        public Spooler(SpoolableFile file) : this(file, null, 0, 0) { }
    }

    /// <summary>Represents a spoolable file stream.</summary>
    public sealed class SpoolableFile : IDisposable
    {
        //------- Static methods --------------------------
        private bool _init; // spool system initialized?
        private int _nSpoolers; // spooler counter

        private IDictionary<int, Spooler> _chunks;

        static SpoolableFile()
        {
            
        }
        //-------------------------------------------------

        public int GetNumSpoolers()
        {
            return _nSpoolers;
        }

        /// <summary>Release all of the resources used by this <see cref="SpoolableFile"/></summary>
        public void Dispose()
        {
            _stream.Dispose();
            _stream = null;

            _filename = null;

            foreach (var s in _chunks)
            {
                s.Value.Dispose();
            }

            _chunks.Clear();

            _init = false;

            if ((_init) || (_stream != null) || (Filename != null)) throw new Exception(e_DestroySpoolerFailed);
        }

        #region Exceptions
        public const string e_CreateNullSpooler = "Cannot create spooler; Null spooler";
        public const string e_CreateSpoolerInvalid = "Cannot create spooler; Master spooler list missing entry";
        public const string e_CreateSpoolerFailed = "Cannot create spoolable file; FATAL ERROR";

        public const string e_ReturnNullStream = "Cannot return stream; Null stream";
        public const string e_ReturnNullSpooler = "Cannot return spooler; Null spooler";
        
        public const string e_DestroySpoolerFailed = "Cannot destroy spoolable file; FATAL ERROR";
        public const string e_SpoolerAlreadyInitialized = "Cannot create spooler; Interface is already initialized";
        #endregion

        // setup
        private const string _name = "SPL01"; // name for our spooler
        private MemoryMappedFile _stream; // the mmf layer

        private string _filename = "";

        /// <summary>Returns the filename associated with this <see cref="SpoolableFile"/></summary>
        public string Filename
        {
            get { return _filename; }
        }

        /// <summary>Returns the internal name for this <see cref="SpoolableFile"/></summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>Returns the <see cref="MemoryMappedFile"/> used by this <see cref="SpoolableFile"/>. An exception is thrown if the stream is null.</summary>
        public MemoryMappedFile BaseStream
        {
            get
            {
                if (_init && _stream != null)
                    return _stream;
                else
                    throw new Exception(e_ReturnNullStream);
            }
        }

        /// <summary>
        /// Adds a new <see cref="Spooler"/> to this <see cref="SpoolableFile"/> with the specified key and value. 
        /// If the operation fails, a fatal error occurs. This should never happen!
        /// </summary>
        /// <param name="id">Zero-based spooler id</param>
        /// <param name="spooler">The spooler to add</param>
        /// <returns>The newly-added spooler is returned upon succeeding.</returns>
        public Spooler AddChunk(Spooler spooler)
        {
            Console.WriteLine("Trying to add chunk {0} ..", spooler.ID);

            if (!_chunks.ContainsKey(spooler.ID))
            {
                _chunks.Add(spooler.ID, spooler);
                
                Console.WriteLine("Done!");

                return _chunks[spooler.ID];
            }
            else
                throw new Exception("Tried to add a chunk that already exists!");
        }

        /// <summary>Removes a <see cref="Spooler"/> with the specified key.</summary>
        /// <param name="id">Zero-based spooler id</param>
        /// <returns>True if the spooler was successfully removed, False otherwise.</returns>
        public bool RemoveChunk(int id)
        {
            if (_chunks.ContainsKey(id))
                _chunks.Remove(id);

            return (!_chunks.ContainsKey(id)) ? true : false;
        }

        /// <summary>Returns a <see cref="Spooler"/> with the specified key.</summary>
        /// <param name="id">Zero-based spooler id</param>
        /// <returns>The <see cref="Spooler"/> if it exists, otherwise throws an exception.</returns>
        public Spooler GetChunk(int id)
        {
            if (_chunks.ContainsKey(id))
                return _chunks[id];
            else
                throw new Exception(e_ReturnNullSpooler);
        }

        /// <summary>Gets or sets the <see cref="Spooler"/> with the specified key.</summary>
        /// <param name="id">Zero-based spooler id</param>
        /// <returns>The <see cref="Spooler"/> if it exists, otherwise throws an exception.</returns>
        public Spooler this[int id]
        {
            get
            {
                return GetChunk(id);
            }
            set
            {
                if (_chunks.ContainsKey(id))
                    _chunks[id] = value;
                else
                    _chunks.Add(id, value);
            }
        }

        /// <summary>Creates a new <see cref="SpoolableFile"/> using the specified file.</summary>
        /// <param name="filename">The path to the file</param>
        public SpoolableFile(string filename)
        {
            // init vals
            _init = false;

            _nSpoolers = 0;
            _chunks = new Dictionary<int, Spooler>();

            //-----------------------------------------

            _filename = filename;

            // init spooler
            _stream = MemoryMappedFile.CreateFromFile(Filename, FileMode.Open, Name);
            _init = (_stream != null) ? true : false;

            // error checking
            if ((!_init) || (_stream == null)) throw new Exception(e_CreateSpoolerFailed);

            Console.WriteLine("Created new SpoolableFile!");
        }
    }
}
