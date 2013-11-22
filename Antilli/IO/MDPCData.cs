namespace DSC.IO.Types
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using DSCript;
    using DSCript.IO;

    using FreeImageAPI;

    public sealed class MDPCData : BlockData
    {
        public class ModelGroup
        {
            public uint GUID { get; set; }
            public uint Handle { get; set; }

            public List<ModelGroupEntry> Entries = new List<ModelGroupEntry> { };
        }

        public class ModelGroupEntry
        {
            public uint Offset { get; set; }
            public uint Unk1 { get; set; }
            public uint Unk2 { get; set; }
            public uint Unk3 { get; set; }

            public List<MeshEntry> Children = new List<MeshEntry> { };
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Mesh
        {
            public uint Offset { get; set; }
            public uint VIndex { get; set; }
            public uint VCount { get; set; }
            public uint TIndex { get; set; }
            public uint TCount { get; set; }
            public uint MaterialID { get; set; }
            public uint TexFlag { get; set; }
        }


        public class MeshEntry
        {
            public uint Offset { get; set; }
            public uint Count { get; set; }

            public uint Unk1 { get; set; }

            public List<Mesh> Mesh = new List<Mesh> { };
        }

        public class IndexBuffer
        {
            public uint Count { get; set; }
            public uint Size { get; set; }
            public uint Offset { get; set; }

            public byte[] Buffer;
        }

        public class VertexBuffer
        {
            public class Vertex15
            {
                public float vx;
                public float vy;
                public float vz;
                public float nx;
                public float ny;
                public float nz;
                public float tu;
                public float tv;
            }

            public uint Count { get; set; }
            public uint Size { get; set; }
            public uint Offset { get; set; }
            public uint FVFOffset { get; set; }
            public uint Length { get; set; }

            public List<Vertex15> Buffer = new List<Vertex15> { };
        }

        private const uint Version = 0x6;

        public uint MType { get; set; }

        public IndexBuffer Indicies = new IndexBuffer();
        public VertexBuffer Vertices = new VertexBuffer();

        public List<ModelGroup> Group = new List<ModelGroup> { };
        public List<Mesh> Meshes = new List<Mesh> { };
        public List<MeshEntry> MeshEntries = new List<MeshEntry> { };

        public PCMPData PCMP = new PCMPData();

        public uint nGroups { get; set; }
        public uint GroupsOffset { get; set; }

        public uint nMeshes { get; set; }
        public uint MeshesOffset { get; set; }

        public uint nMeshEntries { get; set; }
        public uint MeshEntriesOffset { get; set; }

        public const ushort UnkID = 0x474A;
        //0x4 padding

        public uint DDSOffset { get; set; }
        public uint PCMPOffset { get; set; }

        public uint nIndicies { get; set; }

        public uint IndiciesSize { get; set; }
        public uint IndiciesOffset { get; set; }

        /// <summary>Unknown. Possibly a face type? (untested)</summary>
        public uint UnkFaceType { get; set; }

        public MDPCData(SubChunkBlock block, byte[] buffer)
        {
            this.CreateBlockData(block, buffer);

            using (BinaryReader reader = new BinaryReader(this.Buffer))
            {
                if (reader.ReadUInt32() == Version)
                {
                    this.MType = reader.ReadUInt32();
                    this.nGroups = reader.ReadUInt32();
                    this.GroupsOffset = reader.ReadUInt32();
                    this.nMeshes = reader.ReadUInt32();
                    this.MeshesOffset = reader.ReadUInt32();
                    this.nMeshEntries = reader.ReadUInt32();
                    this.MeshEntriesOffset = reader.ReadUInt32();

                    if (reader.ReadUInt16() == this.MType)
                    {
                        // Console.WriteLine("Unknown MDPC type check passed.");
                    }
                    else
                    {
                        // Console.WriteLine("WARNING: Unknown MDPC type check failed!");
                    }

                    if (reader.ReadUInt16() == UnkID)
                    {
                        // Console.WriteLine("Unknown id(?) check passed.");
                    }
                    else
                    {
                        // Console.WriteLine("WARNING: Unknown id(?) check failed!");
                    }

                    //Skip byte-aligned padding(?)
                    reader.BaseStream.Seek(0x4, SeekOrigin.Current);

                    this.DDSOffset = reader.ReadUInt32();
                    this.PCMPOffset = reader.ReadUInt32();
                    this.nIndicies = reader.ReadUInt32();
                    this.IndiciesSize = reader.ReadUInt32();
                    this.IndiciesOffset = reader.ReadUInt32();
                    this.UnkFaceType = reader.ReadUInt32();

                    // Console.WriteLine(
                    //     (this.UnkFaceType == 0x01) ? "Unknown face type check passed." : "WARNING: Unknown face type check failed!"
                    //     );

                    this.Vertices.FVFOffset = reader.ReadUInt32();

                    /* -----------------------------------
                       reader.Read PCMP Data
                       ----------------------------------- */

                    reader.BaseStream.Seek((int)this.PCMPOffset, SeekOrigin.Begin);

                    if (reader.ReadUInt32() == PCMPData.Magic && reader.ReadUInt32() == PCMPData.Version)
                    {
                        this.PCMP.tGroupCount = reader.ReadUInt32();
                        this.PCMP.tGroupOffset = reader.ReadUInt32();

                        this.PCMP.t2Count = reader.ReadUInt32();
                        this.PCMP.t2Offset = reader.ReadUInt32();

                        this.PCMP.tSubMatCount = reader.ReadUInt32();
                        this.PCMP.tSubMatOffset = reader.ReadUInt32();

                        this.PCMP.t4Count = reader.ReadUInt32();
                        this.PCMP.t4Offset = reader.ReadUInt32();

                        this.PCMP.DDSInfoCount = reader.ReadUInt32();
                        this.PCMP.DDSInfoOffset = reader.ReadUInt32();

                        this.PCMP.DDSOffset = reader.ReadUInt32();
                        this.PCMP.Size = reader.ReadUInt32();

                        reader.BaseStream.Seek((int)(this.PCMPOffset + this.PCMP.tGroupOffset), SeekOrigin.Begin);

                        for (int p = 0; p <= this.PCMP.tGroupCount - 1; p++)
                        {
                            this.PCMP.Materials.Insert(p, new PCMPData.Material());

                            this.PCMP.Materials[p].t2Offset = reader.ReadUInt32();
                            this.PCMP.Materials[p].t2Count = reader.ReadUInt32();

                            this.PCMP.Materials[p].Unk1 = reader.ReadUInt32();
                            this.PCMP.Materials[p].Unk2 = reader.ReadUInt32();
                            this.PCMP.Materials[p].Unk3 = reader.ReadUInt32();
                            this.PCMP.Materials[p].Unk4 = reader.ReadUInt32();
                        }

                        for (int p = 0; p <= PCMP.tGroupCount - 1; p++)
                        {
                            reader.BaseStream.Seek((int)(this.PCMPOffset + this.PCMP.Materials[p].t2Offset), SeekOrigin.Begin);

                            for (int g = 0; g <= this.PCMP.Materials[p].t2Count - 1; g++)
                            {
                                this.PCMP.Materials[p].SubMaterial.Insert(g, new PCMPData.SubMaterial());

                                this.PCMP.Materials[p].SubMaterial[g].Offset = reader.ReadUInt32();
                                reader.BaseStream.Seek(0x4, SeekOrigin.Current);
                            }

                            for (int g = 0; g <= this.PCMP.Materials[p].t2Count - 1; g++)
                            {
                                reader.BaseStream.Seek((int)(this.PCMPOffset + this.PCMP.Materials[p].SubMaterial[g].Offset), SeekOrigin.Begin);

                                this.PCMP.Materials[p].SubMaterial[g].Unk1 = reader.ReadUInt32();
                                this.PCMP.Materials[p].SubMaterial[g].Unk2 = reader.ReadUInt32();
                                this.PCMP.Materials[p].SubMaterial[g].Unk3 = reader.ReadUInt32();
                                this.PCMP.Materials[p].SubMaterial[g].Unk4 = reader.ReadUInt32();

                                this.PCMP.Materials[p].SubMaterial[g].t4Offset = reader.ReadUInt32();
                                this.PCMP.Materials[p].SubMaterial[g].t4Count = reader.ReadUInt32();

                                this.PCMP.Materials[p].SubMaterial[g].Unk5 = reader.ReadUInt32();
                                this.PCMP.Materials[p].SubMaterial[g].Unk6 = reader.ReadUInt32();

                                reader.BaseStream.Seek((int)(this.PCMPOffset + this.PCMP.Materials[p].SubMaterial[g].t4Offset), SeekOrigin.Begin);

                                this.PCMP.Materials[p].SubMaterial[g].DDSInfoOffset = reader.ReadUInt32();

                                reader.BaseStream.Seek((int)(this.PCMPOffset + this.PCMP.Materials[p].SubMaterial[g].DDSInfoOffset), SeekOrigin.Begin);

                                for (int t = 0; t <= this.PCMP.Materials[p].SubMaterial[g].t4Count - 1; t++)
                                {
                                    this.PCMP.Materials[p].SubMaterial[g].Textures.Insert(t, new PCMPData.DDSInfo());

                                    this.PCMP.Materials[p].SubMaterial[g].Textures[t].Unk1 = reader.ReadByte();
                                    this.PCMP.Materials[p].SubMaterial[g].Textures[t].Unk2 = reader.ReadByte();
                                    this.PCMP.Materials[p].SubMaterial[g].Textures[t].Unk3 = reader.ReadByte();
                                    this.PCMP.Materials[p].SubMaterial[g].Textures[t].Unk4 = reader.ReadByte();

                                    this.PCMP.Materials[p].SubMaterial[g].Textures[t].CRC32 = reader.ReadUInt32();
                                    this.PCMP.Materials[p].SubMaterial[g].Textures[t].Offset = reader.ReadUInt32();
                                    this.PCMP.Materials[p].SubMaterial[g].Textures[t].Size = reader.ReadUInt32();
                                    this.PCMP.Materials[p].SubMaterial[g].Textures[t].Type = reader.ReadUInt32();

                                    this.PCMP.Materials[p].SubMaterial[g].Textures[t].Width = reader.ReadUInt16();
                                    this.PCMP.Materials[p].SubMaterial[g].Textures[t].Height = reader.ReadUInt16();

                                    this.PCMP.Materials[p].SubMaterial[g].Textures[t].Unk5 = reader.ReadUInt32();
                                    this.PCMP.Materials[p].SubMaterial[g].Textures[t].Unk6 = reader.ReadUInt32();

                                    this.PCMP.Materials[p].SubMaterial[g].Textures[t].Filename = String.Format("{0:X}", this.DDSOffset + this.PCMP.Materials[p].SubMaterial[g].Textures[t].Offset);
                                }

                                for (int t = 0; t <= this.PCMP.Materials[p].SubMaterial[g].t4Count - 1; t++)
                                {
                                    reader.BaseStream.Seek((int)(this.PCMPOffset + this.PCMP.DDSOffset + this.PCMP.Materials[p].SubMaterial[g].Textures[t].Offset), SeekOrigin.Begin);

                                    byte[] DDSBuffer = new byte[this.PCMP.Materials[p].SubMaterial[g].Textures[t].Size];

                                    DDSBuffer = reader.ReadBytes(DDSBuffer.Length);
                                    FREE_IMAGE_FORMAT format = FREE_IMAGE_FORMAT.FIF_DDS;

                                    MemoryStream stream = new MemoryStream(DDSBuffer);
                                    FIBITMAP bmap = FreeImage.LoadFromStream(stream, ref format);
                                    bmap = FreeImage.ConvertTo24Bits(bmap);

                                    this.PCMP.Materials[p].SubMaterial[g].Textures[t].File = FreeImage.GetBitmap(bmap);
                                }
                            }
                        }

                        // Console.WriteLine("Successfully finished reading PCMP block!");
                    }
                    else
                    {
                        Console.WriteLine("No valid PCMP block was found!");
                    }

                }
                else
                {
                    // Console.WriteLine("ERROR: Version check failed!");
                }

                // Console.WriteLine("Done reading MDPC information.");
            }

            this.Buffer.Dispose();
        }
    }
}