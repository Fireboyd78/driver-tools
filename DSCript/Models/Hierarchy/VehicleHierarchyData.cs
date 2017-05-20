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
using System.Text;

using System.Xml;
using System.Xml.Serialization;

using DSCript.Spooling;

namespace DSCript.Models
{
    public class VehicleHierarchyData : SpoolableResource<SpoolableBuffer>
    {
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

            public short Unknown3 { get; set; }

            public short Hinge { get; set; }
            public byte PartId { get; set; }
            public byte Unknown4 { get; set; }
            
            public PDLEntry Physics { get; set; }
            public Vector4? Position { get; set; }

            public Thing2 Offset { get; set; }
            public Thing3 Transform { get; set; }

            public Thing4 Axis { get; set; }

            public short Unknown6 { get; set; }

            public List<PartEntry> Children { get; set; }
        }

        public class Thing2
        {
            public Vector4 Position { get; set; }
            public Vector4 Rotation { get; set; }
        }

        public class Thing3
        {
            public Vector4 RotationX { get; set; }
            public Vector4 RotationY { get; set; }
            public Vector4 RotationZ { get; set; }
            public Vector4 Unknown4 { get; set; }
            public Vector4 Unknown5 { get; set; }
        }

        public class Thing4
        {
            public Vector4 Unknown1 { get; set; }
            public Vector4 Unknown2 { get; set; }
            public Vector4 Unknown3 { get; set; }
            public Vector4 Unknown4 { get; set; }
        }

        public class PDLData
        {
            public Vector4 Position { get; set; }

            public Vector4 Unknown1 { get; set; }
            public Vector4 Unknown2 { get; set; }
            public Vector4 Unknown3 { get; set; }
            public Vector4 Unknown4 { get; set; }

            public int Unknown5 { get; set; }

            public Vector3 Unknown6 { get; set; }
        }

        public class PDLEntry
        {
            public double Unknown { get; set; }
            public int Reserved { get; set; }

            public List<PDLData> Children { get; set; }
        }

        public enum BulletMaterialType : int
        {
            Metal = 0,
            Glass = 1,
        }
        
        public struct BulletData
        {
            private const int PACKED_SIZE = 0x14;
            private const int UNPACKED_SIZE = 0x38; // VPK format

            // number of component bits
            private const int POS_NBITS = 32;

            private const int POS_NBITS_T = 10;
            private const int POS_NBITS_H = 12;

            // max component values
            private const float POS_MAX_T = 3.0f;
            private const float POS_MAX_H = 15.0f;

            // component bit-shifts
            private const int POS_BITS_T = POS_NBITS - POS_NBITS_T;
            private const int POS_BITS_H = POS_NBITS - POS_NBITS_H;

            private const int POS_BITS_TX = POS_NBITS - (POS_NBITS_T * 1);
            private const int POS_BITS_TY = POS_NBITS - (POS_NBITS_T * 2);

            // packing constants
            private const float POS_PACKING_T = POS_MAX_T / (1 << (POS_NBITS_T - 1));
            private const float POS_PACKING_H = POS_MAX_H / (1 << (POS_NBITS_H - 1));

            private const float ROT_PACKING = 0.0039215689f; // [+/-](0.0 - 1.0)
            private const float WGT_PACKING = 0.000015259022f; // [+/-](0.0 - 1.0)
            
            public Vector3 Position1 { get; set; }
            public Vector3 Position2 { get; set; }
            
            public Vector3 Rotation1 { get; set; }
            public Vector3 Rotation2 { get; set; }

            public BulletMaterialType MaterialType { get; set; }

            public float Weight { get; set; }
            
            private static float Normalize(float value, float max)
            {
                if (value > max)
                    return max;
                if (value < -max)
                    return -max;

                return value;
            }
            
            private static int PackPos(Vector3 value)
            {
                var pos = 0;

                pos |= (int)(Normalize(value.X, POS_MAX_T) / POS_PACKING_T) << (POS_NBITS_T * 0);
                pos |= (int)(Normalize(value.Y, POS_MAX_T) / POS_PACKING_T) << (POS_NBITS_T * 1);
                pos |= (int)(Normalize(value.Z, POS_MAX_H) / POS_PACKING_H) << (POS_NBITS - POS_NBITS_H);

                return pos;
            }
            
