using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using DSCript;

using Zartex.Settings;

namespace Zartex
{
    public sealed class MPCFile
    {
        static string m_locale = "English";
        static string m_territory = Driv3r.InvalidPath;
        
        // can we load locale files automagically?
        static bool m_autoLocale = false;

        // only valid if we found the right territory!
        static string m_localeDir = String.Empty;

        static MPCFile()
        {
            m_locale = Configuration.Settings.Locale;

            var terrDir = Driv3r.GetTerritoryName();

            if (terrDir != Driv3r.InvalidPath)
            {
                m_territory = Driv3r.GetTerritoryPath(terrDir);

                m_autoLocale = true;
                m_localeDir = Driv3r.GetLocalePath(m_territory, m_locale);
            }
        }
        
        // driv3r's way of getting the base id
        // leaving this here just in case
        private static int GetBaseMissionId(int missionId)
        {
            switch (missionId)
            {
            case 1:
            case 101:
            case 102:
                return 1;
            case 2:
            case 103:
                return 2;
            case 3:
            case 105:
                return 3;
            case 4:
            case 106:
            case 107:
                return 4;
            case 5:
            case 108:
            case 109:
            case 121:
                return 5;
            case 6:
            case 110:
            case 111:
                return 6;
            case 7:
            case 112:
            case 122:
                return 7;
            case 8:
            case 113:
            case 114:
            case 115:
                return 8;
            case 9:
            case 116:
            case 117:
                return 9;
            case 10:
            case 118:
            case 119:
            case 120:
                return 10;
            case 11:
            case 130:
            case 131:
                return 11;
            case 12:
                return 12;
            case 13:
            case 134:
                return 13;
            case 14:
            case 135:
            case 150:
                return 14;
            case 15:
            case 136:
                return 15;
            case 16:
            case 137:
            case 138:
            case 139:
            case 149:
                return 16;
            case 17:
            case 140:
            case 151:
            case 152:
                return 17;
            case 18:
            case 141:
            case 142:
                return 18;
            case 19:
            case 143:
            case 144:
                return 19;
            case 20:
                return 20;
            case 21:
            case 146:
            case 147:
            case 148:
                return 21;
            case 22:
            case 160:
            case 161:
            case 162:
                return 22;
            case 23:
                return 23;
            case 24:
            case 164:
            case 165:
            case 180:
                return 24;
            case 25:
            case 166:
            case 167:
            case 168:
            case 181:
                return 25;
            case 26:
                return 26;
            case 27:
            case 171:
            case 172:
                return 27;
            case 28:
            case 173:
            case 174:
                return 28;
            case 29:
            case 175:
            case 182:
                return 29;
            case 30:
            case 176:
                return 30;
            case 31:
            case 177:
            case 178:
            case 179:
                return 31;
            }

            // mission has no sub-missions
            return missionId;
        }
        
        public static string GetMissionLocaleDirectory()
        {
            return (m_autoLocale) ? Path.Combine(m_localeDir, "Missions") : Driv3r.RootDirectory;
        }

        public static string GetMissionLocaleFilepath(int missionId)
        {
            return (m_autoLocale) ? Driv3r.GetMissionLocale(missionId, m_territory, m_locale) : Driv3r.InvalidPath;
        }

        public static string GetMissionScriptDirectory()
        {
            return Driv3r.GetPath("Missions");
        }

        private bool _isLoaded = false;
        
        public bool IsLoaded
        {
            get { return _isLoaded; }
        }
        
        public string Filename { get; set; }

#if FALSE
        // LEGACY
        public ChunkReader ChunkFile { get; set; }

        public SpoolableChunkOld Chunk { get; set; }

        public IList<MissionObject> ExportedMissionObjects { get; set; }

        public IDictionary<int, string> StringCollection { get; set; }
        

        public IList<LogicDefinition> ActorDefinitions { get; set; }
        public IList<LogicDefinition> LogicNodeDefinitions { get; set; }

        public IDictionary<int, int> ActorSetTable { get; set; }

        public IList<WireCollectionGroup> WireCollections { get; set; }

        // LEGACY
        public FileStream Stream
        {
            get { return new FileStream(ChunkFile.Filename, FileMode.Open, FileAccess.Read, FileShare.Read); }
        }

        

