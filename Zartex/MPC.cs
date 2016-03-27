using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Zartex;
using Zartex.MissionObjects;
using Zartex.Settings;

using ChunkType = DSCript.ChunkType;

namespace Zartex
{
    public sealed class MPCFile
    {
        #region Static properties / formatting strings
        static bool _localeError = false;

        static string ROOT   = Configuration.Settings.InstallDirectory;
        static string LOCALE = Configuration.Settings.Locale;

        static string FMT_FILE_M  = "mission{0:d2}";
        static string FMT_DIR_M   = @"{0}\Missions";
        //static string FMT_W  = "mood{0:d2}";

        static string F_TERRITORY = String.Format(@"{0}\Territory", ROOT);
        static string F_MISSIONS  = String.Format(@"{0}\{1}", String.Format(FMT_DIR_M, ROOT), FMT_FILE_M);
        //static string F_MOODS     = String.Format(@"{0}\Moods\{1}", ROOT, FMT_W);
        //static string F_VEHICLES  = String.Format(@"{0}\Vehicles\{1}", ROOT, FMT_M);

        static string MI_OBJECTS  = String.Format(@"{0}.dam", F_MISSIONS, FMT_FILE_M);
        static string MI_SCRIPT   = String.Format(@"{0}.mpc", F_MISSIONS, FMT_FILE_M);
        //static string MI_VEHICLES = String.Format(@"{0}\{1}.vvv", F_LOCALE, FMT_M);

        // These get set below
        static string REGION, F_LOCALE, F_LOCALE_M, MI_LOCALE;

