using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DSCript.Models
{
    public enum BulletMaterialType : byte
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

                    ms.Write((ushort)MagicNumber.FB); // ;)

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
                    ms.Write((ushort)MagicNumber.FB); // ;)
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
}