        private T ParseDefinition<T>(BinaryReader f, T definition)
            where T : LogicDefinition
        {
            definition.Offset = (int)f.GetPosition();

            definition.Opcode = f.ReadByte();

            if (f.ReadByte() != 0x3E)
                throw new Exception(String.Format(
                    "The sign for the definition @ 0x{0:X} is incorrect!",
                    f.GetPosition() - 2)
                );

            definition.StringId = f.ReadInt16();

            if (definition is ActorDefinition)
                definition.Reserved = f.ReadInt32();

            definition.R = f.ReadByte();
            definition.G = f.ReadByte();
            definition.B = f.ReadByte();
            definition.A = f.ReadByte();

            definition.Unknown = f.ReadInt16();
            definition.Flags = f.ReadInt16();

            return definition;
        }

#region Legacy loaders
        public void LoadStringCollection(SubChunkBlock stringCollection)
        {
            using (BinaryReader f = new BinaryReader(Stream))
            {
                long baseOffset = f.Seek(stringCollection.BaseOffset, SeekOrigin.Begin);
                long keyOffset = 0;

                int nStrings = f.ReadInt32();
                keyOffset += sizeof(uint);

                StringCollection = new Dictionary<int, string>();

                for (int s = 0; s < nStrings; s++)
                {
                    uint key = f.ReadUInt32();
                    keyOffset += sizeof(uint);

                    f.Seek(baseOffset + key, SeekOrigin.Begin);

                    StringCollection.Add(s, f.ReadCString());

                    f.Seek(baseOffset + keyOffset, SeekOrigin.Begin);
                }
            }
        }

        public void LoadActorSetTable(SubChunkBlock actorSetTable)
        {
            using (BinaryReader f = new BinaryReader(Stream))
            {
                f.Seek(actorSetTable.BaseOffset, SeekOrigin.Begin);

                if (f.ReadUInt32() != 0x1)
                    throw new Exception(String.Format("Error in Actor Set Table @ 0x{0:X}!", f.GetPosition()));

                int nActors = f.ReadInt32();

                ActorSetTable = new Dictionary<int, int>();

                for (int i = 0; i < nActors; i++)
                    ActorSetTable.Add(i, f.ReadInt32());
            }
        }

        public void LoadWireCollection(SubChunkBlock wireCollection)
        {
            using (BinaryReader f = new BinaryReader(Stream))
            {
                f.Seek(wireCollection.BaseOffset, SeekOrigin.Begin);

                int nWires = f.ReadInt32();
                WireCollections = new List<WireCollectionGroup>(nWires);

                for (int i = 0; i < nWires; i++)
                {
                    uint offset = (uint)f.GetPosition();
                    int count = f.ReadInt32();

                    WireCollectionGroup wireGroup = 
                        new WireCollectionGroup(count) { Offset = offset };

                    for (int ii = 0; ii < count; ii++)
                    {
                        WireCollectionEntry wireEntry = new WireCollectionEntry() {
                            Unk = f.ReadByte(),
                            Opcode = f.ReadByte(),
                            NodeId = f.ReadInt16()
                        };

                        wireGroup.Entries.Add(wireEntry);
                    }

                    WireCollections.Add(wireGroup);
                }
            }
        }

        public void LoadActors(SubChunkBlock actors)
        {
            LoadLogicData(actors, 0);
        }

        public void LoadLogicNodes(SubChunkBlock logicNodes)
        {
            LoadLogicData(logicNodes, 1);
        }

