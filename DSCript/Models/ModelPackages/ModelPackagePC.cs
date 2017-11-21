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
using System.Windows.Media.Media3D;

namespace DSCript.Models
{
    public enum MeshType
    {
        // Driv3r (PC), DPL (Xbox/PC)
        Default,
        // Driv3r (Xbox)
        Small
    }
    
    public struct ModelPackageData
    {
        public struct ModelDefinition
        {
            public int UID;
            public int Handle;
            
            public short VertexBufferId;

            public short Unknown1;
            public short Unknown2;

            public int Unknown3;
        }

        public readonly int Version;

        public int UID;

        public int PartsCount;
        public int PartsOffset;

        public int MeshGroupsCount;
        public int MeshGroupsOffset;

        public int MeshesCount;
        public int MeshesOffset;

        public int TextureDataOffset;
        public int MaterialDataOffset;

        public int IndicesCount;
        public int IndicesLength;
        public int IndicesOffset;

        public int VertexDeclsCount;
        public int VertexDeclsOffset;
        
        public MeshType MeshType;

        public void SetMeshType(MeshType type)
        {
            MeshType = type;
        }

        public int PartSize
        {
            get
            {
                switch (Version)
                {
                case 1:
                case 9:
                    return 0x14C;
                case 6:
                    return 0x188;
                }
                return 0;
            }
        }

        public int LODSize
        {
            get
            {
                switch (Version)
                {
                case 1:
                case 9:
                    return 0x18;
                case 6:
                    return 0x20;
                }
                return 0;
            }
        }

        public int MeshGroupSize
        {
            get
            {
                switch (Version)
                {
                case 1:
                case 9:
                    return 0x4E;

                case 6:
                    return 0x58;
                }
                return 0;
            }
        }

        public int MeshSize
        {
            get
            {
                switch (MeshType)
                {
                case MeshType.Default:
                    return 0x38;

                case MeshType.Small:
                    return 0x18;
                }
                return 0;
            }
        }

        public int VertexDeclSize
        {
            get
            {
                switch (Version)
                {
                case 1:
                case 9:
                    return 0x20;

                case 6:
                    return 0x1C;
                }
                return 0;
            }
        }

        public int GetSizeOfParts()
        {
            return PartsCount * PartSize;
        }

        public int GetSizeOfMeshGroups()
        {
            return MeshGroupsCount * MeshGroupSize;
        }

        public int GetSizeOfMeshes()
        {
            return MeshesCount * MeshSize;
        }

        public int GetSizeOfVertexDecls()
        {
            return VertexDeclsCount * VertexDeclSize;
        }

        public int GetVertexBuffersOffset()
        {
            return Memory.Align(IndicesOffset + IndicesLength, 4096);
        }

        public void ReadHeader(Stream stream)
        {
            if (stream.ReadInt32() != Version)
                throw new Exception("Bad version, cannot load ModelPackage!");

            UID = stream.ReadInt32();

            PartsCount = stream.ReadInt32();
            PartsOffset = stream.ReadInt32();

            MeshGroupsCount = stream.ReadInt32();
            MeshGroupsOffset = stream.ReadInt32();

            MeshesCount = stream.ReadInt32();
            MeshesOffset = stream.ReadInt32();

            stream.Position += 0x8;

            TextureDataOffset = stream.ReadInt32();
            MaterialDataOffset = stream.ReadInt32();

            IndicesCount = stream.ReadInt32();
            IndicesLength = stream.ReadInt32();
            IndicesOffset = stream.ReadInt32();

            VertexDeclsCount = stream.ReadInt32();
            VertexDeclsOffset = stream.ReadInt32();
        }

        public void WriteHeader(Stream stream)
        {
            stream.Write(Version);

            stream.Write(UID);

            stream.Write(PartsCount);
            stream.Write(PartsOffset);

            stream.Write(MeshGroupsCount);
            stream.Write(MeshGroupsOffset);

            stream.Write(MeshesCount);
            stream.Write(MeshesOffset);

            if (Version == 6)
            {
                // legacy support (not actually used in-game)
                stream.Write((UID & 0xFFFF) | (0x4B4D << 16));
                stream.Write(0);
            }
            else
            {
                // quick way to write 8-bytes
                stream.Write((long)0);
            }

            stream.Write(TextureDataOffset);
            stream.Write(MaterialDataOffset);

            stream.Write(IndicesCount);
            stream.Write(IndicesLength);
            stream.Write(IndicesOffset);

            stream.Write(VertexDeclsCount);
            stream.Write(VertexDeclsOffset);
        }

