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

using DSCript.Spooling;

namespace DSCript.Models
{
    public class StandaloneTextureData : SpoolableResource<SpoolablePackage>
    {
        public short UID { get; set; }

        public ModelPackagePC ModelPackage { get; set; }

        public List<PCMPMaterial> StandaloneTextures { get; set; }

        protected override void Load()
        {
            var upst = Spooler.GetFirstChild(ChunkType.StandaloneTextures) as SpoolableBuffer;
            var mdpc = Spooler.GetFirstChild(ChunkType.ModelPackagePC) as SpoolableBuffer;

            if (upst == null || mdpc == null)
                return;

            ModelPackage = SpoolableResourceFactory.Create<ModelPackagePC>(mdpc, true);

            var materials = ModelPackage.Materials;

            using (var f = upst.GetMemoryStream())
            {
                f.Position = 0x10;

                UID = f.ReadInt16();

                var count = f.ReadInt16();

                if (count != materials.Count)
                    throw new Exception("Failed to load StandaloneTextureData - texture count mismatch!");

                StandaloneTextures = new List<PCMPMaterial>(count);

                for (int i = 0; i < count; i++)
                {
                    var matId = f.ReadInt16();

                    StandaloneTextures.Add(materials[matId]);

                    f.Position += 0x2;
                }
            }

            DSC.Log("Successfully loaded StandaloneTextureData!");
        }

        protected override void Save()
        {
            throw new NotImplementedException();
        }
    }

    public class StandaloneTextureFile : FileChunker
    {
        public StandaloneTextureData StandaloneTextureData { get; set; }

        public PCMPMaterial GetStandaloneTexture(int id)
        {
            return (HasTextures) ? StandaloneTextureData.StandaloneTextures[id] : null;
        }

        public ModelPackagePC GetModelPackage()
        {
            return (HasTextures) ? StandaloneTextureData.ModelPackage : null;
        }

        public override bool CanSave
        {
            get { return (HasTextures); }
        }

        public bool HasTextures
        {
            get { return (StandaloneTextureData != null && StandaloneTextureData.StandaloneTextures.Count > 0); }
        }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            if (sender is SpoolablePackage && sender.Magic == 0x0)
                StandaloneTextureData = sender.AsResource<StandaloneTextureData>(true);

            base.OnSpoolerLoaded(sender, e);
        }