        private void LoadLogicData(SubChunkBlock logicData, int type)
        {
            using (BinaryReader f = new BinaryReader(Stream))
            {
                f.Seek(logicData.BaseOffset, SeekOrigin.Begin);

                ChunkBlock LogicData = ChunkFile.GetBlockChildOrNull(logicData);

                SubChunkBlock Definitions = LogicData.Subs[0];
                SubChunkBlock Properties = LogicData.Subs[1];

                f.Seek(Definitions.BaseOffset, SeekOrigin.Begin);

                int count = f.ReadInt32();

                IList<LogicDefinition> definitions = new List<LogicDefinition>(count);

                for (int i = 0; i < count; i++)
                {
                    LogicDefinition node;

                    switch (type)
                    {
                    case 0:
                        node = new ActorDefinition();
                        break;
                    case 1:
                        node = new LogicNodeDefinition();
                        break;
                    default:
                        throw new Exception("Invalid type param!");
                    }

                    node = ParseDefinition(f, node);

                    definitions.Add(node);
                }

                f.Seek(Properties.BaseOffset, SeekOrigin.Begin);

                if (f.ReadUInt32() != count)
                    throw new Exception("The number of properties is mismatched to the number of definitions!");

                for (int d = 0; d < count; d++)
                {
                    LogicDefinition node = definitions[d];

                    int pCount = f.ReadInt32();

                    node.Properties = new List<LogicProperty>(pCount);

                    for (int p = 0; p < pCount; p++)
                    {
                        LogicProperty prop;

                        uint offset = (uint)f.GetPosition();
                        int op = f.ReadByte();

                        f.ReadByte();

                        int id = f.ReadInt16();
                        int reserved = f.ReadInt32();

                        switch (op)
                        {
                        case 1:
                            {
                                int val = f.ReadInt32();
                                prop = new IntegerProperty(val);
                            }
                            break;
                        case 2:
                            {
                                float val = f.ReadSingle();
                                prop = new FloatProperty(val);
                            }
                            break;
                        case 3:
                            {
                                int val = f.ReadUInt16();
                                prop = new FilenameProperty(StringCollection[val]);
                            }
                            break;
                        case 4:
                            {
                                bool val = f.ReadBoolean();
                                prop = new BooleanProperty(val);
                            }
                            break;
                        // TODO: IMPLEMENT ENUM PROPERTY
                        // TODO: IMPLEMENT FLAGS PROPERTY
                        case 6:
                        case 9:
                            {
                                uint val = f.ReadUInt32();
                                prop = new UnknownProperty(op, val);
                            }
                            break;
                        //===============================
                        case 7:
                            {
                                int val = f.ReadInt32();
                                prop = new ActorProperty(val);
                            }
                            break;
                        case 8:
                            {
                                int val = f.ReadUInt16();
                                prop = new StringProperty(StringCollection[val]);
                            }
                            break;
                        case 11:
                            {
                                long val = (long)f.ReadUInt64();
                                prop = new AudioProperty(val);
                            }
                            break;
                        case 17:
                            {
                                float[] float4 = new float[4] {
                                    f.ReadSingle(),
                                    f.ReadSingle(),
                                    f.ReadSingle(),
                                    f.ReadSingle()
                                };
                                prop = new Float4Property(float4);
                            }
                            break;
                        case 19:
                            {
                                int val = f.ReadInt32();
                                prop = new WireCollectionProperty(val);
                            }
                            break;
                        case 20:
                            {
                                int val = f.ReadInt32();
                                prop = new LocalisedStringProperty(val);
                            }
                            break;
                        case 21:
                            {
                                byte[] val = f.ReadBytes(reserved);
                                prop = new UnicodeStringProperty(val);
                            }
                            break;
                        case 22:
                            {
                                byte[] val = f.ReadBytes(reserved);
                                prop = new RawDataProperty(val);
                            }
                            break;
                        default:
                            {
                                uint unk = f.ReadUInt32();
                                prop = new UnknownProperty(op, unk);
                            }
                            break;
                        }

                        f.Align(4);

                        prop.Offset = offset;
                        prop.StringId = id;
                        prop.Reserved = reserved;

                        node.Properties.Add(prop);
                    }
                }

                if (type == 0)
                    ActorDefinitions = definitions;
                else if (type == 1)
                    LogicNodeDefinitions = definitions;
            }
        }

        public void LoadExportedMissionObjects(SubChunkBlock missionObjects)
        {
            using (BinaryReader f = new BinaryReader(Stream))
            {
                f.Seek(missionObjects.BaseOffset, SeekOrigin.Begin);

                int count = f.ReadInt32();

                ExportedMissionObjects = new List<MissionObject>(count);

                bool doBreak = false;

                for (int i = 0; i < count && !doBreak; i++)
                {
                    uint type = f.ReadUInt32();

                    switch (type)
                    {
                    case (0x0):
                        f.Align(16);
                        break;
                    case 0x1: ExportedMissionObjects.Add(new BlockType_0x1(f)); break;
                    case 0x2: ExportedMissionObjects.Add(new BlockType_0x2(f)); break;
                    case 0x3: ExportedMissionObjects.Add(new BlockType_0x3(f)); break;
                    case 0x4: ExportedMissionObjects.Add(new BlockType_0x4(f)); break;
                    case 0x5: ExportedMissionObjects.Add(new BlockType_0x5(f)); break;
                    case 0x6: ExportedMissionObjects.Add(new BlockType_0x6(f)); break;
                    case 0x7: ExportedMissionObjects.Add(new BlockType_0x7(f)); break;
                    case 0x8: ExportedMissionObjects.Add(new BlockType_0x8(f)); break;
                    case 0x9: ExportedMissionObjects.Add(new BlockType_0x9(f)); break;
                    case 0xA: ExportedMissionObjects.Add(new BlockType_0xA(f)); break;
                    case 0xB: ExportedMissionObjects.Add(new BlockType_0xB(f)); break;
                    case 0xC: ExportedMissionObjects.Add(new BlockType_0xC(f)); break;
                    default:
                        Console.WriteLine("NO READER FOR TYPE 0x{0:X}", type);
                        doBreak = true;
                        break;
                    }
                }
            }
        }
#endregion

#region new loaders
        public void LoadStringCollection(SpoolableDataOld stringCollection)
        {
            using (var ms = new MemoryStream(stringCollection.Buffer))
            using (BinaryReader f = new BinaryReader(ms))
            {
                int nStrings = f.ReadInt32();

                var baseOffset = f.GetPosition();

                StringCollection = new Dictionary<int, string>();

                for (int s = 0; s < nStrings; s++)
                {
                    f.Seek(baseOffset + (s * 4), SeekOrigin.Begin);

                    var key = f.ReadInt32();

                    f.Seek(key, SeekOrigin.Begin);

                    StringCollection.Add(s, f.ReadCString());
                }
            }
        }

