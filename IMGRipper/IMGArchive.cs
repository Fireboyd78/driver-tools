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

namespace IMGRipper
{
    public static class CustomHasher
    {
        public static bool PSPMode = false;

        public static uint GetHash(string value)
        {
            var hash = 0u;
            var str = value.ToUpper();

            if (PSPMode)
            {
                for (int i = str.Length - 1; i >= 0; i--)
                {
                    var c = (byte)str[i];

                    var h = (hash << 7) + (hash << 1);

                    h += hash;
                    h += c;

                    hash = h;
                }
            }
            else
            {
                // jenkins' one-at-a-time
                for (int i = 0; i < str.Length; i++)
                {
                    var c = str[i];

                    if (c == 0)
                        break;

                    hash = (hash + c) * 1025;
                    hash = hash ^ (hash >> 6);
                }

                hash += (hash * 8);
                hash = ((hash >> 11) ^ hash) * 32769;
            }

            return hash;
        }
    }

    public class IMGArchive
    {
        public class Entry
        {
            // was FileName set directly?
            private bool _hasFilename;

            private string _fileName;
            private uint _fileNameHash;
            
            public string FileName
            {
                get { return _fileName; }
                set
                {
                    _hasFilename = true;

                    _fileName = value;
                    _fileNameHash = CustomHasher.GetHash(value);
                }
            }

            public uint FileNameHash
            {
                get { return _fileNameHash; }
                set
                {
                    _hasFilename = false;

                    _fileNameHash = value;
                    _fileName = $"{_fileNameHash:X8}";
                }
            }

            public virtual long FileOffset
            {
                get { return (Offset * 2048L); }
                set { Offset = (int)(value / 2048L); }
            }

            public bool HasFileName
            {
                get { return _hasFilename; }
            }
            
            public int Offset { get; set; }
            public int Length { get; set; }

            public override sealed int GetHashCode()
            {
                // hopefully this will prevent any unwanted behavior
                if (_fileNameHash == 0)
                    return base.GetHashCode();

                return (int)_fileNameHash;
            }
        }

        public class XBoxEntry : Entry
        {
            public int LumpIndex { get; set; }
        }

        public class PSPEntry : Entry
        {
            // no calculation needed
            public override long FileOffset
            {
                get { return Offset; }
                set { Offset = (int)value; }
            }

            // ???
            public int UncompressedLength { get; set; }

            public bool IsCompressed
            {
                get { return (Length != UncompressedLength); }
            }
        }

        public class FileDescriptor
        {
            public string FileName { get; set; }
            
            public int Index { get; set; }
            public int DataIndex { get; set; }

            public Entry EntryData { get; set; }
        }
        
        public int Reserved { get; set; }
        public List<Entry> Entries { get; set; }

        public bool IsLoaded { get; protected set; }

        public IMGVersion Version { get; set; }
        
        private static readonly Dictionary<uint, string> LookupTable =
            new Dictionary<uint, string>();

        private static readonly Dictionary<uint, string> HashLookup =
            new Dictionary<uint, string>();

