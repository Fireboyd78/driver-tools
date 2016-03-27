using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

using Zartex;
using Zartex.Converters;

namespace Zartex.MissionObjects
{
    public abstract class ContainerBlock : MissionObject
    {
        protected byte[] buffer;

        public override int Id
        {
            get { return -1; }
        }

        public override int Size
        {
            get
            {
                if (buffer == null)
                    throw new Exception("Cannot return the size of an unintialized block.");

                return (Memory.Align((Offset + 4), 16) + Buffer.Length);
            }
        }

        public byte Unknown { get; set; }

        public byte Reserved
        {
            get { return 16; }
        }

        public byte[] Buffer
        {
            get
            {
                if (buffer == null)
                    throw new Exception("Cannot return an uninitialized buffer.");

                return buffer;
            }
        }

        protected ContainerBlock(BinaryReader reader)
        {
            Offset = (int)reader.GetPosition();

            var size = reader.ReadInt16();

            Unknown = reader.ReadByte();

            if (reader.ReadByte() != Reserved)
                throw new Exception("The unknown constant is incorrect, this may or may not be a developer error.");

            reader.Align(16);

            buffer = new byte[size];
            reader.Read(buffer, 0, buffer.Length);
        }
    }
}
