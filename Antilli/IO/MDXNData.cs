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

    using DSC.Base;
    using DSC.IO;

    using FreeImageAPI;

    public sealed class MDXNData : BlockData
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct PCMPV2
        {
            [StructLayout(LayoutKind.Sequential)]
            public class DDSTexture
            {
                public uint Offset { get; set; }
                public uint Size { get; set; }

                public FIBITMAP File { get; set; }
            }

            [StructLayout(LayoutKind.Sequential)]
            public class SubMaterial
            {
                public uint Offset { get; set; }

                public ushort UnkFlag1 { get; set; }
                public ushort UnkFlag2 { get; set; }

                public uint UnkLong1 { get; set; }
                public uint UnkLong2 { get; set; }

                public ushort UnkShort1 { get; set; }
                public ushort UnkShort2 { get; set; }

                public uint TextureOffset { get; set; }

                public uint UnkNull1 { get; set; }

                public ushort UnkShort3 { get; set; }
                public ushort UnkShort4 { get; set; }

                public uint UnkNull2 { get; set; }

                public PCMPV2.DDSTexture Texture { get; set; }
            }

            [StructLayout(LayoutKind.Sequential)]
            public class Material
            {
                public uint Offset { get; set; }
                public uint nSubMaterials { get; set; }

                public ushort UnkFlag1 { get; set; }
                public ushort UnkFlag2 { get; set; }

                public uint Unk1 { get; set; }

                public uint SubMaterialOffset { get; set; }
                public uint SubMaterialCount { get; set; }

                public uint UnkNull1 { get; set; }
                public uint UnkNull2 { get; set; }

                public uint UnkFlag3 { get; set; }

                public List<PCMPV2.SubMaterial> SubMaterials = new List<PCMPV2.SubMaterial> { };
            }

            [StructLayout(LayoutKind.Sequential)]
            public class MaterialGroup
            {
                public uint Offset { get; set; }
                public uint Count { get; set; }

                public uint UnkNull1 { get; set; }
                public uint UnkNull2 { get; set; }

                public List<PCMPV2.Material> Materials = new List<PCMPV2.Material> { };
            }

            public const uint Magic = 0x504D4350; // 'PCMP'
            public const uint Version = 0x3;

            public uint nGroups { get; set; }
            public uint GroupsOffset { get; set; }

            public uint nMatEntries { get; set; }
            public uint MatEntriesOffset { get; set; }

            public uint nMaterials { get; set; }
            public uint MaterialsOffset { get; set; }

            public uint nTexEntries { get; set; }
            public uint TexEntriesOffset { get; set; }

            public uint nTextures { get; set; }
            public uint TexturesOffset { get; set; }

            public uint DDSOffset { get; set; }
            public uint DDSSize { get; set; }

            public List<PCMPV2.MaterialGroup> Groups;
            public List<PCMPV2.DDSTexture> Textures;

        }

        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public struct MDXNHeader
        {
            public uint UnkLong1;

            public uint nGroups;
            public uint GroupsOffset;

            public uint nMeshes;
            public uint MeshesOffset;

            public uint nMeshEntries;
            public uint MeshEntriesOffset;
            
            public uint Null1;
            public uint Null2;

            public uint DDSOffset;
            public uint PCMPOffset;

            
            public uint nIndicies;
            public uint IndiciesSize;
            public uint IndiciesOffset;
            public uint UnkFaceType;

            public uint FVFOffset;
        }

        public MDXNHeader MDXN;
        public PCMPV2 PCMP;

        public MDXNData(SubChunkBlock block, byte[] buffer)
        {
            this.CreateBlockData(block, buffer);

            this.InitBuffer();

            using (BinaryReader reader = new BinaryReader(this.Buffer))
            {
                if (reader.ReadUInt32() == 0x1)
                {
                    MDXN.UnkLong1 = reader.ReadUInt32();

                    MDXN.nGroups = reader.ReadUInt32();
                    MDXN.GroupsOffset = reader.ReadUInt32();

                    MDXN.nMeshes = reader.ReadUInt32();
                    MDXN.MeshesOffset = reader.ReadUInt32();

                    MDXN.nMeshEntries = reader.ReadUInt32();
                    MDXN.MeshEntriesOffset = reader.ReadUInt32();

                    MDXN.Null1 = reader.ReadUInt32();
                    MDXN.Null2 = reader.ReadUInt32();

                    MDXN.DDSOffset = reader.ReadUInt32();
                    MDXN.PCMPOffset = reader.ReadUInt32();
                    MDXN.nIndicies = reader.ReadUInt32();
                    MDXN.IndiciesSize = reader.ReadUInt32();
                    MDXN.IndiciesOffset = reader.ReadUInt32();
                    MDXN.UnkFaceType = reader.ReadUInt32();

                    MDXN.FVFOffset = reader.ReadUInt32();

                    /* -----------------------------------
                       Read PCMP Data
                       ----------------------------------- */

                    long BaseAddr = MDXN.PCMPOffset;
                    // Console.WriteLine("Setting BaseAddr to 0x{0:X}", MDXN.PCMPOffset);

                    PCMP.Groups = new List<PCMPV2.MaterialGroup> { };
                    PCMP.Textures = new List<PCMPV2.DDSTexture> { };

                    reader.BaseStream.Seek(BaseAddr, SeekOrigin.Begin);

                    if (reader.ReadUInt32() == PCMPV2.Magic && reader.ReadUInt32() == PCMPV2.Version)
                    {
                        PCMP.nGroups = reader.ReadUInt32();
                        PCMP.GroupsOffset = reader.ReadUInt32();

                        PCMP.nMatEntries = reader.ReadUInt32();
                        PCMP.MatEntriesOffset = reader.ReadUInt32();

                        PCMP.nMaterials = reader.ReadUInt32();
                        PCMP.MaterialsOffset = reader.ReadUInt32();

                        //reserved
                        reader.BaseStream.Seek(0x10, SeekOrigin.Current);

                        PCMP.nTexEntries = reader.ReadUInt32();
                        PCMP.TexEntriesOffset = reader.ReadUInt32();

                        PCMP.nTextures = reader.ReadUInt32();
                        PCMP.TexturesOffset = reader.ReadUInt32();

                        PCMP.DDSOffset = reader.ReadUInt32();
                        // Console.WriteLine("DDSOffset is 0x{0:X}, position was 0x{1:X}", PCMP.DDSOffset, reader.BaseStream.Position - 0x4);

                        /* -----------------------------------
                           Read Groups
                           ----------------------------------- */
                        reader.BaseStream.Seek(BaseAddr + PCMP.GroupsOffset, SeekOrigin.Begin);

                        for (int g = 0; g <= PCMP.nGroups - 1; g++)
                        {
                            PCMP.Groups.Insert(g, new PCMPV2.MaterialGroup());

                            PCMP.Groups[g].Offset = reader.ReadUInt32();
                            PCMP.Groups[g].Count = reader.ReadUInt32();
                            PCMP.Groups[g].UnkNull1 = reader.ReadUInt32();
                            PCMP.Groups[g].UnkNull2 = reader.ReadUInt32();
                        }

                        /* -----------------------------------
                           Read Materials
                           ----------------------------------- */

                        reader.BaseStream.Seek(BaseAddr + PCMP.MatEntriesOffset, SeekOrigin.Begin);

                        for (int g = 0; g <= PCMP.nGroups - 1; g++)
                        {
                            reader.BaseStream.Seek(BaseAddr + PCMP.Groups[g].Offset, SeekOrigin.Begin);

                            long NextGroup = reader.BaseStream.Position;

                            for (int t = 0; t <= PCMP.Groups[g].Count - 1; t++)
                            {
                                PCMP.Groups[g].Materials.Insert(t, new PCMPV2.Material());
                                PCMP.Groups[g].Materials[t].Offset = reader.ReadUInt32();

                                long NextMaterial = reader.BaseStream.Position;

                                reader.BaseStream.Seek(BaseAddr + PCMP.Groups[g].Materials[t].Offset, SeekOrigin.Begin);

                                PCMP.Groups[g].Materials[t].UnkFlag1 = reader.ReadUInt16();
                                PCMP.Groups[g].Materials[t].UnkFlag2 = reader.ReadUInt16();

                                PCMP.Groups[g].Materials[t].Unk1 = reader.ReadUInt32();

                                PCMP.Groups[g].Materials[t].SubMaterialOffset = reader.ReadUInt32();
                                PCMP.Groups[g].Materials[t].SubMaterialCount = reader.ReadUInt32();

                                PCMP.Groups[g].Materials[t].UnkNull1 = reader.ReadUInt32();
                                PCMP.Groups[g].Materials[t].UnkNull2 = reader.ReadUInt32();

                                PCMP.Groups[g].Materials[t].UnkFlag3 = reader.ReadUInt32();

                                reader.BaseStream.Seek(BaseAddr + PCMP.Groups[g].Materials[t].SubMaterialOffset, SeekOrigin.Begin);

                                for (int s = 0; s <= PCMP.Groups[g].Materials[t].SubMaterialCount - 1; s++)
                                {
                                    PCMP.Groups[g].Materials[t].SubMaterials.Insert(s, new PCMPV2.SubMaterial());
                                    PCMP.Groups[g].Materials[t].SubMaterials[s].Offset = reader.ReadUInt32();

                                    long NextSubMaterial = reader.BaseStream.Position;

                                    reader.BaseStream.Seek(BaseAddr + PCMP.Groups[g].Materials[t].SubMaterials[s].Offset, SeekOrigin.Begin);

                                    PCMP.Groups[g].Materials[t].SubMaterials[s].UnkFlag1 = reader.ReadUInt16();
                                    PCMP.Groups[g].Materials[t].SubMaterials[s].UnkFlag2 = reader.ReadUInt16();

                                    PCMP.Groups[g].Materials[t].SubMaterials[s].UnkLong1 = reader.ReadUInt32();
                                    PCMP.Groups[g].Materials[t].SubMaterials[s].UnkLong2 = reader.ReadUInt32();

                                    PCMP.Groups[g].Materials[t].SubMaterials[s].UnkShort1 = reader.ReadUInt16();
                                    PCMP.Groups[g].Materials[t].SubMaterials[s].UnkShort2 = reader.ReadUInt16();

                                    PCMP.Groups[g].Materials[t].SubMaterials[s].TextureOffset = reader.ReadUInt32();

                                    PCMP.Groups[g].Materials[t].SubMaterials[s].UnkNull1 = reader.ReadUInt32();

                                    PCMP.Groups[g].Materials[t].SubMaterials[s].UnkShort3 = reader.ReadUInt16();
                                    PCMP.Groups[g].Materials[t].SubMaterials[s].UnkShort4 = reader.ReadUInt16();

                                    PCMP.Groups[g].Materials[t].SubMaterials[s].UnkNull2 = reader.ReadUInt32();

                                    PCMP.Textures.Add(new PCMPV2.DDSTexture{
                                        Offset = PCMP.Groups[g].Materials[t].SubMaterials[s].TextureOffset
                                    });

                                    reader.BaseStream.Seek(NextSubMaterial, SeekOrigin.Begin);
                                }

                                reader.BaseStream.Seek(NextMaterial, SeekOrigin.Begin);
                            }

                            reader.BaseStream.Seek(NextGroup, SeekOrigin.Begin);
                        }

                        uint ddsBufferSize = 0;

                        for (int d = 0; d <= PCMP.nTextures - 1; d++)
                        {
                            // long lastDDSOffset = BaseAddr + PCMP.DDSOffset + ddsBufferSize;
                            // reader.BaseStream.Seek(lastDDSOffset, SeekOrigin.Begin);

                            reader.BaseStream.Seek(BaseAddr + PCMP.DDSOffset + PCMP.Textures[d].Offset, SeekOrigin.Begin);
                            //Console.WriteLine("Seeking to 0x{0:X}", block.BaseOffset + reader.BaseStream.Position);

                            if (reader.ReadUInt32() == 0x20534444)
                            {
                                ddsBufferSize += 0x4;

                                bool end = false;

                                while (!end)
                                {
                                    uint b = reader.ReadUInt32();

                                    if (b == 0x20534444 || b == 0x3E3E3E3E)
                                    {
                                        end = true;
                                        break;
                                    }
                                    else
                                    {
                                        ddsBufferSize += 0x4;
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception("Invalid DDS texture!");
                            }

                            //Console.WriteLine("Stopped reading @ 0x{0:X}", block.BaseOffset + (uint)(reader.BaseStream.Position - 0x4));

                            uint lastDDSSize = 
                                ((uint)(reader.BaseStream.Position - 0x4)) - ((uint)BaseAddr + PCMP.DDSOffset + PCMP.Textures[d].Offset);

                            //Console.WriteLine("Last DDS texture size: 0x{0:X}", lastDDSSize);

                            PCMP.Textures[d].Size = (uint)lastDDSSize;

                            reader.BaseStream.Seek(BaseAddr + PCMP.DDSOffset + PCMP.Textures[d].Offset, SeekOrigin.Begin);
                            byte[] DDSBuffer = unchecked(reader.ReadBytes((int)PCMP.Textures[d].Size));

                            //Console.WriteLine("Reading a texture from 0x{0:X} of size 0x{1:X}\r\n",
                            //    (uint)block.BaseOffset + BaseAddr + PCMP.DDSOffset + PCMP.Textures[d].Offset,
                            //    (int)DDSBuffer.Length
                            //    );

                            FREE_IMAGE_FORMAT format = FREE_IMAGE_FORMAT.FIF_DDS;

                            using (MemoryStream ddsStream = new MemoryStream(DDSBuffer))
                            {
                                //Console.Write("Loading from stream...");
                                FIBITMAP tex = FreeImage.LoadFromStream(ddsStream, ref format);
                                //Console.Write(
                                //  "OK\n" +
                                //  "Converting to 24-bits...");
                                tex = FreeImage.ConvertTo24Bits(tex);
                                //Console.Write(
                                //  "OK\n" +
                                //  "Inserting into texture slot...");
                                PCMP.Textures[d].File = tex;
                                //Console.Write(
                                //  "OK\n" +
                                //  "Done!\r\n");
                            }

                            // Console.WriteLine("The last texture's size is 0x{0:X}!", (this.Buffer.Length - ddsBufferSize));

                            // PCMP.Textures[d].Size = (uint)reader.BaseStream.Length - PCMP.Textures[d].Offset;
                            
                        }

                        PCMP.DDSSize = (uint)ddsBufferSize;

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

            this.DisposeBuffer();
        }
    }
}