        private List<String> HACK_CompileDriv3rFiles()
        {
            var knownFiles = new List<String>() {
                "US_GAMECONFIG.TXT",
                "EU_GAMECONFIG.TXT",
                "AS_GAMECONFIG.TXT",

                "ANIMS\\ANIMATION_MIAMI.AB3",
                "ANIMS\\ANIMATION_NICE.AB3",
                "ANIMS\\ANIMATION_ISTANBUL.AB3",

                "ANIMS\\ANIMATIONLOOKUP.TXT",
                "ANIMS\\MACROLIST.TXT",

                "ANIMS\\MACROS\\FEMALE_DIE_BACKWARDS.ANM",
                "ANIMS\\MACROS\\FEMALE_DIE_FORWARDS.ANM",
                "ANIMS\\MACROS\\FEMALE_DIE_LEFT.ANM",
                "ANIMS\\MACROS\\FEMALE_IDLE_LOOK_WATCH.ANM",
                "ANIMS\\MACROS\\FEMALE_IDLE_SCRATCH.ANM",
                "ANIMS\\MACROS\\FEMALE_IDLE_STANDING.ANM",
                "ANIMS\\MACROS\\FEMALE_IDLE_STAND_SIT_C.ANM",
                "ANIMS\\MACROS\\FEMALE_IDLE_TAP_FOOT.ANM",
                "ANIMS\\MACROS\\FEMALE_IDLE_THINK.ANM",
                "ANIMS\\MACROS\\FEMALE_IDLE_WEIGHT_SHIFT.ANM",
                "ANIMS\\MACROS\\FEMALE_IGCS_M08B_MID_CAL_01.ANM",
                "ANIMS\\MACROS\\FEMALE_RUN.ANM",
                "ANIMS\\MACROS\\FEMALE_WALK.ANM",
                "ANIMS\\MACROS\\MALE_2HANDED_SHOOT_CROUCHING_AHEAD.ANM",
                "ANIMS\\MACROS\\MALE_2HANDED_SHOOT_CROUCHING_AHEAD_V2.ANM",
                "ANIMS\\MACROS\\MALE_2HANDED_SHOOT_CROUCHING_LEFT.ANM",
                "ANIMS\\MACROS\\MALE_2HANDED_SHOOT_CROUCHING_UP.ANM",
                "ANIMS\\MACROS\\MALE_2HANDED_SHOOT_CROUCHING_UP_A.ANM",
                "ANIMS\\MACROS\\MALE_ARRESTED.ANM",
                "ANIMS\\MACROS\\MALE_BIKE_BALE.ANM",
                "ANIMS\\MACROS\\MALE_CRAWL.ANM",
                "ANIMS\\MACROS\\MALE_CROUCH_TO_STAND.ANM",
                "ANIMS\\MACROS\\MALE_CROUCH_TURN.ANM",
                "ANIMS\\MACROS\\MALE_DIE_BACKWARDS.ANM",
                "ANIMS\\MACROS\\MALE_DIE_FALL.ANM",
                "ANIMS\\MACROS\\MALE_DIE_FALL_GET_UP.ANM",
                "ANIMS\\MACROS\\MALE_DIE_FLAIL.ANM",
                "ANIMS\\MACROS\\MALE_DIE_FORWARDS.ANM",
                "ANIMS\\MACROS\\MALE_DIE_LEFT.ANM",
                "ANIMS\\MACROS\\MALE_DIE_RIGHT.ANM",
                "ANIMS\\MACROS\\MALE_DODGE_LEFT.ANM",
                "ANIMS\\MACROS\\MALE_DODGE_LEFT_LEAP.ANM",
                "ANIMS\\MACROS\\MALE_DODGE_LEFT_ROLL.ANM",
                "ANIMS\\MACROS\\MALE_DODGE_RIGHT.ANM",
                "ANIMS\\MACROS\\MALE_DRAW_GUN_BADDIES.ANM",
                "ANIMS\\MACROS\\MALE_GET_UP_BACK.ANM",
                "ANIMS\\MACROS\\MALE_HIT_BY_CAR.ANM",
                "ANIMS\\MACROS\\MALE_IDLE_BREATHING_TANNER.ANM",
                "ANIMS\\MACROS\\MALE_IDLE_BREATHING_WIDER_STANCE.ANM",
                "ANIMS\\MACROS\\MALE_IDLE_LOOK_WATCH.ANM",
                "ANIMS\\MACROS\\MALE_IDLE_PRESS_BUTTON.ANM",
                "ANIMS\\MACROS\\MALE_IDLE_SCRATCH.ANM",
                "ANIMS\\MACROS\\MALE_IDLE_STANDING.ANM",
                "ANIMS\\MACROS\\MALE_IDLE_STAND_SIT_C.ANM",
                "ANIMS\\MACROS\\MALE_IDLE_TAP_FOOT.ANM",
                "ANIMS\\MACROS\\MALE_IDLE_THINK.ANM",
                "ANIMS\\MACROS\\MALE_IDLE_WEIGHT_SHIFT.ANM",
                "ANIMS\\MACROS\\MALE_IGCS_MOBILE_ANSWER.ANM",
                "ANIMS\\MACROS\\MALE_IGCS_WALK_STOP.ANM",
                "ANIMS\\MACROS\\MALE_JOG.ANM",
                "ANIMS\\MACROS\\MALE_JOG_BACKWARDS.ANM",
                "ANIMS\\MACROS\\MALE_JOG_BACKWARDS_STRAFE_LEFT_45.ANM",
                "ANIMS\\MACROS\\MALE_JOG_BACKWARDS_STRAFE_RIGHT_45.ANM",
                "ANIMS\\MACROS\\MALE_JOG_STRAFE_LEFT.ANM",
                "ANIMS\\MACROS\\MALE_JOG_STRAFE_LEFT_45.ANM",
                "ANIMS\\MACROS\\MALE_JOG_STRAFE_RIGHT.ANM",
                "ANIMS\\MACROS\\MALE_JOG_STRAFE_RIGHT_45.ANM",
                "ANIMS\\MACROS\\MALE_JUMP_AIR.ANM",
                "ANIMS\\MACROS\\MALE_JUMP_AIR_THREE.ANM",
                "ANIMS\\MACROS\\MALE_JUMP_AIR_TWO.ANM",
                "ANIMS\\MACROS\\MALE_JUMP_FORWARD_END.ANM",
                "ANIMS\\MACROS\\MALE_JUMP_FORWARD_START.ANM",
                "ANIMS\\MACROS\\MALE_JUMP_ON_SPOT_START.ANM",
                "ANIMS\\MACROS\\MALE_JUMP_RUN_END.ANM",
                "ANIMS\\MACROS\\MALE_JUMP_RUN_LEFT_45_END.ANM",
                "ANIMS\\MACROS\\MALE_JUMP_RUN_LEFT_45_START.ANM",
                "ANIMS\\MACROS\\MALE_JUMP_RUN_RIGHT_45_END.ANM",
                "ANIMS\\MACROS\\MALE_JUMP_RUN_RIGHT_45_START.ANM",
                "ANIMS\\MACROS\\MALE_JUMP_RUN_START.ANM",
                "ANIMS\\MACROS\\MALE_LOOK_DOWN.ANM",
                "ANIMS\\MACROS\\MALE_LOOK_LEFT.ANM",
                "ANIMS\\MACROS\\MALE_LOOK_RIGHT.ANM",
                "ANIMS\\MACROS\\MALE_LOOK_UP.ANM",
                "ANIMS\\MACROS\\MALE_PISTOL_SHOOT_CROUCHING_AHEAD.ANM",
                "ANIMS\\MACROS\\MALE_PISTOL_SHOOT_CROUCHING_AHEAD_V2.ANM",
                "ANIMS\\MACROS\\MALE_PISTOL_SHOOT_CROUCHING_LEFT.ANM",
                "ANIMS\\MACROS\\MALE_PISTOL_SHOOT_CROUCHING_UP.ANM",
                "ANIMS\\MACROS\\MALE_PISTOL_SHOOT_CROUCHING_UP_A.ANM",
                "ANIMS\\MACROS\\MALE_PISTOL_SHOOT_FORWARDS_AHEAD.ANM",
                "ANIMS\\MACROS\\MALE_PISTOL_SHOOT_FORWARDS_AHEAD_V2.ANM",
                "ANIMS\\MACROS\\MALE_PISTOL_SHOOT_FORWARDS_LEFT.ANM",
                "ANIMS\\MACROS\\MALE_PISTOL_SHOOT_FORWARDS_RIGHT.ANM",
                "ANIMS\\MACROS\\MALE_PISTOL_SHOOT_FORWARDS_UP.ANM",
                "ANIMS\\MACROS\\MALE_PISTOL_SHOOT_FORWARDS_UP_A.ANM",
                "ANIMS\\MACROS\\MALE_RELOAD_2HANDED.ANM",
                "ANIMS\\MACROS\\MALE_RELOAD_2HANDED_RIFLE.ANM",
                "ANIMS\\MACROS\\MALE_RELOAD_2HANDED_RIFLE_CROUCHING.ANM",
                "ANIMS\\MACROS\\MALE_RELOAD_LAUNCHER.ANM",
                "ANIMS\\MACROS\\MALE_RELOAD_SINGLE.ANM",
                "ANIMS\\MACROS\\MALE_RELOAD_SINGLE_CROUCH.ANM",
                "ANIMS\\MACROS\\MALE_ROLL_RUN.ANM",
                "ANIMS\\MACROS\\MALE_ROLL_RUN_LEFT_45.ANM",
                "ANIMS\\MACROS\\MALE_ROLL_RUN_RIGHT_45.ANM",
                "ANIMS\\MACROS\\MALE_RUN.ANM",
                "ANIMS\\MACROS\\MALE_RUN_BACKWARDS.ANM",
                "ANIMS\\MACROS\\MALE_RUN_BACKWARDS_STRAFE_LEFT_45.ANM",
                "ANIMS\\MACROS\\MALE_RUN_BACKWARDS_STRAFE_RIGHT_45.ANM",
                "ANIMS\\MACROS\\MALE_RUN_ROLL_END.ANM",
                "ANIMS\\MACROS\\MALE_RUN_ROLL_END_LEFT_45.ANM",
                "ANIMS\\MACROS\\MALE_RUN_ROLL_END_RIGHT_45.ANM",
                "ANIMS\\MACROS\\MALE_RUN_ROLL_START.ANM",
                "ANIMS\\MACROS\\MALE_RUN_ROLL_START_LEFT_45.ANM",
                "ANIMS\\MACROS\\MALE_RUN_ROLL_START_RIGHT_45.ANM",
                "ANIMS\\MACROS\\MALE_RUN_STRAFE_LEFT.ANM",
                "ANIMS\\MACROS\\MALE_RUN_STRAFE_LEFT_45.ANM",
                "ANIMS\\MACROS\\MALE_RUN_STRAFE_RIGHT.ANM",
                "ANIMS\\MACROS\\MALE_RUN_STRAFE_RIGHT_45.ANM",
                "ANIMS\\MACROS\\MALE_SEDAN_FWD_BALE.ANM",
                "ANIMS\\MACROS\\MALE_SMASH_OPEN_DOOR.ANM",
                "ANIMS\\MACROS\\MALE_SPORTS_BALE.ANM",
                "ANIMS\\MACROS\\MALE_SWIM.ANM",
                "ANIMS\\MACROS\\MALE_TAKE_HIT_BACKWARDS.ANM",
                "ANIMS\\MACROS\\MALE_TREAD_WATER.ANM",
                "ANIMS\\MACROS\\MALE_TRUCK_BALE.ANM",
                "ANIMS\\MACROS\\MALE_TURN.ANM",
                "ANIMS\\MACROS\\MALE_TURN_WIDER_STANCE.ANM",
                "ANIMS\\MACROS\\MALE_TWO_HANDED_SHOOT_FORWARDS_AHEAD.ANM",
                "ANIMS\\MACROS\\MALE_TWO_HANDED_SHOOT_FORWARDS_AHEAD_2.ANM",
                "ANIMS\\MACROS\\MALE_TWO_HANDED_SHOOT_FORWARDS_LEFT.ANM",
                "ANIMS\\MACROS\\MALE_TWO_HANDED_SHOOT_FORWARDS_RIGHT.ANM",
                "ANIMS\\MACROS\\MALE_TWO_HANDED_SHOOT_FORWARDS_UP.ANM",
                "ANIMS\\MACROS\\MALE_TWO_HANDED_SHOOT_FORWARDS_UP_A.ANM",
                "ANIMS\\MACROS\\MALE_WALK.ANM",
                "ANIMS\\MACROS\\MALE_WALK_BACKWARDS.ANM",
                "ANIMS\\MACROS\\MALE_WALK_BACKWARDS_STRAFE_LEFT_45.ANM",
                "ANIMS\\MACROS\\MALE_WALK_BACKWARDS_STRAFE_RIGHT_45.ANM",
                "ANIMS\\MACROS\\MALE_WALK_STRAFE_LEFT.ANM",
                "ANIMS\\MACROS\\MALE_WALK_STRAFE_LEFT_45.ANM",
                "ANIMS\\MACROS\\MALE_WALK_STRAFE_RIGHT.ANM",
                "ANIMS\\MACROS\\MALE_WALK_STRAFE_RIGHT_45.ANM",

                "ANIMS\\SKELETON_MACROS\\FEMALE_SKELETON.SKM",
                "ANIMS\\SKELETON_MACROS\\MALE_SKELETON.SKM",

                "FMV\\CONTROLSCREENBG.XMV",
                "FMV\\FRONTEND.XMV",
                "FMV\\MAINMENU.XMV",
                "FMV\\PLACEHOLDER.XMV",
                "FMV\\UNDERCOVER.XMV",
                "FMV\\VIEWCUTSCENE.XMV",
                
                "GUNS\\GUNS.CPR",

                "MISSIONS\\PERSONALITIES\\DEFAULT.ACP",

                "OVERLAYS\\CHASE_TRAIN.BIN",
                "OVERLAYS\\DG_GENERAL.BIN",
                "OVERLAYS\\DG_GETAWAY.BIN",
                "OVERLAYS\\DG_QUICKCHASE.BIN",
                "OVERLAYS\\TAKE_A_RIDE.BIN",

                "OVERLAYS\\OVERLAYS.GFX",

                "OVERLAYS\\MIAMI.MAP",
                "OVERLAYS\\NICE.MAP",
                "OVERLAYS\\ISTANBUL.MAP",
                "OVERLAYS\\TRAIN_MI.MAP",

                "SKIES\\SKY_ABDUCTION.D3S",
                "SKIES\\SKY_ALLEYWAY.D3S",
                "SKIES\\SKY_ANOTHERLEAD.D3S",
                "SKIES\\SKY_ARMSDEAL.D3S",
                "SKIES\\SKY_ARTICWAGON.D3S",
                "SKIES\\SKY_BOMBTRUCK.D3S",
                "SKIES\\SKY_CALITAINTROUBLE.D3S",
                "SKIES\\SKY_CALITASTAIL.D3S",
                "SKIES\\SKY_CHASETHETRAIN.D3S",
                "SKIES\\SKY_COUNTDOWN.D3S",
                "SKIES\\SKY_DODGEISLAND.D3S",
                "SKIES\\SKY_HUNTED.D3S",
                "SKIES\\SKY_IMPRESSLOMAZ.D3S",
                "SKIES\\SKY_IMPRESSLOMAZSUNSET.D3S",
                "SKIES\\SKY_ITAKERIDEDUSKR.D3S",
                "SKIES\\SKY_ITAKRIDEDAWNR.D3S",
                "SKIES\\SKY_LITTLEHAVANA.D3S",
                "SKIES\\SKY_MOUNTAINCHASE.D3S",
                "SKIES\\SKY_MTAKERIDEDAY.D3S",
                "SKIES\\SKY_MTAKERIDENIGHTR.D3S",
                "SKIES\\SKY_NTAKERIDEDAWNR.D3S",
                "SKIES\\SKY_NTAKERIDEDAYR.D3S",
                "SKIES\\SKY_POLICEHQ.D3S",
                "SKIES\\SKY_RESCUEDUBOIS.D3S",
                "SKIES\\SKY_RETRIBUTION.D3S",
                "SKIES\\SKY_ROOFTOPS.D3S",
                "SKIES\\SKY_SMASHANDRUN.D3S",
                "SKIES\\SKY_SMUGGLERS.D3S",
                "SKIES\\SKY_SPEED.D3S",
                "SKIES\\SKY_SUNRISETAKERIDE.D3S",
                "SKIES\\SKY_SURVEILLANCE.D3S",
                "SKIES\\SKY_TANNERESCAPES.D3S",
                "SKIES\\SKY_THECHASE.D3S",
                "SKIES\\SKY_THESIEGE.D3S",
                "SKIES\\SKY_TRAPPED.D3S",

                "SOUNDS\\FEMUSIC.DAT",
            };

            // Cities
            var d3cTypes = new[] {
                "PS2",
                "XBOX",
            };

            var cities = new[] {
                "MIAMI",
                "NICE",
                "ISTANBUL",
            };

            foreach (var type in d3cTypes)
            {
                foreach (var city in cities)
                {
                    knownFiles.Add(String.Format("CITIES\\{0}_DAY_{1}.D3C", city, type));
                    knownFiles.Add(String.Format("CITIES\\{0}_NIGHT_{1}.D3C", city, type));
                }

                knownFiles.Add(String.Format("CITIES\\TRAIN_MI_DAY_{0}.D3C", type));
            }

            // vehicle stuff
            foreach (var city in cities)
            {
                knownFiles.Add(String.Format("CONFIGS\\VEHICLES\\BIGVO3\\{0}.BO3", city));
                knownFiles.Add(String.Format("SOUNDS\\{0}.VSB", city));

                knownFiles.Add(String.Format("VEHICLES\\{0}\\CARGLOBALS{0}.VGT", city));

                knownFiles.Add(String.Format("VEHICLES\\{0}.VVS", city));
                knownFiles.Add(String.Format("VEHICLES\\{0}.VVV", city));
                knownFiles.Add(String.Format("VEHICLES\\{0}_ARTIC_TRUCK.VVV", city));
                knownFiles.Add(String.Format("VEHICLES\\{0}_BIKE.VVV", city));
                knownFiles.Add(String.Format("VEHICLES\\{0}_BOAT.VVV", city));
            }

            // FMVs
            var fmvFiles = new[] {
                "FMV\\ATTRACT",

                "FMV\\LEGAL_AS",
                "FMV\\LEGAL_EU",
                "FMV\\LEGAL_US",

                "FMV\\RECAP",
                "FMV\\REFLECT",

                "FMV\\T3RED_EU",
                "FMV\\T3RED_US",

                "FMV\\THEMAKINGOF",

                "FMV\\SHADOWOPS_EU",
                "FMV\\SHADOWOPS_US",
            };

            foreach (var fmv in fmvFiles)
            {
                knownFiles.Add(String.Format("{0}.SBN", fmv));
                knownFiles.Add(String.Format("{0}.XMV", fmv));
            }

            var dgCities = new[] {
                "MIAMI",
                "NICE",
                "BUL",
            };

            foreach (var dgCity in dgCities)
            {
                knownFiles.Add(String.Format("FMV\\{0}CHASE.XMV", dgCity));
                knownFiles.Add(String.Format("FMV\\{0}CHECKPNT.XMV", dgCity));
                knownFiles.Add(String.Format("FMV\\{0}GATERACE.XMV", dgCity));
                knownFiles.Add(String.Format("FMV\\{0}GETAWAY.XMV", dgCity));
                knownFiles.Add(String.Format("FMV\\{0}SURVIVAL.XMV", dgCity));
                knownFiles.Add(String.Format("FMV\\{0}TRAILBLZ.XMV", dgCity));
            }

            var selCities = new[] {
                "MIAMI",
                "NICE",
                "ISTAN",
            };

            var selTimes = new[] {
                "DAWN",
                "DAY",
                "DUSK",
                "NIGHT",
            };

            foreach (var city in selCities)
            {
                for (int i = 1; i <= 30; i++)
                    knownFiles.Add(String.Format("FMV\\{0}_CAR{1:D2}.XMV", city, i));

                foreach (var time in selTimes)
                {
                    knownFiles.Add(String.Format("FMV\\{0}_{1}_DRY.XMV", city, time));
                    knownFiles.Add(String.Format("FMV\\{0}_{1}_OVR.XMV", city, time));
                    knownFiles.Add(String.Format("FMV\\{0}_{1}_WET.XMV", city, time));
                }
            }

            for (int i = 1; i <= 200; i++)
            {
                knownFiles.Add(String.Format("FMV\\IGCS{0:D2}.XMV", i));
                knownFiles.Add(String.Format("FMV\\IGCS{0:D2}.SBN", i));
            }

            var scFiles = new[] {
                "01",
                "02",
                "03",
                "04",
                "05a", "05b",
                "06",
                "07",
                "08",
                "09",
                "10", "10b",
                "11",
                "12",
                "13a", "13b",
                "14",
                "15",
                "16",
                "17a", "17b",
                "18a", "18b",
                "19",
                "20",
                "21",
                "22",
                "23",
                "24",
                "25a", "25b",
                "26",
                "27",
                "28",
                "29",
                "30",
                "31",
            };

            foreach (var sc in scFiles)
            {
                knownFiles.Add(String.Format("FMV\\SC{0}.XMV", sc));
                knownFiles.Add(String.Format("FMV\\SC{0}.SBN", sc));
            }

            // Missions
            var missionExts = new[] {
                "MPC", // PC (just in case)
                "MPS", // PS2
                "MXB", // XBox
            };

            // mission scripts
            foreach (var ext in missionExts)
            {
                for (int i = 1; i <= 200; i++)
                    knownFiles.Add(String.Format("MISSIONS\\MISSION{0:D2}.{1}", i, ext));

                // leftover scripts that may still exist
                foreach (var city in cities)
                {
                    knownFiles.Add(String.Format("MISSIONS\\TAKEARIDE{0}.{1}", city, ext));
                    knownFiles.Add(String.Format("MISSIONS\\TEMPLATE{0}.{1}", city, ext));
                }
            }

            // recordings
            var padFiles = new[] {
                "M02_COP",
            };

            foreach (var padFile in padFiles)
                knownFiles.Add(String.Format("MISSIONS\\RECORDINGS\\{0}", padFile));

            for (int i = 1; i <= 100; i++)
            {
                var misName = String.Format("MISSION{0:D2}", i);

                knownFiles.Add(String.Format("MISSIONS\\PERSONALITIES\\{0}.ACP", misName));
                knownFiles.Add(String.Format("MISSIONS\\{0}.DAM", misName));

                knownFiles.Add(String.Format("VEHICLES\\{0}.VVV", misName));
            }

            for (int i = 1; i <= 80; i++)
                knownFiles.Add(String.Format("MOODS\\MOOD{0:D2}.TXT", i));

            // GUI
            var guiFiles = new[] {
                "GUI\\BOOTUP\\BOOTUP",
                "GUI\\FD\\FILMDIRECTOR",
                "GUI\\MAIN\\FRONT",
                "GUI\\MUGAME\\MUGAME",
                "GUI\\MUMAIN\\MUMAIN",
                "GUI\\NAME\\NAME",
                "GUI\\NOCONT\\NOCONT",

                "GUI\\PAUSE\\D3PAUSE",
                "GUI\\PAUSE\\DGPAUSE",
                "GUI\\PAUSE\\LMPAUSE",
                "GUI\\PAUSE\\RPLYPAUSE",
                "GUI\\PAUSE\\TARPAUSE",

                // PS2 only
                "GUI\\BOOTUP\\BOOTUP_US",
                "GUI\\FD\\FILMDIRECTOR_US",
                "GUI\\MAIN\\FRONT_US",
                "GUI\\MUGAME\\MUGAME_US",
                "GUI\\MUMAIN\\MUMAIN_US",
                "GUI\\NAME\\NAME_US",
                "GUI\\NOCONT\\NOCONT_US",

                "GUI\\PAUSE\\D3PAUSE_US",
                "GUI\\PAUSE\\DGPAUSE_US",
                "GUI\\PAUSE\\LMPAUSE_US",
                "GUI\\PAUSE\\RPLYPAUSE_US",
                "GUI\\PAUSE\\TARPAUSE_US",

                // XBox only
                "GUI\\XBLIVE_FD\\XBLIVE_FD",
                "GUI\\XBLIVE_FD\\XBLPAUSE",
            };

            foreach (var guiFile in guiFiles)
                knownFiles.Add(String.Format("{0}.MEC", guiFile));

            var langs = new[] {
                "ENGLISH",
                "CHINESE",
                "TAIWANESE",
                "KOREAN",
                "JAPANESE",
                "SPANISH",
                "ITALIAN",
                "GERMAN",
                "FRENCH",
            };

            foreach (var lang in langs)
            {
                knownFiles.Add(String.Format("LOCALE\\{0}\\FONTS\\FONT.BNK", lang));

                foreach (var guiFile in guiFiles)
                    knownFiles.Add(String.Format("LOCALE\\{0}\\{1}.TXT", lang, guiFile));

                for (int i = 1; i <= 100; i++)
                    knownFiles.Add(String.Format("LOCALE\\{0}\\MISSIONS\\MISSION{1:D2}.TXT", lang, i));

                for (int i = 1; i <= 200; i++)
                {
                    knownFiles.Add(String.Format("LOCALE\\{0}\\FMV\\IGCS{1:D2}.TXT", lang, i));
                    knownFiles.Add(String.Format("LOCALE\\{0}\\MUSIC\\IGCS{1:D2}.XA", lang, i));
                }

                foreach (var fmv in fmvFiles)
                    knownFiles.Add(String.Format("LOCALE\\{0}\\{1}.TXT", lang, fmv));

                foreach (var sc in scFiles)
                    knownFiles.Add(String.Format("LOCALE\\{0}\\FMV\\SC{1}.TXT", lang, sc));

                knownFiles.Add(String.Format("LOCALE\\{0}\\TEXT\\CONTROLS.TXT", lang));
                knownFiles.Add(String.Format("LOCALE\\{0}\\TEXT\\GENERIC.TXT", lang));
                knownFiles.Add(String.Format("LOCALE\\{0}\\TEXT\\OVERLAYS.TXT", lang));

                knownFiles.Add(String.Format("LOCALE\\{0}\\SOUNDS\\SOUND.GSD", lang));
            }

            knownFiles.Sort();

            return knownFiles;
        }

