using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Models;

namespace DSCript
{
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

    public sealed class Driv3r
    {
        public struct City
        {
            public static readonly City Istanbul                        = "Istanbul";
            public static readonly City Miami                           = "Miami";
            public static readonly City Nice                            = "Nice";

            private string Name;
            public static implicit operator City(string name) { return new City(name); }
            public static implicit operator string(City city) { return city.ToString(); }
            public override string ToString() { return Name; }
            internal City(string name) { Name = name; }
        }

        public struct FilePath
        {
            public static readonly string AnimationLookup               = new PathFormat("AnimationLookup.txt", Folder.Animations);
            public static readonly string SkeletonMacroList             = new PathFormat("MacroList.txt", Folder.Animations);

            public static readonly string MaleSkeletonMacro             = new PathFormat("male_skeleton.skm", Folder.SkeletonMacros);
            public static readonly string FemaleSkeletonMacro           = new PathFormat("female_skeleton.skm", Folder.SkeletonMacros);

            public static readonly string Guns                          = new PathFormat("Guns.Cpr", Folder.Guns);
            public static readonly string ParticleEffects               = new PathFormat("Sfx.pmu", Folder.SFX);
        }

        public struct Folder
        {
            public static readonly Folder Animations                    = "Anims";
            public static readonly Folder Cities                        = "Cities";
            public static readonly Folder Configs                       = "Configs";
            public static readonly Folder Guns                          = "Guns";
            public static readonly Folder Missions                      = "Missions";
            public static readonly Folder Moods                         = "Moods";
            public static readonly Folder Overlays                      = "Overlays";
            public static readonly Folder Saves                         = "Saves";
            public static readonly Folder SFX                           = "sfx";
            public static readonly Folder Skies                         = "Skies";
            public static readonly Folder Sounds                        = "Sounds";
            public static readonly Folder Territory                     = "Territory";
            
            public static readonly Folder Vehicles                      = "Vehicles";

            public static readonly Folder SkeletonMacros                = new PathFormat("Skeleton_Macros", Folder.Animations);
            public static readonly Folder VehicleConfigs                = new PathFormat("Vehicles\\BigVO3", Folder.Configs);

            private string Name;
            public static implicit operator Folder(string name) { return new Folder(name); }
            public static implicit operator Folder(PathFormat format) { return new Folder(format); }
            public static implicit operator string(Folder folder) { return folder.ToString(); }
            public override string ToString() { return Name; }
            internal Folder(string name) { Name = name; }
        }

        public struct Locale
        {
            internal static readonly string Region_FMV                  = "FMV";
            internal static readonly string Region_GUI                  = "GUI";
            internal static readonly string Region_Locale               = "Locale";

            internal static readonly string Region_GameConfig           = "gameconfig.txt";

            public struct FilePath
            {
                public static readonly string Fonts                     = new Driv3r.PathFormat("font.bnk", Folder.Fonts);

                public static readonly string GUI_Bootup                = new Driv3r.PathFormat("bootup.TXT", Folder.GUI);
                public static readonly string GUI_FilmDirector          = new Driv3r.PathFormat("FilmDirector.TXT", Folder.GUI);
                public static readonly string GUI_FrontEnd              = new Driv3r.PathFormat("front.TXT", Folder.GUI);
                public static readonly string GUI_Language              = new Driv3r.PathFormat("language.TXT", Folder.GUI);
                public static readonly string GUI_Profiles              = new Driv3r.PathFormat("MUMain.TXT", Folder.GUI);
                public static readonly string GUI_NameEntry             = new Driv3r.PathFormat("Name.TXT", Folder.GUI);
                public static readonly string GUI_NoController          = new Driv3r.PathFormat("nocont.TXT", Folder.GUI);
                public static readonly string GUI_PauseMenu             = new Driv3r.PathFormat("Pause.TXT", Folder.GUI);
                public static readonly string GUI_ReplayPause           = new Driv3r.PathFormat("RplyPause.TXT", Folder.GUI);

