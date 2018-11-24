using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace Zartex
{
    public class StringCollectionData : SpoolableResource<SpoolableBuffer>
    {
        public List<String> Strings { get; set; }

        public int Count
        {
            get { return Strings.Count; }
        }

        public string this[int index]
        {
            get { return Strings[index]; }
            set { Strings[index] = value; }
        }

        public int AppendString(string value)
        {
            if (!Strings.Contains(value))
            {
                var idx = Strings.Count;
                Strings.Add(value);
                return idx;
            }
            return Strings.IndexOf(value);
        }

        protected override void Load()
        {
            using (var f = Spooler.GetMemoryStream())
            {
                var baseOffset = f.Position;
                var nStrings = f.ReadInt32();

                Strings = new List<string>(nStrings);
                
                for (int i = 0; i < nStrings; i++)
                {
                    f.Position = baseOffset + (i * 4) + 4;
                    f.Position = baseOffset + f.ReadInt32();
                    
                    char c;
                    string str = "";

                    while ((c = f.ReadChar()) != '\0')
                        str += c;
                    
                    Strings.Add(str);
                }
            }
        }

        protected override void Save()
        {
            var nStrings = Strings.Count;

            // header size + offset entries
            var bufSize = 0x4 + (nStrings * 0x4);
            var offsets = new int[nStrings];

            for (int i = 0; i < nStrings; i++)
            {
                var str = Strings[i];

                offsets[i] = bufSize;
                bufSize += (str.Length + 1); // include null-terminator
            }

            var strBuffer = new byte[bufSize];
            
            using (var fStr = new MemoryStream(strBuffer))
            {
                fStr.Write(nStrings);

                foreach (var offset in offsets)
                    fStr.Write(offset);

                foreach (var str in Strings)
                {
                    if (!String.IsNullOrEmpty(str))
                        fStr.Write(str);

                    fStr.Position++;
                }
            }

            Spooler.SetBuffer(strBuffer);
        }
    }
}