        private List<String> HACK_CompileDriverPLFiles()
        {
            var knownFiles = new List<String>() {
                "TERRITORIES.TXT",

                "ANIMS\\NYC.AN4",
                "ANIMS\\NYC_NOW.AN4",

                "CHARACTERS\\NYC.SP",

                "CITIES\\AIEXPORT",
                "CITIES\\LEVELS.TXT",

                "FMV\\EXTRAS\\GPTRAILER.XMV",
                "FMV\\EXTRAS\\BOCRASHES.XMV",
                "FMV\\EXTRAS\\BOCHASES.XMV",
                "FMV\\EXTRAS\\INTERVIEW.XMV",

                "FONTS\\70S\\FONT.BNK",
                "FONTS\\90S\\FONT.BNK",

                "INPUT\\FRONTEND.TXT",
                "INPUT\\SIMULATION.TXT",
                "INPUT\\VISUALS.TXT",

                "MUSIC\\A.XA",
                "MUSIC\\B.XA",
                "MUSIC\\C.XA",
                "MUSIC\\CITY.XA",
                "MUSIC\\D.XA",
                "MUSIC\\E.XA",
                "MUSIC\\F.XA",
                "MUSIC\\G.XA",
                "MUSIC\\H.XA",

                "SFX\\ENVIRONMENT_MAP.PMU",
                "SFX\\PARTICLEEFFECTS.PPX",
                "SFX\\RENDERER.PMU",

                "VEHICLES\\NYC.VGT",
                "VEHICLES\\NYC_VEHICLES.SP",

                "VEHICLES\\TEST_NYC_VEHICLES.SP",

                "VEHICLES\\VEHICLES.TXT",
                "VEHICLES\\VVARS.TXT",
            };

            var igcsFiles = new[] {
                "MUSIC\\01_2_TUT_NAG_01A",
                "MUSIC\\01_2_TUT_NAG_01B",
                "MUSIC\\01_2_TUT_NAG_01C",

                "MUSIC\\01_2_TUT_NAG_02A",
                "MUSIC\\01_2_TUT_NAG_02B",
                "MUSIC\\01_2_TUT_NAG_02C",
            };

            var guiFiles = new[] {
                "GUI\\DEV",
                "GUI\\FRONT",
                "GUI\\FRONTEND",
                "GUI\\OPTIONS",

                "GUI\\PAUSE70S",
                "GUI\\PAUSE90S",

                "GUI\\TRPAUSE70S",

                "GUI\\VETUT70S",

                "GUI\\VEGARAGE70S",
                "GUI\\VEGARAGE90S",
            };

            var platforms = new[] {
                "PS2",
                "XBOX",
            };

            var eras = new[] {
                "NOW",
                "THEN",
            };

            var territories = new[] {
                "AMERICAS",
                "ASIA",
                "JAPAN",
                "EUROPE",
            };

            var langs = new[] {
                "ENGLISH",
                "CHINESE",
                "KOREAN",
                "JAPANESE",
                "SPANISH",
                "ITALIAN",
                "FRENCH",
                "GERMAN",
                "PORTUGUESE",
            };

            var moods = new[] {
                "DAY",
                "NIGHT",
            };
            
            foreach (var era in eras)
            {
                foreach (var platform in platforms)
                    knownFiles.Add(String.Format("CITIES\\NYC_{0}_{1}.D4C", era, platform));

                knownFiles.Add(String.Format("LIFEEVENTS\\NYC_{0}_MISSIONS.SP", era));
                knownFiles.Add(String.Format("LITTER\\{0}\\LITTER.D4L", era));

                int m = 1;
                int mEnd = 5;

                foreach (var mood in moods)
                {
                    while (m <= mEnd)
                    {
                        knownFiles.Add(String.Format("MOODS\\{0:D2}_{1}_{2}_BEGINNING_MOOD.TXT", m, era, mood));
                        knownFiles.Add(String.Format("MOODS\\{0:D2}_{1}_{2}_EARLY_MOOD.TXT", m, era, mood));
                        knownFiles.Add(String.Format("MOODS\\{0:D2}_{1}_{2}_MOOD.TXT", m, era, mood));
                        knownFiles.Add(String.Format("MOODS\\{0:D2}_{1}_{2}_LATE_MOOD.TXT", m, era, mood));
                        knownFiles.Add(String.Format("MOODS\\{0:D2}_{1}_{2}_END_MOOD.TXT", m, era, mood));

                        ++m;
                    }

                    mEnd *= 2;
                }

                knownFiles.Add(String.Format("MOODS\\DAYNIGHTCYCLE_{0}.TXT", era));

                knownFiles.Add(String.Format("OVERLAYS\\HUD-{0}.BIN", era));
                knownFiles.Add(String.Format("OVERLAYS\\HUD-{0}.GFX", era));

                knownFiles.Add(String.Format("OVERLAYS\\NYC_{0}.MAP", era));

                knownFiles.Add(String.Format("SKIES\\{0}\\DYNAMIC_SKY_DOME.PKG", era));

                knownFiles.Add(String.Format("SOUNDS\\NYC_{0}.VSB", era));
            }

            foreach (var terr in territories)
            {
                knownFiles.Add(String.Format("TERRITORY\\{0}\\GAMECONFIG.TXT", terr));

                knownFiles.Add(String.Format("TERRITORY\\{0}\\FMV\\LEGAL.XMV", terr));

                foreach (var guiFile in guiFiles)
                {
                    knownFiles.Add(String.Format("TERRITORY\\{0}\\{1}.MEC", terr, guiFile));
                    knownFiles.Add(String.Format("TERRITORY\\{0}\\{1}.MEM", terr, guiFile));
                }

                foreach (var lang in langs)
                {
                    knownFiles.Add(String.Format("TERRITORY\\{0}\\LOCALE\\{1}\\FONTS\\FONT.BNK", terr, lang));

                    knownFiles.Add(String.Format("TERRITORY\\{0}\\LOCALE\\{1}\\FONTS\\70S\\FONT.BNK", terr, lang));
                    knownFiles.Add(String.Format("TERRITORY\\{0}\\LOCALE\\{1}\\FONTS\\90S\\FONT.BNK", terr, lang));

                    foreach (var guiFile in guiFiles)
                        knownFiles.Add(String.Format("TERRITORY\\{0}\\LOCALE\\{1}\\{2}.TXT", terr, lang, guiFile));

                    knownFiles.Add(String.Format("TERRITORY\\{0}\\LOCALE\\{1}\\SOUNDS\\CHRSOUND.DAT", terr, lang));
                    knownFiles.Add(String.Format("TERRITORY\\{0}\\LOCALE\\{1}\\SOUNDS\\SOUND.GSD", terr, lang));
                    knownFiles.Add(String.Format("TERRITORY\\{0}\\LOCALE\\{1}\\SOUNDS\\SOUND.SP", terr, lang));

                    knownFiles.Add(String.Format("TERRITORY\\{0}\\LOCALE\\{1}\\TEXT\\CONTROLS.TXT", terr, lang));
                    knownFiles.Add(String.Format("TERRITORY\\{0}\\LOCALE\\{1}\\TEXT\\GENERIC.TXT", terr, lang));
                    knownFiles.Add(String.Format("TERRITORY\\{0}\\LOCALE\\{1}\\TEXT\\NETWORK.TXT", terr, lang));
                    knownFiles.Add(String.Format("TERRITORY\\{0}\\LOCALE\\{1}\\TEXT\\OVERLAYS.TXT", terr, lang));
                    knownFiles.Add(String.Format("TERRITORY\\{0}\\LOCALE\\{1}\\TEXT\\VEHNAMES.TXT", terr, lang));
                    
                    knownFiles.Add(String.Format("TERRITORY\\{0}\\LOCALE\\{1}\\LIFEEVENTS\\NYC_NOW_MISSION_TEXT.SP", terr, lang));
                    knownFiles.Add(String.Format("TERRITORY\\{0}\\LOCALE\\{1}\\LIFEEVENTS\\NYC_THEN_MISSION_TEXT.SP", terr, lang));
                }
            }

            knownFiles.Sort();

            return knownFiles;
        }

