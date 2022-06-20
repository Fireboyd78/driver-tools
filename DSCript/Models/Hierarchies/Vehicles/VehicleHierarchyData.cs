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
    public class VehicleHierarchyData : SpoolableResource<SpoolableBuffer>, IDetailProvider
    {
        public static readonly MagicNumber VPKBulletMagic = "BULL";
        public static readonly int VPKBulletVersion = 2;
        
        public class MovingPart : IDetail
        {
            public Vector4 Position { get; set; }
            public Vector4 Rotation { get; set; }

            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                Position = stream.Read<Vector4>();
                Rotation = stream.Read<Vector4>();
            }

            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                stream.Write(Position);
                stream.Write(Rotation);
            }
        }

        public class MarkerPoint : IDetail
        {
            public Vector4 Position { get; set; }
            public Vector4 Rotation { get; set; }

            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                Position = stream.Read<Vector4>();

                // thank god Reflections did this, otherwise we'd be f*$%ed!
                // ...oh wait, they DIDN'T do this for DPL on Xbox...
                if (provider.Version == 1 || (provider.Version == 0 && provider.Platform == PlatformType.Xbox))
                    Rotation = stream.Read<Vector4>();
            }

            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                stream.Write(Position);

                if (provider.Version == 1)
                    stream.Write(Rotation);
            }
        }

        public class DamagingPart : IDetail
        {
            public Matrix44 Transform { get; set; }
            public Vector4 Unknown { get; set; }

            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                Transform = stream.Read<Matrix44>();
                Unknown = stream.Read<Vector4>();
            }

            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                stream.Write(Transform);
                stream.Write(Unknown);
            }
        }

        public class InstancePart : IDetail
        {
            public Matrix44 Transform { get; set; }

            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                Transform = stream.Read<Matrix44>();
            }

            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                stream.Write(Transform);
            }
        }
        
        public ModelPackageResource ModelPackage { get; set; }

        public List<VehiclePartData> Parts { get; set; }

        public List<MarkerPoint> MarkerPoints { get; set; }
        public List<MovingPart> MovingParts { get; set; }
        public List<DamagingPart> DamagingParts { get; set; }
        public List<InstancePart> InstanceParts { get; set; }

        public List<PhysicsCollisionModel> CollisionModels { get; set; }

        public List<BulletHolder> BulletHolders { get; set; }

        // should ALWAYS be Generic (unless it's for DPL on Xbox!)
        public PlatformType Platform { get; set; }

        public int Version
        {
            get { return Spooler.Version; }
            set { Spooler.Version = value; }
        }

        public int UID { get; set; }
        public int Flags { get; set; }

        public int ExtraData { get; set; }

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
            using (var stream = Spooler.GetMemoryStream())
            {
                // skip header
                stream.Position = 0xC;

                var header = stream.Read<HierarchyInfo>();

                if (header.Type != 6)
                    throw new InvalidOperationException("Cannot load vehicle hierarchy data -- invalid hierarchy type!");

                var vInfo = this.Deserialize<VehicleHierarchyInfo>(stream);

                UID = header.UID;
                Flags = vInfo.Flags;

                ExtraData = vInfo.ExtraData;

                //
                // Parts
                //
                
                Parts = new List<VehiclePartData>(header.Count);
                
                for (int i = 0; i < header.Count; i++)
                {
                    var part = stream.Read<VehiclePartData>();
                    Parts.Add(part);
                }

                //
                // Moving parts
                //

                MovingParts = new List<MovingPart>(vInfo.MovingPartsCount);
                
                for (int i = 0; i < vInfo.MovingPartsCount; i++)
                {
                    var point = this.Deserialize<MovingPart>(stream);
                    
                    MovingParts.Add(point);
                }

                //
                // Damaging parts
                //

                DamagingParts = new List<DamagingPart>(vInfo.DamagingPartsCount);
                
                for (int i = 0; i < vInfo.DamagingPartsCount; i++)
                {
                    var t3 = this.Deserialize<DamagingPart>(stream);

                    DamagingParts.Add(t3);
                }

                //
                // Marker points
                //

                MarkerPoints = new List<MarkerPoint>(vInfo.MarkerPointsCount);

                for (int i = 0; i < vInfo.MarkerPointsCount; i++)
                {
                    var point = this.Deserialize<MarkerPoint>(stream);

                    MarkerPoints.Add(point);
                }
                
                //
                // Instance parts
                //

                InstanceParts = new List<InstancePart>(vInfo.InstancePartsCount);
                
                for (int i = 0; i < vInfo.InstancePartsCount; i++)
                {
                    var inst = this.Deserialize<InstancePart>(stream);

                    InstanceParts.Add(inst);
                }

                //
                // Physics data
                //
                
                var pdlOffset = (int)stream.Position;
                var pdl = this.Deserialize<PhysicsData>(stream);

                CollisionModels = pdl.CollisionModels;

                //
                // Bullet data
                //

                var bulDataOffset = (pdlOffset + header.PDLSize);
                
                stream.Position = bulDataOffset;
                
                var nBullets = stream.ReadInt32();
                var nHolders = stream.ReadInt32(); // => t3Count

                // idk man ¯\_(ツ)_/¯
                //if (nHolders != vInfo.T3Count)
                //    throw new InvalidOperationException("UH-OH! Looks like the bullet hole data is corrupt!");

                BulletHolders = new List<BulletHolder>(nHolders);

                var holders = new int[nHolders];

                for (int i = 0; i < nHolders; i++)
                    holders[i] = stream.ReadInt32();

                stream.Position = bulDataOffset + Memory.Align(0x8 + (nHolders * 0x4), 16);

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

                        var bullet = BulletData.Unpack(stream);
                        ++nBulletsRead;

                        holder.Bullets.Add(bullet);
                    }

                    BulletHolders.Add(holder);
                }
            }
        }

        protected override void Save()
        {
            MagicNumber magic = "Antilli!";

            var header = new HierarchyInfo()
            {
                Type = 6,
                UID = UID,
                Count = Parts.Count,
            };

            var vInfo = new VehicleHierarchyInfo()
            {
                Flags = Flags,

                MarkerPointsCount = (short)MarkerPoints.Count,
                MovingPartsCount = (short)MovingParts.Count,
                DamagingPartsCount = (short)DamagingParts.Count,
                InstancePartsCount = (short)InstanceParts.Count,

                ExtraData = ExtraData,
            };

            // original version (excluding DPL on Xbox)
            if (Version == 0 && Platform != PlatformType.Xbox)
                vInfo.ExtraFlags = MagicNumber.FIREBIRD;

            byte[] buffer = null;

            using (var stream = new MemoryStream())
            {
                // skip the header portion for now
                stream.Position = 0x30;

                //
                // Parts
                //

                foreach (var part in Parts)
                    stream.Write(part);

                //
                // Moving parts
                //

                foreach (var point in MovingParts)
                    this.Serialize(stream, point);

                //
                // Damaging parts
                //

                foreach (var t3 in DamagingParts)
                    this.Serialize(stream, t3);

                //
                // Marker points
                //

                foreach (var point in MarkerPoints)
                    this.Serialize(stream, point);

                //
                // Instance parts
                //

                foreach (var inst in InstanceParts)
                    this.Serialize(stream, inst);

                //
                // Physics data
                //

                var pdlOffset = (int)stream.Position;
                var pdl = new PhysicsData()
                {
                    CollisionModels = CollisionModels,
                };

                this.Serialize(stream, pdl);

                header.PDLSize = (int)stream.Position - pdlOffset;

                //
                // Bullet data
                //

                var bulDataOffset = (pdlOffset + header.PDLSize);

                var nHolders = BulletHolders.Count; // => t3Count
                var nBullets = 0;

                var holders = new int[nHolders];

                // bullet data is aligned to 16-byte boundary after header
                stream.Position = bulDataOffset + Memory.Align(8 + (nHolders * 4), 16);

                for (int p = 0; p < nHolders; ++p)
                {
                    var holder = BulletHolders[p];
                    var bullets = holder.Bullets;

                    // current bullets number/offset
                    holders[p] = nBullets;

                    foreach (var bullet in bullets)
                    {
                        // pack the bullet data tightly
                        var bulData = bullet.ToBinary(true);

                        stream.Write(bulData, 0, bulData.Length);
                        nBullets++;
                    }
                }

                // write out bullet data header
                stream.Position = bulDataOffset;

                stream.Write(nBullets);
                stream.Write(nHolders);

                foreach (var holder in holders)
                    stream.Write(holder);

                //
                // Header
                //

                stream.Position = 0;

                stream.Write((long)magic);
                stream.Write((int)MagicNumber.FIREBIRD);

                stream.Write(header);

                this.Serialize(stream, vInfo);

                buffer = stream.ToArray();
            }

            // done! :D
            Spooler.SetBuffer(buffer);
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
                        var phy = CollisionModels[part.PhysicsId];

                        fs.Write(phy.Children.Count);
                        fs.Write(phy.BoundingRadius);
                        fs.Write(phy.Flags);

                        if (version == 3)
                        {
                            // padding
                            fs.Write(0x99999999);
                        }

                        foreach (var pdl in phy.Children)
                        {
                            fs.Write(pdl.Bounds);
                            fs.Write(pdl.Transform);
                            
                            fs.Write(pdl.Flags);

                            fs.Write(pdl.Elasticity);
                            fs.Write(pdl.Friction);
                            fs.Write(pdl.Zestiness);
                        }
                    }

                    if (hasPosition)
                        fs.Write(MarkerPoints[part.PositionId]);

                    if (hasOffset)
                    {
                        var off = MovingParts[part.OffsetId];

                        fs.Write(off.Position);
                        fs.Write(off.Rotation);
                    }

                    if (hasTransform)
                    {
                        var unk5 = DamagingParts[part.TransformId];

                        fs.Write(unk5.Transform);
                        fs.Write(unk5.Unknown);
                    }

                    if (hasAxis)
                    {
                        var axis = InstanceParts[part.AxisId];

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