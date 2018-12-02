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
        public static readonly MagicNumber VPKBulletMagic = "BULL";
        public static readonly int VPKBulletVersion = 2;
        
        public class CenterPoint
        {
            public Vector4 Position { get; set; }
            public Vector4 Rotation { get; set; }
        }

        // bounding box? collision plane?
        public class Thing3
        {
            public Matrix44 Transform { get; set; }
            public Vector4 Unknown { get; set; }
        }
        
        public class PDLData
        {
            public Vector4 Position { get; set; }
            public Matrix44 Transform { get; set; }
            
            public int Unknown1 { get; set; }

            public Vector3 Unknown2 { get; set; }
        }

        public class PDLEntry
        {
            public float Unknown { get; set; }

            public int Reserved { get; set; }

            public List<PDLData> Children { get; set; }
        }
        
        public ModelPackageResource ModelPackage { get; set; }

        public List<VehiclePartData> Parts { get; set; }

        public List<Vector4> MarkerPoints { get; set; }

        public List<CenterPoint> CenterPoints { get; set; }
        public List<Thing3> T3Entries { get; set; }
        public List<Matrix44> PivotPoints { get; set; }

        public List<PDLEntry> PDLEntries { get; set; }

        public List<BulletHolder> BulletHolders { get; set; }

        public int UID { get; set; }
        public int Flags { get; set; }

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

                var header = f.Read<HierarchyDataHeader>();
                var vInfo = header.VehicleHierarchyInfo;

                if (header.HierarchyType != HierarchyDataType.Vehicle)
                    throw new InvalidOperationException("Cannot load vehicle hierarchy data -- invalid hierarchy type!");
                
                UID = header.UID;
                Flags = vInfo.Flags;
                
                var bulDataOffset = (header.PDLOffset + header.PDLSize);

                // read thing1 data
                f.Position = (header.ExtraPartsDataOffset + vInfo.T1Offset);

                MarkerPoints = new List<Vector4>(vInfo.T1Count);

                for (int i = 0; i < vInfo.T1Count; i++)
                {
                    var v4 = f.Read<Vector4>();

                    MarkerPoints.Add(v4);
                }

                // read thing2 data
                f.Position = (header.ExtraPartsDataOffset + vInfo.T2Offset);

                CenterPoints = new List<CenterPoint>(vInfo.T2Count);
                
                for (int i = 0; i < vInfo.T2Count; i++)
                {
                    CenterPoints.Add(new CenterPoint() {
                        Position = f.Read<Vector4>(),
                        Rotation = f.Read<Vector4>(),
                    });
                }

                // read thing3 data
                f.Position = (header.ExtraPartsDataOffset + vInfo.T3Offset);

                T3Entries = new List<Thing3>(vInfo.T3Count);

                for (int i = 0; i < vInfo.T3Count; i++)
                {
                    T3Entries.Add(new Thing3() {
                        Transform = f.Read<Matrix44>(),
                        Unknown = f.Read<Vector4>(),
                    });
                }

                // read thing4 data
                f.Position = (header.ExtraPartsDataOffset + vInfo.T4Offset);

                PivotPoints = new List<Matrix44>(vInfo.T4Count);

                for (int i = 0; i < vInfo.T4Count; i++)
                {
                    var m44 = f.Read<Matrix44>();

                    PivotPoints.Add(m44);
                }

                // read PDL data
                var pdlOffset = header.PDLOffset;

                f.Position = header.PDLOffset;

                // TODO: Write a struct to encapsulate this stuff
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
                    var unknown = f.ReadSingle();
                    var reserved = f.ReadInt32();

                    var dataEntries = new List<PDLData>(count);

                    if (count < 1)
                        throw new Exception("ERROR - invalid PDL entry!!!");

                    for (int t = 0; t < count; t++)
                    {
                        f.Position = (t * 0x60) + offset;

                        dataEntries.Add(new PDLData() {
                            Position = f.Read<Vector4>(),
                            Transform = f.Read<Matrix44>(),
                            Unknown1 = f.ReadInt32(),
                            Unknown2 = f.Read<Vector3>(),
                        });
                    }

                    PDLEntries.Add(new PDLEntry() {
                        Children = dataEntries,
                        Unknown = unknown,
                        Reserved = reserved
                    });
                }

                // read parts
                f.Position = header.PartsOffset;

                Parts = new List<VehiclePartData>(header.PartsCount);

                for (int i = 0; i < header.PartsCount; i++)
                {
                    var part = f.Read<VehiclePartData>();
                    Parts.Add(part);
                }
                
                // read bullet hole data
                f.Position = bulDataOffset;

                var nBullets = f.ReadInt32();
                var nHolders = f.ReadInt32(); // => t3Count

                if (nHolders != vInfo.T3Count)
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
            var version = 7;

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

                fs.Write(Flags);
                
                var isEnabled = new Func<bool, short>((b) => {
                    return (short)(b ? 1 : -1);
                });
                
                foreach (var part in Parts)
                {
                    var hasPhysics      = (part.PhysicsId != -1);
                    var hasPosition     = (part.PositionId != -1);
                    var hasOffset       = (part.OffsetId != -1);
                    var hasTransform    = (part.TransformId != -1);
                    var hasAxis         = (part.AxisId != -1);
                    
                    fs.Write(part.PartType);
                    fs.Write(part.SlotType);

                    fs.Write(part.Flags);
                    fs.Write(part.TypeFlags);

                    fs.Write(part.NumChildren);

                    fs.Write(part.Unknown);
                    fs.Write(part.Hinge);

                    fs.WriteByte(part.ModelId);
                    
                    if (version == 3)
                    {
                        fs.WriteByte(0x99);

                        fs.Write(isEnabled(hasPhysics));
                        fs.Write(isEnabled(hasPosition));
                        fs.Write(isEnabled(hasOffset));
                        fs.Write(isEnabled(hasTransform));
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
                        if (hasTransform)
                            flags |= 8;
                        if (hasAxis)
                            flags |= 16;

                        fs.WriteByte(flags);
                    }
                    
                    fs.Write((part.Reserved != 0) ? part.Reserved : (short)MagicNumber.FB);
                    
                    if (hasPhysics)
                    {
                        var phy = PDLEntries[part.PhysicsId];

                        fs.Write(phy.Children.Count);
                        fs.Write(phy.Unknown);
                        fs.Write(phy.Reserved);

                        if (version == 3)
                        {
                            // padding
                            fs.Write(0x99999999);
                        }

                        foreach (var pdl in phy.Children)
                        {
                            fs.Write(pdl.Position);
                            fs.Write(pdl.Transform);
                            
                            fs.Write(pdl.Unknown1);

                            fs.Write(pdl.Unknown2);
                        }
                    }

                    if (hasPosition)
                        fs.Write(MarkerPoints[part.PositionId]);

                    if (hasOffset)
                    {
                        var off = CenterPoints[part.OffsetId];

                        fs.Write(off.Position);
                        fs.Write(off.Rotation);
                    }

                    if (hasTransform)
                    {
                        var unk5 = T3Entries[part.TransformId];

                        fs.Write(unk5.Transform);
                        fs.Write(unk5.Unknown);
                    }

                    if (hasAxis)
                    {
                        var axis = PivotPoints[part.AxisId];

                        fs.Write(axis);
                    }
                }

                if (version >= 4)
                    fs.Align(16);
                
                var bulDataOffset = (int)fs.Position;
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
                fs.Write((int)VPKBulletMagic); // 'BULL'
                fs.Write((short)VPKBulletVersion);
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