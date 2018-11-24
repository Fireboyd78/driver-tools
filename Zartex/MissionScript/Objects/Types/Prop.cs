using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex
{
    public class PropObject : MissionObject
    {
        public override int TypeId
        {
            get { return 8; }
        }

        public int Id { get; set; }

        public int Unk1 { get; set; }
        public int Unk2 { get; set; }
        public int Unk3 { get; set; }
        public int Unk4 { get; set; }
        public int Unk5 { get; set; }

        protected override void LoadData(Stream stream)
        {
            Id = stream.ReadInt32();

            Unk1 = stream.ReadInt32();
            Unk2 = stream.ReadInt32();
            Unk3 = stream.ReadInt32();
            Unk4 = stream.ReadInt32();
            Unk5 = stream.ReadInt32();
        }

        protected override void SaveData(Stream stream)
        {
            stream.Write(Id);
            stream.Write(Unk1);
            stream.Write(Unk2);
            stream.Write(Unk3);
            stream.Write(Unk4);
            stream.Write(Unk5);
        }
    }
}
