﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.Win32;

using DSCript;
using DSCript.Models;

namespace DSCript
{
    public sealed class FormattedPath
    {
        public string Name { get; }
        public string Directory { get; set; }

        public string FullPath => Path.Combine(Directory, Name);

        public static implicit operator string(FormattedPath path) { return path.ToString(); }
        public static implicit operator FormattedPath(string path) { return new FormattedPath(path); }
        public string Format(string arg) { return String.Format(FullPath, arg); }
        public string Format(string directory, object arg)
        {
            return String.Format(Path.Combine(directory, Name), arg);
        }
        public override string ToString() { return FullPath; }
        internal FormattedPath(string fileFormat) { Name = fileFormat; Directory = String.Empty; }
        internal FormattedPath(string fileFormat, params string[] folder)
        {
            Name = fileFormat;
            Directory = Path.Combine(folder);
        }
    }

    public sealed class Driv3r
    {
        public enum City
        {
            Istanbul,
            Miami,
            Nice,

            Unknown
        }

        public enum CityDefinitionType
        {
            CITY_DAY_PC,
            CITY_NIGHT_PC
        }

        public enum VVVType
        {
            Default,
            ArticTruck,
            Bike,
            Boat
        }

        enum VehicleType
        {
            Camaro,                 // speculation: cut very early in development; Stuntman car?
            Capri,                  // speculation: cut very early in development; Stuntman car?
            Taxi,
            Mustang_Blue,           // speculation: used in early alpha before cars had colors
            Mustang_White,          // speculation: used in early alpha before cars had colors
            Mustang_Yellow,         // speculation: used in early alpha before cars had colors
            Mustang_Grey,           // speculation: used in early alpha before cars had colors; why this became the final vehicle? who knows..
            Mustang_Red,            // speculation: used in early alpha before cars had colors
            Dougal,                 // speculation: cut very early in development; what the heck is a Dougal?! something from Stuntman?
            CopCar_Miami,
            Jalpa,
            Vantage,                // speculation: alternative version of AstonMartin?
            Citroen2CV,             // cut late in development, seen in few screenshots
            Countach,
            CopCar_Istanbul,
            DodgeTruck,
            Bora,
            Panda,
            HierarchyTest,          // speculation: used for testing 18-wheelers?
            RigTruck,
            RigTrailer,
            IstanbulTaxi,
            Challenger,
            ChevyVan,
            MiamiFordPickup,
            CitroenCX,
            Starion,                // speculation: cut late in development (it's an unreleased cut car, so we'll never know)
            BMW507,
            Datsun240Z,
            ChevyBelaire,
            IstanbulFordPickup,
            Ducati,
            ChevyBlazer,
            Ferrari,
            TransAm,
            Bentley,
            Dumpster,
            Corvette,
            Torino,
            Miami_Bus,
            Lincoln,
            AuburnSpeedster,
            Hotrod,
            Gokart,
            VRod,
            Scarab,                 // speculation: Tanner originally had his own unique speedboat; later became the only speed boat
            Miami_Cruiser_Boat,
            Miami_Small_Boat,
            Miami_Speed_Boat,       // speculation: cut early in development - Scarab used instead?
            MetroMover,
            BMW_Alpina,
            Jaguar,
            RenaultVan,
            RenaultVanPickup,
            Forklift,
            AstonMartin,
            Citroen_DS,             // cut very early in development, seen in a couple of screenshots (as a cop car!)
            CopCar_Nice,
            Nice_Bus,
            Nice_Van,
            Nice_Truck,
            Nice_Taxi,
            Reanult_5,
            Porsche,                // speculation: cut very early in development
            SwatVan,                // speculation: cut due to time constraints, so replaced with the Sobe truck gimmick xD
            CamperVan,
            Cobra,
            Nice_SportsBike,
            Nice_Moped,
            Nice_SpeedBoat,
            Nice_Cruiser_Boat,
            Nice_Small_Boat,
            Chevy_Impala,
            Istanbul_Bus,
            RenaultAlpine,
            Wagonaire,
            MackTruck,
            Merc,
            Bugatti,
            Racer,
            GT40,
            Istanbul_StreetBike,
            Istanbul_Moped,
            Fishing_Boat,
            Big_Cruiser_Boat,
            Istanbul_SpeedBoat,
            Tram,
            Train,
            RigCarrierTrailer,
            Miami_Cop_Boat,
            Nice_Cop_Boat,
            Istanbul_Cop_Boat,
            CitroenZX,
            VW_Passat,
            MercSL500,
            Carriage,

