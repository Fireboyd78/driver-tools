using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

using Zartex;
using Zartex.Converters;

namespace Zartex
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

                return -1;
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
    }
}