                public static readonly string Sounds                    = new Driv3r.PathFormat("SOUND.GSD", Folder.Sounds);

                public static readonly string Text_Controls             = new Driv3r.PathFormat("controls.txt", Folder.Text);
                public static readonly string Text_Generic              = new Driv3r.PathFormat("generic.txt", Folder.Text);
                public static readonly string Text_Overlays             = new Driv3r.PathFormat("overlays.txt", Folder.Text);
            }

            public struct Folder
            {
                public static readonly Driv3r.Folder FMV                = "FMV";
                public static readonly Driv3r.Folder Fonts              = "Fonts";
                public static readonly Driv3r.Folder GUI                = "GUI";
                public static readonly Driv3r.Folder Missions           = "Missions";
                public static readonly Driv3r.Folder Music              = "Music";
                public static readonly Driv3r.Folder Sounds             = "Sounds";
                public static readonly Driv3r.Folder Text               = "Text";
            }

            public struct PathFormat
            {
                public static readonly Driv3r.PathFormat Subtitles      = new Driv3r.PathFormat("{0}.txt", Folder.FMV);

                public static readonly Driv3r.PathFormat Speech         = new Driv3r.PathFormat("IGCS{0}.XA", Folder.Music);
                public static readonly Driv3r.PathFormat Locale         = new Driv3r.PathFormat("mission{0}.txt", Folder.Missions);
            }
        }

        public struct PathFormat
        {
            public static readonly PathFormat Animations                = new PathFormat("animation_{0}.ab3", Folder.Animations);
            public static readonly PathFormat AnimationMacros           = new PathFormat("{0}.anm", Folder.Animations, "macros");
            
            public static readonly PathFormat CityDefinition            = new PathFormat("{0}.d3c", Folder.Cities);

            public static readonly PathFormat CityDayPC                 = new PathFormat("{0}_DAY_PC", Folder.Cities);
            public static readonly PathFormat CityNightPC               = new PathFormat("{0}_NIGHT_PC", Folder.Cities);

            public static readonly PathFormat SoundDefinition           = new PathFormat("{0}.DAT", Folder.Sounds);
            
            public static readonly PathFormat MissionMood               = new PathFormat("mood{0}.txt", Folder.Moods);
            public static readonly PathFormat MissionCharacters         = new PathFormat("{0}.dam", Folder.Vehicles);
            public static readonly PathFormat MissionScript             = new PathFormat("{0}.mpc", Folder.Vehicles);
            public static readonly PathFormat MissionVehicles           = new PathFormat("{0}.vvv", Folder.Vehicles);

            public static readonly PathFormat OverlayBin                = new PathFormat("{0}.bin", Folder.Overlays);
            public static readonly PathFormat OverlayGfx                = new PathFormat("{0}.gfx", Folder.Overlays);
            public static readonly PathFormat OverlayMap                = new PathFormat("{0}.map", Folder.Overlays);

            public static readonly PathFormat SaveFile                  = new PathFormat("D3P_{0}", Folder.Saves);

            public static readonly PathFormat SkyFile                   = new PathFormat("sky_{0}.d3s", Folder.Skies);

            public static readonly PathFormat VehicleGlobals            = new PathFormat("{0}\\CarGlobals{0}.vgt", Folder.Vehicles);
            public static readonly PathFormat SpooledVehicles           = new PathFormat("{0}.vvs", Folder.Vehicles);

            public static readonly PathFormat VehicleConfig             = new PathFormat("{0}.BO3", Folder.VehicleConfigs);
            public static readonly PathFormat VehicleSounds             = new PathFormat("{0}.VSB", Folder.Sounds);

            public string Format(string arg)
            {
                return String.Format(Path, arg);
            }

