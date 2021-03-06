﻿using System;
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
                    Context     = ChunkType.ModelPackagePC,
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
                    Context     = ChunkType.StandaloneTextures,
                    Description = "Standalone textures"
                };
            }
        }
        
        public static SpoolableBuffer VehicleHierarchy
        {
            get
            {
                return new SpoolableBuffer() {
                    Context     = ChunkType.VehicleHierarchy,
                    Description = "Vehicle Hierarchy"
                };
            }
        }
        
        public static SpoolableBuffer ExtraCharacterData
        {
            get
            {
                return new SpoolableBuffer() {
                    Context     = ChunkType.ExtraCharacterData,
                    Description = "Extra Character Data package"
                };
            }
        }
        
        public static SpoolableBuffer SkeletonData
        {
            get
            {
                return new SpoolableBuffer() {
                    Context     = ChunkType.CharacterSkeletons,
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
                    Context     = ChunkType.MissionSummary,
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
                    Context     = ChunkType.ExportedMissionObjects,
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
                    Context     = ChunkType.ExportedMissionPropHandles,
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
                    Context     = ChunkType.LogicExportStringCollection,
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
                    Context     = ChunkType.LogicExportSoundBank,
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
                    Context     = ChunkType.LogicExportActorDefinitions,
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
                    Context     = ChunkType.LogicExportPropertiesTable,
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
                    Context     = ChunkType.LogicExportActorSetTable,
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
                    Context     = ChunkType.LogicExportNodeDefinitionsTable,
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
                    Context       = ChunkType.LogicExportPropertiesTable,
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
                    Context     = ChunkType.LogicExportScriptCounters,
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
                    Context     = ChunkType.LogicExportWireCollections,
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
                    Context     = ChunkType.UnifiedPackage,
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
                    Context     = ChunkType.UnifiedPackage,
                    Description = "Exported Mission"
                };
            }
        }
            
    }
}