        public ModelPackageData(int version)
        {
            Version = version;

            UID = 0;

            PartsCount = 0;
            PartsOffset = 0;

            MeshGroupsCount = 0;
            MeshGroupsOffset = 0;

            MeshesCount = 0;
            MeshesOffset = 0;

            TextureDataOffset = 0;
            MaterialDataOffset = 0;

            IndicesCount = 0;
            IndicesLength = 0;
            IndicesOffset = 0;

            VertexDeclsCount = 0;
            VertexDeclsOffset = 0;

            MeshType = MeshType.Default;
        }

        public ModelPackageData(int version, int uid, int nParts, int nGroups, int nMeshes, int nIndices, int nVertexDecls, MeshType meshType)
            : this(version)
        {
            // we need this to calculate the size of meshes
            MeshType = meshType;

            Version = version;
            UID = uid;

            PartsCount = nParts;
            PartsOffset = Memory.Align(0x44, 128);

            // only calculate offsets if there's model data
            if (PartsCount > 0)
            {
                MeshGroupsCount = nGroups;
                MeshGroupsOffset = Memory.Align(PartsOffset + (PartsCount * PartSize), 128);

                MeshesCount = nMeshes;
                MeshesOffset = MeshGroupsOffset + (MeshGroupsCount * MeshGroupSize);

                IndicesCount = nIndices;
                IndicesLength = IndicesCount * sizeof(short);

                VertexDeclsCount = nVertexDecls;
                VertexDeclsOffset = Memory.Align(MeshesOffset + (MeshesCount * MeshSize), 128);

                IndicesOffset = VertexDeclsOffset + (VertexDeclsCount * VertexDeclSize);
            }
        }

        public ModelPackageData(int version, int uid, int nParts, int nGroups, int nMeshes, int nIndices, int nVertexDecls)
            : this(version, uid, nParts, nGroups, nMeshes, nIndices, nVertexDecls, MeshType.Default)
        {

        }

        public ModelPackageData(int version, int uid)
            : this(version, uid, 0, 0, 0, 0, 0)
        {

        }

        public ModelPackageData(int version, Stream stream) : this(version)
        {
            ReadHeader(stream);
        }
    }

    public enum MaterialPackageType : int
    {
        PC      = 0x504D4350,   // 'PCMP'
        PS2     = 0x32435354,   // 'TSC2'
        Xbox    = 0x504D4258,   // 'XBMP'

        Unknown = -1,
    }

    public struct MaterialPackageHeader
    {
        public MaterialPackageType PackageType;

        public bool UseLargeFormat;

        public int MaterialsCount;
        public int MaterialsOffset;

        public int SubstanceLookupCount;
        public int SubstanceLookupOffset;

        public int SubstancesCount;
        public int SubstancesOffset;

        public int TextureLookupCount;
        public int TextureLookupOffset;
        
        public int TexturesCount;
        public int TexturesOffset;
        
        public int TextureDataOffset;
        public int DataSize;

        public int HeaderSize
        {
            get
            {
                switch (PackageType)
                {
                case MaterialPackageType.PC:
                    return (UseLargeFormat) ? 0x48 : 0x38;
                case MaterialPackageType.Xbox:
                    return 0x48;
                case MaterialPackageType.PS2:
                    return 0x14;
                }
                return 0;
            }
        }

        public int MaterialSize
        {
            get
            {
                switch (PackageType)
                {
                case MaterialPackageType.PC:
                    return (UseLargeFormat) ? 0x10 : 0x18;
                case MaterialPackageType.PS2:
                case MaterialPackageType.Xbox:
                    return 0x10;
                }
                return 0;
            }
        }

        public int SubstanceSize
        {
            get
            {
                switch (PackageType)
                {
                case MaterialPackageType.PC:
                    return (UseLargeFormat) ? 0x1C : 0x20;
                case MaterialPackageType.Xbox:
                    return 0x1C;
                case MaterialPackageType.PS2:
                    return 0xC;
                }
                return 0;
            }
        }

        public int TextureSize
        {
            get
            {
                switch (PackageType)
                {
                case MaterialPackageType.PC:
                case MaterialPackageType.Xbox:
                    return 0x20;
                case MaterialPackageType.PS2:
                    return 0x28;
                }
                return 0;
            }
        }

