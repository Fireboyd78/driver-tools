using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;

using DSCript.Spooling;
using System.IO;

namespace DSCript.Models
{
    public interface IHierarchyData : IDetail
    {
        UID ModelUID { get; set; }

        int Format { get; set; }
        int Type { get; set; }

        int UID { get; set; }
        
        int Flags { get; set; }
        
        PhysicsData PhysicsData { get; }
    }

    public struct HierarchyDataHeader : IDetail
    {
        public static readonly MagicNumber Magic = "AWHF"; // +rep for Allan Walton's Hierarchy Format ;)

        private int m_Reserved1; // Model.UID.High & ~0xFF
        private int m_Reserved2; // Model.UID.Low

        public UID ModelUID
        {
            get
            {
                var high = (m_Reserved1 & ~0xFF);
                var low = m_Reserved2;

                return new UID(low, high);
            }
            set
            {
                m_Reserved1 = Format | (value.High & ~0xFF);
                m_Reserved2 = value.Low;
            }
        }

        public int Format
        {
            get { return (m_Reserved1 & 0xFF); }
            set { m_Reserved1 = ((m_Reserved1 & ~0xFF) | (byte)value); }
        }
        
        public int Type;

        public int Count;   // number of parts in hierarchy
        public int UID;     // unique identifier for the hierarchy

        public int PDLSize; // used as offset past PDL for stuff like bullet hole data

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write((int)Magic);

            stream.Write(m_Reserved1);
            stream.Write(m_Reserved2);

            stream.Write(Type);

            stream.Write(Count);
            stream.Write(UID);

            stream.Write(PDLSize);
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            // discarded
            var magic = stream.ReadInt32();
            
            m_Reserved1 = stream.ReadInt32();
            m_Reserved2 = stream.ReadInt32();

            Type = stream.ReadInt32();

            Count = stream.ReadInt32();
            UID = stream.ReadInt32();

            PDLSize = stream.ReadInt32();
        }
    }

    public struct HierarchyInfo
    {
        public int Type;

        public int Count;   // number of parts in hierarchy
        public int UID;     // unique identifier for the hierarchy

        public int PDLSize; // used as offset past PDL for stuff like bullet hole data
    }

    public abstract class HierarchyData : IHierarchyData
    {
        public UID ModelUID { get; set; }

        public int Format { get; set; }
        public int Type { get; set; }

        public int UID { get; set; }
        
        public int Flags { get; set; }
        
        public PhysicsData PhysicsData { get; set; }

        protected HierarchyDataHeader Header;

        protected abstract void ReadData(Stream stream, IDetailProvider provider);
        protected abstract void WriteData(Stream stream, IDetailProvider provider);

        protected virtual void ReadHeader(Stream stream, IDetailProvider provider)
        {
            Header = provider.Deserialize<HierarchyDataHeader>(stream);

            Format = Header.Format;
            Type = Header.Type;

            ModelUID = Header.ModelUID;
            UID = Header.UID;
        }

        protected virtual void WriteHeader(Stream stream, IDetailProvider provider)
        {
            Header = new HierarchyDataHeader()
            {
                ModelUID = ModelUID,
                Format = Format,
                Type = Type,
                UID = UID,
            };

            provider.Serialize(stream, ref Header);
        }

        public void Serialize(Stream stream, IDetailProvider provider)
        {
            WriteData(stream, provider);

            stream.Position = 0;
            WriteHeader(stream, provider);
        }

        public void Deserialize(Stream stream, IDetailProvider provider)
        {
            ReadHeader(stream, provider);
            ReadData(stream, provider);
        }
    }
}