            // these were never used/defined, so names are placeholders
            Extra1,
            Extra2,
            Extra3,
            Extra4,
            Extra5,
            Extra6,
            Extra7,

            // end of the list
            MaxVehicles,
        };

        static readonly Dictionary<int, string> VehicleNames = new Dictionary<int, string>();

        public static readonly string FileFilter;

        public static readonly string InvalidPath = "???";

        static Driv3r()
        {
            //var filters = new[] {
            //    "Driv3r|*.vvs;*.vvv;*.vgt;*.d3c;*.pcs;*.cpr;*.dam;*.map;*.gfx;*.pmu;*.d3s;*.mec;*.bnk",
            //    "Characters|*.dam",
            //    "Cities|*.d3c;*.pcs",
            //    "Fonts|*.bnk",
            //    "Guns|*.cpr",
            //    "Maps|*.map",
            //    "Menus|*.mec",
            //    "Overlays|*.gfx",
            //    "Particles|*.pmu",
            //    "Skies|*.d3s",
            //    "Vehicles|*.vvs;*.vvv;*.vgt"
            //};

            //FileFilter = String.Join("|", filters);

            FileFilter = "Driv3r|*.vvs;*.vvv;*.vgt;*.d3c;*.pcs;*.cpr;*.dam;*.map;*.gfx;*.pmu;*.d3s;*.mec;*.bnk";

            foreach (VehicleType vehicleType in Enum.GetValues(typeof(VehicleType)))
            {
                if (vehicleType == VehicleType.MaxVehicles)
                    break;

                var vehicleId = (int)vehicleType;

                VehicleNames[vehicleId] = vehicleType.ToString();
            }
        }

        public sealed class Files
        {
            public static readonly FormattedPath AnimationLookup        = new FormattedPath("AnimationLookup.txt", Folders.Animations);
            public static readonly FormattedPath SkeletonMacroList      = new FormattedPath("MacroList.txt", Folders.Animations);

            public static readonly FormattedPath MaleSkeletonMacro      = new FormattedPath("male_skeleton.skm", Folders.SkeletonMacros);
            public static readonly FormattedPath FemaleSkeletonMacro    = new FormattedPath("female_skeleton.skm", Folders.SkeletonMacros);

            public static readonly FormattedPath Guns                   = new FormattedPath("Guns.Cpr", Folders.Guns);
            public static readonly FormattedPath ParticleEffects        = new FormattedPath("Sfx.pmu", Folders.SFX);
        }

        public sealed class Folders
        {
            public static readonly string Animations                = "Anims";
            public static readonly string Cities                    = "Cities";
            public static readonly string Configs                   = "Configs";
            public static readonly string Guns                      = "Guns";
            public static readonly string Missions                  = "Missions";
            public static readonly string Moods                     = "Moods";
            public static readonly string Overlays                  = "Overlays";
            public static readonly string Saves                     = "Saves";
            public static readonly string SFX                       = "sfx";
            public static readonly string Skies                     = "Skies";
            public static readonly string Sounds                    = "Sounds";
            public static readonly string Territory                 = "Territory";
        
            public static readonly string Vehicles                  = "Vehicles";

            public static readonly string SkeletonMacros            = new FormattedPath("Skeleton_Macros", Folders.Animations);
            public static readonly string VehicleConfigs            = new FormattedPath("Vehicles\\BigVO3", Folders.Configs);
        }