            private string Path;
            public static implicit operator string(PathFormat path) { return path.ToString(); }
            public static implicit operator PathFormat(string path) { return new PathFormat(path); }
            public override string ToString() { return Path; }
            internal PathFormat(string fileFormat) { Path = fileFormat; }
            internal PathFormat(string fileFormat, params string[] folder) : this(System.IO.Path.Combine(System.IO.Path.Combine(folder), fileFormat)) {}
        }

        public const string DirectoryKey            = "Driv3r";
        public static bool AppendRootDirectory      = true;

        public static readonly string RootDirectory = DSC.Configuration.GetDirectory(DirectoryKey);

        internal static string GetDirectory(string path)
        {
            return (AppendRootDirectory) ? Path.Combine(RootDirectory, path) : path;
        }

        internal static string GetCityType(City city, CityDefinitionType type)
        {
            switch (type)
            {
            case CityDefinitionType.CITY_DAY_PC:
                return PathFormat.CityDayPC.Format(city);
            case CityDefinitionType.CITY_NIGHT_PC:
                return PathFormat.CityNightPC.Format(city);
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
                var path = Path.GetFileNameWithoutExtension(filename);
                int missionId = -1;

                if (int.TryParse(path.Substring(path.Length - 2, 2), out missionId))
                    return GetCityFromMissionId(missionId);
            }

            return null;
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
            case 01: case 02: case 03: case 04: case 05: case 06:
            case 07: case 08: case 09: case 10: case 32: case 33:
            case 38: case 39: case 40: case 50: case 51: case 56:
            case 59: case 60: case 61: case 71: case 72:
                return City.Miami;
            case 11: case 12: case 13: case 14: case 15: case 16:
            case 17: case 18: case 19: case 20: case 21: case 34:
            case 35: case 42: case 43: case 44: case 52: case 53:
            case 57: case 62: case 63: case 64: case 73: case 74:
                return City.Nice;
            case 22: case 23: case 24: case 25: case 26: case 27:
            case 28: case 29: case 30: case 31: case 36: case 37:
            case 46: case 47: case 48: case 54: case 55: case 58:
            case 65: case 66: case 67: case 75: case 76:
                return City.Istanbul;
            default:
                return null;
            }
        }

        public static string GetAnimations(City city)
        {
            return GetDirectory(PathFormat.Animations.Format(((string)city).ToLower()));
        }

        public static string GetAnimationMacro(string macroName)
        {
            return GetDirectory(PathFormat.AnimationMacros.Format(macroName));
        }

        public static string GetCityDefinition(City city, CityDefinitionType type)
        {
            var folder = GetCityType(city, type);
            return GetDirectory(PathFormat.CityDefinition.Format(folder));
        }

        public static string GetCitySuperRegions(City city, CityDefinitionType type)
        {
            var folder = GetCityType(city, type);
            return GetDirectory(Path.Combine(folder, "SuperRegions.pcs"));
        }

        public static string GetCityInteriors(City city, CityDefinitionType type)
        {
            var folder = GetCityType(city, type);
            return GetDirectory(Path.Combine(folder, "Interiors.pcs"));
        }

        public static string GetSpooledVehicles(City city)
        {
            return GetDirectory(PathFormat.SpooledVehicles.Format(city));
        }

        public static string GetMissionVehicles(City city)
        {
            return GetMissionVehicles(city, VVVType.Default);
        }

        public static string GetMissionVehicles(City city, VVVType type)
        {
            string path = ((string)city).ToLower();

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

            return GetDirectory(PathFormat.MissionVehicles.Format(path));
        }

        public static string GetMissionVehicles(int missionId)
        {
            var path = String.Format("mission{0}", missionId);

            return GetDirectory(PathFormat.MissionVehicles.Format(path));
        }

        public static string GetVehicleGlobals(City city)
        {
            return GetDirectory(PathFormat.VehicleGlobals.Format(city));
        }

        public static string GetVehicleGlobals(int missionId)
        {
            var city = GetCityFromMissionId(missionId);
            return (city != null) ? GetVehicleGlobals(city) : null;
        }
    }
}