            private static int PackRot(Vector3 value)
            {
                var rot = 0;

                rot |= (byte)(((Normalize(value.X, 1.0f) / 2) + 0.5f) / ROT_PACKING) << 0;
                rot |= (byte)(((Normalize(value.Y, 1.0f) / 2) + 0.5f) / ROT_PACKING) << 8;
                rot |= (byte)(((Normalize(value.Z, 1.0f) / 2) + 0.5f) / ROT_PACKING) << 16;

                return rot;
            }

            private static short PackWeight(float value)
            {
                return (short)(Normalize(value, 1.0f) / WGT_PACKING);
            }
            
            private static Vector3 UnpackPos(byte[] bytes, int offset)
            {
                var pos = BitConverter.ToInt32(bytes, offset);

                return new Vector3() {
                    X = ((pos << POS_BITS_TX) >> POS_BITS_T) * POS_PACKING_T,
                    Y = ((pos << POS_BITS_TY) >> POS_BITS_T) * POS_PACKING_T,
                    Z = (pos >> POS_BITS_H) * POS_PACKING_H,
                };
            }

            private static Vector3 UnpackRot(byte[] bytes, int offset)
            {
                return new Vector3() {
                    X = ((bytes[offset + 0] * ROT_PACKING) - 0.5f) * 2,
                    Y = ((bytes[offset + 1] * ROT_PACKING) - 0.5f) * 2,
                    Z = ((bytes[offset + 2] * ROT_PACKING) - 0.5f) * 2,
                };
            }

            private static float UnpackWeight(byte[] buffer, int offset)
            {
                return (BitConverter.ToInt16(buffer, offset) * WGT_PACKING);
            }

            public byte[] ToBinary(bool packed = false)
            {
                var buffer = new byte[(packed) ? PACKED_SIZE : UNPACKED_SIZE];

                using (var ms = new MemoryStream(buffer))
                {
                    if (packed)
                    {
                        ms.Write(PackPos(Position1));
                        ms.Write(PackPos(Position2));

                        ms.Write(PackRot(Rotation1));
                        ms.Write(PackRot(Rotation2));

                        ms.Write(PackWeight(Weight));

                        ms.Write((ushort)0xDF83); // ;)

                        // set the material type
                        ms.Position = 11;

                        ms.WriteByte((byte)MaterialType);
                    }
                    else
                    {
                        ms.Write(Position1);
                        ms.Write(Position2);

                        ms.Write(Rotation1);
                        ms.Write(Rotation2);

                        ms.Write(Weight);

                        ms.Write((ushort)MaterialType);
                        ms.Write((ushort)0xDF83); // ;)
                    }
                }
                
                return buffer;
            }

            public static BulletData Unpack(Stream stream)
            {
                var buffer = stream.ReadBytes(PACKED_SIZE);

                return Unpack(buffer);
            }

            public static BulletData Unpack(byte[] buffer)
            {
                return new BulletData() {
                    Position1 = UnpackPos(buffer, 0),
                    Position2 = UnpackPos(buffer, 4),

                    Rotation1 = UnpackRot(buffer, 8),
                    Rotation2 = UnpackRot(buffer, 12),

                    MaterialType = (BulletMaterialType)buffer[11],

                    Weight = UnpackWeight(buffer, 16),
                };
            }
        }

        public class BulletHolder
        {
            public List<BulletData> Bullets { get; set; }

            public BulletHolder()
            {
                Bullets = new List<BulletData>();
            }

            public BulletHolder(int numBullets)
            {
                Bullets = new List<BulletData>(numBullets);
            }
        }
        
        public ModelPackage ModelPackage { get; set; }

        public List<PartEntry> Parts { get; set; }

        public List<Thing2> T2Entries { get; set; }
        public List<Thing3> T3Entries { get; set; }
        public List<Thing4> T4Entries { get; set; }

        public List<PDLEntry> PDLEntries { get; set; }

        public List<BulletHolder> BulletHolders { get; set; }

        public int UID { get; set; }
        public int Reserved { get; set; }

        public int BulletCount
        {
            get
            {
                var nBullets = 0;

                foreach (var holder in BulletHolders)
                    nBullets += holder.Bullets.Count;

                return nBullets;
            }
        }

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

                var bulDataOffset = f.ReadInt32();

                Reserved = f.ReadInt32();

                var t1Count = f.ReadInt16();
                var t2Count = f.ReadInt16();
                var t3Count = f.ReadInt16();
                var t4Count = f.ReadInt16();

                // parts offset is hard-coded
                var partsOffset = 0x30;

