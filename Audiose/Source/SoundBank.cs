using System.Collections.Generic;
using System.Linq;

using System.Xml;

namespace Audiose
{
    public enum SoundBankFormat
    {
        Invalid = -1,

        BK01,
        BK31,

        CS11 = 10,
        CS12,
    }

    public interface ISoundBankInfoDetail
    {
        int HeaderSize { get; }
        int SampleSize { get; }

        int SampleChannelFlags { get; }

        void SetDataInfo(int offset, int size);

        int DataOffset { get; }
        int DataSize { get; }

        void Copy(SoundBank bank);
        void CopyTo(SoundBank bank);
    }

    public struct SoundBankInfo1 : ISoundBankInfoDetail
    {
        int ISoundBankInfoDetail.HeaderSize
        {
            get { return 0xC; }
        }

        int ISoundBankInfoDetail.SampleSize
        {
            get { return 0x14; }
        }

        int ISoundBankInfoDetail.SampleChannelFlags
        {
            get { return 0x80; }
        }

        int ISoundBankInfoDetail.DataOffset
        {
            get { return DataOffset; }
        }

        int ISoundBankInfoDetail.DataSize
        {
            get { return DataSize; }
        }

        public int NumSamples;

        public int DataOffset;
        public int DataSize;

        void ISoundBankInfoDetail.SetDataInfo(int offset, int size)
        {
            DataOffset = offset;
            DataSize = size;
        }

        public void Copy(SoundBank bank)
        {
            NumSamples = bank.Samples.Count;
        }

        public void CopyTo(SoundBank bank)
        {
            bank.Samples = new List<SoundSample>(NumSamples);
        }
    }

    public struct SoundBankInfo3 : ISoundBankInfoDetail
    {
        int ISoundBankInfoDetail.HeaderSize
        {
            get { return 0x10; }
        }

        int ISoundBankInfoDetail.SampleSize
        {
            get { return 0x10; }
        }

        int ISoundBankInfoDetail.SampleChannelFlags
        {
            get { return 0x10; }
        }

        int ISoundBankInfoDetail.DataOffset
        {
            get { return DataOffset; }
        }

        int ISoundBankInfoDetail.DataSize
        {
            get { return DataSize; }
        }

        public int Index;
        public int NumSamples;

        public int DataOffset;
        public int DataSize;

        void ISoundBankInfoDetail.SetDataInfo(int offset, int size)
        {
            DataOffset = offset;
            DataSize = size;
        }

        public void Copy(SoundBank bank)
        {
            Index = bank.Index;
            NumSamples = bank.Samples.Count;
        }

        public void CopyTo(SoundBank bank)
        {
            bank.Index = Index;
            bank.Samples = new List<SoundSample>(NumSamples);
        }
    }

    public struct CharacterSoundBankInfo : ISoundBankInfoDetail
    {
        int ISoundBankInfoDetail.HeaderSize
        {
            get { return 0x10; }
        }

        int ISoundBankInfoDetail.SampleSize
        {
            get { return 0x10; }
        }

        int ISoundBankInfoDetail.SampleChannelFlags
        {
            get { return 0x10; }
        }

        int ISoundBankInfoDetail.DataOffset
        {
            get { return DataOffset; }
        }

        int ISoundBankInfoDetail.DataSize
        {
            get { return DataSize; }
        }

        public int NumSamples;

        public int DataOffset;
        public int DataSize;

        void ISoundBankInfoDetail.SetDataInfo(int offset, int size)
        {
            DataOffset = offset;
            DataSize = size;
        }

        public void Copy(SoundBank bank)
        {
            NumSamples = bank.Samples.Count;
        }

        public void CopyTo(SoundBank bank)
        {
            bank.Samples = new List<SoundSample>(NumSamples);
        }
    }

    public class SoundBank : ISerializer<XmlNode>
    {
        public string SubDirectory { get; set; }

        public int Index { get; set; }

        public bool IsNull
        {
            get { return Index == -1; }
        }
        
        public List<SoundSample> Samples { get; set; }

        public void Serialize(XmlNode xml)
        {
            var xmlDoc = (xml as XmlDocument) ?? xml.OwnerDocument;
            var elem = (xml as XmlElement);

            if (elem == null)
            {
                var bankXml = xmlDoc.CreateElement("SoundBank");

                bankXml.SetAttribute("Index", $"{Index:D}");

                Serialize(bankXml);
                xml.AppendChild(bankXml);
            }
            else if (!IsNull)
            {
                foreach (var sample in Samples)
                {
                    var smpXml = xmlDoc.CreateElement("Sample");
                    var smpFile = sample.FileName;
                    
                    smpXml.SetAttribute("File", smpFile);

                    sample.Serialize(smpXml);
                    elem.AppendChild(smpXml);
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
                case "Index":
                    Index = int.Parse(value);
                    break;
                }
            }
            
            Samples = new List<SoundSample>();

            foreach (var node in xml.ChildNodes.OfType<XmlElement>())
            {
                var sample = new SoundSample();
                sample.Deserialize(node);
                
                Samples.Add(sample);
            }
        }
    }
}
