using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace Zartex
{
    public class LogicCollectionData<T> : SpoolableResource<SpoolablePackage>
        where T : LogicDefinition, new()
    {
        protected SpoolableBuffer DefinitionsTable { get; set; }
        protected SpoolableBuffer PropertiesTable { get; set; }

        private List<T> _definitions;
        
        public List<T> Definitions
        {
            get
            {
                if (_definitions == null)
                    _definitions = new List<T>();

                return _definitions;
            }
            set { _definitions = value; }
        }

        protected T ReadDefinition(Stream stream)
        {
            var opcode = (byte)stream.ReadByte();
            var sign = stream.ReadByte();

            if (sign == 0)
                throw new Exception("Driver: Parallel Lines mission scripts not supported");
            else if (sign != 0x3E)
                throw new FormattedException("Bad definition sign @ 0x{0:X}!", (stream.Position - 1));

            var strId = stream.ReadInt16();

            var definition = new T() {
                OpCode = opcode,
                StringId = strId
            };

            if (typeof(T) == typeof(ActorDefinition))
                (definition as ActorDefinition).ObjectId = stream.ReadInt32();

            definition.Color = new NodeColor() {
                R = (byte)stream.ReadByte(),
                G = (byte)stream.ReadByte(),
                B = (byte)stream.ReadByte(),
                A = (byte)stream.ReadByte()
            };

            definition.Reserved = stream.ReadInt16();
            definition.Flags = stream.ReadInt16();

            return definition;
        }

        protected ILogicProperty ReadProperty(Stream stream)
        {
            // definitions get parsed first, so the sign of the property doesn't matter
            var opcode = (byte)(stream.ReadInt16() & 0xFF);

            short strId = stream.ReadInt16();
            int length = stream.ReadInt32();

            ILogicProperty prop;

            switch (opcode)
            {
            case 1:
                {
                    int val = stream.ReadInt32();
                    prop = new IntegerProperty(val);
                } break;
            case 2:
                {
                    float val = stream.ReadSingle();
                    prop = new FloatProperty(val);
                } break;
            case 3:
                {
                    int val = stream.ReadUInt16();
                    prop = new FilenameProperty(val);
                } break;
            case 4:
                {
                    bool val = (stream.ReadByte() > 0);
                    prop = new BooleanProperty(val);
                } break;
            case 6:
                {
                    int val = stream.ReadInt32();
                    prop = new EnumProperty(val);
                } break;
            case 9:
                {
                    int val = stream.ReadInt32();
                    prop = new FlagsProperty(val);
                } break;
            case 7:
                {
                    int val = stream.ReadInt32();
                    prop = new ActorProperty(val);
                } break;
            case 8:
                {
                    int val = stream.ReadUInt16();
                    prop = new StringProperty(val);
                } break;
            case 11:
                {
                    long val = (long)stream.ReadUInt64();
                    prop = new AudioProperty(val);
                } break;
            case 17:
                {
                    Vector4 val = new Vector4() {
                        X = stream.ReadFloat(),
                        Y = stream.ReadFloat(),
                        Z = stream.ReadFloat(),
                        W = stream.ReadFloat()
                    };

                    prop = new Float4Property(val);
                } break;
            case 19:
                {
                    int val = stream.ReadInt32();
                    prop = new WireCollectionProperty(val);
                } break;
            case 20:
                {
                    int val = stream.ReadInt32();
                    prop = new LocalisedStringProperty(val);
                } break;
            case 21:
                {
                    string val = Encoding.Unicode.GetString(stream.ReadBytes(length));
                    prop = new UnicodeStringProperty(val);
                } break;
            case 22:
                {
                    byte[] val = stream.ReadBytes(length);
                    prop = new RawDataProperty(val);
                } break;
            default:
                throw new Exception("UNKNOWN PROPERTY!");
            }

            stream.Align(4);

            prop.StringId = strId;

            return prop;
        }

        protected void WriteDefinition(Stream stream, T definition)
        {
            if (definition == null)
                return;

            stream.WriteByte(definition.OpCode);
            stream.WriteByte(0x3E);
            stream.Write(definition.StringId);

            if (definition is ActorDefinition)
                stream.Write((definition as ActorDefinition).ObjectId);

            stream.WriteByte(definition.Color.R);
            stream.WriteByte(definition.Color.G);
            stream.WriteByte(definition.Color.B);
            stream.WriteByte(definition.Color.A);

            stream.Write(definition.Reserved);
            stream.Write(definition.Flags);
        }

        protected virtual ChunkType DefinitionsType
        {
            get
            {
                if (typeof(T) == typeof(ActorDefinition))
                    return ChunkType.LogicExportActorDefinitions;
                else
                    return ChunkType.LogicExportNodeDefinitionsTable;
            }
        }

        protected override void Load()
        {
            DefinitionsTable = Spooler.GetFirstChild(DefinitionsType) as SpoolableBuffer;
            PropertiesTable = Spooler.GetFirstChild(ChunkType.LogicExportPropertiesTable) as SpoolableBuffer;

            using (var fP = PropertiesTable.GetMemoryStream())
            using (var fD = DefinitionsTable.GetMemoryStream())
            {
                var count = fP.ReadInt32();

                // Verify number of definitions/properties
                if (fD.ReadInt32() != count)
                    throw new Exception("Number of definitions/properties mismatch!");

                Definitions = new List<T>(count);

                for (int i = 0; i < count; i++)
                {
                    var def = ReadDefinition(fD);

                    // now read the properties associated with this definition
                    var nProps = fP.ReadInt32();
                    var props = new List<ILogicProperty>(nProps);

                    for (int ii = 0; ii < nProps; ii++)
                    {
                        var prop = ReadProperty(fP);
                        props.Add(prop);
                    }

                    def.Properties = props;

                    Definitions.Add(def);
                }
            }
        }

        protected override void Save()
        {
            var sizeOfDef = (typeof(T) == typeof(ActorDefinition)) ? 0x10 : 0xC;

            var count = (Definitions != null) ? Definitions.Count : 0;
            var bufSize = (4 + (count * sizeOfDef));

            var propBufferSize = 4;

            // First pass: Write definitions, calculate size of properties buffer
            using (var buf = new MemoryStream(bufSize))
            {
                foreach (var definition in Definitions)
                {
                    WriteDefinition(buf, definition);

                    // number of properties
                    propBufferSize += 4;

                    foreach (var prop in definition.Properties)
                    {
                        // size of header + length field
                        propBufferSize += 8;
                        propBufferSize += prop.SizeOf;
                    }
                }

                DefinitionsTable.SetBuffer(buf.ToArray());

                // Second pass: Write properties
                using (var propBuf = new MemoryStream(propBufferSize))
                {
                    foreach (var definition in Definitions)
                    {
                        foreach (var prop in definition.Properties)
                        {
                            propBuf.WriteByte(prop.OpCode);
                            propBuf.WriteByte(0x3E);
                            propBuf.Write(prop.StringId);

                            propBuf.Write(prop.SizeOf);

                            if (prop is BooleanProperty)
                            {
                                propBuf.WriteByte(((bool)prop.Value) ? 0 : -1);
                                
                                // write 3 padding bytes
                                for (int i = 0; i < 3; i++)
                                    propBuf.Write(0x3E);
                            }
                            if (prop is IntegerProperty)
                            {
                                propBuf.Write((int)prop.Value);
                            }
                            else if (prop is FloatProperty)
                            {
                                propBuf.Write((float)prop.Value);
                            }
                            else if (prop is AudioProperty)
                            {
                                propBuf.Write((long)prop.Value);
                            }
                            else if (prop is Float4Property)
                            {
                                var vec = (Vector4)prop.Value;

                                propBuf.Write(vec.X);
                                propBuf.Write(vec.Y);
                                propBuf.Write(vec.Z);
                                propBuf.Write(vec.W);
                            }
                            else if (prop is UnicodeStringProperty)
                            {
                                var str = (string)prop.Value;
                                var val = Encoding.Unicode.GetBytes(str);

                                propBuf.Write(val, 0, val.Length);
                            }
                            else if (prop is RawDataProperty)
                            {
                                var val = (byte[])prop.Value;

                                propBuf.Write(val, 0, val.Length);
                            }
                            else
                            {
                                throw new InvalidOperationException("FATAL ERROR: Unhandled property type - failed to write!");
                            }
                        }
                    }
                    PropertiesTable.SetBuffer(propBuf.ToArray());
                }
            }
        }
    }
}
