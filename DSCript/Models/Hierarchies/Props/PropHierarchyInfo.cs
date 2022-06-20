using System.Collections.Generic;
using System.IO;

namespace DSCript.Models
{
    public struct PropHierarchyInfo
    {
        // related to mass?
        public float V1;
        public float V2;

        public int Reserved;
    }

    public struct PropData
    {
        // offset - 0x40
        // bunch of stuff before this; unused???
        public Vector3 MomentsOfInertia;
        public float Mass;
    }

    public struct FragmentData : IDetail
    {
        short Handle;
        short PhysicsId;

        public int Reserved;

        public int Unknown1;
        public int Unknown2;

        public Matrix44 Transform;
        
        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            Handle = stream.ReadInt16();
            PhysicsId = stream.ReadInt16();

            Reserved = stream.ReadInt32();

            Unknown1 = stream.ReadInt32();
            Unknown2 = stream.ReadInt32();

            Transform = stream.Read<Matrix44>();
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            Handle = stream.ReadInt16();
            PhysicsId = stream.ReadInt16();

            Reserved = stream.ReadInt16();

            Unknown1 = stream.ReadInt16();
            Unknown2 = stream.ReadInt16();
        }
    }

    public class PropHierarchyData : HierarchyData
    {
        public struct Detail : IDetail
        {
            public float V1;
            public float V2;

            public int Reserved;

            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                V1 = stream.ReadSingle();
                V2 = stream.ReadSingle();

                Reserved = stream.ReadInt32();

                // skip 
                stream.Position += 0x18;
            }

            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                stream.Write(V1);
                stream.Write(V2);

                stream.Write(Reserved);

                for (int i = 0; i < 3; i++)
                    stream.Write(0L);
            }
        }

        public float V1 { get; set; }
        public float V2 { get; set; }

        public int Reserved { get; set; }

        public List<PropData> Props { get; set; }
        
        protected override void ReadData(Stream stream, IDetailProvider provider)
        {
            if (provider.Version == 2)
            {

            }
            else
            {
                

                PhysicsData = provider.Deserialize<PhysicsData>(stream);
            }
        }

        protected override void WriteData(Stream stream, IDetailProvider provider)
        {
            if (provider.Version == 2)
            {

            }
            else
            {
                stream.Write(Flags);

                provider.Serialize(stream, PhysicsData);
            }
        }

        protected override void ReadHeader(Stream stream, IDetailProvider provider)
        {
            if (provider.Version == 2)
            {
                UID = stream.ReadInt32();
                Header.Count = stream.ReadInt32();
            }
            else
            {
                base.ReadHeader(stream, provider);
            }
        }

        protected override void WriteHeader(Stream stream, IDetailProvider provider)
        {
            if (provider.Version == 2)
            {
                stream.Write(UID);
                stream.Write(Props.Count);
            }
            else
            {
                base.WriteHeader(stream, provider);
            }
        }
    }
}