        public void LoadActorSetTable(SpoolableDataOld actorSetTable)
        {
            using (var ms = new MemoryStream(actorSetTable.Buffer))
            using (BinaryReader f = new BinaryReader(ms))
            {
                //if (f.ReadInt32() != 0x1)
                //    throw new Exception(String.Format("Error in Actor Set Table @ 0x{0:X}!", f.GetPosition() - 4));

                f.Seek(0x4, SeekOrigin.Current);

                int nActors = f.ReadInt32();

                ActorSetTable = new Dictionary<int, int>();

                for (int i = 0; i < nActors; i++)
                    ActorSetTable.Add(i, f.ReadInt32());
            }
        }

        public void LoadWireCollection(SpoolableDataOld wireCollection)
        {
            // TODO: Deprecate use of BinaryReader
            using (var ms = new MemoryStream(wireCollection.Buffer))
            using (BinaryReader f = new BinaryReader(ms))
            {
                int nWires = f.ReadInt32();
                WireCollections = new List<WireCollectionGroup>(nWires);

                for (int i = 0; i < nWires; i++)
                {
                    uint offset = (uint)f.GetPosition();
                    int count = f.ReadInt32();

                    WireCollectionGroup wireGroup = 
                        new WireCollectionGroup(count) { Offset = offset };

                    for (int ii = 0; ii < count; ii++)
                    {
                        WireCollectionEntry wireEntry = new WireCollectionEntry() {
                            Unk = f.ReadByte(),
                            Opcode = f.ReadByte(),
                            NodeId = f.ReadInt16()
                        };

                        wireGroup.Entries.Add(wireEntry);
                    }

                    WireCollections.Add(wireGroup);
                }
            }
        }

        public void LoadActors(SpoolableChunkOld actors)
        {
            LoadLogicData(actors, 0);
        }

        public void LoadLogicNodes(SpoolableChunkOld logicNodes)
        {
            LoadLogicData(logicNodes, 1);
        }

        private void LoadLogicData(SpoolableChunkOld logicData, int type)
        {
            if (logicData.Spoolers.Count < 2)
                throw new Exception("Bad logic data chunk - cannot load data!");
            if (type > 1)
                throw new Exception("Invalid type param - cannot load logic data!");

            var definitions = logicData.Spoolers[0] as SpoolableDataOld;
            var properties = logicData.Spoolers[1] as SpoolableDataOld;

            var defList = new List<LogicDefinition>();
            var propList = new List<LogicProperty>();

            int count = 0;

            // load definitions
            // TODO: Deprecate use of BinaryReader
            using (var ms = new MemoryStream(definitions.Buffer))
            using (var f = new BinaryReader(ms))
            {
                count = f.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    LogicDefinition def = null;

                    if (type == 0)
                        def = new ActorDefinition();
                    else
                        def = new LogicDefinition();

                    def.Offset = (int)f.GetPosition();
                    def.Opcode = f.ReadByte();

                    // 0x3E for Driv3r; 0x0 for DPL
                    var sign = f.ReadByte();

                    def.StringId = f.ReadInt16();

                    if (def is ActorDefinition)
                        def.Reserved = f.ReadInt32();

                    if (sign == 0x3E)
                    {
                        def.R = f.ReadByte();
                        def.G = f.ReadByte();
                        def.B = f.ReadByte();
                        def.A = f.ReadByte();

                        def.Unknown = f.ReadInt16();
                        def.Flags = f.ReadInt16();
                    }
                    else if (sign == 0x0)
                    {
                        // DPL flips order of things

                        def.Unknown = f.ReadInt16();
                        def.Flags = f.ReadInt16();

                        def.R = f.ReadByte();
                        def.G = f.ReadByte();
                        def.B = f.ReadByte();
                        def.A = f.ReadByte();
                    }
                    else
                    {
                        throw new Exception(String.Format("The sign for the definition @ 0x{0:X} is incorrect!", def.Offset));
                    }

                    defList.Add(def);
                }
            }

