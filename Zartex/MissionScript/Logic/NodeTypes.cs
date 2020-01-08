using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zartex
{
    public static class NodeTypes
    {
        public static IDictionary<int, string> LogicNodeTypes = new Dictionary<int, string>() {
            { 1, "LogicStart" },
            { 2, "DebugText" },
            { 3, "Timer" },
            { 4, "CounterWatch" },
            { 5, "MissionComplete" },
            { 6, "FailMission" },
            { 7, "Comment" },
            { 8, "GroupBroadcast" },
            { 9, "DeadNode" },

            { 10, "Random" },
            { 11, "Converter" },
            { 12, "AreaWatch" },
            { 13, "ActionButtonWatch" },
            { 14, "CameraSelect" },
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
            { 25, "FMVStart" },
            { 26, "HardCoded" },
            { 27, "PlayAudio" },
            { 28, "CountdownIntro" },
            { 29, "OverlayClockWatch" },

            { 30, "ProximityCheck" },
            
            { 100, "IGCSModeControl" },
            { 101, "ActorCreation" },
            { 102, "CopControl" },
            { 104, "CivilianTrafficControl" },
            { 105, "RearVehicleShooting" },
            { 106, "WatchLineOfSight" },
            { 107, "CharacterNameControl" },
            { 108, "RearVehicleShooting(2)" },
            { 109, "VehicleGunnerControl" },
            { 110, "PercentageBar" },
            { 111, "RangeResultsScreen" },
            { 112, "<MISSING>" }, // if this ever turns up, maybe we can figure out what it was?
            { 113, "CraneControl" },
            { 114, "BombTruckControl" },
            { 115, "PassengerJumpControl" },
            { 116, "RangeTargetsControl" },
            { 117, "ChaseModeControl" },
            { 118, "MusicControl" },

            { 120, "WarehouseDoorCutter" },
            { 121, "AmbientSounds" },
            { 122, "BombCarWatch" },
            { 123, "TrainControl" },
            { 124, "FadeScreen" },
            { 125, "CargoVehicleWatch" },
            { 126, "ArmsCrateControl" },
            { 128, "PedestrianDensityControl" },
            { 129, "AwardWeaponToPlayer" },
            
            { 131, "SkipCutscene" },
            { 132, "ConeDataControl" },
            { 133, "InterestActorControl" },
            { 134, "CheatControl" },
            { 135, "OverlayClockControl" },
            { 136, "OpenCargoDoorsControl" },
            { 137, "SetChaseVehicle" },
            { 138, "FollowPath" },
            { 139, "Convoy" },
            
            { 140, "AwardVehicleToPlayer" },
            { 141, "FadeSounds" },
            { 142, "SetSpoolRadius" },
            { 143, "ProfileQuery" },
        };

        public static IDictionary<int, string> ActorNodeTypes = new Dictionary<int, string>() {
            { 2, "Character" },
            { 3, "Vehicle" },
            { 4, "TestVolume" },
            { 5, "ObjectiveIcon" },
            { 6, "AIPath" },
            { 7, "AITarget" },
            { 8, "SpecialEffect" },
            { 9, "Camera" },
            { 100, "Area" },
            { 101, "Switch" },
            { 102, "Prop" },
            { 103, "Collectable" },
            { 104, "AnimProp" },
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
