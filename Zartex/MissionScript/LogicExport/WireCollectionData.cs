using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DSCript.Spooling;

namespace Zartex
{
    public class WireCollectionData : SpoolableResource<SpoolableBuffer>
    {
        public List<WireCollection> WireCollections { get; set; }

        public WireCollection this[int index]
        {
            get { return WireCollections[index]; }
        }

        private WireNode ReadWireNode(Stream stream)
        {
            return new WireNode() {
                WireType = (byte)stream.ReadByte(),
                OpCode = (byte)stream.ReadByte(),

                NodeId = stream.ReadInt16()
            };
        }

        private void WriteWireNode(Stream stream, WireNode wireNode)
        {
            stream.WriteByte(wireNode.WireType);
            stream.WriteByte(wireNode.OpCode);
            stream.Write(wireNode.NodeId);
        }

        protected override void Load()
        {
            using (var f = Spooler.GetMemoryStream())
            {
                int nCollections = f.ReadInt32();

                WireCollections = new List<WireCollection>(nCollections);

                for (int i = 0; i < nCollections; i++)
                {
                    int nWires = f.ReadInt32();

                    WireCollection wireGroup = new WireCollection(nWires);

                    for (int ii = 0; ii < nWires; ii++)
                    {
                        var wireNode = ReadWireNode(f);

                        wireGroup.Wires.Add(wireNode);
                    }

                    WireCollections.Add(wireGroup);
                }
            }
        }

        protected override void Save()
        {
            var nCollections = WireCollections.Count;
            var bufSize = 0x4;

            foreach (var wireCol in WireCollections)
                bufSize += (0x4 + (wireCol.Wires.Count * 0x4));

            var wireBuffer = new byte[bufSize];

            using (var fW = new MemoryStream(wireBuffer))
            {
                fW.Write(nCollections);

                foreach (var wireCol in WireCollections)
                {
                    var wires = wireCol.Wires;
                    var nWires = wires.Count;

                    fW.Write(nWires);

                    foreach (var wire in wires)
                        WriteWireNode(fW, wire);
                }
            }

            Spooler.SetBuffer(wireBuffer);
        }
    }
}