                // calculate offsets so we can read data
                var t2Offset = partsOffset + (nParts * 0x20);
                var t3Offset = t2Offset + (t2Count * 0x20);
                var t1Offset = t3Offset + (t3Count * 0x50);
                var t4Offset = t1Offset + (t1Count * 0x10);
                var pdlOffset = t4Offset + (t4Count * 0x40);

                // make offset absolute
                bulDataOffset += pdlOffset;

                // read thing2 data
                T2Entries = new List<Thing2>(t2Count);

                for (int i = 0; i < t2Count; i++)
                {
                    f.Position = (i * 0x20) + t2Offset;

                    T2Entries.Add(new Thing2() {
                        Position = f.Read<Vector4>(),
                        Rotation = f.Read<Vector4>(),
                    });
                }

                // read thing3 data
                T3Entries = new List<Thing3>(t3Count);

                for (int i = 0; i < t3Count; i++)
                {
                    f.Position = (i * 0x50) + t3Offset;

                    T3Entries.Add(new Thing3() {
                        RotationX = f.Read<Vector4>(),
                        RotationY = f.Read<Vector4>(),
                        RotationZ = f.Read<Vector4>(),
                        Unknown4 = f.Read<Vector4>(),
                        Unknown5 = f.Read<Vector4>(),
                    });
                }

                // read thing4 data
                T4Entries = new List<Thing4>(t4Count);

                for (int i = 0; i < t4Count; i++)
                {
                    f.Position = (i * 0x40) + t4Offset;

                    T4Entries.Add(new Thing4() {
                        Unknown1 = f.Read<Vector4>(),
                        Unknown2 = f.Read<Vector4>(),
                        Unknown3 = f.Read<Vector4>(),
                        Unknown4 = f.Read<Vector4>(),
                    });
                }

                // read PDL data
                f.Position = pdlOffset;

                var entriesCount = f.ReadInt32();
                var pdlDataCount = f.ReadInt32();

                var entriesOffset = f.ReadInt32() + pdlOffset;
                var pdlDataOffset = f.ReadInt32() + pdlOffset;

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
                    var reserved = f.ReadInt32();

                    var dataEntries = new List<PDLData>(count);

                    if (count < 1)
                        throw new Exception("ERROR - invalid PDL entry!!!");

                    for (int t = 0; t < count; t++)
                    {
                        f.Position = (t * 0x60) + offset;

                        dataEntries.Add(new PDLData() {
                            Position = f.Read<Vector4>(),
                            Unknown1 = f.Read<Vector4>(),
                            Unknown2 = f.Read<Vector4>(),
                            Unknown3 = f.Read<Vector4>(),
                            Unknown4 = f.Read<Vector4>(),
                            Unknown5 = f.ReadInt32(),
                            Unknown6 = f.Read<Vector3>(),
                        });
                    }