        public sealed class Locale
        {
            internal static readonly string Region_FMV              = "FMV";
            internal static readonly string Region_GUI              = "GUI";
            internal static readonly string Region_Locale           = "Locale";

            internal static readonly string Region_GameConfig       = "gameconfig.txt";

            public sealed class Files
            {
                public static readonly string Fonts                 = new FormattedPath("font.bnk", Folders.Fonts);

                public static readonly string GUI_Bootup            = new FormattedPath("bootup.TXT", Folders.GUI);
                public static readonly string GUI_FilmDirector      = new FormattedPath("FilmDirector.TXT", Folders.GUI);
                public static readonly string GUI_FrontEnd          = new FormattedPath("front.TXT", Folders.GUI);
                public static readonly string GUI_Language          = new FormattedPath("language.TXT", Folders.GUI);
                public static readonly string GUI_Profiles          = new FormattedPath("MUMain.TXT", Folders.GUI);
                public static readonly string GUI_NameEntry         = new FormattedPath("Name.TXT", Folders.GUI);
                public static readonly string GUI_NoController      = new FormattedPath("nocont.TXT", Folders.GUI);
                public static readonly string GUI_PauseMenu         = new FormattedPath("Pause.TXT", Folders.GUI);
                public static readonly string GUI_ReplayPause       = new FormattedPath("RplyPause.TXT", Folders.GUI);

                public static readonly string Sounds                = new FormattedPath("SOUND.GSD", Folders.Sounds);

                public static readonly string Text_Controls         = new FormattedPath("controls.txt", Folders.Text);
                public static readonly string Text_Generic          = new FormattedPath("generic.txt", Folders.Text);
                public static readonly string Text_Overlays         = new FormattedPath("overlays.txt", Folders.Text);
            }

            public sealed class Folders
            {
                public static readonly string FMV                   = "FMV";
                public static readonly string Fonts                 = "Fonts";
                public static readonly string GUI                   = "GUI";
                public static readonly string Missions              = "Missions";
                public static readonly string Music                 = "Music";
                public static readonly string Sounds                = "Sounds";
                public static readonly string Text                  = "Text";
            }

            public sealed class Paths
            {
                public static readonly FormattedPath Subtitles      = new FormattedPath("{0}.txt", Folders.FMV);

                public static readonly FormattedPath Speech         = new FormattedPath("IGCS{0}.XA", Folders.Music);

                public static readonly FormattedPath GUILocale      = new FormattedPath("{0}.txt", Folders.GUI);
                public static readonly FormattedPath MissionLocale  = new FormattedPath("mission{0}.txt", Folders.Missions);
            }
        }

        public sealed class Paths
        {
            public static readonly FormattedPath Animations         = new FormattedPath("animation_{0}.ab3", Folders.Animations);
            public static readonly FormattedPath AnimationMacros    = new FormattedPath("{0}.anm", Folders.Animations, "macros");

            public static readonly FormattedPath CityDefinition     = new FormattedPath("{0}.d3c", Folders.Cities);

            public static readonly FormattedPath CityDayPC          = new FormattedPath("{0}_DAY_PC", Folders.Cities);
            public static readonly FormattedPath CityNightPC        = new FormattedPath("{0}_NIGHT_PC", Folders.Cities);

            public static readonly FormattedPath SoundDefinition    = new FormattedPath("{0}.DAT", Folders.Sounds);

            public static readonly FormattedPath MissionMood        = new FormattedPath("mood{0}.txt", Folders.Moods);
            public static readonly FormattedPath MissionCharacters  = new FormattedPath("mission{0}.dam", Folders.Missions);
            public static readonly FormattedPath MissionScript      = new FormattedPath("mission{0}.mpc", Folders.Missions);
            public static readonly FormattedPath MissionVehicles    = new FormattedPath("{0}.vvv", Folders.Vehicles);

