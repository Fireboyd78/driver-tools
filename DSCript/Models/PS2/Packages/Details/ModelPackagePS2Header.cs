using System;
using System.IO;
using System.Text;

namespace DSCript.Models
{
    public struct ModelPackagePS2Header
    {
        public static readonly int Magic = 0x4B41504D; // 'MPAK'

        public int UID;

        public int Reserved1;
        public int Reserved2;
        public int Reserved3;

        public int ModelCount;

        public int MaterialDataOffset;
        public int DataSize;

        // does not include model offset list
        public int HeaderSize
        {
            get { return 0x20; }
        }

        public void ReadHeader(Stream stream)
        {
            if (stream.ReadInt32() != Magic)
                throw new Exception("Bad magic, cannot load ModelPackagePS2!");

            UID = stream.ReadInt32();

            Reserved1 = stream.ReadInt32();
            Reserved2 = stream.ReadInt32();
            Reserved3 = stream.ReadInt32();

            ModelCount = stream.ReadInt32();

            MaterialDataOffset = stream.ReadInt32();
            DataSize = stream.ReadInt32();
        }

        public void WriteHeader(Stream stream)
        {
            stream.Write(Magic);

            stream.Write(UID);

            stream.Write(Reserved1);
            stream.Write(Reserved2);
            stream.Write(Reserved3);

            stream.Write(ModelCount);

            stream.Write(MaterialDataOffset);
            stream.Write(DataSize);
        }
    }
}
