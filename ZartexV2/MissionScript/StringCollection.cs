using System;
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

        public int RegisterString(string value)
        {
            if (!Strings.Contains(value))
            {
                var idx = Strings.Count;

                Strings.Add(value);

                return idx;
            }
            else
            {
                return Strings.IndexOf(value);
            }
        }

        protected override void Load()
        {
            using (var f = Spooler.GetMemoryStream())
            {
                var nStrings = f.ReadInt32();

                Strings = new List<string>(nStrings);

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < nStrings; i++)
                {
                    char c;

                    while ((c = f.ReadChar()) != '\0')
                        sb.Append(c);

                    Strings.Add(sb.ToString());

                    sb.Clear();
                }
            }
        }

        protected override void Save()
        {
            var nStrings = Strings.Count;

            // header size + offset entries
            int bufSize = (nStrings * 4) + 4;
            int[] offsets = new int[nStrings];

            for (int i = 0; i < nStrings; i++)
            {
                var str = Strings[i];

                offsets[i] = bufSize;
                bufSize += (str.Length + 1); // include null-terminator
            }

            using (var strBuffer = new MemoryStream(bufSize))
            {
                strBuffer.Write(nStrings);

                foreach (var offset in offsets)
                    strBuffer.Write(offset);

                foreach (var str in Strings)
                {
                    strBuffer.Write(str);
                    strBuffer.Position++; // null-terminator
                }

                Spooler.SetBuffer(strBuffer.ToArray());
            }
        }
    }
}