            public static readonly FormattedPath OverlayBin         = new FormattedPath("{0}.bin", Folders.Overlays);
            public static readonly FormattedPath OverlayGfx         = new FormattedPath("{0}.gfx", Folders.Overlays);
            public static readonly FormattedPath OverlayMap         = new FormattedPath("{0}.map", Folders.Overlays);

            public static readonly FormattedPath SaveFile           = new FormattedPath("D3P_{0}", Folders.Saves);

            public static readonly FormattedPath SkyFile            = new FormattedPath("sky_{0}.d3s", Folders.Skies);

            public static readonly FormattedPath VehicleGlobals     = new FormattedPath("{0}\\CarGlobals{0}.vgt", Folders.Vehicles);
            public static readonly FormattedPath SpooledVehicles    = new FormattedPath("{0}.vvs", Folders.Vehicles);

            public static readonly FormattedPath VehicleConfig      = new FormattedPath("{0}.BO3", Folders.VehicleConfigs);
            public static readonly FormattedPath VehicleSounds      = new FormattedPath("{0}.VSB", Folders.Sounds);
        }

        public static readonly string DirectoryKey  = "Driv3r";
        public static bool AppendRootDirectory      = true;

        public static readonly string RootDirectory = DSC.Configuration.GetDirectory(DirectoryKey);

        public static string GetPath(string path)
        {
            return (AppendRootDirectory) ? Path.Combine(RootDirectory, path) : path;
        }

        public static string GetPathFormat(string path, string file)
        {
            return GetPath(String.Format(path, file));
        }

        public static string GetTerritoryPath(string territory)
        {
            return GetPath(Path.Combine(Folders.Territory, territory));
        }

        public static string GetTerritoryName()
        {
            try
            {
                var gIniFile = GetPath("game.ini");
                var gIni = new IniFile(gIniFile);

                return gIni.GetSections()[0];
            }
            catch (Exception)
            {
                return InvalidPath;
            }
        }

        public static string GetLocalePath(string territory, string locale = "English")
        {
            return Path.Combine(GetTerritoryPath(territory), "Locale", locale);
        }

        private static string GetCityType(City city, CityDefinitionType type)
        {
            if (city == City.Unknown)
                return null;

            switch (type)
            {
            case CityDefinitionType.CITY_DAY_PC:
                return String.Format(Paths.CityDayPC, city);
            case CityDefinitionType.CITY_NIGHT_PC:
                return String.Format(Paths.CityNightPC, city);
            default:
                return null;
            }
        }

        public static City GetCityFromFileName(string filename)
        {
            filename = filename.ToLower();

            if (filename.Contains("miami"))
                return City.Miami;
            if (filename.Contains("nice"))
                return City.Nice;
            if (filename.Contains("istanbul"))
                return City.Istanbul;
            if (filename.Contains("mission"))
            {
                var misName = Path.GetFileNameWithoutExtension(filename);
                int missionId = -1;

                if (int.TryParse(misName.Substring(misName.Length - 2, 2), out missionId))
                    return GetCityFromMissionId(missionId);
            }

            return City.Unknown;
        }

        public static City GetCityFromMissionId(int missionId)
        {
            //--Found these in the .EXE ;D
            /* === mission%02d.vvv ===
            miami: 	    01 - 10, 32-33, 38-40, 50-51, 56, 59-61, 71-72, 77-78
            nice: 	    11 - 21, 34-35, 42-44, 52-53, 57, 62-64, 73-74, 80-81
            istanbul: 	22 - 31, 36-37, 46-48, 54-55, 58, 65-67, 75-76, 83-84 */

            //-- Be careful not to break the formatting!!
            switch (missionId)
            {
            case 01: case 02: case 03: case 04: case 05: case 06: case 07:
            case 08: case 09: case 10: case 32: case 33: case 38: case 39:
            case 40: case 50: case 51: case 56: case 59: case 60: case 61:
            case 71: case 72: case 77: case 78:
                return City.Miami;
            case 11: case 12: case 13: case 14: case 15: case 16: case 17:
            case 18: case 19: case 20: case 21: case 34: case 35: case 42:
            case 43: case 44: case 52: case 53: case 57: case 62: case 63:
            case 64: case 73: case 74: case 80: case 81:
                return City.Nice;
            case 22: case 23: case 24: case 25: case 26: case 27: case 28:
            case 29: case 30: case 31: case 36: case 37: case 46: case 47:
            case 48: case 54: case 55: case 58: case 65: case 66: case 67:
            case 75: case 76: case 83: case 84:
                return City.Istanbul;
            default:
                return City.Unknown;
            }
        }