        public int LookupSize
        {
            get
            {
                switch (PackageType)
                {
                case MaterialPackageType.PC:
                    return (UseLargeFormat) ? 0x4 : 0x8;
                case MaterialPackageType.Xbox:
                case MaterialPackageType.PS2:
                    return 0x4;
                }
                return 0;
            }
        }

        private void GenerateOffsets()
        {
            var alignment = (PackageType == MaterialPackageType.PS2) ? 128 : 4096;

            MaterialsOffset = HeaderSize;

            SubstanceLookupOffset = MaterialsOffset + (MaterialsCount * MaterialSize);
            SubstancesOffset      = SubstanceLookupOffset + (SubstanceLookupCount * LookupSize);

            TextureLookupOffset     = SubstancesOffset + (SubstancesCount * SubstanceSize);
            TexturesOffset          = TextureLookupOffset + (TextureLookupCount * LookupSize);

            TextureDataOffset = Memory.Align(TexturesOffset + (TexturesCount * TextureSize), alignment);
        }
        
        public void Read(Stream stream)
        {
            if (stream.ReadInt32() != (int)PackageType)
                throw new InvalidOperationException("Bad magic - Cannot load material package!");

            if (PackageType == MaterialPackageType.PS2)
            {
                MaterialsCount = stream.ReadInt16();
                SubstanceLookupCount = stream.ReadInt16();
                SubstancesCount = stream.ReadInt16();
                TextureLookupCount = stream.ReadInt16();
                TexturesCount = stream.ReadInt16();

                // skip version
                stream.Position += 2;

                DataSize = stream.ReadInt32();
                
                // offsets need to be generated manually
                GenerateOffsets();
            }
            else
            {
                // skip version
                stream.Position += 4;

                MaterialsCount = stream.ReadInt32();
                MaterialsOffset = stream.ReadInt32();

                SubstanceLookupCount = stream.ReadInt32();
                SubstanceLookupOffset = stream.ReadInt32();

                SubstancesCount = stream.ReadInt32();
                SubstancesOffset = stream.ReadInt32();

                if (PackageType == MaterialPackageType.Xbox)
                {
                    // int PaletteInfoLookupCount;
                    // int PaletteInfoLookupOffset;

                    // int PaletteInfoCount;
                    // int PaletteInfoOffset;

                    throw new NotImplementedException();
                }
                else if (UseLargeFormat)
                {
                    stream.Position += 0x10;
                }

                TextureLookupCount = stream.ReadInt32();
                TextureLookupOffset = stream.ReadInt32();

                TexturesCount = stream.ReadInt32();
                TexturesOffset = stream.ReadInt32();

                TextureDataOffset = stream.ReadInt32();
                DataSize = stream.ReadInt32();
            }
        }

        public void Write(Stream stream)
        {
            stream.Write((int)PackageType);

            switch (PackageType)
            {
            case MaterialPackageType.PC:
                stream.Write(3);
                break;
            case MaterialPackageType.Xbox:
                stream.Write(2);
                break;
            }

            if (PackageType == MaterialPackageType.PS2)
            {
                stream.Write((short)MaterialsCount);
                stream.Write((short)SubstanceLookupCount);
                stream.Write((short)SubstancesCount);
                stream.Write((short)TextureLookupCount);
                stream.Write((short)TexturesCount);
                stream.Write((short)2);
            }
            else
            {
                stream.Write(MaterialsCount);
                stream.Write(MaterialsOffset);
                stream.Write(SubstanceLookupCount);
                stream.Write(SubstanceLookupOffset);
                stream.Write(SubstancesCount);
                stream.Write(SubstancesOffset);
                
                if (UseLargeFormat)
                {
                    switch (PackageType)
                    {
                    case MaterialPackageType.PC:
                        {
                            for (int i = 0; i < 4; i++)
                                stream.Write(0);
                        } break;
                    case MaterialPackageType.Xbox:
                        throw new NotImplementedException();
                    }
                }

                stream.Write(TextureLookupCount);
                stream.Write(TextureLookupOffset);
                stream.Write(TexturesCount);
                stream.Write(TexturesOffset);

                stream.Write(TextureDataOffset);
                stream.Write(DataSize);
            }
        }

