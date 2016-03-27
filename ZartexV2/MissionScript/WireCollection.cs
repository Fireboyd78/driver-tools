using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace Zartex
{
    public class WireCollection
    {
        public List<WireNode> Wires { get; set; }

        public WireCollection(int nWires)
        {
            Wires = new List<WireNode>(nWires);
        }
    }

    public enum WireNodeType
    {
        OnSuccessEnable     = 1,
        OnSuccessDisable    = 2,

        OnFailureEnable     = 3,
        OnFailureDisable    = 4,

        OnConditionEnable   = 5,
        OnConditionDisable  = 6,

        LogicEnable         = 11,
        LogicDisable        = 12
    }

    public class WireNode
    {
        public byte WireType { get; set; }
        public byte OpCode { get; set; }

        public short NodeId { get; set; }

        public WireNodeType GetWireNodeType()
        {
            return (WireNodeType)WireType;
        }
    }

    public class WireCollectionData : SpoolableResource<SpoolableBuffer>
    {
        public List<WireCollection> WireCollections { get; set; }

        private WireNode ReadWireNode(Stream stream)
        {
            return new WireNode() {
                WireType = (byte)stream.ReadByte(),
                OpCode   = (byte)stream.ReadByte(),

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

                    WireCollection wireGroup =  new WireCollection(nWires);

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
            int bufSize = 4;

            foreach (var wireCol in WireCollections)
                bufSize += (wireCol.Wires.Count * 4) + 4;

            using (var wireBuffer = new MemoryStream(bufSize))
            {
                wireBuffer.Write(WireCollections.Count);

                foreach (var wireCol in WireCollections)
                {
                    wireBuffer.Write(wireCol.Wires.Count);

                    foreach (var wire in wireCol.Wires)
                        WriteWireNode(wireBuffer, wire);
                }

                Spooler.SetBuffer(wireBuffer.ToArray());
            }
        }
    }
}