        private List<String> HACK_CompileDriver76Files()
        {
            var knownFiles = new List<String>() {
                "GS.PAK",

                "ANIMS\\NYC.AN4.HDRX",

                "Characters\\CharsThen.sp",
                "Characters\\CharsNow.sp",
                "CHARACTERS\\CHARSTHEN.SP.HDRX",
                "CHARACTERS\\CHARSNOW.SP.HDRX",
                
                "CITIES\\GS_ARENA_BRONX_PSP.D4C",
                "CITIES\\GS_HUNTS_POINT_PSP.D4C",
                "CITIES\\GS_JERSEY_PSP.D4C",
                "CITIES\\GS_RIKERS_PSP.D4C",

                "CITIES\\GS_ARENA_BRONX_PSP.D4C.HDRX",
                "CITIES\\GS_HUNTS_POINT_PSP.D4C.HDRX",
                "CITIES\\GS_JERSEY_PSP.D4C.HDRX",
                "CITIES\\GS_RIKERS_PSP.D4C.HDRX",

                "Cities\\NYC_THEN_PSP.d4c",
                "Cities\\nyc_then_multiplayer.d4c",

                "Development\\DebugMenus\\MainDebugMenu.txt",
                "Development\\DebugMenus\\PersistentValues.txt",
                "Development\\DebugMenus\\Locations.txt",
                "Development\\DebugMenus\\BuildInfo.txt",
                "Development\\DebugMenus\\LifeSystem.txt",
                "Development\\DebugMenus\\MoodSettings.txt",
                "Development\\DebugMenus\\PropSettingsMenu.txt",
                "Development\\DebugMenus\\LodDistances.txt",
                "Development\\DebugMenus\\WeaponSettings70.txt",
                "Development\\DebugMenus\\WeaponSettings00.txt",
                "Development\\DebugMenus\\LightingMenu.txt",

                "FMV\\EXTRAS\\BOCHASES.XMV",
                "FMV\\EXTRAS\\BOCRASHES.XMV",
                "FMV\\EXTRAS\\GPTRAILER.XMV",
                "FMV\\EXTRAS\\INTERVIEW.XMV",

                "FRONTEND\\UNLOCKABLES\\UNLOCKABLES_POLAROID.TGA",
                "FrontEnd\\AnimSaveIcon.tga",
                "FrontEnd\\Backgrounds.TEX",
                "FrontEnd\\Backgrounds\\full_bg_1.tga",
                "FrontEnd\\Backgrounds\\full_bg_12.tga",
                "FrontEnd\\Backgrounds\\full_bg_3.tga",
                "FrontEnd\\Backgrounds\\full_bg_9.tga",
                "FrontEnd\\Backgrounds\\garageBG_02.tga",
                "FrontEnd\\Backgrounds\\splash_screen_bg.tga",
                "FrontEnd\\Backgrounds_part2.TEX",
                "FrontEnd\\Collectables.TEX",
                "FrontEnd\\Collectables\\MissionInfo.bin",
                "FrontEnd\\Collectables\\awards.bin",
                "FrontEnd\\Collectables\\collectables.bin",
                "FRONTEND\\FONT0.FNT",
                "FRONTEND\\FONTS.TEX",
                "FRONTEND\\FONTSMALL.FNT",
                "FrontEnd\\MainMaps.TEX",
                "FrontEnd\\Maps\\mapscreen_mainmap_01.tga",
                "FrontEnd\\Maps\\mapscreen_mainmap_02.tga",
                "FrontEnd\\Maps\\mapscreen_mainmap_03.tga",
                "FrontEnd\\Maps\\mapscreen_mainmap_04.tga",
                "FrontEnd\\Maps\\mapscreen_mainmap_05.tga",
                "FrontEnd\\Maps\\mapscreen_mainmap_06.tga",
                "FrontEnd\\Maps\\mapscreen_mainmap_07.tga",
                "FrontEnd\\Maps\\mapscreen_mainmap_08.tga",
                "FrontEnd\\Maps\\mapscreen_mainmap_09.tga",
                "FrontEnd\\Maps\\mapscreen_mainmap_10.tga",
                "FrontEnd\\Maps\\mapscreen_mainmap_11.tga",
                "FrontEnd\\Maps\\mapscreen_mainmap_12.tga",
                "FrontEnd\\NonResident.TEX",
                "FrontEnd\\Resident.TEX",
                "FrontEnd\\SHADEMAP_DAY.TGA",
                "FrontEnd\\Sound\\FESound.Pack",
                "FrontEnd\\Vehicles\\vehmod.bin",
                "FrontEnd\\Weapons\\ar10.DMDL",
                "FrontEnd\\Weapons\\grenadelauncher.DMDL",
                "FrontEnd\\Weapons\\magnum.DMDL",
                "FrontEnd\\Weapons\\revolver.DMDL",
                "FrontEnd\\Weapons\\shotgun.DMDL",
                "FrontEnd\\Weapons\\uzi.DMDL",
                "FrontEnd\\fonts.TEX",
                "FrontEnd\\icons_large_42grid.tga",
                "FrontEnd\\icons_small_32grid_orange.tga",
                "FrontEnd\\icons_small_32grid_yellow.tga",
                "FrontEnd\\localised\\main_headers.tga",
                "FrontEnd\\localised\\main_headers_02.tga",
                "FrontEnd\\localised\\main_headers_03.tga",
                "FrontEnd\\localised\\tradingCard_apparel.tga",
                "FrontEnd\\localised\\tradingCard_badges.tga",
                "FrontEnd\\localised\\tradingCard_keyfobs.tga",
                "FrontEnd\\localised\\tradingCard_landmarks.tga",
                "FrontEnd\\localised\\tradingCard_paints.tga",
                "FrontEnd\\localised\\tradingCard_vinyl.tga",
                "FrontEnd\\master_elements.tga",
                "FrontEnd\\psp_assets.tga",
                "FrontEnd\\startup_logos.tga",
                "FrontEnd\\track_HuntsPoint.tga",
                "FrontEnd\\track_Kearny.tga",
                "FrontEnd\\track_LaGuardia.tga",
                "FrontEnd\\unlockables.TEX",
                "Frontend\\Music\\FEMusic.AT3",

                "LifeEvents\\Carnage_02.sp",
                "LifeEvents\\Carnage_03.sp",
                "LifeEvents\\Circuit01.sp",
                "LifeEvents\\Circuit01PS.sp",
                "LifeEvents\\Circuit02.sp",
                "LifeEvents\\Circuit02PS.sp",
                "LifeEvents\\Circuit03.sp",
                "LifeEvents\\Circuit03PS.sp",
                "LifeEvents\\Sprint_01.sp",
                "LifeEvents\\Sprint_02.sp",
                "LifeEvents\\Sprint_03.sp",
                "LifeEvents\\Sprint_04.sp",
                "LifeEvents\\Sprint_05.sp",
                "LifeEvents\\Street_01.sp",
                "LifeEvents\\Street_02.sp",
                "LifeEvents\\Street_03.sp",
                "LifeEvents\\Street_04.sp",
                "LifeEvents\\Street_05.sp",
                "LifeEvents\\Street_06.sp",
                "LifeEvents\\Street_07.sp",
                "LifeEvents\\Street_08.sp",
                "LifeEvents\\Street_09.sp",
                "LifeEvents\\Street_10.sp",
                "LifeEvents\\Street_11.sp",
                "LifeEvents\\Street_12.sp",
                "LifeEvents\\Survival.sp",

                "LIFEEVENTS\\NETWORK.TXT",

                "LifeEvents\\test_nyc_then_missions.sp",
                "LifeEvents\\nyc_then_missions_NED.sp",

                "LifeEvents\\dev\\demo.sp",
                "LifeEvents\\dev\\incidentalsnow.sp",
                "LifeEvents\\dev\\incidentalsthen.sp",
                "LifeEvents\\Now\\base\\Base_Bear_Cage.mpc",
                "LifeEvents\\Now\\base\\Base_Bishop.mpc",
                "LifeEvents\\Now\\base\\Base_Corrigan.mpc",
                "LifeEvents\\Now\\base\\Base_Gate_Crasher.mpc",
                "LifeEvents\\Now\\base\\Base_Incidentals_Now.mpc",
                "LifeEvents\\Now\\base\\Base_Pot_10.mpc",
                "LifeEvents\\Now\\base\\Base_Pot_11.mpc",
                "LifeEvents\\Now\\base\\Base_Pot_12.mpc",
                "LifeEvents\\Then\\base\\Base_Incidentals_Then.mpc",
                "LifeEvents\\Then\\base\\base_01.mpc",
                "LifeEvents\\Then\\base\\base_Demo.mpc",

                "MOODS\\01_MORNING_MOOD.TXT",
                "MOODS\\02_DAY_MOOD.TXT",
                "MOODS\\03_DUSK_MOOD.TXT",
                "MOODS\\04_NIGHT_MOOD.TXT",
                "MOODS\\DAYNIGHTCYCLE_THEN.TXT",

                "OVERLAYS\\HUD-THEN.BIN.HDRX",
                "OVERLAYS\\HUD-THEN.GFX.HDRX",
                "OVERLAYS\\NYC_THEN.MAP.HDRX",
                
                "SFX\\PARTICLEEFFECTS.PPX.HDRX",
                "SFX\\SFX.PMU.HDRX",

                "SKIES\\THEN\\DYNAMIC_SKY_DOME.PKG.HDRX",

                "SOUNDS\\GAMEDATA.DAT.HDRX",
                "SOUNDS\\NYC_THEN.VSB.HDRX",

                "VEHICLES\\NYC_VEHICLES.SP.HDRX",
            };

            return knownFiles;
        }
        
