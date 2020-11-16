using System;
using System.Runtime.InteropServices;

using System.Xml;

namespace Audiose
{
    public struct SoundSampleInfo
    {
        public static readonly int SizeOf = Marshal.SizeOf(typeof(SoundSampleInfo));

        public int Offset;
        public int Size;

        public ushort SampleRate;

        public byte Flags;
        public byte Unk_0B;

        public int Unk_0C;
    }

    public class SoundSample : ISerializer<XmlNode>
    {
        // relative path (e.g. '00.wav' and NOT 'c:\path\to\file\00.wav')
        public string FileName { get; set; }

        public int NumChannels { get; set; }
        public int SampleRate { get; set; }

        public int Flags { get; set; }

        public int ClearAfter { get; set; }
        public int Unknown2 { get; set; }

        public byte[] Buffer { get; set; }

        public bool IsPS1Format { get; set; }

        public static explicit operator AudioFormatChunk(SoundSample sample)
        {
            return new AudioFormatChunk(sample.NumChannels, sample.SampleRate);
        }

        public void Serialize(XmlNode xml)
        {
            var xmlDoc = (xml as XmlDocument) ?? xml.OwnerDocument;
            var elem = (xml as XmlElement);

            if (elem == null)
            {
                elem = xmlDoc.CreateElement("Sample");
                elem.SetAttribute("File", FileName);

                Serialize(elem);
                xml.AppendChild(elem);
            }
            else
            {
                elem.SetAttribute("NumChannels", $"{NumChannels:D}");
                elem.SetAttribute("SampleRate", $"{SampleRate:D}");

                if (!IsPS1Format)
                {
                    elem.SetAttribute("Flags", $"{Flags:D}");
                    elem.SetAttribute("ClearAfter", $"{ClearAfter:D}");
                    elem.SetAttribute("Unk2", $"{Unknown2:D}");
                }
            }
        }

        public void Deserialize(XmlNode xml)
        {
            foreach (XmlAttribute attr in xml.Attributes)
            {
                var value = attr.Value;

                switch (attr.Name)
                {
                case "File":
                    FileName = value;
                    break;
                case "NumChannels":
                    NumChannels = int.Parse(value);
                    break;
                case "SampleRate":
                    SampleRate = int.Parse(value);
                    break;
                case "Flags":
                    Flags = int.Parse(value);
                    break;
                case "Unk1": // backwards compat
                case "ClearAfter":
                    ClearAfter = int.Parse(value);
                    break;
                case "Unk2":
                    Unknown2 = int.Parse(value);
                    break;
                }
            }

            if (String.IsNullOrEmpty(FileName))
                throw new InvalidOperationException("Empty samples are NOT allowed!");
        }
    }
}