        public MaterialPackageHeader(MaterialPackageType packageType) : this(packageType, false) { }
        public MaterialPackageHeader(MaterialPackageType packageType, bool useLargeFormat)
        {
            PackageType = packageType;
            UseLargeFormat = useLargeFormat;

            MaterialsCount = 0;
            MaterialsOffset = 0;

            SubstancesCount = 0;
            SubstancesOffset = 0;

            SubstanceLookupCount = 0;
            SubstanceLookupOffset = 0;

            TexturesCount = 0;
            TexturesOffset = 0;

            TextureLookupCount = 0;
            TextureLookupOffset = 0;

            TextureDataOffset = 0;
            DataSize = 0;
        }

        public MaterialPackageHeader(MaterialPackageType packageType, int nMaterials, int nSubMaterials, int nTextures)
            : this(packageType, nMaterials, nSubMaterials, nTextures, false)
        { }

        public MaterialPackageHeader(MaterialPackageType packageType, int nMaterials, int nSubMaterials, int nTextures, bool useLargeFormat)
            : this(packageType, useLargeFormat)
        {
            if (PackageType == MaterialPackageType.Xbox)
                throw new NotImplementedException();

            MaterialsCount = nMaterials;

            SubstanceLookupCount = nSubMaterials;
            SubstancesCount = nSubMaterials;

            TextureLookupCount = nTextures;
            TexturesCount = nTextures;

            GenerateOffsets();
        }

        public MaterialPackageHeader(MaterialPackageType packageType, Stream stream) : this(packageType, stream, false) { }
        public MaterialPackageHeader(MaterialPackageType packageType, Stream stream, bool useLargeFormat) : this(packageType, useLargeFormat)
        {
            Read(stream);
        }
    }

    public class ModelPackagePC : ModelPackage
    {
        protected ModelPackageData Header;
        protected MaterialPackageHeader MaterialsHeader;

        protected void ReadHeader(Stream stream)
        {
            Header = new ModelPackageData(Spooler.Version, stream);
            UID = Header.UID;
        }

        protected void ReadVertexBuffers(Stream stream)
        {
            var vBuffersCount = Header.VertexDeclsCount;
            var vBuffersOffset = Header.VertexDeclsOffset;

            var declSize = Header.VertexDeclSize;

            VertexBuffers = new List<VertexBuffer>(vBuffersCount);

            /* ------------------------------
             * Read vertex buffer header(s)
             * ------------------------------ */
            for (int vB = 0; vB < vBuffersCount; vB++)
            {
                stream.Position = vBuffersOffset + (vB * declSize);

                var nVerts = stream.ReadInt32();
                var vertsSize = stream.ReadInt32();
                var vertsOffset = stream.ReadInt32();
                var vertLength = stream.ReadInt32();

                if (Header.Version == 1 || Header.Version == 9)
                {
                    var unk1 = stream.ReadInt32();
                    var unk2 = stream.ReadInt32();
                    var unk3 = stream.ReadInt32();
                    var unk4 = stream.ReadInt32();

                    DSC.Log($"vBuffer[{vB}] unknown data: {unk1:X8}, {unk2:X8}, {unk3:X8}, {unk4:X8}");
                }
                
                /* ------------------------------
                 * Read vertices in buffer
                 * ------------------------------ */
                stream.Position = vertsOffset;

                var vbuf = new byte[vertsSize];
                stream.Read(vbuf, 0, vertsSize);

                var vertexBuffer = VertexBuffer.CreateD3Buffer(vbuf, nVerts, vertsSize, vertLength);
                VertexBuffers.Add(vertexBuffer);
            }
        }
    
        protected void ReadIndexBuffer(Stream stream)
        {
            var nIndices = Header.IndicesCount;

            /* ------------------------------
             * Read index buffer
             * ------------------------------ */
            stream.Position = Header.IndicesOffset;

            IndexBuffer = new IndexData(nIndices);

            for (int i = 0; i < nIndices; i++)
                IndexBuffer.Buffer[i] = stream.ReadInt16();
        }
        
