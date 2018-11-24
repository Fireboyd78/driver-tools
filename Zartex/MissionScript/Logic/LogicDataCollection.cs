using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace Zartex
{
    public class LogicDataCollection<T> : SpoolableResource<SpoolablePackage>
        where T : NodeDefinition, new()
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

        public T this[int index]
        {
            get { return Definitions[index]; }
            set { Definitions[index] = value; }
        }

        protected T ReadDefinition(Stream stream)
        {
            var opcode = (byte)stream.ReadByte();
            var sign = stream.ReadByte();

            if (sign == 0)
                throw new Exception("Driver: Parallel Lines mission scripts not supported");
            else if (sign != 0x3E)
                throw new Exception(String.Format("Bad definition sign @ 0x{0:X}!", (stream.Position - 1)));

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

        protected NodeProperty ReadProperty(Stream stream)
        {
            // definitions get parsed first, so the sign of the property doesn't matter
            var opcode = (byte)(stream.ReadInt16() & 0xFF);

            var strId = stream.ReadInt16();
            var length = stream.ReadInt32();

            NodeProperty prop;

            switch (opcode)
            {
            case 1:
                {
                    var val = stream.ReadInt32();
                    prop = new IntegerProperty(val);
                } break;
            case 2:
                {
                    var val = stream.ReadSingle();
                    prop = new FloatProperty(val);
                } break;
            case 3:
                {
                    var val = stream.ReadInt16();
                    prop = new StringProperty(val);
                } break;
            case 4:
                {
                    var val = (stream.ReadByte() > 0);
                    prop = new BooleanProperty(val);
                } break;
            case 6:
                {
                    var val = stream.ReadInt32();
                    prop = new EnumProperty(val);
                } break;
            case 9:
                {
                    var val = stream.ReadInt32();
                    prop = new FlagsProperty(val);
                } break;
            case 7:
                {
                    var val = stream.ReadInt32();
                    prop = new ActorProperty(val);
                } break;
            case 8:
                {
                    var val = stream.ReadInt16();
                    var val2 = stream.ReadInt16();

                    prop = new AIPersonalityProperty(val, val2);
                } break;
            case 11:
                {
                    var val = stream.Read<AudioInfo>();
                    prop = new AudioProperty(val);
                } break;
            case 17:
                {
                    var val = stream.Read<Vector4>();
                    prop = new Float4Property(val);
                } break;
            case 19:
                {
                    var val = stream.ReadInt32();
                    prop = new WireCollectionProperty(val);
                } break;
            case 20:
                {
                    var val = stream.ReadInt32();
                    prop = new LocalisedStringProperty(val);
                } break;
            case 21:
                {
                    var val = Encoding.Unicode.GetString(stream.ReadBytes(length));
                    prop = new UnicodeStringProperty(val);
                } break;
            case 22:
            RAW_DATA:
                {
                    var val = stream.ReadBytes(length);
                    prop = new RawDataProperty(val);
                } break;
            default:
                {
                    if (length == 4)
                    {
                        var unk = stream.ReadUInt32();
                        prop = new UnknownProperty(opcode, unk);
                    }
                    else
                        goto RAW_DATA;
                } break;
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

        protected void WriteProperty(Stream stream, NodeProperty property, bool alignStream)
        {
            stream.WriteByte(property.OpCode);
            stream.WriteByte(0x3E);
            stream.Write(property.StringId);

            if (property is BooleanProperty)
            {
                stream.Write(0x1);
                stream.WriteByte((bool)property.Value ? 255 : 0);
            }
            else if (property is StringProperty)
            {
                stream.Write(0x2);
                stream.Write((short)property.Value);
            }
            else if (property is AIPersonalityProperty)
            {
                stream.Write(0x4);
                stream.Write((short)property.Value);
                stream.Write(((AIPersonalityProperty)property).PersonalityIndex);
            }
            else if (property is IntegerProperty)
            {
                stream.Write(0x4);
                stream.Write((int)property.Value);
            }
            else if (property is FloatProperty)
            {
                stream.Write(0x4);
                stream.Write((float)property.Value);
            }
            else if (property is AudioProperty)
            {
                stream.Write(0x8);
                stream.Write(((AudioProperty)property).Value);
            }
            else if (property is Float4Property)
            {
                stream.Write(0x10);
                stream.Write((Vector4)property.Value);
            }
            else if (property is UnicodeStringProperty)
            {
                var str = (string)property.Value;
                var val = Encoding.Unicode.GetBytes(str);

                stream.Write(val.Length);
                stream.Write(val, 0, val.Length);
            }
            else if (property is RawDataProperty)
            {
                var val = (byte[])property.Value;

                stream.Write(val.Length);
                stream.Write(val, 0, val.Length);
            }
            else
            {
                throw new InvalidOperationException("FATAL ERROR: Unhandled property type - failed to write!");
            }

            if (alignStream)
            {
                while ((stream.Position & 0x3) != 0)
                    stream.WriteByte(0x3E);
            }
        }

        protected int GetSizeOfProperty(NodeProperty prop)
        {
            // size of header + length field
            var size = 0x8;

            if (prop is BooleanProperty)
                size += 0x1;
            else if (prop is StringProperty)
                size += 0x2;
            else if (prop is AIPersonalityProperty)
                size += 0x4;
            else if (prop is IntegerProperty || prop is FloatProperty)
                size += 0x4;
            else if (prop is AudioProperty)
                size += 0x8;
            else if (prop is Float4Property)
                size += 0x10;
            else if (prop is UnicodeStringProperty)
                size += (((string)prop.Value).Length * 2);
            else if (prop is RawDataProperty)
                size += ((byte[])prop.Value).Length;
            else
                throw new InvalidOperationException("FATAL ERROR: Cannot calculate size of property!");

            return size;
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
                    var props = new List<NodeProperty>(nProps);

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
            var defBufferSize = (4 + (count * sizeOfDef));
            var propBufferSize = 4;

            var dBuffer = new byte[defBufferSize];

            // First pass: Write definitions, calculate size of properties buffer
            using (var fD = new MemoryStream(dBuffer))
            {
                fD.Write(count);

                foreach (var definition in Definitions)
                {
                    WriteDefinition(fD, definition);

                    // number of properties
                    propBufferSize += 4;

                    foreach (var prop in definition.Properties)
                    {
                        propBufferSize = Memory.Align(propBufferSize, 4);
                        propBufferSize += GetSizeOfProperty(prop);
                    }
                }
            }
            
            var pBuffer = new byte[propBufferSize];

            // Second pass: Write properties
            using (var fP = new MemoryStream(pBuffer))
            {
                fP.Write(count);

                foreach (var definition in Definitions)
                {
                    fP.Write(definition.Properties.Count);

                    foreach (var prop in definition.Properties)
                    {
                        if (prop is UnknownProperty)
                            throw new InvalidOperationException("FATAL ERROR: Cannot write an unknown property!");

                        WriteProperty(fP, prop, true);
                    }
                }
            }

            DefinitionsTable.SetBuffer(dBuffer);
            PropertiesTable.SetBuffer(pBuffer);
        }
    }
}
