using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        public sealed class PartThingAttribute : Attribute
        {

        }

        public enum SlotType : short
        {
            Generic                 = 0x00,
            
            Hood                    = 0x04,
            Trunk                   = 0x05,

            ExhaustSmokeEmitter     = 0x06,
            EngineSmokeEmitter      = 0x07,

            CameraTargetExternal    = 0x08,

            DriverSeat              = 0x0A,
            PassengerSeat           = 0x0B,

            SignalLeft              = 0x0C,
            SignalRight             = 0x0D,

            BumperCameraRear        = 0x10,
            CameraPositionExternal  = 0x11,
            BumperCameraFront       = 0x12,

            DashboardCamera         = 0x13,

            WheelCameraLeft         = 0x14,
            WheelCameraRight        = 0x15,

            CameraInteriorUnknown   = 0x16,

            FirstPersonCamera       = 0x17,

            WheelFrontLeft          = 0x1B,
            WheelFrontRight         = 0x1C,
            WheelRearLeft           = 0x1D,
            WheelRearRight          = 0x1E,

            DoorFrontLeft           = 0x1F,
            DoorFrontRight          = 0x20,

            FenderLeft              = 0x21,
            FenderRight             = 0x22,

            BumperFront             = 0x23,
            BumperRear              = 0x24,

            BodyFront               = 0x25,
            BodyMiddle              = 0x26,
            BodyRear                = 0x27,

            MirrorLeft              = 0x28,
            MirrorRight             = 0x29,

            WheelRearLeftExtra      = 0x2A,
            WheelRearRightExtra     = 0x2B,

            TrailerJack             = 0x2D,

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

            Ramp                    = 0x3D,

            TrailerDoorLeft         = 0x3E,
            TrailerDoorRight        = 0x3F,

            CargoDoorLeft           = 0x40,
            CargoDoorRight          = 0x41,

            TrailerUnknown1         = 0x42,
            TrailerUnknown2         = 0x43,
            TrailerUnknown3         = 0x44,

            WheelDamaged            = 0x48,
            WheelDamagedExtra       = 0x4A,

            TailLightLeft           = 0x4E,
            TailLightRight          = 0x4F,

            SirenLeft               = 0x50,
            SirenRight              = 0x52,

            TrainAxleFront          = 0x54,
            TrainAxleRear           = 0x55,

            TrailerContainer        = 0x56,

            ForkliftHoist           = 0x57,
            ForkliftLoader          = 0x59,

            BoatRotorLeft           = 0x5A,
            BoatRotorRight          = 0x5B,

            FrontGrille             = 0x70,
            CornerBumper            = 0x71
        }

        public class PartEntry
        {
            public short Type { get; set; }
            public SlotType SlotType { get; set; }

            public short Flags1 { get; set; }
            public short Flags2 { get; set; }

            public short Unknown1 { get; set; }
            public short Unknown2 { get; set; }

            public List<PartEntry> Children { get; set; }

            public short Unknown3 { get; set; }

            public short Hinge { get; set; }
            public byte PartId { get; set; }
            public byte Unknown4 { get; set; }
            
            [PartThing]
            public PDLEntry Physics { get; set; }

            public Point4D? Position { get; set; }

            [PartThing]
            public Thing2 Offset { get; set; }

            [PartThing]
            public Thing4 Unknown5 { get; set; }

            [PartThing]
            public Thing3 Axis { get; set; }

            public short Unknown6 { get; set; }
        }

        public class Thing2
        {
            public Point4D Position { get; set; }
            public Point4D Rotation { get; set; }
        }

        public class Thing3
        {
            public Point4D Unknown1 { get; set; }
            public Point4D Unknown2 { get; set; }
            public Point4D Unknown3 { get; set; }
            public Point4D Unknown4 { get; set; }
            public Point4D Unknown5 { get; set; }
        }

        public class Thing4
        {
            public Point4D Unknown1 { get; set; }
            public Point4D Unknown2 { get; set; }
            public Point4D Unknown3 { get; set; }
            public Point4D Unknown4 { get; set; }
        }

        public class PDLData
        {
            public Point4D Position { get; set; }

            public Point4D Unknown1 { get; set; }
            public Point4D Unknown2 { get; set; }
            public Point4D Unknown3 { get; set; }
            public Point4D Unknown4 { get; set; }

            public int Unknown5 { get; set; }

            public Point3D Unknown6 { get; set; }
        }

        public class PDLEntry
        {
            public double Unknown { get; set; }

            public List<PDLData> Children { get; set; }
        }

        public ModelPackage ModelPackage { get; set; }

        public List<PartEntry> Parts { get; set; }

        public List<Thing2> T2Entries { get; set; }
        public List<Thing3> T3Entries { get; set; }
        public List<Thing4> T4Entries { get; set; }

        public List<PDLEntry> PDLEntries { get; set; }

        public int UID { get; set; }
        public int Reserved { get; set; }

        protected override void Load()
        {
            var awhf = this.Spooler;

            using (var f = awhf.GetMemoryStream())
            {
                // skip header
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

                var partsOffset = f.Align(16);

                // calculate offsets so we can read data
                var t2Offset    = f.Position + (nParts * 0x20);
                var t3Offset    = t2Offset + (t2Count * 0x20);
                var t1Offset    = t3Offset + (t3Count * 0x50);
                var t4Offset    = t1Offset + (t1Count * 0x10);
                var pdlOffset   = t4Offset + (t4Count * 0x40);

                // read thing2 data
                T2Entries = new List<Thing2>(t2Count);

                for (int i = 0; i < t2Count; i++)
                {
                    f.Position = (i * 0x20) + t2Offset;

                    T2Entries.Add(new Thing2() {
                        Position = new Point4D() {
                            X = f.ReadFloat(),
                            Y = f.ReadFloat(),
                            Z = f.ReadFloat(),
                            W = f.ReadFloat(),
                        },
                        Rotation = new Point4D() {
                            X = f.ReadFloat(),
                            Y = f.ReadFloat(),
                            Z = f.ReadFloat(),
                            W = f.ReadFloat(),
                        }
                    });
                }

                // read thing3 data
                T3Entries = new List<Thing3>(t3Count);

                for (int i = 0; i < t3Count; i++)
                {
                    f.Position = (i * 0x50) + t3Offset;

                    T3Entries.Add(new Thing3() {
                        Unknown1 = new Point4D() {
                            X = f.ReadFloat(),
                            Y = f.ReadFloat(),
                            Z = f.ReadFloat(),
                            W = f.ReadFloat(),
                        },
                        Unknown2 = new Point4D() {
                            X = f.ReadFloat(),
                            Y = f.ReadFloat(),
                            Z = f.ReadFloat(),
                            W = f.ReadFloat(),
                        },
                        Unknown3 = new Point4D() {
                            X = f.ReadFloat(),
                            Y = f.ReadFloat(),
                            Z = f.ReadFloat(),
                            W = f.ReadFloat(),
                        },
                        Unknown4 = new Point4D() {
                            X = f.ReadFloat(),
                            Y = f.ReadFloat(),
                            Z = f.ReadFloat(),
                            W = f.ReadFloat(),
                        },
                        Unknown5 = new Point4D() {
                            X = f.ReadFloat(),
                            Y = f.ReadFloat(),
                            Z = f.ReadFloat(),
                            W = f.ReadFloat(),
                        },
                    });
                }

                // read thing4 data
                T4Entries = new List<Thing4>(t4Count);

                for (int i = 0; i < t3Count; i++)
                {
                    f.Position = (i * 0x40) + t4Offset;

                    T4Entries.Add(new Thing4() {
                        Unknown1 = new Point4D() {
                            X = f.ReadFloat(),
                            Y = f.ReadFloat(),
                            Z = f.ReadFloat(),
                            W = f.ReadFloat(),
                        },
                        Unknown2 = new Point4D() {
                            X = f.ReadFloat(),
                            Y = f.ReadFloat(),
                            Z = f.ReadFloat(),
                            W = f.ReadFloat(),
                        },
                        Unknown3 = new Point4D() {
                            X = f.ReadFloat(),
                            Y = f.ReadFloat(),
                            Z = f.ReadFloat(),
                            W = f.ReadFloat(),
                        },
                        Unknown4 = new Point4D() {
                            X = f.ReadFloat(),
                            Y = f.ReadFloat(),
                            Z = f.ReadFloat(),
                            W = f.ReadFloat(),
                        }
                    });
                }

                // read PDL data
                f.Position = pdlOffset;

                var entriesCount    = f.ReadInt32();
                var pdlDataCount    = f.ReadInt32();

                var entriesOffset   = f.ReadInt32() + pdlOffset;
                var pdlDataOffset   = f.ReadInt32() + pdlOffset;

                var pdlMagic = f.ReadString();

                if (pdlMagic != "PDL001.002.003a")
                    throw new Exception("ERROR - invalid PDL magic!!!");

                PDLEntries = new List<PDLEntry>(entriesCount);

                for (int i = 0; i < entriesCount; i++)
                {
                    var baseOffset = (f.Position = (i * 0x10) + entriesOffset);

                    var count = f.ReadInt32();
                    var offset = f.ReadInt32() + baseOffset;
                    var unknown = f.ReadFloat();

                    var dataEntries = new List<PDLData>(count);

                    if (count < 1)
                        throw new Exception("ERROR - invalid PDL entry!!!");

                    for (int t = 0; t < count; t++)
                    {
                        f.Position = (t * 0x60) + offset;

                        dataEntries.Add(new PDLData() {
                            Position = new Point4D() {
                                X = f.ReadFloat(),
                                Y = f.ReadFloat(),
                                Z = f.ReadFloat(),
                                W = f.ReadFloat(),
                            },
                            Unknown1 = new Point4D() {
                                X = f.ReadFloat(),
                                Y = f.ReadFloat(),
                                Z = f.ReadFloat(),
                                W = f.ReadFloat(),
                            },
                            Unknown2 = new Point4D() {
                                X = f.ReadFloat(),
                                Y = f.ReadFloat(),
                                Z = f.ReadFloat(),
                                W = f.ReadFloat(),
                            },
                            Unknown3 = new Point4D() {
                                X = f.ReadFloat(),
                                Y = f.ReadFloat(),
                                Z = f.ReadFloat(),
                                W = f.ReadFloat(),
                            },
                            Unknown4 = new Point4D() {
                                X = f.ReadFloat(),
                                Y = f.ReadFloat(),
                                Z = f.ReadFloat(),
                                W = f.ReadFloat(),
                            },
                            Unknown5 = f.ReadInt32(),
                            Unknown6 = new Point3D() {
                                X = f.ReadFloat(),
                                Y = f.ReadFloat(),
                                Z = f.ReadFloat()
                            }
                        });
                    }

                    PDLEntries.Add(new PDLEntry() {
                        Children = dataEntries,
                        Unknown = unknown
                    });
                }

                // read parts
                Parts = new List<PartEntry>(nParts);

                for (int i = 0; i < nParts; i++)
                {
                    f.Position = (i * 0x20) + partsOffset;

                    var entry = new {
                        Type        = f.ReadInt16(),
                        SlotType    = (SlotType)f.ReadInt16(),

                        Flags1      = f.ReadInt16(),
                        Flags2      = f.ReadInt16(),

                        Unknown1    = f.ReadInt16(),
                        Unknown2    = f.ReadInt16(),

                        NumChildren = f.ReadInt16(),
                        Unknown3    = f.ReadInt16(),

                        Hinge       = f.ReadInt16(),
                        PartId      = (byte)f.ReadByte(),
                        Unknown4    = (byte)f.ReadByte(),

                        PhysicsId   = f.ReadInt16(),
                        PositionId  = f.ReadInt16(),

                        OffsetId    = f.ReadInt16(),
                        Unknown5    = f.ReadInt16(),

                        AxisId      = f.ReadInt16(),
                        Unknown6    = f.ReadInt16(),
                    };

                    var position = new Point4D();

                    // get position data
                    if (entry.PositionId != -1)
                    {
                        f.Position = (entry.PositionId * 0x10) + t1Offset;

                        position = new Point4D() {
                            X = f.ReadFloat(),
                            Y = f.ReadFloat(),
                            Z = f.ReadFloat(),
                            W = f.ReadFloat(),
                        };
                    }

                    var part = new PartEntry() {
                        Type        = entry.Type,
                        SlotType    = entry.SlotType,

                        Flags1      = entry.Flags1,
                        Flags2      = entry.Flags2,

                        Unknown1    = entry.Unknown1,
                        Unknown2    = entry.Unknown2,

                        Children    = (entry.NumChildren > 1) ? new List<PartEntry>(entry.NumChildren - 1) : null,
                        Unknown3    = entry.Unknown3,

                        Hinge       = entry.Hinge,
                        PartId      = entry.PartId,
                        Unknown4    = entry.Unknown4,

                        Physics     = (entry.PhysicsId >= 0) ? PDLEntries[entry.PhysicsId] : null,
                        Position    = position,

                        Offset      = (entry.OffsetId >= 0) ? T2Entries[entry.OffsetId] : null,
                        Unknown5    = (entry.Unknown5 >= 0) ? T4Entries[entry.Unknown5] : null,

                        Axis        = (entry.AxisId >= 0) ? T3Entries[entry.AxisId] : null,
                        Unknown6    = entry.Unknown6
                    };

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
