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
                    var def = NodeDefinition.Create<T>(fD);

                    // now read the properties associated with this definition
                    var nProps = fP.ReadInt32();
                    var props = new List<NodeProperty>(nProps);

                    for (int ii = 0; ii < nProps; ii++)
                    {
                        var prop = NodeProperty.Create(fP);
                        props.Add(prop);

                        if ((fP.Position & 0x3) != 0)
                            fP.Align(4);
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
            var dBufferSize = (4 + (count * sizeOfDef));
            var pBufferSize = 4;

            var dBuffer = new byte[dBufferSize];

            // First pass: Write definitions, calculate size of properties buffer
            using (var fD = new MemoryStream(dBuffer))
            {
                fD.Write(count);

                foreach (var definition in Definitions)
                {
                    definition.WriteTo(fD);

                    // number of properties
                    pBufferSize += 4;

                    foreach (var prop in definition.Properties)
                    {
                        pBufferSize = Memory.Align(pBufferSize, 4);

                        // size of header + data
                        pBufferSize += (prop.Size + 0x8);
                    }
                }
            }
            
            var pBuffer = new byte[pBufferSize];

            // Second pass: Write properties
            using (var fP = new MemoryStream(pBuffer))
            {
                fP.Write(count);

                foreach (var definition in Definitions)
                {
                    fP.Write(definition.Properties.Count);

                    foreach (var prop in definition.Properties)
                    {
                        prop.WriteTo(fP);

                        if (fP.Position < pBufferSize)
                        {
                            while ((fP.Position & 0x3) != 0)
                                fP.WriteByte(0x3E);
                        }
                    }
                }
            }

            DefinitionsTable.SetBuffer(dBuffer);
            PropertiesTable.SetBuffer(pBuffer);
        }
    }
}