        protected void ReadModels(Stream stream)
        {
            var partSize = Header.PartSize;
            var partLodSize = Header.LODSize;
            var groupSize = Header.MeshGroupSize;

            var meshSize = 0; // we don't know yet

            // Driv3r on Xbox has a special mesh type, so we'll figure it out now
            var verifyMeshSize = true;

            /* ------------------------------
             * Read parts groups
             * ------------------------------ */
            for (int p = 0; p < Header.PartsCount; p++)
            {
                stream.Position = Header.PartsOffset + (p * partSize);

                var pGroup = new PartsGroup() {
                    UID = stream.ReadInt32(),
                    Handle = stream.ReadInt32(),

                    Unknown = stream.Read<Vector4>(),

                    // INCOMING TRANSMISSION...
                    // RE: OPERATION S.T.E.R.N....
                    // ...
                    // YOUR ASSISTANCE HAS BEEN NOTED...
                    // ...
                    // <END OF TRANSMISSION>...
                    VertexBuffer = VertexBuffers[stream.ReadInt16()],

                    VertexType = stream.ReadInt16(),
                    
                    Unknown2 = stream.ReadInt32(),
                    Unknown3 = stream.ReadInt32()
                };

                Parts.Add(pGroup);

                if (Header.Version == 6)
                    stream.Position += 4;
                
                // read unknown list of 8 Point4Ds
                for (int t = 0; t < 8; t++)
                {
                    pGroup.Transform[t] = stream.Read<Vector4>();
                }

                var lodStart = stream.Position;
                
                // 7 LODs per part
                for (int k = 0; k < 7; k++)
                {
                    stream.Position = lodStart + (k * partLodSize);

                    var partEntry = new PartDefinition(k) {
                        Parent = pGroup
                    };

                    pGroup.Parts[k] = partEntry;

                    var gOffset = stream.ReadInt32();

                    if (Header.Version == 6)
                        stream.Position += 0x4;

                    var gCount = stream.ReadInt32();

                    stream.Position += 0x8;

                    partEntry.Type = stream.ReadInt32();
                    
                    // the rest is padding, but we calculate the position
                    // at the beginning of each iteration...

                    // nothing to see here, move along
                    if (gCount == 0)
                        continue;

                    /* ------------------------------
                     * Read mesh groups
                     * ------------------------------ */
                    for (int g = 0; g < gCount; g++)
                    {
                        stream.Position = gOffset + (g * groupSize);

                        MeshGroup mGroup = new MeshGroup() {
                            Parent = partEntry
                        };

                        var mOffset = stream.ReadInt32();

                        if (Header.Version == 6)
                            stream.Position += 4;

                        for (int i = 0; i < 3; i++)
                        {
                            mGroup.Transform[i] = stream.Read<Vector4>();
                        }

                        mGroup.Unknown = stream.Read<Vector4>();

                        var mCount = stream.ReadInt16();

                        var unk1 = stream.ReadInt16();

                        if (Header.Version == 6)
                            stream.Position += 4;

                        var unk2 = stream.ReadInt32();

                        //Console.WriteLine($"mGroup[{g}] unknown data: {unk1}, {unk2}");
                        
                        partEntry.Groups.Add(mGroup);
                        MeshGroups.Add(mGroup);

                        /* ------------------------------
                         * Read mesh definitions
                         * ------------------------------ */
                        for (int m = 0; m < mCount; m++)
                        {
                            stream.Position = mOffset + (m * meshSize);

                            var primType = stream.ReadInt32();

                            // we'll only have to do this once
                            if (verifyMeshSize)
                            {
                                // driv3r mesh size hack
                                if (Header.Version == 9 && (primType & 0xFFFFFFF0) != 0)
                                    Header.SetMeshType(MeshType.Small);

                                meshSize = Header.MeshSize;
                                verifyMeshSize = false;
                            }

                            if (Header.MeshType == MeshType.Small)
                                throw new NotImplementedException("Small mesh types not supported!");

                            var mesh = new MeshDefinition(this) {
                                PrimitiveType = (PrimitiveType)primType,
                                BaseVertexIndex = stream.ReadInt32(),
                                MinIndex = stream.ReadUInt32(),
                                NumVertices = stream.ReadUInt32(),
                                StartIndex = stream.ReadUInt32(),
                                PrimitiveCount = stream.ReadUInt32(),

                                MeshGroup = mGroup,
                                PartsGroup = pGroup
                            };

                            if (Header.MeshType == MeshType.Default)
                                stream.Position += 0x18;

                            mesh.MaterialId = stream.ReadInt16();
                            mesh.SourceUID = stream.ReadUInt16();

                            mGroup.Meshes.Add(mesh);
                            Meshes.Add(mesh);
                        }
                    }
                }
            }
        }
        