            // load properties
            // TODO: Deprecate use of BinaryReader
            using (var ms = new MemoryStream(properties.Buffer))
            using (var f = new BinaryReader(ms))
            {
                if (f.ReadInt32() != count)
                    throw new Exception("The number of properties is mismatched to the number of definitions!");

                for (int d = 0; d < count; d++)
                {
                    LogicDefinition node = defList[d];

                    int pCount = f.ReadInt32();

                    node.Properties = new List<LogicProperty>(pCount);

                    for (int p = 0; p < pCount; p++)
                    {
                        LogicProperty prop;

                        uint offset = (uint)f.GetPosition();
                        int op = f.ReadByte();

                        var sign = f.ReadByte();

                        int id = f.ReadInt16();
                        int reserved = f.ReadInt32();

                        switch (op)
                        {
                        case 1:
                            {
                                int val = f.ReadInt32();
                                prop = new IntegerProperty(val);
                            }
                            break;
                        case 2:
                            {
                                float val = f.ReadSingle();
                                prop = new FloatProperty(val);
                            }
                            break;
                        case 3:
                            {
                                int val = f.ReadUInt16();
                                prop = new FilenameProperty((StringCollection.ContainsKey(val)) ? StringCollection[val] : "<NULL>");
                            }
                            break;
                        case 4:
                            {
                                bool val = f.ReadBoolean();
                                prop = new BooleanProperty(val);
                            }
                            break;
                        // TODO: IMPLEMENT ENUM PROPERTY
                        // TODO: IMPLEMENT FLAGS PROPERTY
                        case 6:
                        case 9:
                            {
                                uint val = f.ReadUInt32();
                                prop = new UnknownProperty(op, val);
                            }
                            break;
                        //===============================
                        case 7:
                            {
                                int val = f.ReadInt32();
                                prop = new ActorProperty(val);
                            }
                            break;
                        case 8:
                            {
                                int val = f.ReadUInt16();
                                prop = new StringProperty(StringCollection[val]);
                            }
                            break;
                        case 11:
                            {
                                long val = (long)f.ReadUInt64();
                                prop = new AudioProperty(val);
                            }
                            break;
                        case 17:
                            {
                                float[] float4 = new float[4] {
                                    f.ReadSingle(),
                                    f.ReadSingle(),
                                    f.ReadSingle(),
                                    f.ReadSingle()
                                };
                                prop = new Float4Property(float4);
                            }
                            break;
                        case 19:
                            {
                                int val = f.ReadInt32();
                                prop = new WireCollectionProperty(val);
                            }
                            break;
                        case 20:
                            {
                                int val = f.ReadInt32();
                                prop = new LocalisedStringProperty(val);
                            }
                            break;
                        case 21:
                            {
                                byte[] val = f.ReadBytes(reserved);
                                prop = new UnicodeStringProperty(val);
                            }
                            break;
                        case 22:
                        RAW_DATA:
                            {
                                byte[] val = f.ReadBytes(reserved);
                                prop = new RawDataProperty(val);
                            }
                            break;
                        default:
                            {
                                if (reserved == 4)
                                {
                                    uint unk = f.ReadUInt32();
                                    prop = new UnknownProperty(op, unk);
                                }
                                else
                                    goto RAW_DATA;
                            }
                            break;
                        }

                        f.Align(4);

                        prop.Offset = offset;
                        prop.StringId = id;
                        prop.Reserved = reserved;

                        node.Properties.Add(prop);
                    }
                }

                if (type == 0)
                    ActorDefinitions = defList;
                else
                    LogicNodeDefinitions = defList;
            }
        }

