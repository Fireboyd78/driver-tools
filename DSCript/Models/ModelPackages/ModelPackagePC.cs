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

using System.Xml;
using System.Xml.Linq;

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

        public int ModelsCount;
        public int ModelsOffset;

        public int LodInstancesCount;
        public int LodInstancesOffset;

        public int SubModelsCount;
        public int SubModelsOffset;

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

        public int ModelSize
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

        public int LodSize
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

        public int LodInstanceSize
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

        public int SubModelSize
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

        public int GetSizeOfModels()
        {
            return ModelsCount * ModelSize;
        }

        public int GetSizeOfLodInstances()
        {
            return LodInstancesCount * LodInstanceSize;
        }

        public int GetSizeOfSubModels()
        {
            return SubModelsCount * SubModelSize;
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

            ModelsCount = stream.ReadInt32();
            ModelsOffset = stream.ReadInt32();

            LodInstancesCount = stream.ReadInt32();
            LodInstancesOffset = stream.ReadInt32();

            SubModelsCount = stream.ReadInt32();
            SubModelsOffset = stream.ReadInt32();

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
            const int revision = 1;

            stream.Write(Version);

            stream.Write(UID);

            stream.Write(ModelsCount);
            stream.Write(ModelsOffset);

            stream.Write(LodInstancesCount);
            stream.Write(LodInstancesOffset);

            stream.Write(SubModelsCount);
            stream.Write(SubModelsOffset);
            
            stream.Write(((Version == 6) ? UID : -1) & 0xFFFF | (MagicNumber.FB << 16));
            stream.Write(revision | (0x9999 << 16)); // reserve other part
            
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

            ModelsCount = 0;
            ModelsOffset = 0;

            LodInstancesCount = 0;
            LodInstancesOffset = 0;

            SubModelsCount = 0;
            SubModelsOffset = 0;

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

            ModelsCount = nParts;
            ModelsOffset = Memory.Align(0x44, 128);

            // only calculate offsets if there's model data
            if (ModelsCount > 0)
            {
                LodInstancesCount = nGroups;
                LodInstancesOffset = Memory.Align(ModelsOffset + (ModelsCount * ModelSize), 128);

                SubModelsCount = nMeshes;
                SubModelsOffset = LodInstancesOffset + (LodInstancesCount * LodInstanceSize);

                IndicesCount = nIndices;
                IndicesLength = IndicesCount * sizeof(short);

                VertexDeclsCount = nVertexDecls;
                VertexDeclsOffset = Memory.Align(SubModelsOffset + (SubModelsCount * SubModelSize), 128);

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
        public int Version;

        public bool HasPaletteInfo;

        public int MaterialsCount;
        public int MaterialsOffset;

        public int SubstanceLookupCount;
        public int SubstanceLookupOffset;

        public int SubstancesCount;
        public int SubstancesOffset;

        public int PaletteInfoLookupCount;
        public int PaletteInfoLookupOffset;

        public int PaletteInfoCount;
        public int PaletteInfoOffset;

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
                    return (HasPaletteInfo) ? 0x48 : 0x38;
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
                    return (HasPaletteInfo) ? 0x10 : 0x18;
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
                    return (HasPaletteInfo) ? 0x1C : 0x20;
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
                    return (HasPaletteInfo) ? 0x4 : 0x8;
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
            SubstancesOffset = SubstanceLookupOffset + (SubstanceLookupCount * LookupSize);

            TextureLookupOffset = SubstancesOffset + (SubstancesCount * SubstanceSize);
            TexturesOffset = TextureLookupOffset + (TextureLookupCount * LookupSize);

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

                // TODO: process palette information (used on Xbox & possibly DPL PC?)
                if (HasPaletteInfo)
                {
                    PaletteInfoLookupCount = stream.ReadInt32();
                    PaletteInfoLookupOffset = stream.ReadInt32();

                    PaletteInfoCount = stream.ReadInt32();
                    PaletteInfoOffset = stream.ReadInt32();
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
            
            if (PackageType == MaterialPackageType.PS2)
            {
                stream.Write((short)MaterialsCount);
                stream.Write((short)SubstanceLookupCount);
                stream.Write((short)SubstancesCount);
                stream.Write((short)TextureLookupCount);
                stream.Write((short)TexturesCount);
                stream.Write((short)Version);
            }
            else
            {
                stream.Write(Version);

                stream.Write(MaterialsCount);
                stream.Write(MaterialsOffset);
                stream.Write(SubstanceLookupCount);
                stream.Write(SubstanceLookupOffset);
                stream.Write(SubstancesCount);
                stream.Write(SubstancesOffset);
                
                if (HasPaletteInfo)
                {
                    switch (PackageType)
                    {
                    case MaterialPackageType.PC:
                        {
                            // no palette info present
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
        public MaterialPackageHeader(MaterialPackageType packageType, bool hasPaletteInfo)
        {
            PackageType = packageType;
            Version = -1;

            switch (PackageType)
            {
            case MaterialPackageType.PS2:
            case MaterialPackageType.Xbox:
                Version = 2;
                break;
            case MaterialPackageType.PC:
                Version = 3;
                break;
            }
            
            HasPaletteInfo = hasPaletteInfo;

            MaterialsCount = 0;
            MaterialsOffset = 0;

            SubstancesCount = 0;
            SubstancesOffset = 0;

            SubstanceLookupCount = 0;
            SubstanceLookupOffset = 0;

            PaletteInfoLookupCount = 0;
            PaletteInfoLookupOffset = 0;

            PaletteInfoCount = 0;
            PaletteInfoOffset = 0;

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

        public MaterialPackageHeader(MaterialPackageType packageType, int nMaterials, int nSubMaterials, int nTextures, bool hasPaletteInfo)
            : this(packageType, hasPaletteInfo)
        {
            MaterialsCount = nMaterials;

            SubstanceLookupCount = nSubMaterials;
            SubstancesCount = nSubMaterials;

            TextureLookupCount = nTextures;
            TexturesCount = nTextures;

            GenerateOffsets();
        }

        public MaterialPackageHeader(MaterialPackageType packageType, Stream stream) : this(packageType, stream, false) { }
        public MaterialPackageHeader(MaterialPackageType packageType, Stream stream, bool hasPaletteInfo) : this(packageType, hasPaletteInfo)
        {
            Read(stream);
        }
    }

    public class ModelPackage : ModelPackageResource
    {
        protected ModelPackageData Header;
        protected MaterialPackageHeader MaterialsHeader;

        protected virtual void ReadHeader(Stream stream)
        {
            Header = new ModelPackageData(Spooler.Version, stream);

            Version = Header.Version;
            UID = Header.UID;
        }

        protected void ReadVertexBuffers(Stream stream, bool populateBuffers)
        {
            var vBuffersCount = Header.VertexDeclsCount;
            var vBuffersOffset = Header.VertexDeclsOffset;

            var declSize = Header.VertexDeclSize;

            if (populateBuffers)
            {
                if ((VertexBuffers == null) || (VertexBuffers.Count != vBuffersCount))
                    throw new InvalidOperationException("You dun goofed!");
                
                /* ------------------------------
                 * Read vertex buffer header(s)
                 * ------------------------------ */
                for (int vB = 0; vB < vBuffersCount; vB++)
                {
                    stream.Position = vBuffersOffset + (vB * declSize);

                    var vBuffer = VertexBuffers[vB];

                    var nVerts = stream.ReadInt32();
                    var vertsSize = stream.ReadInt32();
                    var vertsOffset = stream.ReadInt32();
                    var vertLength = stream.ReadInt32();

                    if (Header.Version == 1 || Header.Version == 9)
                    {
                        //--var unk1 = stream.ReadInt32();
                        //--var unk2 = stream.ReadInt32();
                        //--var unk3 = stream.ReadInt32();
                        //--var unk4 = stream.ReadInt32();
                        //--
                        //--DSC.Log($"vBuffer[{vB}] unknown data: {unk1:X8}, {unk2:X8}, {unk3:X8}, {unk4:X8}");

                        // (0, 1, 0, 0) ???
                        stream.Position += 0x10;
                    }
                    
                    /* ------------------------------
                     * Read vertices in buffer
                     * ------------------------------ */
                    stream.Position = vertsOffset;

                    var buffer = new byte[vertsSize];
                    stream.Read(buffer, 0, vertsSize);

                    vBuffer.CreateVertices(buffer, nVerts, vertsSize);
                }
            }
            else
            {
                VertexBuffers = (vBuffersCount != 0) ? new List<VertexBuffer>(vBuffersCount) : null;
            }
        }
    
        protected void ReadIndexBuffer(Stream stream)
        {
            var nIndices = Header.IndicesCount;

            if (nIndices != 0)
            {
                stream.Position = Header.IndicesOffset;

                IndexBuffer = new IndexData(nIndices);

                for (int i = 0; i < nIndices; i++)
                    IndexBuffer.Buffer[i] = stream.ReadInt16();
            }
            else
            {
                IndexBuffer = null;
            }
        }
        
        protected void ReadModels(Stream stream)
        {
            var partSize = Header.ModelSize;
            var partLodSize = Header.LodSize;
            var groupSize = Header.LodInstanceSize;

            var meshSize = 0; // we don't know yet

            // Driv3r on Xbox has a special mesh type, so we'll figure it out now
            var verifyMeshSize = true;

            var meshGroupIdx = 0;
            var meshIdx = 0;

            /* ------------------------------
             * Read models
             * ------------------------------ */
            for (int p = 0; p < Header.ModelsCount; p++)
            {
                stream.Position = Header.ModelsOffset + (p * partSize);

                var uid = stream.Read<UID>();

                var unknown = stream.Read<Vector4>();

                // INCOMING TRANSMISSION...
                // RE: OPERATION S.T.E.R.N....
                // ...
                // YOUR ASSISTANCE HAS BEEN NOTED...
                // ...
                // <END OF TRANSMISSION>...
                var vBufIdx = stream.ReadInt16();
                var vBufType = stream.ReadInt16();

                var flags = stream.ReadInt32();

                // reserved space for effect index
                // sadly can't be used to force a specific effect cause game overwrites it :(
                var reserved = stream.ReadInt32();

                var model = new Model() {
                    UID = uid,

                    Scale = unknown,

                    VertexType = vBufType,

                    Flags = flags,
                };

                if (VertexBuffers == null)
                    throw new InvalidOperationException("You dun goofed!");

                VertexBuffer vBuffer = null;

                // initialize first one, second one, etc.
                if (VertexBuffers.Count == vBufIdx)
                {
                    vBuffer = VertexBuffer.Create(Header.Version, vBufType);
                    VertexBuffers.Add(vBuffer);
                }
                else
                {
                    vBuffer = VertexBuffers[vBufIdx];

                    if (!vBuffer.CanUseForType(Header.Version, vBufType))
                        throw new InvalidOperationException("Something has gone HORRIBLY wrong! The fuck did you do?!");
                }

                model.VertexBuffer = vBuffer;
                
                Models.Add(model);

                if (Header.Version == 6)
                    stream.Position += 4;
                
                // culling transforms
                for (int t = 0; t < 8; t++)
                    model.Transform[t] = stream.Read<Vector4>();

                var lodStart = stream.Position;
                
                // 7 LODs per part
                for (int k = 0; k < 7; k++)
                {
                    stream.Position = lodStart + (k * partLodSize);

                    var partEntry = new Lod(k) {
                        Parent = model
                    };

                    model.Lods[k] = partEntry;

                    var gOffset = stream.ReadInt32();

                    if (Header.Version == 6)
                        stream.Position += 0x4;

                    var gCount = stream.ReadInt32();

                    // skip irrelevant data
                    stream.Position += 0x8;
                    
                    partEntry.Type = stream.ReadInt32();
                    
                    // nothing to see here, move along
                    if (gCount == 0)
                        continue;
                    
                    /* ------------------------------
                     * Read lod instances
                     * ------------------------------ */
                    for (int g = 0; g < gCount; g++)
                    {
                        stream.Position = gOffset + (g * groupSize);

                        var curGroupIdx = ((int)stream.Position - Header.LodInstancesOffset) / Header.LodInstanceSize;

                        if (curGroupIdx != meshGroupIdx)
                        {
                            Debug.WriteLine($"WARNING: expected mesh group {meshGroupIdx} / {Header.LodInstancesCount} but got {curGroupIdx}!");
                            meshGroupIdx = curGroupIdx;
                        }
                        
                        var mGroup = new LodInstance() {
                            Parent = partEntry
                        };

                        var mOffset = stream.ReadInt32();

                        if (Header.Version == 6)
                            stream.Position += 4;

                        for (int i = 0; i < 4; i++)
                        {
                            mGroup.Transform[i] = stream.Read<Vector4>();
                        }
                        
                        var mCount = stream.ReadInt16();

                        mGroup.UseTransform = (stream.ReadInt16() != 0);

                        if (Header.Version == 6)
                            stream.Position += 4;

                        mGroup.Reserved = stream.ReadInt32();
                        
                        partEntry.Instances.Add(mGroup);
                        LodInstances.Add(mGroup);

                        /* ------------------------------
                         * Read sub models
                         * ------------------------------ */
                        for (int m = 0; m < mCount; m++)
                        {
                            stream.Position = mOffset + (m * meshSize);

                            var curMeshIdx = ((int)stream.Position - Header.SubModelsOffset) / Header.SubModelSize;

                            if (curMeshIdx != meshIdx)
                            {
                                Debug.WriteLine($"WARNING: expected mesh {meshIdx} / {Header.SubModelsCount} but got {curMeshIdx}!");
                                meshIdx = curMeshIdx;
                            }

                            var primType = stream.ReadInt32();

                            // we'll only have to do this once
                            if (verifyMeshSize)
                            {
                                // driv3r mesh size hack
                                if (Header.Version == 9 && (primType & 0xFFFFFFF0) != 0)
                                    Header.SetMeshType(MeshType.Small);

                                meshSize = Header.SubModelSize;
                                verifyMeshSize = false;
                            }

                            if (Header.MeshType == MeshType.Small)
                                throw new NotImplementedException("Small mesh types not supported!");

                            var mesh = new SubModel(this) {
                                PrimitiveType = (PrimitiveType)primType,
                                VertexBaseOffset = stream.ReadInt32(),
                                VertexOffset = stream.ReadInt32(),
                                VertexCount = stream.ReadInt32(),
                                IndexOffset = stream.ReadInt32(),
                                IndexCount = stream.ReadInt32(),

                                LodInstance = mGroup,
                                Model = model
                            };

                            if (Header.MeshType == MeshType.Default)
                                stream.Position += 0x18;

                            mesh.MaterialId = stream.ReadInt16();
                            mesh.SourceUID = stream.ReadUInt16();

                            mGroup.SubModels.Add(mesh);
                            SubModels.Add(mesh);

                            ++meshIdx;
                        }

                        ++meshGroupIdx;
                    }
                }
            }
        }

        protected virtual void ReadMaterials(Stream stream)
        {
            var matDataOffset = Header.MaterialDataOffset;
            var texDataOffset = Header.TextureDataOffset;
            
            if (matDataOffset != 0)
            {
                stream.Position = matDataOffset;
                
                MaterialsHeader = new MaterialPackageHeader(MaterialPackageType, stream, (Version != 6));

                if (MaterialsHeader.DataSize == 0)
                {
                    texDataOffset = matDataOffset + MaterialsHeader.TextureDataOffset;
                    MaterialsHeader.DataSize = (Spooler.Size - matDataOffset);
                }

                Materials = new List<MaterialDataPC>(MaterialsHeader.MaterialsCount);
                Substances = new List<SubstanceDataPC>(MaterialsHeader.SubstancesCount);
                Textures = new List<TextureDataPC>(MaterialsHeader.TexturesCount);

                var texLookup = new Dictionary<int, byte[]>();
                
                // Materials (Size: 0x18)
                for (int m = 0; m < MaterialsHeader.MaterialsCount; m++)
                {
                    stream.Position = matDataOffset + (MaterialsHeader.MaterialsOffset + (m * MaterialsHeader.MaterialSize));

                    // table info
                    var mOffset = stream.ReadInt32() + matDataOffset;
                    var mCount = stream.ReadInt32();

                    var mAnimType = stream.ReadInt32();
                    var mAnimSpeed = stream.ReadSingle();

                    var material = new MaterialDataPC() {
                        Type = (MaterialType)mAnimType,
                        AnimationSpeed = mAnimSpeed,
                    };

                    Materials.Add(material);

                    // get substance(s)
                    for (int s = 0; s < mCount; s++)
                    {
                        stream.Position = mOffset + (s * MaterialsHeader.LookupSize);

                        var sOffset = stream.ReadInt32() + matDataOffset;

                        stream.Position = sOffset;

                        if (MaterialsHeader.HasPaletteInfo)
                        {
                            var sInf = stream.ReadInt32();
                            var sFlg = stream.ReadInt32();

                            //--internal stuff from DPL, might help in the future (bits are wrong)
                            //--var bin = (sFlg >> 21) & 0x7F;
                            //--var effect = (sFlg >> 14) & 0x7F;
                            //--var subcontainer = (sInf >> 15) & 0x3FFF;
                            //--var subid = (sInf >> 1) & 0x3FFF;
                            //--var buf = (sInf >> 29) & 0xF;
                            //--
                            //--
                            //--Debug.WriteLine("sortkey = BIN: {0}, EFFECT {1}, SUB CONTAINER: {2}, SUB ID: {3}, BUFFER: {3}",
                            //--    bin, effect, subcontainer, subid, buf);

                            var sMode = (sInf & 0xFFFF);
                            var sType = (sInf >> 16) & 0xFFFF;

                            var substance = new SubstanceDataPC() {
                                Bin = (sFlg & 0xFF),
                                Flags = (sFlg >> 8),
                                Mode = sMode,
                                Type = sType,
                            };

                            material.Substances.Add(substance);
                            Substances.Add(substance);

                            var tOffset = stream.ReadInt32() + matDataOffset;
                            var tCount = stream.ReadInt32();
                            
                            // TODO: handle palette info
                            var pOffset = stream.ReadInt32();
                            var pCount = stream.ReadInt32();
                            
                            var reserved = stream.ReadInt32();

                            // TODO: figure out why DPL does this
                            var check = (reserved & 0x8000000) != 0;

                            for (int t = 0; t < tCount; t++)
                            {
                                stream.Position = tOffset + (t * MaterialsHeader.LookupSize);

                                var texOffset = stream.ReadInt32() + matDataOffset;

                                stream.Position = texOffset;
                                
                                var uid = stream.ReadInt32();
                                var crc = stream.ReadInt32();

                                // possibly a header?
                                var unk1 = stream.ReadInt32(); // 0xF00?
                                var unk2 = stream.ReadInt16(); // 1?
                                var unk3 = stream.ReadInt16(); // 4?

                                if ((unk1 != 0xF00) || ((unk2 != 1) || (unk3 != 4)))
                                    throw new InvalidDataException("Oops!");

                                var offset = stream.ReadInt32() + texDataOffset;
                                var size = stream.ReadInt32();
                                
                                var format = stream.ReadInt32();

                                // packed data -- very clever!
                                var width = (format >> 24) & 0xF;
                                var height = (format >> 20) & 0xF;

                                // not 100% sure on this one
                                var type = (format >> 16) & 0xF;

                                // TODO: figure this stuff out
                                var flags = (format & 0xFFFF);
                                
                                var tex = new TextureDataPC() {
                                    UID = uid,
                                    Hash = crc,
                                    
                                    Type = type,

                                    Width = (1 << width),
                                    Height = (1 << height),

                                    Flags = flags,
                                };

                                substance.Textures.Add(tex);
                                Textures.Add(tex);

                                stream.Position = offset;
                                
                                // thanks, reflections!
                                if (size == 0)
                                {
                                    var header = default(DDSHeader);

                                    if (!DDSUtils.GetHeaderInfo(stream, ref header))
                                        throw new InvalidDataException("Can't determine data size of texture!");

                                    size = (header.PitchOrLinearSize + header.Size + 4);

                                    if (header.MipMapCount > 0)
                                    {
                                        // why you gotta do me dirty, reflections?!
                                        stream.Position += size;

                                        while (stream.ReadInt32() != 0x20534444)
                                        {
                                            if ((offset + size) >= MaterialsHeader.DataSize)
                                                break;

                                            size += 4;
                                        }
                                    }
                                }

                                if (!texLookup.ContainsKey(offset))
                                {
                                    stream.Position = offset;
                                    texLookup.Add(offset, stream.ReadBytes(size));
                                }

                                tex.Buffer = texLookup[offset];
                            }
                        }
                        else
                        {
                            var sFlg = stream.ReadInt32();
                            var sMode = stream.ReadUInt16();
                            var sType = stream.ReadUInt16();

                            var substance = new SubstanceDataPC() {
                                Bin = (sFlg & 0xFF),
                                Flags = (sFlg >> 8),
                                Mode = sMode,
                                Type = sType,
                            };

                            material.Substances.Add(substance);
                            Substances.Add(substance);

                            stream.Position += 0x8;

                            var tOffset = stream.ReadInt32() + matDataOffset;
                            var tCount = stream.ReadInt32();

                            for (int t = 0; t < tCount; t++)
                            {
                                stream.Position = tOffset + (t * MaterialsHeader.LookupSize);

                                var texOffset = stream.ReadInt32() + matDataOffset;

                                stream.Position = texOffset;

                                var textureInfo = new TextureDataPC();

                                substance.Textures.Add(textureInfo);
                                Textures.Add(textureInfo);

                                textureInfo.UID = stream.ReadInt32();
                                textureInfo.Hash = stream.ReadInt32();

                                var offset = stream.ReadInt32() + texDataOffset;
                                var size = stream.ReadInt32();

                                textureInfo.Type = stream.ReadInt32();

                                textureInfo.Width = stream.ReadInt16();
                                textureInfo.Height = stream.ReadInt16();

                                // I think this is AlphaTest or something
                                textureInfo.Flags = stream.ReadInt32();

                                if (!texLookup.ContainsKey(offset))
                                {
                                    stream.Position = offset;
                                    texLookup.Add(offset, stream.ReadBytes(size));
                                }

                                textureInfo.Buffer = texLookup[offset];
                            }
                        }
                    }
                }

                // lookup tables no longer needed
                texLookup.Clear();
            }
        }
        
        protected override void Load()
        {
            switch (Spooler.Context)
            {
            case MGX_ModelPackagePC:
            case MGX_ModelPackageXN:
                Platform = PlatformType.PC;
                break;
            case MGX_ModelPackagePS2:
                Platform = PlatformType.PS2;
                break;
            case MGX_ModelPackageXBox:
                Platform = PlatformType.Xbox;
                break;
            }

            if (Platform != PlatformType.PC)
                throw new Exception("Unsupported model package type.");

            using (var f = Spooler.GetMemoryStream())
            {
                // header will handle offsets for us
                ReadHeader(f);
                
                // skip packages with no models
                if (Header.ModelsCount > 0)
                {
                    Models           = new List<Model>(Header.ModelsCount);
                    LodInstances      = new List<LodInstance>(Header.LodInstancesCount);
                    SubModels          = new List<SubModel>(Header.SubModelsCount);

                    ReadVertexBuffers(f, false);
                    ReadIndexBuffer(f);

                    ReadModels(f);
                    ReadVertexBuffers(f, true);
                }

                ReadMaterials(f);
            }
        }

        protected override void Save()
        {
            var deadMagic   = 0xCDCDCDCD;
            var deadCode    = BitConverter.GetBytes(deadMagic);

            if (Version != 6)
                throw new NotImplementedException("Saving not implemented for model package format!");

            var bufferSize  = 0;

            if (Models?.Count > 0)
            {
                Header = new ModelPackageData(Version, UID, Models.Count, LodInstances.Count, SubModels.Count, IndexBuffer.Buffer.Length, VertexBuffers.Count);

                bufferSize = Memory.Align(Header.IndicesOffset + Header.IndicesLength, 4096);
                
                // add up vertex buffer sizes
                foreach (var vBuffer in VertexBuffers)
                    bufferSize += vBuffer.Size;
            }
            else
            {
                // model package has no models
                Header = new ModelPackageData(Version, UID);

                bufferSize += Header.ModelsOffset;
            }

            bufferSize = Memory.Align(bufferSize, 4096);

            var pcmpOffset = bufferSize;
            var pcmpSize = 0;
            
            MaterialsHeader = new MaterialPackageHeader(MaterialPackageType, Materials.Count, Substances.Count, Textures.Count);

            pcmpSize = MaterialsHeader.TextureDataOffset;
            
            var texOffsets = new Dictionary<int, int>(MaterialsHeader.TexturesCount);
            var texOffsetList = new List<int>();

            for (int i = 0; i < MaterialsHeader.TexturesCount; i++)
            {
                var tex = Textures[i];

                var hash = (int)Memory.GetCRC32(tex.Buffer);

                if (!texOffsets.ContainsKey(hash))
                {
                    pcmpSize = Memory.Align(pcmpSize, 128);

                    texOffsets.Add(hash, (pcmpSize - MaterialsHeader.TextureDataOffset));

                    pcmpSize += tex.Buffer.Length;
                }

                texOffsetList.Add(texOffsets[hash]);
            }

            MaterialsHeader.DataSize = pcmpSize;

            // add the PCMP size to the buffer size
            bufferSize += Memory.Align(pcmpSize, 4096);

            // Now that we have our initialized buffer size, write ALL the data!
            var buffer = new byte[bufferSize];
            
            using (var f = new MemoryStream(buffer))
            {
                const short revHeader = 0x78FB;

                var writeRevision = new Action<int>((revision) => {
                    f.Write(revHeader | (revision << 16));
                });

                Header.WriteHeader(f);

                if (Models?.Count > 0)
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

                    var meshLookup = new int[Header.SubModelsCount];
                    var groupLookup = new int[Header.LodInstancesCount];

                    // Write meshes
                    for (int m = 0; m < Header.SubModelsCount; m++)
                    {
                        var mesh = SubModels[m];
                        var mOffset = Header.SubModelsOffset + (m * Header.SubModelSize);

                        meshLookup[m] = mOffset;

                        f.Position = mOffset;

                        f.Write((int)mesh.PrimitiveType);
                        f.Write(mesh.VertexBaseOffset);
                        f.Write(mesh.VertexOffset);
                        f.Write(mesh.VertexCount);

                        f.Write(mesh.IndexOffset);
                        f.Write(mesh.IndexCount);
                        
                        f.Position += 0x18;

                        f.Write((short)mesh.MaterialId);
                        f.Write((short)mesh.SourceUID);
                    }

                    var mIdx = 0;

                    // Write groups
                    for (int g = 0; g < Header.LodInstancesCount; g++)
                    {
                        var group = LodInstances[g];
                        var gOffset = Header.LodInstancesOffset + (g * Header.LodInstanceSize);

                        groupLookup[g] = gOffset;

                        f.Position = gOffset;

                        f.Write(meshLookup[mIdx]);

                        f.Position += 0x4;

                        foreach (var transform in group.Transform)
                            f.Write(transform);
                        
                        var mCount = (short)group.SubModels.Count;
                        var useTransform = (short)(group.UseTransform ? 1 : 0);

                        f.Write(mCount);
                        f.Write(useTransform);
                        
                        f.Write((int)MagicNumber.FIREBIRD); // ;)
                        f.Write(group.Reserved);

                        writeRevision(2);

                        mIdx += mCount;
                    }

                    var gIdx = 0;

                    // Write parts
                    for (int p = 0; p < Header.ModelsCount; p++)
                    {
                        var part = Models[p];
                        
                        f.Position = Header.ModelsOffset + (p * Header.ModelSize);

                        f.Write(part.UID);
                        f.Write(part.Scale);
                       
                        var vBufferId = VertexBuffers.IndexOf(part.VertexBuffer);

                        if (vBufferId == -1)
                            throw new Exception("FATAL ERROR: Cannot get Vertex Buffer ID - CANNOT EXPORT MODEL PACKAGE!!!");

                        f.Write((short)vBufferId);

                        f.Write(part.VertexType);

                        f.Write(part.Flags);

                        f.Position += 0x4;

                        f.Write((int)MagicNumber.FIREBIRD); // ;)

                        foreach (var transform in part.Transform)
                            f.Write(transform);
                        
                        var lodsOffset = f.Position;
                        
                        for (int d = 0; d < part.Lods.Length; d++)
                        {
                            f.Position = lodsOffset + (d * Header.LodSize);

                            var lod = part.Lods[d];

                            if (lod?.Instances?.Count > 0)
                            {
                                var count = lod.Instances.Count;

                                f.Write(groupLookup[gIdx]);

                                f.Position += 0x4;
                                f.Write(count);

                                f.Write((int)MagicNumber.FIREBIRD); // ;)

                                writeRevision(3);

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

                // put offset to texture/material data in header
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

                    var dataOffset = texOffsetList[t];

                    f.Write(tex.UID);
                    f.Write(tex.Hash);

                    f.Write(dataOffset);
                    f.Write(tex.Buffer.Length);
                    f.Write(tex.Type);

                    f.Write((short)tex.Width);
                    f.Write((short)tex.Height);

                    f.Write(tex.Flags);

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

                    f.Write((substance.Flags << 8) | substance.Bin);

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

                    f.Write((int)material.Type);
                    f.Write(material.AnimationSpeed);

                    sIdx += material.Substances.Count;
                }
            }

            Spooler.SetBuffer(buffer);
        }

        public void LoadMaterials(XmlElement elem)
        {
            throw new NotImplementedException();
        }

        public void SaveMaterials(XmlElement parent)
        {
            var xmlDoc = parent.OwnerDocument;
            
            foreach (var material in Materials)
            {
                var mat = xmlDoc.CreateElement("Material");

                if (material.Type == MaterialType.Animated)
                    mat.SetAttribute("AnimationSpeed", material.AnimationSpeed.ToString());
                
                foreach (var substance in material.Substances)
                {
                    var sub = xmlDoc.CreateElement("Substance");

                    sub.SetAttribute("Flags", substance.Flags.ToString("X8"));

                    sub.SetAttribute("Mode", substance.Mode.ToString("X4"));
                    sub.SetAttribute("Type", substance.Type.ToString("X4"));
                    
                    foreach (var texture in substance.Textures)
                    {
                        var tex = xmlDoc.CreateElement("Texture");

                        tex.SetAttribute("UID", texture.UID.ToString("X8"));
                        tex.SetAttribute("Hash", texture.Hash.ToString("X8"));
                        tex.SetAttribute("Type", texture.Type.ToString());
                        tex.SetAttribute("Width", texture.Width.ToString());
                        tex.SetAttribute("Height", texture.Height.ToString());
                        tex.SetAttribute("Flags", texture.Flags.ToString());

                        sub.AppendChild(tex);
                    }

                    mat.AppendChild(sub);
                }

                parent.AppendChild(mat);
            }
        }
    }
}