        protected override void Load()
        {
            if (Spooler.Version != 6)
                throw new Exception("Bad version, cannot load ModelPackage!");

            using (var f = Spooler.GetMemoryStream())
            {
                // header will handle offsets for us
                ReadHeader(f);
                
                // skip packages with no models
                if (Header.PartsCount > 0)
                {
                    Parts           = new List<PartsGroup>(Header.PartsCount);
                    MeshGroups      = new List<MeshGroup>(Header.MeshGroupsCount);
                    Meshes          = new List<MeshDefinition>(Header.MeshesCount);

                    ReadVertexBuffers(f);
                    ReadIndexBuffer(f);

                    ReadModels(f);
                }

                var pcmpOffset = Header.MaterialDataOffset;
                var ddsOffset = Header.TextureDataOffset;

                // Read PCMP
                if (pcmpOffset == 0)
                    return;

                f.Position = pcmpOffset;

                MaterialsHeader = new MaterialPackageHeader(MaterialPackageType.PC, f);
                
                Materials       = new List<MaterialDataPC>(MaterialsHeader.MaterialsCount);
                Substances    = new List<SubstanceDataPC>(MaterialsHeader.SubstancesCount);
                Textures        = new List<TextureDataPC>(MaterialsHeader.TexturesCount);

                var texLookup   = new Dictionary<int, byte[]>();

                // Materials (Size: 0x18)
                for (int m = 0; m < MaterialsHeader.MaterialsCount; m++)
                {
                    f.Position = pcmpOffset + (MaterialsHeader.MaterialsOffset + (m * MaterialsHeader.MaterialSize));

                    // table info
                    var mOffset = f.ReadInt32() + pcmpOffset;
                    var mCount  = f.ReadInt32();

                    var mAnimToggle = (f.ReadInt32() == 1);
                    var mAnimSpeed = f.ReadSingle();
                    
                    var material = new MaterialDataPC() {
                        IsAnimated        = mAnimToggle,
                        AnimationSpeed  = mAnimSpeed
                    };

                    Materials.Add(material);

                    // get substance(s)
                    for (int s = 0; s < mCount; s++)
                    {
                        f.Position  = mOffset + (s * MaterialsHeader.LookupSize);

                        var sOffset = f.ReadInt32() + pcmpOffset;

                        f.Position  = sOffset;

                        var substance = new SubstanceDataPC() {
                            Flags   = f.ReadInt32(),
                            Mode    = f.ReadUInt16(),
                            Type    = f.ReadUInt16()
                        };

                        material.Substances.Add(substance);
                        Substances.Add(substance);

                        f.Position += 0x8;

                        var tOffset = f.ReadInt32() + pcmpOffset;
                        var tCount  = f.ReadInt32();

                        for (int t = 0; t < tCount; t++)
                        {
                            f.Position = tOffset + (t * MaterialsHeader.LookupSize);

                            var texOffset = f.ReadInt32() + pcmpOffset;

                            f.Position = texOffset;

                            var textureInfo = new TextureDataPC();
                            
                            substance.Textures.Add(textureInfo);
                            Textures.Add(textureInfo);
                            
                            textureInfo.Reserved    = f.ReadInt32();
                            textureInfo.CRC32       = f.ReadInt32();

                            var offset          = f.ReadInt32() + ddsOffset;
                            var size            = f.ReadInt32();

                            textureInfo.Type    = f.ReadInt32();

                            textureInfo.Width   = f.ReadInt16();
                            textureInfo.Height  = f.ReadInt16();

                            // I think this is AlphaTest or something
                            textureInfo.Unknown = f.ReadInt32();
                            
                            if (!texLookup.ContainsKey(offset))
                            {
                                f.Position = offset;
                                texLookup.Add(offset, f.ReadBytes(size));
                            }

                            textureInfo.Buffer = texLookup[offset];
                        }
                    }
                }

                // lookup tables no longer needed
                texLookup.Clear();
            }
        }

