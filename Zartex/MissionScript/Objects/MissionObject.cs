using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex
{
    public abstract class MissionObject
    {
        public abstract int TypeId { get; }

        [Browsable(false)]
        public virtual bool HasCreationData
        {
            get { return false; }
        }

        protected virtual int Alignment
        {
            get { return 16; }
        }

        public void Load(Stream stream)
        {
            if (HasCreationData)
            {
                var size = stream.ReadUInt16();
                var alignment = (stream.ReadUInt16() >> 8) & 0xFF;

                if (size != 0)
                {
                    stream.Align(alignment);

                    var creationData = new byte[size];
                    stream.Read(creationData, 0, size);

                    using (var ms = new MemoryStream(creationData))
                    {
                        LoadCreationData(ms);
                    }
                }
            }

            LoadData(stream);
        }

        public void Save(Stream stream)
        {
            stream.Write(TypeId);

            if (HasCreationData)
            {
                byte[] buffer = null;
                var size = 0;

                using (var ms = new MemoryStream(128))
                {
                    SaveCreationData(ms);

                    size = (int)ms.Position;

                    if (size != 0)
                        buffer = ms.ToArray();
                }

                if (buffer != null)
                {
                    if (size > 65535)
                        throw new InvalidOperationException("Too much creation data!");

                    var header = (size | (0xFB << 16) | (Alignment << 24));

                    stream.Write(header);

                    while ((stream.Position & (Alignment - 1)) != 0)
                        stream.Write(0x3E3E3E3E);

                    stream.Write(buffer, 0, size);
                }
                else
                {
                    // no creation data
                    stream.Write(0);
                }
            }

            SaveData(stream);
        }

        protected virtual void LoadData(Stream stream)
        {
            throw new NotImplementedException("Loading not implemented for object.");
        }

        protected virtual void SaveData(Stream stream)
        {
            throw new NotImplementedException("Saving not implemented for object.");
        }

        protected virtual void LoadCreationData(Stream stream)
        {
            throw new NotImplementedException("Object does not implement creation data.");
        }

        protected virtual void SaveCreationData(Stream stream)
        {
            throw new NotImplementedException("Object does not implement creation data.");
        }
    }
}
