using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zartex
{
    public enum LogicNodeType : byte
    {
        MissionStart            = 1,
        DebugText               = 2,
        Timer                   = 3,
        CounterWatch            = 4,
        MissionEnd              = 5,
        MissionFail             = 6,
        Comment                 = 7,
        GroupBroadcast          = 8,

        Random                  = 10,
        FrameDelay              = 11,
        VolumeWatch             = 12,
        SwitchWatch             = 13,
        CameraSelect            = 14,
        Accumulator             = 15,
        VehicleWatch            = 16,
        CounterControl          = 17,
        CharacterControl        = 18,
        CharacterWatch          = 19,

        AnimationControl        = 20,
        ObjectWatch             = 21,
        CollisionWatch          = 22,
        ObjectControl           = 23,
        DisplayMessage          = 24,
        PlayFMV                 = 25,
        PlayAudio               = 27,
        SetStopwatch            = 28,
        SetValue                = 29,

        CheckThreshold          = 30,

        IGCS                    = 100,
        ActorSet                = 101,
        GoonVehicleControl      = 102,
        CivilianTrafficControl  = 103,
        ProximityCheck          = 106,
        SetCharacterName        = 107,
        SetVehicleGunner        = 109,

        PercentageBar           = 110,
        Tally                   = 111,
        SetVehiclePassenger     = 115,
        SetChaseLeader          = 117,
        SetMusicType            = 118,

        PlayAudio3D             = 121,
        ScreenFade              = 124,
        PedDensityControl       = 128,
        PlayerProfileControl    = 129,

        TextFader               = 131,
        SetConeData             = 132,
        SetInterestActor        = 133,
        SetCheat                = 134,
        SetOverlays             = 135,
        SetChaseVehicle         = 137,
        CameraControl           = 138,

        SetMarker               = 140,
        SimpleFader             = 141,
        ProfileSettingsQuery    = 143,
    }

    public static class NodeTypes
    {
        public static IDictionary<int, string> LogicNodeTypes = new Dictionary<int, string>() {
            { 1, "MissionStart" },
            { 2, "DebugText" },
            { 3, "Timer" },
            { 4, "CounterWatch" },
            { 5, "MissionEnd" },
            { 6, "MissionFail" },
            { 7, "Comment" },
            { 8, "GroupBroadcast" },
                
            { 10, "Random" },
            { 11, "FrameDelay" },
            { 12, "AreaWatch" },
            { 13, "Switch" },
            { 14, "CameraControl" },
            { 15, "Accumulator" },
            { 16, "VehicleWatch" },
            { 17, "CounterControl" },
            { 18, "CharacterControl" },
            { 19, "CharacterWatch" },

            { 20, "AnimationControl" },
            { 21, "ObjectWatch" },
            { 22, "CollisionWatch" },
            { 23, "ObjectControl" },
            { 24, "DisplayMessage" },
            { 25, "PlayFMV" },
            { 27, "PlayAudio" },
            { 28, "Stopwatch" },
            { 29, "SetValue(?)" },

            { 30, "ProximityCheck" },
                
            { 100, "IGCS" },
            { 101, "ActorSet" },
            { 102, "GoonVehicleControl" },
            { 104, "CivilianTrafficControl" },
            { 105, "RearVehicleShooting" },
            { 106, "WatchLineOfSight" },
            { 107, "SetCharacterName" },
            { 108, "RearVehicleShooting(2)" },
            { 109, "SetVehicleGunner" },
            { 110, "MissionStatusWatch" },
                
            { 115, "SetVehiclePassenger" },
            { 117, "SetChaseLeader" },
            { 118, "MusicControl" },
                
            { 121, "3DAudioZone" },
            { 124, "ScreenFade" },
            { 128, "PedestrianDensityControl" },
            { 129, "PlayerProfileControl" },
                
            { 131, "MissionStatusControl (?)" },
            { 132, "SetConeData" },
            { 133, "CharacterRoutefind" },
            { 134, "SetCheat" },
            { 135, "HudBarInitialise" },
            { 137, "SetChaseVehicle" },
            { 138, "PathMove" },
                
            { 140, "SetMarker" },
            { 141, "SimpleFader" },
            { 143, "ProfileSettingsQuery" },
        };

        public static IDictionary<int, string> ActorNodeTypes = new Dictionary<int, string>() {
            { 2, "Character" },
            { 3, "Vehicle" },
            { 4, "Volume" },
            { 5, "Icon" },
            { 6, "Path" },
            { 7, "Target" },
            { 9, "Camera" },
            { 100, "Area" },
            { 101, "Switch(?)" },
            { 102, "SpecialEffect(?)" },
            { 103, "Weapon" },
            { 104, "Pickup" },
            { 105, "Marker" },
        };

        public static string GetNodeType(int type)
        {
            return LogicNodeTypes.ContainsKey(type) ? LogicNodeTypes[type] : type.ToString();
        }

        public static string GetActorType(int type)
        {
            return ActorNodeTypes.ContainsKey(type) ? ActorNodeTypes[type] : type.ToString();
        }
    }
}