        protected override void Save()
        {
            var deadMagic   = 0xCDCDCDCD;
            var deadCode    = BitConverter.GetBytes(deadMagic);

            var bufferSize  = 0;

            if (Parts?.Count > 0)
            {
                Header = new ModelPackageData(6, UID, Parts.Count, MeshGroups.Count, Meshes.Count, IndexBuffer.Buffer.Length, VertexBuffers.Count);

                bufferSize = Memory.Align(Header.IndicesOffset + Header.IndicesLength, 4096);
                
                // add up vertex buffer sizes
                foreach (var vBuffer in VertexBuffers)
                    bufferSize += vBuffer.Size;
            }
            else
            {
                // model package has no models
                Header = new ModelPackageData(6, UID);

                bufferSize += Header.PartsOffset;
            }

            bufferSize = Memory.Align(bufferSize, 4096);

            var pcmpOffset = bufferSize;
            var pcmpSize = 0;
            
            MaterialsHeader = new MaterialPackageHeader(MaterialPackageType.PC, Materials.Count, Substances.Count, Textures.Count);

            pcmpSize = MaterialsHeader.TextureDataOffset;
            
            var texOffsets = new Dictionary<int, int>(MaterialsHeader.TexturesCount);

            for (int i = 0; i < MaterialsHeader.TexturesCount; i++)
            {
                var tex = Textures[i];

                if (!texOffsets.ContainsKey(tex.CRC32))
                {
                    pcmpSize = Memory.Align(pcmpSize, 128);

                    texOffsets.Add(tex.CRC32, (pcmpSize - MaterialsHeader.TextureDataOffset));

                    pcmpSize += tex.Buffer.Length;
                }
            }

            MaterialsHeader.DataSize = pcmpSize;

            // add the PCMP size to the buffer size
            bufferSize += Memory.Align(pcmpSize, 4096);

            // Now that we have our initialized buffer size, write ALL the data!
            var buffer = new byte[bufferSize];

            using (var f = new MemoryStream(buffer))
            {
                Header.WriteHeader(f);

                if (Parts?.Count > 0)
                {
                    // Write vertex buffers & declarations
                    var vBufferOffset = Header.GetVertexBuffersOffset();

                    for (int vB = 0; vB < VertexBuffers.Count; vB++)
                    {
                        f.Position = Header.VertexDeclsOffset + (vB * Header.VertexDeclSize);

                        var vBuffer = VertexBuffers[vB];
                        var vBSize = vBuffer.Size;

                        f.Write(vBuffer.Count);
                        f.Write(vBSize);
                        f.Write(vBufferOffset);
                        f.Write(vBuffer.Declaration.SizeOf);
                        
                        f.Position = vBufferOffset;
                        vBuffer.WriteTo(f);

                        vBufferOffset += vBSize;
                    }

                    // Write indices
                    f.Position = Header.IndicesOffset;

                    foreach (var indice in IndexBuffer.Buffer)
                        f.Write(indice);

                    var meshLookup = new int[Header.MeshesCount];
                    var groupLookup = new int[Header.MeshGroupsCount];

                    // Write meshes
                    for (int m = 0; m < Header.MeshesCount; m++)
                    {
                        var mesh = Meshes[m];
                        var mOffset = Header.MeshesOffset + (m * Header.MeshSize);

                        meshLookup[m] = mOffset;

                        f.Position = mOffset;

                        f.Write((int)mesh.PrimitiveType);
                        f.Write(mesh.BaseVertexIndex);
                        f.Write(mesh.MinIndex);
                        f.Write(mesh.NumVertices);

                        f.Write(mesh.StartIndex);
                        f.Write(mesh.PrimitiveCount);
                        
                        f.Position += 0x18;

                        f.Write((short)mesh.MaterialId);
                        f.Write((short)mesh.SourceUID);
                    }

                    var mIdx = 0;

                    // Write groups
                    for (int g = 0; g < Header.MeshGroupsCount; g++)
                    {
                        var group = MeshGroups[g];
                        var gOffset = Header.MeshGroupsOffset + (g * Header.MeshGroupSize);

                        groupLookup[g] = gOffset;

                        f.Position = gOffset;

                        f.Write(meshLookup[mIdx]);

                        f.Position += 0x4;

                        foreach (var transform in group.Transform)
                            f.Write(transform);
                        
                        f.Write(group.Unknown);
                        
                        var mCount = group.Meshes.Count;

                        f.Write(mCount);

                        mIdx += mCount;
                    }

                    var gIdx = 0;

                    // Write parts
                    for (int p = 0; p < Header.PartsCount; p++)
                    {
                        var part = Parts[p];
                        
                        f.Position = Header.PartsOffset + (p * Header.PartSize);
                        
                        f.Write(part.UID);
                        f.Write(part.Handle);

                        f.Write(part.Unknown);
                       
                        var vBufferId = VertexBuffers.IndexOf(part.VertexBuffer);

                        if (vBufferId == -1)
                            throw new Exception("FATAL ERROR: Cannot get Vertex Buffer ID - CANNOT EXPORT MODEL PACKAGE!!!");

                        f.Write((short)vBufferId);

                        f.Write(part.VertexType);

                        f.Write(part.Unknown2);
                        f.Write(part.Unknown3);

                        f.Position += 0x4;

                        foreach (var transform in part.Transform)
                            f.Write(transform);
                        
                        var lodsOffset = f.Position;
                        
                        for (int d = 0; d < part.Parts.Length; d++)
                        {
                            f.Position = lodsOffset + (d * Header.LODSize);

                            var lod = part.Parts[d];

                            if (lod?.Groups?.Count > 0)
                            {
                                var count = lod.Groups.Count;

                                f.Write(groupLookup[gIdx]);

                                f.Position += 0x4;
                                f.Write(count);

                                f.Position += 0x8;
                                f.Write(lod.Type);

                                gIdx += count;
                            }
                        }
                    }
                }
                
                // -- Write PCMP -- \\
                f.Position = pcmpOffset;
                
                MaterialsHeader.Write(f);
                
                var texLookup = new int[MaterialsHeader.TexturesCount];
                var texDataOffset = pcmpOffset + MaterialsHeader.TextureDataOffset;

                // put offset to texture/material data in header (this sucks)
                f.Position = 0x28;

                f.Write(texDataOffset);
                f.Write(pcmpOffset);
                
                // write textures
                for (int t = 0; t < MaterialsHeader.TexturesCount; t++)
                {
                    var tex = Textures[t];
                    var tOffset = MaterialsHeader.TexturesOffset + (t * MaterialsHeader.TextureSize);
                    
                    texLookup[t] = tOffset;

                    f.Position = pcmpOffset + tOffset;

                    var dataOffset = texOffsets[tex.CRC32];

                    f.Write(tex.Reserved);
                    f.Write(tex.CRC32);

                    f.Write(dataOffset);
                    f.Write(tex.Buffer.Length);
                    f.Write(tex.Type);

                    f.Write((short)tex.Width);
                    f.Write((short)tex.Height);

                    f.Write(tex.Unknown);

                    f.Position = texDataOffset + dataOffset;

                    // skip dupes
                    if (f.PeekInt32() == 0)
                        f.Write(tex.Buffer);
                }

                // write texture lookup
                for (int t = 0; t < MaterialsHeader.TextureLookupCount; t++)
                {
                    var tlOffset = MaterialsHeader.TextureLookupOffset + (t * MaterialsHeader.LookupSize);

                    f.Position = pcmpOffset + tlOffset;
                    f.Write(texLookup[t]);

                    // replace offset with the lookup table one
                    texLookup[t] = tlOffset;
                }

                var sLookup = new int[MaterialsHeader.SubstancesCount];

                var tIdx = 0;

                // write substances
                for (int s = 0; s < MaterialsHeader.SubstancesCount; s++)
                {
                    var substance = Substances[s];
                    var sOffset = MaterialsHeader.SubstancesOffset + (s * MaterialsHeader.SubstanceSize);

                    sLookup[s] = sOffset;

                    f.Position = pcmpOffset + sOffset;

                    f.Write(substance.Flags);

                    f.Write((short)substance.Mode);
                    f.Write((short)substance.Type);

                    f.Position += 0x8;
                    
                    f.Write(texLookup[tIdx]);
                    f.Write(substance.Textures.Count);

                    tIdx += substance.Textures.Count;
                }

                // write substance lookup
                for (int s = 0; s < MaterialsHeader.SubstanceLookupCount; s++)
                {
                    var smlOffset = MaterialsHeader.SubstanceLookupOffset + (s * MaterialsHeader.LookupSize);

                    f.Position = pcmpOffset + smlOffset;
                    f.Write(sLookup[s]);

                    // replace offset with the lookup table one
                    sLookup[s] = smlOffset;
                }

                var sIdx = 0;

                // write materials
                for (int m = 0; m < MaterialsHeader.MaterialsCount; m++)
                {
                    var material = Materials[m];

                    f.Position = pcmpOffset + (MaterialsHeader.MaterialsOffset + (m * MaterialsHeader.MaterialSize));
                    
                    f.Write(sLookup[sIdx]);
                    f.Write(material.Substances.Count);

                    f.Write(material.IsAnimated ? 1 : 0);
                    f.Write(material.AnimationSpeed);

                    sIdx += material.Substances.Count;
                }
            }

            Spooler.SetBuffer(buffer);
        }
    }
}
