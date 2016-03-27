using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace LocSF
{
    public class LocalisationPackage : SpoolableResource<SpoolablePackage>
    {
        static readonly SpoolerContext SpooledLocalisationRoot       = "SLRR";
        static readonly SpoolerContext SpooledLocalisationHeader     = "SLRH";
        static readonly SpoolerContext SpooledLocalisationLookup     = "SLRC";
        static readonly SpoolerContext SpooledLocalisationStrings    = "SLRS";

        public int Reserved { get; set; } = 0x100626A0;

        public List<LocalisationData> Localisations { get; set; }

        protected override void Load()
        {
            var schema = new ChunkSchema(ChunkSchemaType.Sequential) {
                { SpooledLocalisationHeader, "header" },
                { SpooledLocalisationLookup, "lookup" },
                { SpooledLocalisationStrings, "strings" },
            }.Process(Spooler);

            if (schema == null)
                throw new InvalidOperationException("Failed to process localisation package schema!");

            var slrh = schema[0] as SpoolableBuffer;
            var slrc = schema[1] as SpoolableBuffer;
            var slrs = schema[2] as SpoolableBuffer;

            var count = 0; // number of localisation entries
            var lookupTable = new List<int>();

            // read header
            using (var f = slrh.GetMemoryStream())
            {
                count = f.ReadInt32();

                var unk1 = f.ReadInt32();
                var unk2 = f.ReadInt32();

                if (unk1 != 0 || unk2 != 2)
                    throw new InvalidOperationException("Unknown localisation package header!");

                Reserved = f.ReadInt32();
            }

            Localisations = new List<LocalisationData>(count);

            // read lookup
            using (var f = slrc.GetMemoryStream())
            {
                for (int i = 0; i < count; i++)
                {
                    var offset = f.ReadInt32();
                    var id = f.ReadInt32();

                    // add offset to lookup
                    lookupTable.Add(offset);

                    Localisations.Add(new LocalisationData() {
                        ID = id
                    });
                }
            }

            // add the buffer size so we don't need weird hacks
            lookupTable.Add(slrs.Size);

            // read data
            using (var f = slrs.GetMemoryStream())
            {
                for (int i = 0; i < count; i++)
                {
                    var offset = lookupTable[i];
                    var nextOffset = lookupTable[i + 1];

                    var size = (nextOffset - offset);

                    f.Position = offset;

                    // strings are unicode
                    Localisations[i].String = f.ReadUnicodeString(size);
                }
            }
        }

        protected override void Save()
        {
            Spooler = new SpoolablePackage() {
                Alignment   = SpoolerAlignment.Align4096,
                Context     = SpooledLocalisationRoot,
                Version     = 0,
                Description = "Spooled Localisation Root"
            };

            var slrh = new SpoolableBuffer() {
                Alignment   = SpoolerAlignment.Align16,
                Context     = SpooledLocalisationHeader,
                Version     = 1,
                Description = "Spooled Localisation Header"
            };

            Spooler.Children.Add(slrh);

            var slrc = new SpoolableBuffer() {
                Alignment   = SpoolerAlignment.Align16,
                Context     = SpooledLocalisationLookup,
                Version     = 0,
                Description = "Spooled Localisation Lookup"
            };

            Spooler.Children.Add(slrc);

            var slrs = new SpoolableBuffer() {
                Alignment   = SpoolerAlignment.Align16,
                Context     = SpooledLocalisationStrings,
                Version     = 0,
                Description = "Spooled Localisation Strings"
            };

            Spooler.Children.Add(slrs);

            var count = Localisations.Count;

            // write header
            using (var f = new MemoryStream(0x10))
            {
                f.Write(count);

                f.Write(0);
                f.Write(2);

                f.Write(Reserved);

                slrh.SetBuffer(f.ToArray());
            }

            // size of localisation strings buffer
            var bufferSize = 0;

            // write lookup
            using (var f = new MemoryStream(count * 0x8))
            {
                for (int i = 0; i < count; i++)
                {
                    var localisation = Localisations[i];

                    f.Write(bufferSize);
                    f.Write(localisation.ID);

                    // add length of string + null terminator
                    bufferSize += (localisation.String.Length + 1);
                }

                slrc.SetBuffer(f.ToArray());
            }

            // write localisation strings
            using (var f = new MemoryStream(bufferSize))
            {
                // write as unicode strings
                foreach (var localisation in Localisations)
                    f.Write(Encoding.Unicode.GetBytes(localisation.String));

                slrs.SetBuffer(f.ToArray());
            }
        }
    }
}
