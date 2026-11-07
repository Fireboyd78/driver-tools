using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace Zartex
{
    public enum MissionCityType : int
    {
        Miami_Day        = 1,
        Miami_Night      = 2,

        Nice_Day         = 3,
        Nice_Night       = 4,

        Istanbul_Day     = 5,
        Istanbul_Night   = 6
    }

    public class MissionSummaryData : SpoolableResource<SpoolableBuffer>
    {
        private const int DensityDataMagic = 0x55264524;

        public bool HasDensityData { get; set; }

        public double[] StartPosition = { 0.0f, 0.0f };

        public short MissionId { get; set; }
        public short MissionLocaleId { get; set; }

        public MissionCityType CityType { get; set; }

        public bool DensityOverride { get; set; }

        /// <summary>The first int after the override flag: ambient traffic density (the game default is 7).</summary>
        public int TrafficDensity { get; set; }

        /// <summary>The second int: street parked-car density (the game default is 1).</summary>
        public int StreetParkedCarDensity { get; set; }

        [Obsolete("This is the first int, the traffic density. Use TrafficDensity.")]
        public int ParkedCarDensity
        {
            get { return TrafficDensity; }
            set { TrafficDensity = value; }
        }

        [Obsolete("This is the second int, the street parked-car density. Use StreetParkedCarDensity.")]
        public int AttractorParkedCarDensity
        {
            get { return StreetParkedCarDensity; }
            set { StreetParkedCarDensity = value; }
        }

        public double PingInRadius { get; set; }
        public double PingOutRadius { get; set; }
        
        public string GetSummaryAsString(bool forceDensityData = false)
        {
            var sb = new StringBuilder();

            sb.AppendLine("-- Mission Summary Data --");
            sb.AppendLine("StartPosition: [{0:F6}, {1:F6}]", StartPosition[0], StartPosition[1]);
            sb.AppendLine("CityType: {0}", CityType);
            sb.AppendLine("MissionId: {0}", MissionId);
            sb.AppendLine("MissionLocaleId: {0}", MissionLocaleId);

            if (HasDensityData || forceDensityData)
            {
                sb.AppendLine();

                sb.AppendLine("-- Density Override Data --");
                sb.AppendLine("Overrides enabled: {0}", DensityOverride);
                sb.AppendLine("TrafficDensity: {0}", TrafficDensity);
                sb.AppendLine("StreetParkedCarDensity: {0}", StreetParkedCarDensity);
                sb.AppendLine("PingInRadius: {0:F6}", PingInRadius);
                sb.AppendLine("PingOutRadius: {0:F6}", PingOutRadius);
            }

            return sb.ToString();
        }

        protected override void Load()
        {
            using (var f = Spooler.GetMemoryStream())
            {
                StartPosition[0] = f.ReadSingle();
                StartPosition[1] = f.ReadSingle();

                CityType = (MissionCityType)f.ReadInt32();

                // NOTE: Inconsistent usage of these variables!
                MissionId = f.ReadInt16();
                MissionLocaleId = f.ReadInt16();

                HasDensityData = (Spooler.Size > 0x10);

                // new format or old format?
                if (HasDensityData && (f.ReadInt32() == DensityDataMagic))
                {
                    // value in first byte, rest is padding
                    DensityOverride = ((f.ReadInt32() & 0xFF) == 1) ? true : false;

                    TrafficDensity = f.ReadInt32();
                    StreetParkedCarDensity = f.ReadInt32();

                    PingInRadius = f.ReadSingle();
                    PingOutRadius = f.ReadSingle();
                }
                else
                {
                    // this is how the game handles the old format
                    // overrides are still disabled, however
                    TrafficDensity = 7;
                    StreetParkedCarDensity = 1;

                    PingInRadius = 130.0;
                    PingOutRadius = 135.0;
                }
            }
        }

        protected override void Save()
        {
            int bufSize = (HasDensityData) ? 0x28 : 0x10;
            
            var mBuffer = new byte[bufSize];

            using (var fM = new MemoryStream(mBuffer))
            {
                fM.WriteFloat(StartPosition[0]);
                fM.WriteFloat(StartPosition[1]);

                fM.Write((int)CityType);

                fM.Write(MissionId);
                fM.Write(MissionLocaleId);

                if (HasDensityData)
                {
                    fM.Write(DensityDataMagic);

                    // combine value and padding into one
                    fM.Write((0xCCCCCC << 8) | ((DensityOverride) ? 1 : 0));

                    fM.Write(TrafficDensity);
                    fM.Write(StreetParkedCarDensity);

                    fM.WriteFloat(PingInRadius);
                    fM.WriteFloat(PingOutRadius);
                }
            }

            Spooler.SetBuffer(mBuffer);
        }
    }
}
