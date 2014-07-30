using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DSCript.Models
{
    //public class ModelPackagePS2
    //{
    //    // 'MPAK'
    //    public override uint Magic
    //    {
    //        get { return 0x4B41504D; }
    //    }
    //
    //    void Store(ArrayList buffer, ArrayList tempBuffer, int count)
    //    {
    //        int d = 2 ^ 7 - 1;
    //
    //        switch (count)
    //        {
    //        case 1: goto default;
    //        case 2:
    //            {
    //                buffer.Add(tempBuffer[1]);
    //                buffer.Add(tempBuffer[2]);
    //            } break;
    //        case 3:
    //            {
    //                var a = (int)tempBuffer[1] / d;
    //                var b = (int)tempBuffer[2] / d;
    //                var c = (int)tempBuffer[3] / d;
    //
    //                buffer.Add(a);
    //                buffer.Add(b);
    //                buffer.Add(c);
    //            } break;
    //        case 4:
    //            {
    //                buffer.Add(tempBuffer[1]);
    //                buffer.Add(tempBuffer[2]);
    //                buffer.Add(tempBuffer[3]);
    //                buffer.Add(tempBuffer[4]);
    //            } break;
    //        default:
    //            buffer.Add(tempBuffer[1]);
    //            break;
    //        }
    //    }
    //
    //    void Unpack(BinaryReader f, ArrayList buffer)
    //    {
    //        int addr = f.ReadByte();
    //        int flag = f.ReadByte();
    //        int count = f.ReadByte();
    //        int comm_spec = f.ReadByte();
    //
    //        int vn = ((comm_spec & 0xC) >> 2) + 1;
    //        int vl = comm_spec & 0x3;
    //
    //        ArrayList tempBuffer = new ArrayList();
    //
    //        for (int i = 0; i < count; i++)
    //        {
    //            for (int n = 0; n < vn; n++)
    //            {
    //                switch (vl)
    //                {
    //                case 0: tempBuffer.Add(Convert.ToDouble(f.ReadSingle())); break;
    //                case 1:
    //                case 3:
    //                    tempBuffer.Add(f.ReadUInt16());
    //                    break;
    //                case 2: tempBuffer.Add(f.ReadByte()); break;
    //                }
    //            }
    //
    //            Store(buffer, tempBuffer, vn);
    //        }
    //    }
    //
    //    public override void Load()
    //    {
    //        using (BlockEditor blockEditor = new BlockEditor(BlockData))
    //        {
    //            BinaryReader f = blockEditor.Reader;
    //
    //            if (blockEditor.BlockData.Block.Reserved != 1)
    //                throw new Exception("Bad GMC2 version, cannot load ModelPackagePS2!");
    //            if (f.ReadUInt32() != Magic)
    //                throw new Exception("Bad magic, cannot load ModelPackagePS2!");
    //
    //            UID = f.ReadUInt32();
    //
    //            f.Seek(0xC, SeekOrigin.Current);
    //
    //            int nParts         = f.ReadInt32();
    //            uint tsc2Offset     = f.ReadUInt32();
    //
    //            f.Seek(0x4, SeekOrigin.Current);
    //
    //            Parts       = new List<PartsGroup>(nParts);
    //            MeshGroups  = new List<MeshGroup>();
    //            Meshes      = new List<IndexedMesh>();
    //
    //            long holdPos;
    //
    //            // Read all parts
    //            for (int p = 0; p < nParts; p++)
    //            {
    //                uint offset = f.ReadUInt32();
    //
    //                holdPos = f.GetPosition();
    //
    //                // Read GEO2 entry
    //                f.Seek(offset, SeekOrigin.Begin);
    //
    //                // 'GEO2'
    //                if (f.ReadUInt32() != 0x324F4547)
    //                    throw new Exception("Bad geometry identifier, cannot load ModelPackagePS2!");
    //
    //                int numGroups   = f.ReadByte();
    //                int numOffsets  = f.ReadByte();
    //                int numMeshes   = f.ReadByte();
    //
    //                PartsGroup part = new PartsGroup() {
    //                    Handle  = f.ReadUInt32(),
    //                    UID     = f.ReadUInt32()
    //                };
    //
    //                // Skip float padding
    //                f.Seek(0x1C, SeekOrigin.Current);
    //
    //                // No idea what this is, sometimes it's zero
    //                uint unknown = f.ReadUInt32();
    //
    //                // skip padding
    //                f.Seek(0x10, SeekOrigin.Current);
    //
    //                for (int g = 0; g < numGroups; g++)
    //                {
    //                    // skip float padding
    //                    f.Seek(0x10, SeekOrigin.Current);
    //
    //                    
    //                }
    //
    //            }
    //
    //        }
    //    }
    //}
}