        public void LoadExportedMissionObjects(SpoolableDataOld exportedMissionObjects)
        {
            // TODO: Deprecate use of BinaryReader
            using (MemoryStream ms = new MemoryStream(exportedMissionObjects.Buffer))
            using (BinaryReader f = new BinaryReader(ms))
            {
                int count = f.ReadInt32();

                ExportedMissionObjects = new List<MissionObject>(count);

                for (int i = 0; i < count; i++)
                {
                    int type = f.ReadInt32();

                    MissionObject mObj = null;

                    switch (type)
                    {
                    case 0x0:
                        f.Align(16);
                        break;
                    case 0x1: mObj = new BlockType_0x1(f); break;
                    case 0x2: mObj = new BlockType_0x2(f); break;
                    case 0x3: mObj = new BlockType_0x3(f); break;
                    case 0x4: mObj = new BlockType_0x4(f); break;
                    case 0x5: mObj = new BlockType_0x5(f); break;
                    case 0x6: mObj = new BlockType_0x6(f); break;
                    case 0x7: mObj = new BlockType_0x7(f); break;
                    case 0x8: mObj = new BlockType_0x8(f); break;
                    case 0x9: mObj = new BlockType_0x9(f); break;
                    case 0xA: mObj = new BlockType_0xA(f); break;
                    case 0xB: mObj = new BlockType_0xB(f); break;
                    case 0xC: mObj = new BlockType_0xC(f); break;
                    default:
                        Console.WriteLine("NO READER FOR TYPE 0x{0:X}!", type);
                        break;
                    }

                    if (mObj == null)
                        break;

                    ExportedMissionObjects.Add(mObj);
                }
            }
        }
#endregion

        

        public MPCFile(int missionId) : this(GetMissionScriptFilepath(missionId))
        {
            LoadLocaleFile(missionId);
        }

        public MPCFile(int missionId, int localeId) : this(GetMissionScriptFilepath(missionId))
        {
            LoadLocaleFile(localeId);
        }

        public MPCFile(int missionId, string localeFile) : this(GetMissionScriptFilepath(missionId))
        {
            LoadLocaleFile(localeFile);
        }

        public MPCFile(string missionFile, string localeFile) : this(missionFile)
        {
            LoadLocaleFile(localeFile);
        }

        public MPCFile(string filename)
        {
#if FALSE
            // Legacy method

            ChunkFile = new ChunkReader(filename);

            SubChunkBlock missionObjects    = ChunkFile.FirstOrNull(ChunkType.ExportedMissionObjects);
            SubChunkBlock actorSetTable     = ChunkFile.FirstOrNull(ChunkType.LogicExportActorSetTable);
            SubChunkBlock stringCollection  = ChunkFile.FirstOrNull(ChunkType.LogicExportStringCollection);
            SubChunkBlock wireCollection    = ChunkFile.FirstOrNull(ChunkType.LogicExportWireCollections);
            SubChunkBlock actors            = ChunkFile.FirstOrNull(ChunkType.LogicExportActorsChunk);
            SubChunkBlock logicNodes        = ChunkFile.FirstOrNull(ChunkType.LogicExportNodesChunk);

            LoadExportedMissionObjects(missionObjects);
            LoadActorSetTable(actorSetTable);
            LoadStringCollection(stringCollection);
            LoadWireCollection(wireCollection);
            LoadActors(actors);
            LoadLogicNodes(logicNodes);
#else
            // New method

            Filename = filename;

            Chunk = new SpoolableChunkOld(Filename);

            var spoolers = Chunk.GetAllSpoolers();

            var missionObjects = spoolers.First((s) => s.Magic == (int)ChunkType.ExportedMissionObjects) as SpoolableDataOld;
            var actorSetTable = spoolers.First((s) => s.Magic == (int)ChunkType.LogicExportActorSetTable) as SpoolableDataOld;
            var stringCollection = spoolers.First((s) => s.Magic == (int)ChunkType.LogicExportStringCollection) as SpoolableDataOld;
            var wireCollection = spoolers.First((s) => s.Magic == (int)ChunkType.LogicExportWireCollections) as SpoolableDataOld;

            var actors = spoolers.First((s) => s.Magic == (int)ChunkType.LogicExportActorsChunk) as SpoolableChunkOld;
            var logicNodes = spoolers.First((s) => s.Magic == (int)ChunkType.LogicExportNodesChunk) as SpoolableChunkOld;

            LoadExportedMissionObjects(missionObjects);
            LoadActorSetTable(actorSetTable);
            LoadStringCollection(stringCollection);
            LoadWireCollection(wireCollection);

            LoadActors(actors);
            LoadLogicNodes(logicNodes);
#endif

            _isLoaded = true;
        }
#endif
    }

}