        public static string GetAnimations(City city)
        {
            if (city == City.Unknown)
                return null;

            return GetPathFormat(Paths.Animations, city.ToString().ToLower());
        }

        public static string GetAnimationMacro(string macroName)
        {
            return GetPathFormat(Paths.AnimationMacros, macroName);
        }

        public static string GetCityDefinition(City city, CityDefinitionType type)
        {
            if (city == City.Unknown)
                return null;

            var folder = GetCityType(city, type);
            return GetPathFormat(Paths.CityDefinition, folder);
        }

        public static string GetCitySuperRegions(City city, CityDefinitionType type)
        {
            if (city == City.Unknown)
                return null;

            var folder = GetCityType(city, type);
            return GetPath(Path.Combine(folder, "SuperRegions.pcs"));
        }

        public static string GetCityInteriors(City city, CityDefinitionType type)
        {
            if (city == City.Unknown)
                return null;

            var folder = GetCityType(city, type);
            return GetPath(Path.Combine(folder, "Interiors.pcs"));
        }

        public static string GetSpooledVehicles(City city)
        {
            if (city == City.Unknown)
                return null;

            return GetPathFormat(Paths.SpooledVehicles, city.ToString());
        }
        
        public static string GetMissionLocale(int missionId, string territory, string locale = "English")
        {
            return Path.Combine(GetLocalePath(territory, locale), String.Format(Locale.Paths.MissionLocale, $"{missionId:D2}"));
        }
        
        public static string GetMissionScript(int missionId)
        {
            return GetPathFormat(Paths.MissionScript, $"{missionId:D2}");
        }

        public static string GetMissionVehicles(City city)
        {
            if (city == City.Unknown)
                return null;

            return GetMissionVehicles(city, VVVType.Default);
        }

        public static string GetMissionVehicles(City city, VVVType type)
        {
            if (city == City.Unknown)
                return null;

            string path = city.ToString().ToLower();

            switch (type)
            {
            default:
            case VVVType.Default:
                break;
            case VVVType.ArticTruck:
                path = String.Format("{0}_artic_truck", path);
                break;
            case VVVType.Bike:
                path = String.Format("{0}_bike", path);
                break;
            case VVVType.Boat:
                path = String.Format("{0}_boat", path);
                break;
            }

            return GetPathFormat(Paths.MissionVehicles, path);
        }

        public static string GetMissionVehicles(int missionId)
        {
            var path = String.Format("mission{0}", missionId);

            return GetPathFormat(Paths.MissionVehicles, path);
        }

        public static string GetVehicleGlobals(City city)
        {
            if (city == City.Unknown)
                return null;

            return GetPathFormat(Paths.VehicleGlobals, city.ToString());
        }

        public static string GetVehicleGlobals(int missionId)
        {
            City city = GetCityFromMissionId(missionId);

            return (city != City.Unknown) ? GetVehicleGlobals((City)city) : null;
        }

        public static string GetVehicleTypeName(int vehicleId)
        {
            var typeName = $"{vehicleId:X2}";

            if (VehicleNames.ContainsKey(vehicleId))
                typeName = $"{VehicleNames[vehicleId]}";

            return $"VehicleType_{typeName}";
        }
    }
}
