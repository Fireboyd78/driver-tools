using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex
{
    public class AreaObject : MissionObject
    {
        public override int TypeId
        {
            get { return 4; }
        }
        
        public override bool HasCreationData
        {
            get { return true; }
        }

        public byte[] CreationData { get; set; }

        protected override void LoadData(Stream stream)
        {
            // nothing to load
            return;
        }

        protected override void SaveData(Stream stream)
        {
            // nothing to save
            return;
        }

        protected override void LoadCreationData(Stream stream)
        {
            CreationData = stream.ReadAllBytes();
        }

        protected override void SaveCreationData(Stream stream)
        {
            stream.Write(CreationData);
        }
    }
}
