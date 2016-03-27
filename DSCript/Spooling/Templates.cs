using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public sealed class SpoolerTemplates
    {
        public static SpoolableBuffer ModelPackagePC
        {
            get
            {
                return new SpoolableBuffer() {
                    Context     = (int)ChunkType.ModelPackagePC,
                    Version     = 6,
                    Description = "Renderer model package"
                };
            }
        }
            

        public static SpoolableBuffer StandaloneTextures
        {
            get
            {
                return new SpoolableBuffer() {
                    Context     = (int)ChunkType.StandaloneTextures,
                    Description = "Standalone textures"
                };
            }
        }
            

        public static SpoolableBuffer VehicleHierarchy
        {
            get
            {
                return new SpoolableBuffer() {
                    Context     = (int)ChunkType.VehicleHierarchy,
                    Description = "Vehicle Hierarchy"
                };
            }
        }
            

        public static SpoolableBuffer ExtraCharacterData
        {
            get
            {
                return new SpoolableBuffer() {
                    Context     = (int)ChunkType.ExtraCharacterData,
                    Description = "Extra Character Data package"
                };
            }
        }

            

        public static SpoolableBuffer SkeletonData
        {
            get
            {
                return new SpoolableBuffer() {
                    Context     = (int)ChunkType.CharacterSkeletons,
                    Description = "Skeleton package"
                };
            }
        }
            

        #region Mission files (.MPC)
        public static SpoolableBuffer MissionSummary
        {
            get
            {
                return new SpoolableBuffer() {
                    Alignment   = SpoolerAlignment.Align2048,
                    Context     = (int)ChunkType.MissionSummary,
                    Description = "Mission summary"
                };
            }
        }
            

        public static SpoolableBuffer MissionObjects
        {
            get
            {
                return new SpoolableBuffer() {
                    Alignment   = SpoolerAlignment.Align16,
                    Context     = (int)ChunkType.ExportedMissionObjects,
                    Description = "Exported Mission Objects"
                };
            }
        }
            

        public static SpoolableBuffer MissionPropHandles
        {
            get
            {
                return new SpoolableBuffer() {
                    Alignment   = SpoolerAlignment.Align16,
                    Context     = (int)ChunkType.ExportedMissionPropHandles,
                    Description = "Exported Mission Prop Handles"
                };
            }
        }
            

        public static SpoolableBuffer MissionStringCollection
        {
            get
            {
                return new SpoolableBuffer() {
                    Alignment   = SpoolerAlignment.Align4,
                    Context     = (int)ChunkType.LogicExportStringCollection,
                    Description = "String Collection"
                };
            }
        }
            

        public static SpoolableBuffer MissionSoundBankTable
        {
            get
            {
                return new SpoolableBuffer() {
                    Alignment   = SpoolerAlignment.Align4,
                    Context     = (int)ChunkType.LogicExportSoundBank,
                    Description = "Sound Bank Table"
                };
            }
        }
            

        public static SpoolableBuffer MissionActorDefinitions
        {
            get
            {
                return new SpoolableBuffer() {
                    Alignment   = SpoolerAlignment.Align4,
                    Context     = (int)ChunkType.LogicExportActorDefinitions,
                    Description = "Actor definitions table"
                };
            }
        }
            

        public static SpoolableBuffer MissionActorProperties
        {
            get
            {
                return new SpoolableBuffer() {
                    Alignment   = SpoolerAlignment.Align4,
                    Context     = (int)ChunkType.LogicExportPropertiesTable,
                    Description = "Actor properties table"
                };
            }
        }
            

        public static SpoolableBuffer MissionActorSetTable
        {
            get
            {
                return new SpoolableBuffer() {
                    Alignment   = SpoolerAlignment.Align4,
                    Context     = (int)ChunkType.LogicExportActorSetTable,
                    Description = "Actor Set Table"
                };
            }
        }
            

        public static SpoolableBuffer MissionLogicNodeDefinitions
        {
            get
            {
                return new SpoolableBuffer() {
                    Alignment   = SpoolerAlignment.Align4,
                    Context     = (int)ChunkType.LogicExportNodeDefinitionsTable,
                    Description = "Logic node definitions table"
                };
            }
        }
            

        public static SpoolableBuffer MissionLogicNodeProperties
        {
            get
            {
                return new SpoolableBuffer() {
                    Alignment   = SpoolerAlignment.Align4,
                    Context       = (int)ChunkType.LogicExportPropertiesTable,
                    Description = "Logic node properties table"
                };
            }
        }
            

        public static SpoolableBuffer MissionScriptCounters
        {
            get
            {
                return new SpoolableBuffer() {
                    Alignment   = SpoolerAlignment.Align4,
                    Context     = (int)ChunkType.LogicExportScriptCounters,
                    Description = "Script Counters"
                };
            }
        }
            

        public static SpoolableBuffer MissionWireCollection
        {
            get
            {
                return new SpoolableBuffer() {
                    Alignment   = SpoolerAlignment.Align4,
                    Context     = (int)ChunkType.LogicExportWireCollections,
                    Description = "Wire Collection"
                };
            }
        }
            
        #endregion
    }

    public sealed class ChunkTemplates
    {
        public static SpoolablePackage UnifiedPackage
        {
            get
            {
                return new SpoolablePackage() {
                    Context     = (int)ChunkType.UnifiedPackage,
                    Description = "Unified Packager"
                };
            }
        }

        public static SpoolablePackage ExportedMission
        {
            get
            {
                return new SpoolablePackage() {
                    Alignment   = SpoolerAlignment.Align2048,
                    Context     = (int)ChunkType.UnifiedPackage,
                    Description = "Exported Mission"
                };
            }
        }
            
    }
}