        private bool _hashLookupCompiled = false;

        private bool HACK_CompileHashLookup()
        {
            // run once
            if (_hashLookupCompiled)
                return true;

            if (Version == IMGVersion.PSP)
                CustomHasher.PSPMode = true;

            // temporary hack
            var knownFiles = new List<String>() {
                "GAMECONFIG.TXT",

                "SYSTEM.CNF",

                "FMV\\ATARI.XMV",
                "FMV\\CREDITS.XMV",

                "FONTS\\FONT.BNK",

                "OVERLAYS\\LOADING.GFX",
                "OVERLAYS\\WHITE.GFX",
                
                "SFX\\SFX.PMU",
                
                "SOUNDS\\GAMEDATA.DAT",
                "SOUNDS\\MENU.DAT",
            };

            // Music
            for (int i = 1; i <= 100; i++)
                knownFiles.Add(String.Format("MUSIC\\{0:D2}.XA", i));

            knownFiles.AddRange(HACK_CompileDriv3rFiles());
            knownFiles.AddRange(HACK_CompileDriverPLFiles());
            knownFiles.AddRange(HACK_CompileDriver76Files());
            
            var sb = new StringBuilder();

            var psp = (Version == IMGVersion.PSP);

            foreach (var entry in knownFiles)
            {
                var name = entry.ToUpper();

                if (psp)
                {
                    if (name[0] != '\\')
                        name = $"\\{name}";
                }

                var key = CustomHasher.GetHash(name);

                if (HashLookup.ContainsKey(key))
                {
                    if (HashLookup[key] != name)
                    {
                        Program.WriteVerbose("WARNING: Hash conflict for '{0}' -> '{1}'", name, HashLookup[key]);
                        continue;
                    }

                    Program.WriteVerbose("WARNING: Skipping duplicate entry '{0}'", name);
                    continue;
                }

                sb.AppendLine($"0x{key:X8},{name}");
                HashLookup.Add(key, name);
            }

            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "out_files.txt"), sb.ToString());

            _hashLookupCompiled = true;
            return true;
        }

        private string GetFileNameFromHash(uint filenameHash)
        {
            if (HACK_CompileHashLookup() && HashLookup.ContainsKey(filenameHash))
            {
                var name = HashLookup[filenameHash];
                return name;
            }

            return null;
        }

        private void SetEntryFileName(Entry entry, uint hash)
        {
            var filename = GetFileNameFromHash(hash);

            if (filename != null)
                entry.FileName = filename;
            else
                entry.FileNameHash = hash;
        }
        
        private byte[] GetDecryptedData(Stream stream, int version)
        {
            var length = (version > 2) ? stream.ReadInt32() : (Entries.Capacity * 0x38);
            var buffer = new byte[length];

            stream.Read(buffer, 0, length);

            byte decKey = 27;

            if (version > 2)
            {
                byte key = 21;

                for (int i = 0, offset = 1; i < length; i++, offset++)
                {
                    buffer[i] -= decKey;
                    decKey += key;
                    
                    if (offset > 6 && key == 21)
                    {
                        offset = 0;
                        key = 11;
                    }
                    if (key == 11 && offset > 24)
                    {
                        offset = 0;
                        key = 21;
                    }
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    buffer[i] -= decKey;
                    decKey += 11;
                }
            }
            
            return buffer;
        }
        
        private unsafe void ReadIMGEntries(Stream stream, int version)
        {
            var buffer = GetDecryptedData(stream, version);
            var count = Entries.Capacity;
#if DEBUG
            File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, "temp.dat"), buffer);