                    PDLEntries.Add(new PDLEntry() {
                        Children = dataEntries,
                        Unknown = unknown,
                        Reserved = reserved
                    });
                }

                // read parts
                Parts = new List<PartEntry>(nParts);

                for (int i = 0; i < nParts; i++)
                {
                    f.Position = (i * 0x20) + partsOffset;

                    var entry = new {
                        Type = f.ReadInt16(),
                        SlotType = (SlotType)f.ReadInt16(),

                        Flags1 = f.ReadInt16(),
                        Flags2 = f.ReadInt16(),

                        Unknown1 = f.ReadInt16(),
                        Unknown2 = f.ReadInt16(),

                        NumChildren = f.ReadInt16(),
                        Unknown3 = f.ReadInt16(),

                        Hinge = f.ReadInt16(),
                        PartId = (byte)f.ReadByte(),
                        Unknown4 = (byte)f.ReadByte(),

                        PhysicsId = f.ReadInt16(),
                        PositionId = f.ReadInt16(),

                        OffsetId = f.ReadInt16(),
                        TransformId = f.ReadInt16(),

                        AxisId = f.ReadInt16(),
                        Unknown6 = f.ReadInt16(),
                    };

                    var position = new Vector4();

                    // get position data
                    if (entry.PositionId != -1)
                    {
                        f.Position = (entry.PositionId * 0x10) + t1Offset;

                        position = f.Read<Vector4>();
                    }

                    var part = new PartEntry() {
                        Type = entry.Type,
                        SlotType = entry.SlotType,

                        Flags1 = entry.Flags1,
                        Flags2 = entry.Flags2,

                        Unknown1 = entry.Unknown1,
                        Unknown2 = entry.Unknown2,

                        Children = (entry.NumChildren > 1) ? new List<PartEntry>(entry.NumChildren - 1) : null,
                        Unknown3 = entry.Unknown3,

                        Hinge = entry.Hinge,
                        PartId = entry.PartId,
                        Unknown4 = entry.Unknown4,

                        Physics = (entry.PhysicsId >= 0) ? PDLEntries[entry.PhysicsId] : null,
                        Position = position,

                        Offset = (entry.OffsetId >= 0) ? T2Entries[entry.OffsetId] : null,
                        Transform = (entry.TransformId >= 0) ? T3Entries[entry.TransformId] : null,

                        Axis = (entry.AxisId >= 0) ? T4Entries[entry.AxisId] : null,
                        Unknown6 = entry.Unknown6
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

                // read bullet hole data
                f.Position = bulDataOffset;

                var nBullets = f.ReadInt32();
                var nHolders = f.ReadInt32(); // => t3Count

                if (nHolders != t3Count)
                    throw new InvalidOperationException("UH-OH! Looks like the bullet hole data is corrupt!");

                BulletHolders = new List<BulletHolder>(nHolders);

                var holders = new int[nHolders];

                for (int i = 0; i < nHolders; i++)
                    holders[i] = f.ReadInt32();

                f.Position = bulDataOffset + Memory.Align(0x8 + (nHolders * 0x4), 16);

                // resolve bullet hole offsets and counts
                var nBulletsRead = 0;

                for (int p = 0; p < nHolders; ++p)
                {
                    var holderLen = ((p + 1) != nHolders)
                        ? (holders[p + 1] - holders[p])
                        : (nBullets - holders[p]);

                    var holder = new BulletHolder();

                    for (int b = 0; b < holderLen; b++)
                    {
                        if (nBulletsRead == nBullets)
                            throw new InvalidOperationException($"Panel {p} expected {holderLen - b} more bullets when there's no more left!");

                        var bullet = BulletData.Unpack(f);
                        ++nBulletsRead;

                        holder.Bullets.Add(bullet);
                    }

                    BulletHolders.Add(holder);
                }
            }
        }

        protected override void Save()
        {
            throw new NotImplementedException();
        }
        
        public void SaveVPK(string filename)
        {
            var version = 6;

            // allocate 32mb buffer
            using (var fs = new MemoryStream(1024 * 32767))
            {
                fs.Write(0xF12EB12D); // magic
                fs.Write(version); // version
                fs.Write(0); // reserved
                
                // 0xC: skip filesize for now
                fs.Position += 0x4;

                fs.Write(Parts.Count);
                fs.Write(UID);
                
                // 0x18: skip bullet hole data offset
                fs.Position += 0x4;

                fs.Write(Reserved);
                
                var isEnabled = new Func<bool, short>((b) => {
                    return (short)(b ? 1 : -1);
                });
                
                foreach (var part in Parts)
                {
                    var nChildren = 1;

                    var hasPhysics  = (part.Physics != null);
                    var hasPosition = (part.Position != null);
                    var hasOffset   = (part.Offset != null);
                    var hasUnknown5 = (part.Transform != null);
                    var hasAxis     = (part.Axis != null);

                    if (part.Children != null)
                        nChildren += part.Children.Count;

                    fs.Write(part.Type);
                    fs.Write((short)part.SlotType);

                    fs.Write(part.Flags1);
                    fs.Write(part.Flags2);

                    fs.Write(part.Unknown1);
                    fs.Write(part.Unknown2);

                    fs.Write((short)nChildren);
                    fs.Write(part.Unknown3);

                    fs.Write(part.Hinge);

                    fs.WriteByte(part.PartId);
                    
                    if (version == 3)
                    {
                        //Unknown4 is unused
                        fs.WriteByte(0x99);

                        fs.Write(isEnabled(hasPhysics));
                        fs.Write(isEnabled(hasPosition));
                        fs.Write(isEnabled(hasOffset));
                        fs.Write(isEnabled(hasUnknown5));
                        fs.Write(isEnabled(hasAxis));
                    }
                    else if (version >= 4)
                    {
                        // now using a byte flag
                        var flags = 0;

                        if (hasPhysics)
                            flags |= 1;
                        if (hasPosition)
                            flags |= 2;
                        if (hasOffset)
                            flags |= 4;
                        if (hasUnknown5)
                            flags |= 8;
                        if (hasAxis)
                            flags |= 16;

                        fs.WriteByte(flags);
                    }

                    fs.Write(part.Unknown6);

                    if (hasPhysics)
                    {
                        var phy = part.Physics;

                        fs.Write(phy.Children.Count);
                        fs.WriteFloat(phy.Unknown);
                        fs.Write(phy.Reserved);

                        if (version == 3)
                        {
                            // padding
                            fs.Write(0x99999999);
                        }

                        foreach (var pdl in phy.Children)
                        {
                            fs.Write(pdl.Position);
                            fs.Write(pdl.Unknown1);
                            fs.Write(pdl.Unknown2);
                            fs.Write(pdl.Unknown3);
                            fs.Write(pdl.Unknown4);

                            fs.Write(pdl.Unknown5);

                            fs.Write(pdl.Unknown6);
                        }
                    }

                    if (hasPosition)
                        fs.Write(part.Position.Value);

                    if (hasOffset)
                    {
                        var off = part.Offset;

                        fs.Write(off.Position);
                        fs.Write(off.Rotation);
                    }

                    if (hasUnknown5)
                    {
                        var unk5 = part.Transform;

                        fs.Write(unk5.RotationX);
                        fs.Write(unk5.RotationY);
                        fs.Write(unk5.RotationZ);
                        fs.Write(unk5.Unknown4);
                        fs.Write(unk5.Unknown5);
                    }

                    if (hasAxis)
                    {
                        var axis = part.Axis;

                        fs.Write(axis.Unknown1);
                        fs.Write(axis.Unknown2);
                        fs.Write(axis.Unknown3);
                        fs.Write(axis.Unknown4);
                    }
                }

                if (version >= 4)
                    fs.Align(16);
                
                var bulDataOffset = (int)fs.Position;

                var bulVersion = 2;
                var bulType = (version < 6) ? 0 : 1; // packed=0, unpacked=1

                var packBullets = (bulType == 0);

                var nBullets = BulletCount;
                var nHolders = BulletHolders.Count;
                
                var bulletLog = new StringBuilder();

                bulletLog.AppendLine("# Bullet Hole Data Export Log");

                bulletLog.AppendLine($"# Bullets: {nBullets}");
                bulletLog.AppendLine($"# Holders: {nHolders}");
                bulletLog.AppendLine();

                // write bullet hole data!
                fs.Write(0x4C4C5542); // 'BULL'
                fs.Write((short)bulVersion);
                fs.Write((short)bulType);
                
                fs.Write(nHolders);

                var bulletIdx = 0;

                for (int i = 0; i < nHolders; i++)
                {
                    var holder = BulletHolders[i];
                    var nHolderBullets = holder.Bullets.Count;

                    bulletLog.AppendLine($"# -------- Holder {i + 1} -------- #");
                    bulletLog.AppendLine($"# Bullets: {nHolderBullets}");
                    bulletLog.AppendLine();

                    fs.Write(nHolderBullets);

                    for (int b = 0; b < nHolderBullets; b++)
                    {
                        var bullet = holder.Bullets[b];

                        bulletLog.AppendLine($"# Bullet {b + 1} ({bulletIdx + 1})");
                        bulletLog.AppendLine($"type: {bullet.MaterialType}");
                        bulletLog.AppendLine($"pos1: [{bullet.Position1.X,7:F4}, {bullet.Position1.Y,7:F4}, {bullet.Position1.Z,7:F4}]");
                        bulletLog.AppendLine($"pos2: [{bullet.Position2.X,7:F4}, {bullet.Position2.Y,7:F4}, {bullet.Position2.Z,7:F4}]");
                        bulletLog.AppendLine($"rot1: [{bullet.Rotation1.X,7:F4}, {bullet.Rotation1.Y,7:F4}, {bullet.Rotation1.Z,7:F4}]");
                        bulletLog.AppendLine($"rot2: [{bullet.Rotation2.X,7:F4}, {bullet.Rotation2.Y,7:F4}, {bullet.Rotation2.Z,7:F4}]");
                        bulletLog.AppendLine($"weight: {bullet.Weight:F4}");
                        bulletLog.AppendLine();

                        fs.Write(bullet.ToBinary(packBullets));

                        ++bulletIdx;
                    }
                }

                var logfile = Path.ChangeExtension(filename, ".bullets.log");
                File.WriteAllText(logfile, bulletLog.ToString());
                
                // trim file
                fs.SetLength(fs.Position);

                // write bullet hole data offset
                fs.Position = 0x18;
                fs.Write(bulDataOffset);

                // add filesize to header
                fs.Position = 0xC;
                fs.Write((int)fs.Length);

                File.WriteAllBytes(filename, fs.ToArray());
            }
        }
    }
}