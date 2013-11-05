using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zartex.LogicData
{
    public static class Types
    {
        public static IDictionary<int, string> NodeDefinitionTypes =
            new Dictionary<int, string>() {
                { 1, "MissionStart" },
                { 3, "Delay" },
                { 4, "Condition" },
                { 5, "NoDelay" },
                { 6, "MissionEnd" },
                { 8, "Logic" },
                
                { 10, "SetFlags" },
                { 11, "FrameDelay" },
                { 12, "SetInterest" },
                { 13, "Switch" },
                { 14, "SetCamera" },
                { 15, "SetThreshold" },
                { 16, "SetVehicle" },
                { 17, "SetAction" },
                { 18, "SetActor" },
                { 19, "SetActorTrigger" },

                { 20, "SetActorSpeed" },
                { 21, "SetObjectTrigger" },
                { 23, "SetObjectActivity" },
                { 24, "ShowMessage" },
                { 25, "PlayFMV" },
                { 27, "SetActorAudio" },
                { 28, "SetCommand" },
                { 29, "SetValue" },
                
                { 100, "CiviliansDisabled" },
                { 101, "SetActorActivity" },
                { 102, "SetChasers" },
                { 104, "SetTrafficDensity" },
                { 109, "SetVehicleGunner" },
                { 110, "PercentageBar" },
                
                { 117, "SetChaseLeader" },
                { 118, "SetMusicType" },
                
                { 121, "3DAudioZone" },
                { 124, "ColorFader" },
                { 128, "SetLevel" },
                { 129, "SetActorWeapon" },
                
                { 131, "TextFader" },
                { 132, "SetConeData" },
                { 133, "SetInterestActor" },
                { 134, "SetCheat" },
                { 135, "SetElements" },
                { 137, "SetChaseVehicle" },
                { 138, "SetPath" },
                
                { 140, "SetMarker" },
                { 141, "SimpleFader" },
            };

        public static IDictionary<int, string> ActorDefinitionTypes =
            new Dictionary<int, string>() {
                { 2, "Character" },
                { 3, "Vehicle" },
                { 4, "Radius" },
                { 5, "Icon" },
                { 6, "Path" },
                { 7, "Target" },
                { 9, "Camera" },
                { 100, "Exclude(?)" },
                { 101, "Type(?)" },
                { 102, "Flags(?)" },
                { 103, "Weapon" },
                { 104, "Ammo" },
                { 105, "Marker" },
            };
    }
}