        static MPCFile()
        {
            string[] dirs = Directory.GetDirectories(F_TERRITORY);
            string dirName = (dirs.Length > 0) ? new DirectoryInfo(dirs[0]).Name : "NULL";

            if (dirs.Length > 1)
            {
                _localeError = true;
                MessageBox.Show(
                    String.Format("ERROR 0x961:\r\nThe region could not be properly determined. Defaulting to '{0}'.", dirName),
                    "Zartex Mission Editor",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            else if (dirs.Length <= 0)
            {
                _localeError = true;
                MessageBox.Show("ERROR 0x962: FATAL LOCALE ERROR! Please contact the developer!", "Zartex Mission Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            REGION = dirName;

            F_LOCALE = String.Format(@"{0}\{1}\Locale\{2}", F_TERRITORY, REGION, LOCALE);
            F_LOCALE_M = String.Format(FMT_DIR_M, F_LOCALE);
            MI_LOCALE = String.Format(@"{0}\Missions\{1}.txt", F_LOCALE, FMT_FILE_M);
        }
        #endregion

        #region Static script filenames
        public static IDictionary<int, string> ScriptFiles = new Dictionary<int, string>()
        {
            /*-------------------------------------
              MISSIONS
              ------------------------------------*/
            { 1,  "Police HQ"                     }, // Miami
            { 2,  "Lead on Baccus"                },
            { 3,  "The Siege"                     },
            { 4,  "Rooftops"                      },
            { 5,  "Impress Lomaz"                 },
            { 6,  "Gator's Yacht"                 },
            { 7,  "The Hit"                       },
            { 8,  "Trapped"                       },
            { 9,  "Dodge Island"                  },
            { 10, "Retribution"                   },                                                    
            { 11, "Welcome to Nice"               }, // Nice
            { 13, "Smash and Run"                 },
            { 14, "18-wheeler"                    },
            { 15, "Hijack"                        },
            { 16, "Arms Deal"                     },
            { 17, "Booby Trap"                    },
            { 18, "Calita in Trouble"             },
            { 19, "Rescue Dubois"                 },
            { 21, "Hunted"                        },         
            { 22, "Surveillance"                  }, // Istanbul
            { 24, "Tanner Escapes"                },
            { 25, "Another Lead"                  },
            { 27, "Alleyway"                      },
            { 28, "The Chase"                     },
            { 30, "Bomb Truck"                    },
            { 31, "Chase the Train"               },
            /*-------------------------------------
              DRIVING GAMES
              ------------------------------------*/
            { 32, "Quick Chase, Miami #1"         },
            { 33, "Quick Chase, Miami #2"         },
            { 34, "Quick Chase, Nice #1"          },
            { 35, "Quick Chase, Nice #2"          },
            { 36, "Quick Chase, Istanbul #1"      },
            { 37, "Quick Chase, Istanbul #2"      },
            { 38, "Quick Getaway, Miami #1"       },
            { 39, "Quick Getaway, Miami #2"       },
            { 40, "Quick Getaway, Miami #3"       },
            { 42, "Quick Getaway, Nice #1"        },
            { 43, "Quick Getaway, Nice #2"        },
            { 44, "Quick Getaway, Nice #3"        },
            { 46, "Quick Getaway, Istanbul #1"    },
            { 47, "Quick Getaway, Istanbul #2"    },
            { 48, "Quick Getaway, Istanbul #3"    },
            { 50, "Trail Blazer, Miami #1"        },
            { 51, "Trail Blazer, Miami #2"        },
            { 52, "Trail Blazer, Nice #1"         },
            { 53, "Trail Blazer, Nice #2"         },
            { 54, "Trail Blazer, Istanbul #1"     },
            { 55, "Trail Blazer, Istanbul #2"     },
            { 56, "Survival, Miami"               },
            { 57, "Survival, Nice"                },
            { 58, "Survival, Istanbul"            },
            { 59, "Checkpoint Race, Miami #1"     },
            { 60, "Checkpoint Race, Miami #2"     },
            { 61, "Checkpoint Race, Miami #3"     },
            { 62, "Checkpoint Race, Nice #1"      },
            { 63, "Checkpoint Race, Nice #2"      },
            { 64, "Checkpoint Race, Nice #3"      },
            { 65, "Checkpoint Race, Istanbul #1"  },
            { 66, "Checkpoint Race, Istanbul #2"  },
            { 67, "Checkpoint Race, Istanbul #3"  },
            { 71, "Gate Race, Miami #1"           },
            { 72, "Gate Race, Miami #2"           },
            { 73, "Gate Race, Nice #1"            },
            { 74, "Gate Race, Nice #2"            },
            { 75, "Gate Race, Istanbul #1"        },
            { 76, "Gate Race, Istanbul #2"        },
            /*-------------------------------------
              TAKE-A-RIDE
              ------------------------------------*/
            { 77, "Take a Ride, Miami"            },
            { 78, "Take a Ride, Miami (Semi)"     },
            { 80, "Take a Ride, Nice"             },
            { 81, "Take a Ride, Nice (Semi)"      },
            { 83, "Take a Ride, Istanbul"         },
            { 84, "Take a Ride, Istanbul (Semi)"  }
        };
        #endregion

        #region Static methods
        public static string GetMissionLocaleDirectory()
        {
            return F_LOCALE_M;
        }

        public static string GetMissionLocaleFilepath(int missionId)
        {
            return String.Format(MI_LOCALE, missionId);
        }

        public static string GetMissionScriptDirectory()
        {
            return String.Format(FMT_DIR_M, ROOT);
        }

        public static string GetMissionScriptFilepath(int missionId)
        {
            return String.Format(MI_SCRIPT, missionId);
        }

        public static string MissionScriptDebug(int missionId)
        {
            string locale = GetMissionLocaleFilepath(missionId);
            return String.Format(
@"Name: {0}
Script File: {1}
Objects File: {2}
Locale File: {3}

The locale file {4}.",
                ScriptFiles[missionId],
                String.Format(MI_SCRIPT, missionId),
                String.Format(MI_OBJECTS, missionId),
                locale,
                ((File.Exists(locale)) ? "exists" : "DOES NOT exist")
            );
        }
        #endregion Static methods

        private bool _isLoaded = false;
        private bool _hasLocale = false;

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