#endif
            switch (version)
            {
            case 2:
                {
                    for (int i = 0; i < count; i++)
                    {
                        var entry = new Entry();

                        fixed (byte* p = buffer)
                        {
                            var ptr = (byte*)(p + (i * 0x38));

                            entry.FileName = new String((sbyte*)ptr);
                            entry.Offset = *(int*)(ptr + 0x30);
                            entry.Length = *(int*)(ptr + 0x34);
                        }

                        Entries.Add(entry);
                    }
                }
                break;
            case 3:
                {
                    for (int i = 0; i < count; i++)
                    {
                        var entry = new Entry();

                        fixed (byte* p = buffer)
                        {
                            var ptr = (byte*)(p + (i * 0xC));

                            entry.FileName = new String((sbyte*)(p + *(int*)(ptr)));
                            entry.Offset = *(int*)(ptr + 0x4);
                            entry.Length = *(int*)(ptr + 0x8);
                        }

                        Entries.Add(entry);
                    }
                }
                break;
            case 4:
                {
                    // icky hack
                    if (stream.Length < 0x8000)
                        goto IMG4_XBOX;

                    for (int i = 0; i < count; i++)
                    {
                        var entry = new Entry();

                        fixed (byte* p = buffer)
                        {
                            var ptr = (byte*)(p + (i * 0xC));
                            var hash = *(uint*)ptr;

                            SetEntryFileName(entry, hash);

                            entry.Offset = *(int*)(ptr + 0x4);
                            entry.Length = *(int*)(ptr + 0x8);
                        }

                        Entries.Add(entry);
                    }
                }
                break;
                IMG4_XBOX:
                {
                    for (int i = 0; i < count; i++)
                    {
                        var entry = new XBoxEntry();

                        fixed (byte* p = buffer)
                        {
                            var ptr = (byte*)(p + (i * 0xC));
                            var hash = *(uint*)ptr;

                            SetEntryFileName(entry, hash);

                            var data = *(int*)(ptr + 0x4);

                            entry.LumpIndex = data & 0xFF;
                            entry.Offset = (data >> 8) & 0xFFFFFF;

                            entry.Length = *(int*)(ptr + 0x8);
                        }

                        Entries.Add(entry);
                    }
                }
                break;
            }
        }
        
        private void LoadFile(string filename)
        {
            using (var fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var type = fs.ReadInt32();

                var version = -1;
                var count = 0;
                
                if ((type & 0xFFFFFF) == 0x474D49)
                {
                    version = (type >> 24) - 0x30;
                    Version = (version >= 2 && version <= 4) ? (IMGVersion)version : IMGVersion.Unknown;

                    if (Version == IMGVersion.Unknown)
                        throw new Exception("Failed to load IMG file - unsupported version!");

                    count = fs.ReadInt32();
                    Reserved = fs.ReadInt32();
                }
                else
                {
                    // PSP archive?
                    count = type;

                    for (int i = 0; i < 3; i++)
                    {
                        if (fs.ReadInt32() != count)
                        {
                            // nope
                            throw new InvalidOperationException("Could not determine archive type!");
                        }
                    }
                    
                    Version = IMGVersion.PSP;
                    version = (int)Version;
                }

                if (version == -1)
                    throw new Exception("File is not an IMG archive!");
                
                Entries = new List<Entry>(count);
                
                if (Version == IMGVersion.PSP)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var hash = fs.ReadUInt32();

                        var entry = new PSPEntry() {
                            Offset = fs.ReadInt32(),
                            Length = fs.ReadInt32(),
                            UncompressedLength = fs.ReadInt32(),
                        };
                        
                        SetEntryFileName(entry, hash);
                        
                        Entries.Add(entry);
                    }
                }
                else
                {
                    ReadIMGEntries(fs, (int)Version);
                }

                IsLoaded = true;
            }
        }

        public static bool LoadLookupTable(string lookupTable)
        {
            if (!File.Exists(lookupTable))
                Console.WriteLine("WARNING: The lookup table was not found. All files will have the extension '.bin'!");

            using (var sr = new StreamReader(File.Open(lookupTable, FileMode.Open, FileAccess.Read)))
            {
                var splitStr = new[] { "0x", "[", "]", "=", "\"" };

                if (sr.ReadLine() == "# Magic number lookup file")
                {
                    int lineNum = 1;
                    string line = "";

                    while (!sr.EndOfStream)
                    {
                        ++lineNum;

                        if (String.IsNullOrEmpty((line = sr.ReadLine())))
                            continue;

                        // Skip comments
                        if (line.StartsWith("#!"))
                        {
                            // multi-line comments
                            while (!sr.EndOfStream)
                            {
                                line = sr.ReadLine();

                                ++lineNum;

                                if (line.StartsWith("!#"))
                                    break;
                            }

                            continue;
                        }
                        else if (line.StartsWith("#"))
                            continue;

                        var strAry = line.Split(splitStr, StringSplitOptions.RemoveEmptyEntries);

                        // key, val, comment
                        if (strAry.Length < 2)
                        {
                            Console.WriteLine("ERROR: An error occurred while parsing the lookup table.");
                            Console.WriteLine("Line {0}: {1}", lineNum, line);
                            return false;
                        }

                        uint key = 0;
                        string val = strAry[1];

                        // add little-endian lookup
                        if (line.StartsWith("0x", true, null))
                        {
                            key = uint.Parse(strAry[0], NumberStyles.AllowHexSpecifier);
                        }
                        else if (line.StartsWith("["))
                        {
                            key = BitConverter.ToUInt32(Encoding.UTF8.GetBytes(strAry[0]), 0);
                        }
                        else
                        {
                            Console.WriteLine("ERROR: An error occurred while parsing the lookup table.");
                            Console.WriteLine("Line {0}: {1}", lineNum, line);
                            return false;
                        }

                        if (!LookupTable.ContainsKey(key))
                        {
                            Program.WriteVerbose("Adding [0x{0:X8}, {1}] to lookup table.", key, val);
                            LookupTable.Add(key, val);
                        }
                        else
                        {
                            Console.WriteLine("WARNING: Duplicate entry in lookup table. Skipping.");
                            Console.WriteLine("Line {0}: {1}\r\n", lineNum, line);
                        }
                    }

                    return true;
                }
                else
                {
                    Console.WriteLine("ERROR: The specified lookup table cannot be used.");
                    return false;
                }
            }
        }

        private static string GetLumpFilename(string imgFile, XBoxEntry entry)
        {
            return String.Format("{0}.L{1:D2}", Path.GetFileNameWithoutExtension(imgFile), entry.LumpIndex).ToUpper();
        }

        public static void Unpack(string filename, string outputDir)
        {
            var img = new IMGArchive();

            img.LoadFile(filename);

            var sb = new StringBuilder();
            var filesDir = Path.Combine(outputDir, "Files");

            if (!Program.ListOnly && !Directory.Exists(filesDir))
                Directory.CreateDirectory(filesDir);

            const int maxSize = 0x2C000000;

            if (!Program.ListOnly)
            {
                Console.WriteLine("Unpacking files...");

                if (!Program.VerboseLog)
                    Console.Write("Progress: ");
            }
            
            var cL = Console.CursorLeft;
            var cT = Console.CursorTop;

            var nSkip = 0;
            var idx = 1;

            var lump = -1;
            var lumpFilename = "";

            Stream stream = null;

            var fileDescriptors = new List<FileDescriptor>();

            var sbC = new StringBuilder();
            var nextOffset = 0L;

            // find memory caves
            foreach (var entry in img.Entries.OrderBy((e) => e.Offset))
            {
                if (nextOffset == 0)
                {
                    nextOffset = entry.FileOffset;
                }
                else if (entry.FileOffset > nextOffset)
                {
                    sbC.AppendLine($"**** Memory cave @ {nextOffset:X8} (size: {entry.FileOffset - nextOffset:X})");
                }

                nextOffset = MemoryHelper.Align(entry.FileOffset + entry.Length, 2048);
            }

            for (int i = 0; i < img.Entries.Count; i++)
            {
                var entry = img.Entries[i];
                
                if (!Program.VerboseLog)
                {
                    Console.SetCursorPosition(cL, cT);
                    Console.Write("{0} / {1}", (i + 1), img.Entries.Count);
                }

                if (entry is XBoxEntry)
                {
                    var xbEntry = entry as XBoxEntry;

                    if (lump != xbEntry.LumpIndex)
                    {
                        if (stream != null)
                            stream.Dispose();

                        lump = xbEntry.LumpIndex;
                        lumpFilename = GetLumpFilename(filename, xbEntry);
                        
                        stream = File.Open(Path.Combine(Path.GetDirectoryName(filename), lumpFilename), FileMode.Open, FileAccess.Read, FileShare.Read);
                    }

                }
                else if (stream == null && lump == -1)
                    stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                
                stream.Position = entry.FileOffset;

                string name = "";
                string ext = "bin";

                long length = entry.Length;
                
                if (!entry.HasFileName)
                {
                    var magic32 = stream.PeekUInt32();
                    var magic16 = (magic32 & 0xFFFF);

                    if (magic16 != 0xFEFF)
                    {
                        if (LookupTable.ContainsKey(magic32))
                            ext = LookupTable[magic32];
                    }
                    else
                    {
                        // assume unicode text file
                        ext = "txt";
                    }

                    var holdPos = stream.Position;

                    // TODO: move this to its own function
                    // (the while loop is so we don't have deeply nested if's)
                    while (ext == "bin")
                    {
                        // check for embedded .PSP archives
                        // (assuming the higher 16-bits aren't used!)
                        if (((magic32 >> 16) & 0xFFFF) == 0)
                        {
                            var isPSPFile = true;

                            for (int l = 0; l < 3; l++)
                            {
                                if (stream.ReadInt32() != magic32)
                                {
                                    isPSPFile = false;
                                    break;
                                }
                            }

                            if (isPSPFile)
                            {
                                ext = "psp";
                                break;
                            }
                        }

                        // check for xbox video
                        stream.Position = holdPos + 0xC;

                        if (stream.ReadInt32() == 0x58626F78)
                        {
                            // xbox video
                            ext = "xmv";
                            break;
                        }

                        // check for stuff we can identify by the EOF
                        stream.Position = holdPos + (length - 4);

                        var eof = stream.ReadInt32();

                        // recording file? (won't always work)
                        if (eof == 0x21524E4A)
                        {
                            ext = "pad";
                            break;
                        }
                        
                        // text file with a newline @ the end?
                        if ((((eof >> 16) & 0xFFFF) == 0x0A0D) || (((eof >> 8) & 0xFFFF) == 0x0A0D) || ((eof & 0xFFFF) == 0x0D0A))
                        {
                            ext = "txt";
                            break;
                        }

                        // possibly a TGA file w/ footer?
                        if (eof == 0x2E454C)
                        {
                            // verify the footer
                            stream.Position = holdPos + (length - 0x12);

                            if (stream.ReadString(16) == "TRUEVISION-XFILE")
                            {
                                ext = "tga";
                                break;
                            }
                        }

                        // unknown format :(
                        stream.Position = holdPos;
                        break; // BREAK THE LOOP!!!
                    }

                    if (entry is XBoxEntry)
                    {
                        name = String.Format("_UNKNOWN\\L{0:D2}\\{1}.{2}", (entry as XBoxEntry).LumpIndex, entry.FileName, ext);
                    }
                    else
                    {
                        name = String.Format("_UNKNOWN\\{0}.{1}", entry.FileName, ext);
                    }

                    // make filename uppercase
                    name = name.ToUpper();
                }
                else
                {
                    name = entry.FileName;

                    if (name.StartsWith("\\"))
                        name = name.Substring(1);

                    ext = Path.GetExtension(name);

                    // '.ext' -> 'ext' (if applicable)
                    if (!String.IsNullOrEmpty(ext))
                        ext = ext.Substring(1).ToLower();
                }

                if (entry is XBoxEntry)
                {
                    sb.AppendFormat("0x{0:X8}, 0x{1:X8}, {2} -> {3}\r\n",
                        entry.FileOffset, entry.Length,
                        GetLumpFilename(filename, entry as XBoxEntry), name);
                }
                else if (entry is PSPEntry)
                {
                    sb.AppendFormat("0x{0:X8}, 0x{1:X8}, 0x{2:X8}, 0x{3:X8} -> {4}\r\n",
                        entry.FileOffset, entry.Length, ((PSPEntry)entry).UncompressedLength, entry.FileNameHash, name);
                }
                else
                {
                    sb.AppendFormat("0x{0:X8}, 0x{1:X8}, {2}\r\n",
                        entry.FileOffset, entry.Length, name);
                }
                
                // do not write any data if it's only a listing
                if (Program.ListOnly)
                    continue;

                // add file descriptor
                fileDescriptors.Add(new FileDescriptor() {
                    EntryData = entry,
                    FileName = name,

                    Index = i,
                });

                if (Program.NoFMV && ((ext == "xmv") || (ext == "xav")))
                {
                    Program.WriteVerbose("{0}: SKIPPING -> {1}", idx++, name);
                    nSkip++;

                    continue;
                }
                
                var nDir = Path.GetDirectoryName(name);

                if (!String.IsNullOrEmpty(nDir))
                {
                    var eDir = Path.Combine(filesDir, Path.GetDirectoryName(name));

                    if (!Directory.Exists(eDir))
                        Directory.CreateDirectory(eDir);
                }

                var outName = Path.Combine(filesDir, name);

                Program.WriteVerbose("{0}: {1}", idx++, name);

                if (!Program.Overwrite && File.Exists(outName))
                    continue;

                stream.Position = entry.FileOffset;

                if (length < maxSize)
                {
                    var buffer = new byte[length];
                    stream.Read(buffer, 0, buffer.Length);

                    File.WriteAllBytes(outName, buffer);
                }
                else if (length > maxSize)
                {
                    int splitSize = (int)(length - maxSize);

                    using (var file = File.Create(outName))
                    {
                        for (int w = 0; w < maxSize; w += 0x100000)
                            file.Write(stream.ReadBytes(0x100000));

                        file.Write(stream.ReadBytes(splitSize));
                    }
                }
            }

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            if (sb.Length > 0)
                File.WriteAllText(Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(filename)}_files.txt"), sb.ToString());
#if DEBUG
            if (sbC.Length > 0)
                File.WriteAllText(Path.Combine(outputDir, "mem_caves.txt"), sbC.ToString());
#endif
            stream.Dispose();

            if (!Program.ListOnly)
            {
                // clear old data
                sb.Clear();

                // build archive config
                sb.AppendLine($"archive {Path.GetFileName(filename)}");
                sb.AppendLine($"type {(int)img.Version}");
                sb.AppendLine($"version 2");
                sb.AppendLine($"# source {filesDir}");

                sb.AppendLine();

#if USE_DATA_INDEX_SORTING
                var dataIndex = 0;
                var useSorting = false; // if Index != DataIndex for any entries

                // add data index to descriptors
                foreach (var descriptor in fileDescriptors.OrderBy((e) => e.EntryData.FileOffset))
                {
                    // fixup the data index
                    descriptor.DataIndex = dataIndex++;

                    if (!useSorting && (descriptor.Index != descriptor.DataIndex))
                        useSorting = true;
                }

                // write data descriptors by their index (implicitly)
                foreach (var descriptor in fileDescriptors)
                {
                    if (useSorting)
                        sb.Append($"{descriptor.DataIndex:D4},");

                    sb.AppendLine($"0x{descriptor.EntryData.FileNameHash:X8},{descriptor.FileName}");
                }
#else
                foreach (var descriptor in fileDescriptors.OrderBy((e) => e.EntryData.FileOffset))
                {
                    var entryData = descriptor.EntryData;

                    if (img.Version > IMGVersion.IMG3)
                    {
                        sb.Append($"0x{entryData.FileNameHash:X8},");

                        if (entryData is XBoxEntry)
                            sb.Append($"{((XBoxEntry)entryData).LumpIndex},");
                    }

                    sb.AppendLine($"{descriptor.FileName}");
                }     
#endif
                // no empty line at the end :)
                sb.Append("#EOF");
                
                // write the archive config
                File.WriteAllText(Path.Combine(outputDir, "archive.cfg"), sb.ToString());

                Console.WriteLine((!Program.VerboseLog) ? "\r\n" : "");
                Console.WriteLine("Unpacked {0} files.", (img.Entries.Count - nSkip));

                if (nSkip > 0)
                    Console.WriteLine("{0} files skipped.", nSkip);
            }
        }

        public static void BuildArchive(string configFile, string outputDir)
        {
            var inputDir = Path.GetDirectoryName(configFile);
            var filesDir = Path.Combine(inputDir, "Files");

            Console.SetBufferSize(Console.BufferWidth, 4000);

            var blacklistFile = Path.Combine(inputDir, "blacklist.txt");
            var blacklist = new List<string>();
            
            if (File.Exists(blacklistFile))
            {
                Program.WriteVerbose($"Loading blacklist: '{blacklistFile}'");

                using (var sr = new StreamReader(blacklistFile, true))
                {
                    var line = 0;

                    while (!sr.EndOfStream)
                    {
                        var curLine = sr.ReadLine();
                        ++line;

                        if (String.IsNullOrEmpty(curLine))
                            continue;

                        var blEntry = Path.Combine(filesDir, curLine);
                        
                        if (!File.Exists(blEntry))
                        {
                            Program.WriteVerbose($"Skipping invalid blacklist entry on line {line}: '{curLine}'");
                            continue;
                        }

                        blacklist.Add(curLine);
                    }
                }

                if (blacklist.Count > 0)
                    Program.WriteVerbose($"Loaded {blacklist.Count} blacklist entries.");
                else
                    Program.WriteVerbose("Blacklist is empty, no entries added.");
            }
            
            using (var sr = new StreamReader(configFile, true))
            {
                var archive = "";

                var type = -1;
                var source = "";

                var files = new List<FileDescriptor>();
                var numFiles = 0;
                
                var step = 0;
                var line = 0;

                Console.WriteLine("Parsing archive configuration...");

                Func<string> readLine = () => {
                    var l = sr.ReadLine();
                    ++line;

                    return l;
                };

                Func<string, string[]> readToken = (l) => {
                    var idx = l.IndexOf(' ');

                    if (idx != -1)
                    {
                        // token + value
                        return new[] {
                            l.Substring(0, idx),
                            l.Substring(idx + 1),
                        };
                    }
                    else
                    {
                        // no value
                        return new[] {
                            l,
                        };
                    }
                };

                Func<string, string> cleanFileName = (filename) => {
                    if (String.IsNullOrEmpty(filename))
                        return String.Empty;

                    var result = filename;
                    
                    if ((IMGVersion)type == IMGVersion.PSP)
                    {
                        if (filename[0] != '\\')
                            result = $"\\{filename}";
                    }
                    else
                    {
                        if (filename[0] == '\\')
                            result = filename.Substring(1);
                    }

                    return result;
                };

                Func<string, FileDescriptor> readFileDesc = (l) => {
                    var result = new FileDescriptor();

                    var input = l.Split(',');

                    switch ((IMGVersion)type)
                    {
                    case IMGVersion.IMG2:
                    case IMGVersion.IMG3:
                    case IMGVersion.IMG4:
                    case IMGVersion.PSP:
                        {
                            var entryData = ((IMGVersion)type == IMGVersion.PSP)
                                ? new PSPEntry()
                                : new Entry();

                            var filename = "";

                            if (input.Length > 1)
                            {
                                if ((IMGVersion)type == IMGVersion.IMG2)
                                    throw new InvalidOperationException($"IMG2 archives do not support filename hashes! (line {line})");

                                // detect xbox archives (TODO: implement support)
                                if (input.Length > 2)
                                {
                                    if ((IMGVersion)type != IMGVersion.IMG4)
                                        throw new InvalidOperationException($"Malformed entry @ line {line} has invalid syntax for version {type}!");

                                    // no xbox support yet :(
                                    throw new InvalidOperationException($"XBox archives are currently not supported for compilation.");
                                }

                                var strHash = input[0];
                                var isHex = strHash.StartsWith("0x");

                                var hash = 0u;

                                var val = (isHex) ? strHash.Substring(2) : strHash;
                                var style = (isHex) ? NumberStyles.HexNumber : NumberStyles.Integer;

                                if (!uint.TryParse(val, style, CultureInfo.InvariantCulture, out hash))
                                    throw new InvalidOperationException($"Malformed entry @ line {line} has invalid hash '{strHash}'.");

                                filename = input[1];
                                entryData.FileNameHash = hash;   
                            }
                            else
                            {
                                filename = input[0];
                                entryData.FileName = cleanFileName(filename);
                            }

                            if (String.IsNullOrEmpty(filename))
                                throw new InvalidOperationException($"Malformed entry @ line {line} has empty filename.");
                            
                            if (blacklist.Contains(filename))
                            {
                                Program.WriteVerbose($"Skipping blacklisted entry on line {line}: '{filename}'");
                                return null;
                            }

                            // expand to full path
                            var path = Path.Combine(source, filename);

                            if (!File.Exists(path))
                            {
                                Program.WriteVerbose($"WARNING: File '{filename}' is missing, skipping...");
                                return null;
                            }

                            var fileInfo = new FileInfo(path);
                            var fileSize = (int)fileInfo.Length;

                            entryData.Length = fileSize;

                            if (entryData is PSPEntry)
                                ((PSPEntry)entryData).UncompressedLength = fileSize;

                            result.FileName = filename;
                            result.EntryData = entryData;
                        } break;
                    }
                    
                    return result;
                };

                Func<string, bool> isEndOfFile = (l) => {
                    return String.Equals(l, "#EOF", StringComparison.OrdinalIgnoreCase);
                };

                Func<string, bool> isComment = (l) => {
                    return l.StartsWith("#");
                };
                
                var headerTokens = new Dictionary<string, Action<string>>() {
                    { "archive", (v) => {
                        archive = v;
                    } },
                    { "type", (v) => {
                        if (!int.TryParse(v, out type))
                            throw new InvalidOperationException($"Invalid archive type '{v}', must be an integer.");
                    } },
                    { "version", (v) => {
                        int version = -1;
                        if (!int.TryParse(v, out version))
                            throw new InvalidOperationException($"Invalid archive version '{v}', must be an integer.");

                        // cheaping out on this one, sorry
                        if (version == 1)
                            throw new InvalidOperationException($"Old archive configurations are no longer supported.");
                    } },
                    { "source", (v) => {
                        source = v;
                    } },
                };

                // parse header
                while (!sr.EndOfStream)
                {
                    var curLine = readLine();

                    if (isEndOfFile(curLine))
                        throw new InvalidOperationException("Unexpected EOF while parsing archive config!");

                    if (isComment(curLine))
                        continue;

                    if (String.IsNullOrEmpty(curLine))
                    {
                        // end of header
                        if (step > 0)
                            break;

                        // skip any fancy comments before the first token
                        continue;
                    }
                    
                    var input = readToken(curLine);

                    var token = input[0].ToLower();

                    if (!headerTokens.ContainsKey(token))
                        throw new InvalidOperationException($"Invalid archive configuration: unknown token '{curLine}'.");

                    if (input.Length != 2)
                        throw new InvalidOperationException($"Invalid archive configuration: malformed token '{token}'!");

                    var handler = headerTokens[token];

                    var value = input[1];

                    handler(value);
                    ++step;

                    // end of tokens (TODO: check for duplicates?)
                    if (step == headerTokens.Count)
                        break;
                }

                // verify we got the info needed
                if (String.IsNullOrEmpty(archive))
                    throw new InvalidOperationException("Invalid archive configuration: malformed 'archive' data!");
                if (type == -1)
                    throw new InvalidOperationException("Invalid archive configuration: malformed 'type' data!");

                if (source == "")
                {
                    Program.WriteVerbose("No source directory in archive configuration, using defaults.");
                    source = Path.Combine(Path.GetDirectoryName(configFile), "Files");
                }

                // make sure the hasher is setup properly
                CustomHasher.PSPMode = ((IMGVersion)type == IMGVersion.PSP);

                // read files list
                while (!sr.EndOfStream)
                {
                    var curLine = readLine();
                    
                    // EOF?
                    if (isEndOfFile(curLine))
                        break;
                    
                    // skip empty lines/comments
                    if (String.IsNullOrEmpty(curLine) || isComment(curLine))
                        continue;

                    var fileDesc = readFileDesc(curLine);

                    // skip blacklisted/missing files
                    if (fileDesc == null)
                        continue;

                    var idx = numFiles++;

                    // sort entries by hash for PSP archives
                    fileDesc.DataIndex = ((IMGVersion)type != IMGVersion.PSP) ? idx : -1;
                    fileDesc.Index = idx;

                    files.Add(fileDesc);
                }

                Console.WriteLine($"**** {files.Count} files processed *****");
                Console.WriteLine("Preparing buffer...");
                
                var bufferSize = 0L;

                var listOffset = 0;
                var entrySize = 0;

                // calculate header size
                switch ((IMGVersion)type)
                {
                case IMGVersion.IMG2:
                    listOffset = 0xC;
                    entrySize = 0x38;
                    break;
                case IMGVersion.IMG3:
                case IMGVersion.IMG4:
                    listOffset = 0x10;
                    entrySize = 0xC;
                    break;
                case IMGVersion.PSP:
                    listOffset = 0x10;
                    entrySize = 0x10;
                    break;
                }

                bufferSize = listOffset + (numFiles * entrySize);

                if ((IMGVersion)type == IMGVersion.IMG3)
                {
                    // add filenames to buffer size
                    foreach (var file in files)
                    {
                        var entryData = file.EntryData;
                        var filename = entryData.FileName;

                        bufferSize += (filename.Length + 1); // include null-terminator
                    }
                }

                var headerSize = (int)bufferSize;

                bufferSize = MemoryHelper.Align(bufferSize, 2048);
                
                // calculate file offsets + buffer size
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];

                    file.DataIndex = i;
                    file.Index = i;

                    var entryData = file.EntryData;

                    // will set 'Offset' accordingly
                    entryData.FileOffset = bufferSize;

                    bufferSize += entryData.Length;
                    bufferSize = MemoryHelper.Align(bufferSize, 2048);
                }

                if ((IMGVersion)type == IMGVersion.PSP)
                {
                    // sort files by hash
                    var idx = 0;

                    foreach (var file in files.OrderBy((f) => f.EntryData.FileNameHash))
                        file.Index = idx++;
                }
                
                Console.WriteLine("Building archive...");
                
                // let's get to work!
                var archiveDir = Path.Combine(outputDir, "Build");
                var archiveFileName = Path.Combine(archiveDir, archive);

                if (!Program.VerboseLog)
                    Console.Write("> ");

                var cL = Console.CursorLeft;
                var cT = Console.CursorTop;

                if (!Directory.Exists(archiveDir))
                    Directory.CreateDirectory(archiveDir);

                using (var fs = new FileStream(archiveFileName, FileMode.Create, FileAccess.Write, FileShare.Read, (1024 * 1024), FileOptions.SequentialScan))
                {
                    fs.SetLength(bufferSize);

                    // write header
                    fs.Position = 0;
                    
                    switch ((IMGVersion)type)
                    {
                    case IMGVersion.IMG2:
                    case IMGVersion.IMG3:
                    case IMGVersion.IMG4:
                        {
                            fs.Write(0x474D49 | ((type + 0x30) << 24));
                            fs.Write(files.Count);
                            fs.Write(0xF12EB12D); // ;)
                        }
                        break;
                    case IMGVersion.PSP:
                        {
                            // write number of files 4 times
                            for (int n = 0; n < 4; n++)
                                fs.Write(numFiles);
                        }
                        break;
                    }

                    // for IMG3 only
                    var strListSize = 0;

                    byte[] listData = null;

                    // write out files + build lookup table
                    using (var ms = new MemoryStream(headerSize - listOffset))
                    {
                        // process sequentially through the files
                        // we use the data index so write caching doesn't slow us down
                        // but in the header, it'll write wherever needed (PSP archives are weird)
                        foreach (var file in files.OrderBy((f) => f.DataIndex))
                        {
                            var idx = file.DataIndex;
                            
                            var entryData = file.EntryData;
                            var filename = Path.Combine(source, file.FileName);

                            if (!Program.VerboseLog)
                            {
                                Console.SetCursorPosition(cL, cT);
                                Console.Write($"{idx + 1} / {files.Count}");
                            }

                            Program.WriteVerbose($"[{(idx + 1):D4}] '{file.FileName}' @ {entryData.FileOffset:X8} (size: {entryData.Length:X8})");

                            // write file info
                            ms.Position = (file.Index * entrySize);

                            switch ((IMGVersion)type)
                            {
                            case IMGVersion.IMG2:
                                {
                                    byte[] deadcode = { 0xCD, 0xCD, 0xCD, 0xCD };

                                    // due to the unlikeliness of anyone adding new Stuntman files,
                                    // NO error checking is done to ensure filenames aren't too long!

                                    var strBuf = new byte[48];

                                    for (int i = 0; i < strBuf.Length; i += 4)
                                        Buffer.BlockCopy(deadcode, 0, strBuf, i, 4);

                                    var buf = Encoding.UTF8.GetBytes(entryData.FileName);

                                    Buffer.BlockCopy(buf, 0, strBuf, 0, buf.Length);
                                    strBuf[buf.Length] = 0;

                                    ms.Write(strBuf);
                                    ms.Write(entryData.Offset);
                                    ms.Write(entryData.Length);
                                } break;
                            case IMGVersion.IMG3:
                                {
                                    var strOffset = (numFiles * entrySize) + strListSize;
                                    var strBuf = Encoding.UTF8.GetBytes(entryData.FileName);

                                    ms.Write(strOffset);
                                    ms.Write(entryData.Offset);
                                    ms.Write(entryData.Length);

                                    // write filename
                                    ms.Position = strOffset;
                                    ms.Write(strBuf);

                                    strListSize += (strBuf.Length + 1);
                                } break;
                            case IMGVersion.IMG4:
                                {
                                    ms.Write(entryData.FileNameHash);
                                    ms.Write(entryData.Offset);
                                    ms.Write(entryData.Length);
                                } break;
                            case IMGVersion.PSP:
                                {
                                    var pspData = (entryData as PSPEntry);

                                    ms.Write(pspData.FileNameHash);
                                    ms.Write(pspData.Offset);
                                    ms.Write(pspData.Length);
                                    ms.Write(pspData.UncompressedLength);
                                } break;
                            }

                            var length = entryData.Length;

                            // read file data into the archive
                            using (var ffs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, length, FileOptions.SequentialScan))
                            {
                                if (ffs.Length != length)
                                    throw new InvalidOperationException($"FATAL ERROR: File size for '{file.FileName}' was changed before the archive could be built!");

                                // seek to the file offset and write all data
                                fs.Position = entryData.FileOffset;
                                
                                const int maxSize = 0x2C000000;

                                if (length < maxSize)
                                {
                                    var buffer = new byte[length];
                                    ffs.Read(buffer, 0, buffer.Length);

                                    fs.Write(buffer);
                                }
                                else if (length > maxSize)
                                {
                                    var splitSize = (length - maxSize);

                                    for (int w = 0; w < maxSize; w += 0x100000)
                                        fs.Write(ffs.ReadBytes(0x100000));

                                    fs.Write(ffs.ReadBytes(splitSize));
                                }
                            }
                        }

                        // commit list data to buffer
                        listData = ms.ToArray();
                    }

                    // encrypt header data if needed
                    byte decKey = 27;

                    switch ((IMGVersion)type)
                    {
                    case IMGVersion.IMG2:
                        {
                            for (int i = 0; i < listData.Length; i++)
                            {
                                listData[i] += decKey;
                                decKey += 11;
                            }
                        } break;
                    case IMGVersion.IMG3:
                    case IMGVersion.IMG4:
                        {
                            byte key = 21;

                            for (int i = 0, k = 1; i < listData.Length; i++, k++)
                            {
                                listData[i] += decKey;
                                decKey += key;

                                if (k > 6 && key == 21)
                                {
                                    k = 0;
                                    key = 11;
                                }
                                if (key == 11 && k > 24)
                                {
                                    k = 0;
                                    key = 21;
                                }
                            }
                        } break;
                    }

                    // write list size if needed
                    switch ((IMGVersion)type)
                    {
                    case IMGVersion.IMG3:
                    case IMGVersion.IMG4:
                        {
                            fs.Position = (listOffset - 4);
                            fs.Write(listData.Length);
                        } break;
                    }

                    // finally, write out the filelist
                    fs.Position = listOffset;
                    fs.Write(listData);
                }

                if (!Program.VerboseLog)
                    Console.Write("\n");

                Console.WriteLine($"Successfully saved archive '{archive}' to '{archiveDir}'!");
            }
        }

        public void SaveFile(string filename)
        {
            // maybe!
            throw new NotImplementedException();
        }
    }
    
}
