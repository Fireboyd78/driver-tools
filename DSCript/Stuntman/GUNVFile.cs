using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Windows.Media.Media3D;

namespace DSCript.Stuntman
{
    public class GUNVFile
    {
        public class ModelData
        {
            public int Offset { get; set; }
            public int Length { get; set; }

            public ModelData()
            {
            }
        }

	    const int Version = 5;

        const int GUNV = 0x564E5547;
        const int SHAD = 0x44414853;

        const string Separator = "----------------";
        const string TSDCompiled = "TSD-COMPILED";

        public string FileName { get; private set; }

	    public int Length { get; set; }

        public Vector3DCollection Vectors { get; private set; }
        
        public List<ModelData> Models { get; set; }

	    private void Load(Stream stream)
	    {
            if (stream.ReadInt32() != GUNV)
                throw new Exception("Invalid GUNV file - bad magic!");
            if (stream.ReadInt32() != Version)
                throw new Exception("Invalid GUNV file - bad version!");

            var sb = new StringBuilder();

            sb.AppendLine("==== GUNViewer ====");

            if (!String.IsNullOrEmpty(FileName))
                sb.AppendFormat("File: {0}", FileName).AppendLine();

            sb.AppendFormat("Date: {0}", DateTime.Now).AppendLines(2);

            Length = stream.ReadInt32();

            var baseOffset = stream.Align(8);

            int unkSections = stream.ReadInt32();

            sb.AppendFormat("Length: \t0x{0:X} ({0} bytes)", Length).AppendLine();
            sb.AppendFormat("Sections?: \t{0}", unkSections).AppendLine();

            stream.Align(8);
            sb.AppendLine(Separator);

            Vectors = new Vector3DCollection(3);

            sb.Append("Vectors:");

            for (int i = 0; i < 3; i++)
            {
                var vec = new Vector3D(
                    stream.ReadSingle(),
                    stream.ReadSingle(),
                    stream.ReadSingle());

                Vectors.Add(vec);

                sb.AppendLine();

                sb.AppendFormatEx("\t[{0}].X: {1}", (i + 1), Vectors[i].X).AppendLine();
                sb.AppendFormatEx("\t[{0}].Y: {1}", (i + 1), Vectors[i].Y).AppendLine();
                sb.AppendFormatEx("\t[{0}].Z: {1}", (i + 1), Vectors[i].Z).AppendLine();
            }

            stream.Align(8);
            sb.AppendLine(Separator);

            int tsdOffset = stream.ReadInt32();
            int pdlOffset = stream.ReadInt32();
            int unkOffset = stream.ReadInt32();
            int shadOffset = stream.ReadInt32();

            stream.Seek(0x4, SeekOrigin.Current);

            sb.AppendFormat("tsdOffset:\t0x{0:X}", tsdOffset).AppendLine();
            sb.AppendFormat("pdlOffset:\t0x{0:X}", pdlOffset).AppendLine();
            sb.AppendFormat("unkOffset:\t0x{0:X}", unkOffset).AppendLine();
            sb.AppendFormat("shadOffset:\t0x{0:X}", shadOffset).AppendLine();

            sb.AppendLine(Separator);

            stream.Align(8);

            Vector3D unknownVec = new Vector3D(
                    stream.ReadSingle(),
                    stream.ReadSingle(),
                    stream.ReadSingle());

            sb.AppendFormatEx("UnknownVector.X: {0}", unknownVec.X).AppendLine();
            sb.AppendFormatEx("UnknownVector.Y: {0}", unknownVec.Y).AppendLine();
            sb.AppendFormatEx("UnknownVector.Z: {0}", unknownVec.Z).AppendLine();

            sb.AppendLine();

            // skip junk
            stream.Seek(0x3C, SeekOrigin.Current);

            sb.AppendLine("<!-- JUNK (0x3C) -->").AppendLine();

            // read model offset table stuff
            int mOffset = stream.ReadInt32();

            stream.Seek(mOffset, baseOffset);

            int mCount = stream.ReadInt32();

            sb.AppendLine("== Model Offset Table ==");
            sb.AppendFormat("Offset:\t0x{0:X}", mOffset).AppendLine();
            sb.AppendFormat("Count:\t{0}", mCount).AppendLines(2);

            Models = new List<ModelData>(mCount);

            sb.AppendLine("#\t\tOffset\t\t\t\tLength");
            
            for (int m = 0; m < mCount; m++)
            {
                var model = new ModelData() {
                    Length = stream.ReadInt32(),
                    Offset = stream.ReadInt32()
                };

                Models.Add(model);

                sb.AppendFormat("{0}\t\t0x{1:X8}\t\t\t0x{2:X} ({2} bytes)", (m + 1), model.Offset, model.Length).AppendLine();
            }

            sb.AppendLine(Separator);

            stream.Seek(tsdOffset, baseOffset);

            if (stream.ReadString(TSDCompiled.Length) != TSDCompiled)
                throw new Exception("TSD-COMPILED - bad magic!");

            stream.Align(8);

            if (stream.ReadInt32() != 4)
                throw new Exception("TSD-COMPILED - bad version!");

            stream.Align(8);

            int tsdSize = stream.ReadInt32();
            int tsdUnk1 = stream.ReadInt32();

            int t1Offset = stream.ReadInt32(); // 16-byte aligned
            int t1Size = stream.ReadInt32();

            int t2Offset = stream.ReadInt32(); // 16-byte aligned
            int t2Size = stream.ReadInt32();

            int t3Offset = stream.ReadInt32(); // 16-byte aligned
            int t3Size = stream.ReadInt32();

            int t4Size = stream.ReadInt32();
            int t4Offset = stream.ReadInt32(); // 16-byte aligned

            int tsdUnkOffset = stream.ReadInt32();

            sb.AppendLine("== TSD-COMPILED==");

            sb.AppendLine("Finished reading.");

            DSC.Log(sb.ToString());
	    }

        public GUNVFile(string path)
	    {
            using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                FileName = path;
                Load(fs);
            }
	    }
    }
}