        public StandaloneTextureFile() { }
        public StandaloneTextureFile(string filename) : base(filename) { }
    }

    public class Driv3rModelFile : FileChunker
    {
        public List<ModelPackagePC> Models { get; set; }

        public ModelPackagePC GetModelPackage(int uid)
        {
            return (HasModels) ? Models.FirstOrDefault((m) => m.UID == uid) : null;
        }

        public bool HasModels
        {
            get { return (Models != null && Models.Count > 0); }
        }

        public override bool CanSave
        {
            get { return HasModels; }
        }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            if ((ChunkType)sender.Magic == ChunkType.ModelPackagePC)
            {
                var mdpc = SpoolableResourceFactory.Create<ModelPackagePC>(sender);
                mdpc.ModelFile = this;
                
                Models.Add(mdpc);
            }

            base.OnSpoolerLoaded(sender, e);
        }

        protected override void OnFileLoadBegin()
        {
            Models = new List<ModelPackagePC>();

            base.OnFileLoadBegin();
        }

        protected override void OnFileLoadEnd()
        {
            if (Models.Count >= 1)
                DSC.Log("{0} model {1} loaded.", Models.Count, (Models.Count != 1) ? "packages" : "package");

            base.OnFileLoadEnd();
        }

        public Driv3rModelFile() { }
        public Driv3rModelFile(string filename) : base(filename) { }
    }

    public class Driv3rVehiclesFile : Driv3rModelFile
    {
        public StandaloneTextureFile VehicleGlobals { get; set; }
        public List<VehicleHierarchyData> Hierarchies { get; set; }

        public override bool CanSave
        {
            get { return (base.CanSave && (Hierarchies != null && Hierarchies.Count > 0)); }
        }

        public bool HasHierarchies
        {
            get { return (Hierarchies != null && Hierarchies.Count > 0); }
        }

        public bool HasVehicleGlobals
        {
            get { return (VehicleGlobals != null && VehicleGlobals.HasTextures); }
        }

        public bool HasIndividualModels
        {
            get { return (HasModels && HasHierarchies) ? (Models.Count == Hierarchies.Count) : false; }
        }

        /// <summary>
        /// Returns the vehicle container chunk for the specified vehicle id, if applicable.
        /// </summary>
        /// <param name="vehicleId">The vehicle id.</param>
        /// <returns>A vehicle container chunk corresponding to the vehicle id; if nothing is found, null.</returns>
        public SpoolablePackage GetVehicleContainerChunk(int vehicleId)
        {
            // vehicle container chunks are in the root chunk
            // if they're not there, then they don't exist
            var spooler = Content.Children.FirstOrDefault((s) => s.Magic == vehicleId) as SpoolablePackage;
            return spooler;
        }

        /// <summary>
        /// Returns whether or not this is a VVV file.
        /// </summary>
        public bool IsMissionVehicleFile
        {
            get { return (Hierarchies.Count > Models.Count); }
        }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            switch ((ChunkType)sender.Magic)
            {
            case ChunkType.VehicleHierarchy:
                Hierarchies.Add(sender.AsResource<VehicleHierarchyData>(true));
                break;
            }

            base.OnSpoolerLoaded(sender, e);
        }

        protected override void OnFileLoadBegin()
        {
            Hierarchies = new List<VehicleHierarchyData>();

            base.OnFileLoadBegin();
        }

        protected override void OnFileLoadEnd()
        {
            if (Models.Count == Hierarchies.Count)
                DSC.Log("Finished loading a VVS file!");

            base.OnFileLoadEnd();
        }

        public Driv3rVehiclesFile() { }
        public Driv3rVehiclesFile(string filename) : base(filename) { }
    }

    public class VehicleHierarchyData : SpoolableResource<SpoolableBuffer>
    {
        public enum SlotType : short
        {
            Generic                 = 0x00,
            
            Hood                    = 0x04,
            Trunk                   = 0x05,

            DoorFrontLeftEntryNode  = 0x0A,
            DoorFrontRightEntryNode = 0x0B,

            SignalLeft              = 0x0C,
            SignalRight             = 0x0D,

            WheelFrontLeft          = 0x1B,
            WheelFrontRight         = 0x1C,
            WheelRearLeft           = 0x1D,
            WheelRearRight          = 0x1E,

            DoorFrontLeft           = 0x1F,
            DoorFrontRight          = 0x20,

            FenderFrontLeft         = 0x21,
            FenderFrontRight        = 0x22,

            BumperFront             = 0x23,
            BumperRear              = 0x24,

            ChassisFront            = 0x25,
            ChassisMiddle           = 0x26,
            ChassisRear             = 0x27,

            MirrorLeft              = 0x28,
            MirrorRight             = 0x29,

            WheelRearLeftExtra      = 0x2A,
            WheelRearRightExtra     = 0x2B,

            TrailerContainer        = 0x2D,

            BrakelightLeft          = 0x2E,
            BrakelightRight         = 0x2F,

            ReverseLightLeft        = 0x30,
            ReverseLightRight       = 0x31,

            HeadlightLeft           = 0x32,
            HeadlightRight          = 0x33,

            DoorRearLeftExtra       = 0x34,
            DoorRearRightExtra      = 0x35,

            DoorRearLeft            = 0x36,
            DoorRearRight           = 0x37,

            MotorcycleFork          = 0x3A,
            MotorcycleClutch        = 0x3B,
            MotorcycleHandlebars    = 0x3C,

            TrailerDoorLeft         = 0x3E,
            TrailerDoorRight        = 0x3F,

            CargoDoorLeft           = 0x40,
            CargoDoorRight          = 0x41,

            WheelDamaged            = 0x48,
            WheelDamagedExtra       = 0x4A,

            TailLightLeft           = 0x4E,
            TailLightRight          = 0x4F,

            SirenLeft               = 0x50,
            SirenRight              = 0x52,
        }

        public class PartEntry
        {
            public short Type { get; set; }
            public SlotType Slot { get; set; }

            public short Flags1 { get; set; }
            public short Flags2 { get; set; }

            public short Unknown1 { get; set; }
            public short Unknown2 { get; set; }

            public List<PartEntry> Children { get; set; }

            public short Unknown3 { get; set; }

            public short GroupId { get; set; }
            public byte PartId { get; set; }
            public byte Unknown4 { get; set; }
            
            public short PhysicsId { get; set; }
            public Point4D? Position { get; set; }

            public short OffsetId { get; set; }
            public short Unknown6 { get; set; }

            public short AxisId { get; set; }
            public short Unknown7 { get; set; }
        }

        public ModelPackage ModelPackage { get; set; }
        public List<PartEntry> Parts { get; set; }

        public int UID { get; set; }
        public int Reserved { get; set; }

        protected override void Load()
        {
            var awhf = this.Spooler;

            using (var f = awhf.GetMemoryStream())
            {
                f.Position = 0xC;

                if (f.ReadInt32() != 6)
                    throw new Exception("Cannot load hierarchy data - unsupported type");

                var nParts = f.ReadInt32();

                UID = f.ReadInt32();

                var colDataOffset = f.ReadInt32();

                Reserved = f.ReadInt32();

                var t1Count = f.ReadInt16();
                var t2Count = f.ReadInt16();
                var t3Count = f.ReadInt16();
                var t4Count = f.ReadInt16();

                f.Align(16);

                // calculate offsets so we can read data
                var t2Offset    = f.Position + (nParts * 0x20);
                var t3Offset    = t2Offset + (t2Count * 0x20);
                var t1Offset    = t3Offset + (t3Count * 0x50);
                var t4Offset    = t1Offset + (t1Count * 0x10);
                var pdlOffset   = t4Offset + (t4Count * 0x40);

                Parts = new List<PartEntry>(nParts);

                for (int i = 0; i < nParts; i++)
                {
                    var part = new PartEntry() {
                        Type      = f.ReadInt16(),
                        Slot      = (SlotType)f.ReadInt16(),

                        Flags1    = f.ReadInt16(),
                        Flags2    = f.ReadInt16(),

                        Unknown1  = f.ReadInt16(),
                        Unknown2  = f.ReadInt16(),
                    };

                    var nChildren = f.ReadInt16();

                    part.Unknown3 = f.ReadInt16();

                    if (nChildren > 1)
                        part.Children = new List<PartEntry>(nChildren - 1);

                    part.GroupId    = f.ReadInt16();
                    part.PartId     = (byte)f.ReadByte();
                    part.Unknown4   = (byte)f.ReadByte();

                    part.PhysicsId  = f.ReadInt16();
                    
                    var posId = f.ReadInt16();

                    // get position data
                    if (posId != -1)
                    {
                        var holdPos = f.Position;

                        f.Position = t1Offset + (posId * 0x10);

                        part.Position = new Point4D() {
                            X = f.ReadSingle(),
                            Y = f.ReadSingle(),
                            Z = f.ReadSingle(),
                            W = f.ReadSingle()
                        };

                        f.Position = holdPos;
                    }

                    part.OffsetId   = f.ReadInt16();
                    part.Unknown6   = f.ReadInt16();

                    part.AxisId     = f.ReadInt16();
                    part.Unknown7   = f.ReadInt16();

                    Parts.Add(part);
                }

                // add children
                for (int i = 0; i < nParts; i++)
                {
                    var part = Parts[i];

                    if (part.Children != null && part.Children.Capacity > 0)
                    {
                        var count = part.Children.Capacity;

                        for (int ii = 0; ii < count; ii++)
                            part.Children.Add(Parts[i + ii + 1]);
                    }
                }
            }
        }

        protected override void Save()
        {
            throw new NotImplementedException();
        }
    }

    
}
