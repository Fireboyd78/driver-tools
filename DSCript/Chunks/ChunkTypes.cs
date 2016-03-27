using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript
{
    /// <summary>
    /// An enumeration for holding values of every possible type within a Chunk file
    /// </summary>
    public enum ChunkType : int
    {
        /// <summary>
        /// [CHNK] - The header for a CHUNK block
        /// </summary>
        Chunk = 0x4B4E4843,
        
        /// <summary>
        /// [MDPC] - A block that contains various models and textures
        /// </summary>
        ModelPackagePC = 0x4350444D,
        
        /// <summary>
        /// [MDXN] - A block that contains various models and textures (new format found in DPL and DSF)
        /// </summary>
        ModelPackagePC_X = 0x4E58444D,
        
        /// <summary>
        /// [0x0] - A block that acts as a unifiable package for various data types
        /// </summary>
        UnifiedPackage = 0x00000000,
        
        /// <summary>
        /// [UPVH] - A block that contains vehicle hierarchy information
        /// </summary>
        VehicleHierarchy = 0x48565055,
        
        /// <summary>
        /// [UPST] - A block that contains standalone texture IDs
        /// </summary>
        StandaloneTextures = 0x54535055,
        
        /// <summary>
        /// [BINF] - A block that contains developer export information
        /// </summary>
        BuildInfo = 0x464E4942,

        /// <summary>
        /// [MODL] - No research has been done on this format
        /// </summary>
        ModelContainer = 0x4C444F4D,

        /// <summary>
        /// [GPUS] - No research has been done on this format
        /// </summary>
        GPUShader = 0x53555047,

        //Unknown in DPL *_missions.sp
        /// <summary>
        /// [MRRD] - No research has been done on this format (some sort of flags table?)
        /// </summary>
        UnknownMRRD = 0x4452524D,

        /// <summary>
        /// [EPMR] - No research has been done on this format
        /// </summary>
        ExportedMissionRoot = 0x524D5045,

        // =======================================================================================
        // ----------------------------------- SPOOLING SYSTEM -----------------------------------
        // =======================================================================================
        /// <summary>
        /// [SSIC] - No research has been done on this format
        /// </summary>
        SpoolSystemInitChunker = 0x43495353,

        /// <summary>
        /// [SSLP] - No research has been done on this format
        /// </summary>
        SpoolSystemLookup = 0x504C5353,
        
        /// <summary>
        /// [VSCT] - No research has been done on this format (may not be for strings?)
        /// </summary>
        SpooledVehicleStringCollection = 0x54435356,
        
        /// <summary>
        /// [CSCT] - No research has been done on this format (may not be for strings?)
        /// </summary>
        SpooledCharacterStringCollection = 0x54435343,

        /// <summary>
        /// [SSBK] - No research has been done on this format
        /// </summary>
        SpooledSoundBank = 0x4B425353,

        /// <summary>
        /// [ANMC] - No research has been done on this format
        /// </summary>
        SpooledAnimationChunk = 0x434D4E41,

        /// <summary>
        /// [SGSC] - No research has been done on this format
        /// </summary>
        SpooledGameSoundChunk = 0x43534753,

        /// <summary>
        /// [SGSB] - No research has been done on this format
        /// </summary>
        SpooledGameSoundBank = 0x42534753,

        /// <summary>
        /// [CHAC] - No research has been done on this format
        /// </summary>
        SpooledCharacterChunk = 0x43414843,

        /// <summary>
        /// [LOCC] - No research has been done on this format
        /// </summary>
        SpooledLocaleChunk = 0x43434F4C,

        /// <summary>
        /// [LOCT] - No research has been done on this format
        /// </summary>
        SpooledLocaleText = 0x54434F4C,

        /// <summary>
        /// [LTCK] - No research has been done on this format
        /// </summary>
        SpooledLitterChunk = 0x4B43544C,

        /// <summary>
        /// [LTDT] - No research has been done on this format
        /// </summary>
        SpooledLitterData = 0x5444544C,

        /// <summary>
        /// [PTCK] - No research has been done on this format
        /// </summary>
        SpooledParticleChunk = 0x4B435450,
        /// <summary>
        /// [PTDT] - No research has been done on this format
        /// </summary>
        SpooledParticleData = 0x54444550,

        /// <summary>
        /// [SPCK] - No research has been done on this format
        /// </summary>
        SpooledSkyCloudChunk = 0x4B435053,

        /// <summary>
        /// [SPDT] - No research has been done on this format
        /// </summary>
        SpooledSkyCloudData = 0x54445053,

        /// <summary>
        /// [VEHC] - No research has been done on this format
        /// </summary>
        SpooledVehicleChunk = 0x43484556,

        /// <summary>
        /// [VO3D] - No research has been done on this format
        /// </summary>
        VO3HandlingData = 0x44334F56,

        /// <summary>
        /// [CHAR] - No research has been done on this format
        /// </summary>
        CharacterSpoolChunk = 0x52414843,

        /// <summary>
        /// [CHAI] - No research has been done on this format
        /// </summary>
        CharacterSpoolIndex = 0x49414843,

        /// <summary>
        /// [NONR] - No research has been done on this format
        /// </summary>
        NonRendererData = 0x524E4F4E,

        /// <summary>
        /// [PCKG] - No research has been done on this format
        /// </summary>
        ResourcePackage = 0x474B4350,

        /// <summary>
        /// [UIDL] - No research has been done on this format
        /// </summary>
        UIDLookup = 0x4C444955,

        /// <summary>
        /// [SPOI] - No research has been done on this format
        /// </summary>
        SpoolableItem = 0x494F5053,

        /// <summary>
        /// [SPOL] - No research has been done on this format
        /// </summary>
        SpoolableItemLookup = 0x4C4F5053,

        /// <summary>
        /// [SDFD] - No research has been done on this format
        /// </summary>
        SpoolableDataTakeARide = 0x44464453,

        // =======================================================================================
        // ------------------------------------- *.AN4 FILES -------------------------------------
        // =======================================================================================
        /// <summary>
        /// [AIDX] - No research has been done on this format
        /// </summary>
        AnimationIndex = 0x58444941,

        /// <summary>
        /// [ACLP] - No research has been done on this format
        /// </summary>
        AnimationClips = 0x504C4341,

        // =======================================================================================
        // ------------------------------------- *.D3C FILES -------------------------------------
        // =======================================================================================
        /// <summary>
        /// [GESR] - No research has been done on this format
        /// </summary>
        SuperRegion = 0x52534547,

        /// <summary>
        /// [GEPM] - No research has been done on this format
        /// </summary>
        PhysicsModelData = 0x4D504547,

        /// <summary>
        /// [PMLD] - No research has been done on this format
        /// </summary>
        PhysicsModelLookupData = 0x444C4D50,

        /// <summary>
        /// [GWTR] - No research has been done on this format
        /// </summary>
        WaterInstanceData = 0x52545747,

        /// <summary>
        /// [GEBI] - No research has been done on this format
        /// </summary>
        BuildingInstanceData = 0x49424547,

        /// <summary>
        /// [GERD] - No research has been done on this format
        /// </summary>
        SplineRoadData = 0x44524547,

        /// <summary>
        /// [AIEX] - No research has been done on this format
        /// </summary>
        AIExport = 0x58454941,

        /// <summary>
        /// [ANOD] - No research has been done on this format
        /// </summary>
        AnimatedObjectData = 0x444F4E41,

        /// <summary>
        /// [GEPD] - No research has been done on this format
        /// </summary>
        PropDataContainer = 0x44504547,

        /// <summary>
        /// [HRDT] - No research has been done on this format
        /// </summary>
        PropHierarchyInformation = 0x54445248,

        /// <summary>
        /// [GEIR] - No research has been done on this format
        /// </summary>
        Region = 0x52494547,

        /// <summary>
        /// [GWRD] - No research has been done on this format
        /// </summary>
        WaterRegionData = 0x44525747,

        /// <summary>
        /// [GRTD] - No research has been done on this format
        /// </summary>
        RegionTerrainData = 0x44545247,

        /// <summary>
        /// [GEBR] - No research has been done on this format
        /// </summary>
        BuildingRoutefindData = 0x52424547,

        /// <summary>
        /// [JNR!] - No research has been done on this format
        /// </summary>
        BroadphaseData = 0x21524E4A,

        /// <summary>
        /// [ATRD] - No research has been done on this format
        /// </summary>
        AttractorData = 0x44525441,

        /// <summary>
        /// [SCND] - No research has been done on this format
        /// </summary>
        RegionSceneData = 0x444E4353,

        /// <summary>
        /// [OCGD] - No research has been done on this format
        /// </summary>
        OccluderGameData = 0x4447434F,

        /// <summary>
        /// [IRCT] - No research has been done on this format
        /// </summary>
        InteriorRegion = 0x54435249,

        /// <summary>
        /// [ISHM] - No research has been done on this format
        /// </summary>
        InteriorLightingData = 0x4D485349,

        /// <summary>
        /// [PINF] - No research has been done on this format
        /// </summary>
        MissionEditorModelInfo = 0x464E4950,

        /// <summary>
        /// [PIND] - No research has been done on this format
        /// </summary>
        MissionEditorModelData = 0x444E4950,

        /// <summary>
        /// [PINS] - No research has been done on this format
        /// </summary>
        MissionEditorStrings = 0x534E4950,

        /// <summary>
        /// [GEGL] - No research has been done on this format
        /// </summary>
        GrandioseLookup = 0x4C474547,

        /// <summary>
        /// [GESI] - No research has been done on this format
        /// </summary>
        SuperRegionInfo = 0x49534547,

        /// <summary>
        /// [GILH] - No research has been done on this format
        /// </summary>
        LookupHeader = 0x484C4947,

        /// <summary>
        /// [GSIL] - No research has been done on this format
        /// </summary>
        SuperRegionLookup = 0x4C495347,

        /// <summary>
        /// [GRIL] - No research has been done on this format
        /// </summary>
        RegionLookup = 0x4C495247,

        /// <summary>
        /// [IRLU] - No research has been done on this format
        /// </summary>
        InteriorLookupInfo = 0x554C5249,

        /// <summary>
        /// [PCSL] - An unused block type.
        /// </summary>
        ExternalModelLookup = 0x4C534350,

        // =======================================================================================
        // ------------------------------------- *.D4C FILES -------------------------------------
        // =======================================================================================
        /// <summary>
        /// [GEGR] - No research has been done on this format
        /// </summary>
        GlobalRegion = 0x52474547,

        /// <summary>
        /// [IRDT] - No research has been done on this format
        /// </summary>
        SceneData = 0x54445249,

        /// <summary>
        /// [GLUT] - No research has been done on this format
        /// </summary>
        GadgetLookup = 0x54554C47,

        /// <summary>
        /// [WORM] - No research has been done on this format
        /// </summary>
        WormsData = 0x4D524F57,

        /// <summary>
        /// [PROP] - No research has been done on this format
        /// </summary>
        PropData = 0x504F5250,

        /// <summary>
        /// [ASRD] - No research has been done on this format
        /// </summary>
        AttractorSuperRegionData = 0x44525341,

        // =======================================================================================
        // ------------------------------------- *.MPC FILES -------------------------------------
        // =======================================================================================
        /// <summary>
        /// [EM__] - Exported mission data for Driver: Parallel Lines
        /// </summary>
        ExportedMissionChunk = 0x5F5F4D45,

        /// <summary>
        /// [EMMS] - A block that contains data in which the game should handle this mission, such as what city to play in.
        /// </summary>
        MissionSummary = 0x534D4D45,

        /// <summary>
        /// [EMOB] - Contains data for positioning objects and defining actors in a mission.
        /// </summary>
        ExportedMissionObjects = 0x424F4D45,

        /// <summary>
        /// [EMPR] - Unknown. Possibly deprecated.
        /// </summary>
        ExportedMissionPropHandles = 0x52504D45,

        // ===================================
        // --------- MISSION LOGIC -----------
        // ===================================
        /// <summary>
        /// [LELD] - Contains all of the logic data (definitions, properties, etc).
        /// </summary>
        LogicExportData = 0x444C454C,

        /// <summary>
        /// [LESC] - Contains strings associated with the logic data.
        /// </summary>
        LogicExportStringCollection = 0x4353454C,

        /// <summary>
        /// [LESB] - Contains sound id's to be used in the logic data.
        /// </summary>
        LogicExportSoundBank = 0x4253454C,

        /// <summary>
        /// [LEAC] - Contains actor definitions and properties.
        /// </summary>
        LogicExportActorsChunk = 0x4341454C,

        /// <summary>
        /// [LEAD] - Contains definitions for characters, vehicles, weapons, etc. to be used in the mission.
        /// </summary>
        LogicExportActorDefinitions = 0x4441454C,

        /// <summary>
        /// [LENC] - Contains logic node definitions and properties.
        /// </summary>
        LogicExportNodesChunk = 0x434E454C,

        /// <summary>
        /// [LEND] - Contains definitions for mission logic.
        /// </summary>
        LogicExportNodeDefinitionsTable = 0x444E454C,

        /// <summary>
        /// [LEPR] - Contains properties associated with each definition.
        /// </summary>
        LogicExportPropertiesTable = 0x5250454C,

        /// <summary>
        /// [LEAS] - No research has been done on this format
        /// </summary>
        LogicExportActorSetTable = 0x5341454C,

        /// <summary>
        /// [LEWC] - Contains wires that link logic nodes together.
        /// </summary>
        LogicExportWireCollections = 0x4357454C,

        /// <summary>
        /// [LECO] - No research has been done on this format
        /// </summary>
        LogicExportScriptCounters = 0x4F43454C,

        // ===================================
        // -- Unknown purposes (leftovers?) --
        // ===================================
        /// <summary>
        /// [LEPS] - Unknown purpose in Driver: Parallel Lines.
        /// </summary>
        UnknownLogicExportLEPS = 0x5350454C,
        
        /// <summary>
        /// [LENE] - Unknown purpose in Driver: Parallel Lines.
        /// </summary>
        UnknownLogicExportLENE = 0x454E454C,
        
        /// <summary>
        /// [FING] - Unknown purpose in Driver: Parallel Lines.
        /// </summary>
        UnknownLogicExportFING = 0x474E4946,

        /// <summary>
        /// [LEAT] - Unknown purpose in Driver: Parallel Lines.
        /// </summary>
        UnknownLogicExportLEAT = 0x5441454C,

        // =======================================================================================
        // ------------------------------------- *.DAM FILES -------------------------------------
        // =======================================================================================
        /// <summary>
        /// [UPXD] - A block that contains character model and texture data.
        /// </summary>
        ExtraCharacterData = 0x44585055,

        /// <summary>
        /// [UPCB] - A block that contains character skeletons for the male and female models.
        /// </summary>
        CharacterSkeletons = 0x42435055,

        // =======================================================================================
        // ------------------------------------- *.MEC FILES -------------------------------------
        // =======================================================================================
        /// <summary>
        /// [RMDP] - A block that holds data pertaining to the GUI (main menu, pause menu, etc)
        /// </summary>
        ReflectionsMenuDataPackage = 0x50444D52,

        /// <summary>
        /// [RMDL] - A block that contains data for handling all GUI-related data
        /// </summary>
        ReflectionsMenuDataChunk = 0x4C444D52,

        // =======================================================================================
        // ------------------------------------- *.BNK FILES -------------------------------------
        // =======================================================================================
        /// <summary>
        /// [FONT] - A block that holds data pertaining to the fonts
        /// </summary>
        FontContainer = 0x544E4F46,

        /// <summary>
        /// [FBNK] - A block that contains textures used for the fonts
        /// </summary>
        FontData = 0x4B4E4246,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: RESOURCE PACKAGES ---------------------
        // =======================================================================================
        /// <summary>
        /// [DRSP] - No research has been done on this format
        /// </summary>
        ResourceChunk = 0x50535244,

        /// <summary>
        /// [TEXC] - No research has been done on this format
        /// </summary>
        ResourceTexture = 0x43584554,

        /// <summary>
        /// [UIDC] - No research has been done on this format
        /// </summary>
        ResourceTextureUID = 0x43444955,

        /// <summary>
        /// [DXTC] - No research has been done on this format
        /// </summary>
        ResourceTextureDDS = 0x43545844,

        /// <summary>
        /// [MACO] - No research has been done on this format
        /// </summary>
        ResourceMaterialContainer = 0x4F43414D,

        /// <summary>
        /// [MAHE] - No research has been done on this format
        /// </summary>
        ResourceMaterialHeader = 0x4548414D,

        /// <summary>
        /// [MATX] - No research has been done on this format
        /// </summary>
        ResourceMaterialXML = 0x5844414D,

        /// <summary>
        /// [MARH] - No research has been done on this format
        /// </summary>
        ResourceMaterialRenderMethodHeader = 0x4852414D,

        /// <summary>
        /// [MARP] - No research has been done on this format
        /// </summary>
        ResourceMaterialRenderMethodParameter = 0x5052414D,

        /// <summary>
        /// [SBMC] - No research has been done on this format
        /// </summary>
        SubmodelChunk = 0x434D4253,

        /// <summary>
        /// [SBBH] - No research has been done on this format
        /// </summary>
        SubmodelHeader = 0x48424253,

        /// <summary>
        /// [VXCK] - No research has been done on this format
        /// </summary>
        PooledVertexBufferChunk = 0x4B435856,

        /// <summary>
        /// [VXHD] - No research has been done on this format
        /// </summary>
        PooledVertexHeader = 0x44485856,

        /// <summary>
        /// [VXDP] - No research has been done on this format
        /// </summary>
        PooledVertexDependencies = 0x50445856,

        /// <summary>
        /// [VXDT] - No research has been done on this format
        /// </summary>
        PooledVertexBufferData = 0x54445856,

        /// <summary>
        /// [VCDL] - No research has been done on this format
        /// </summary>
        PooledVertexDeclaration = 0x4C434456,

        /// <summary>
        /// [IXCK] - No research has been done on this format
        /// </summary>
        PooledIndexChunk = 0x4B435849,

        /// <summary>
        /// [IXHD] - No research has been done on this format
        /// </summary>
        PooledIndexHeader = 0x44485849,

        /// <summary>
        /// [IXDP] - No research has been done on this format
        /// </summary>
        PooledIndexDependencies = 0x50445849,

        /// <summary>
        /// [IXDT] - No research has been done on this format
        /// </summary>
        PooledIndexBuffer = 0x54445849,

        /// <summary>
        /// [MDID] - No research has been done on this format
        /// </summary>
        RendererDataUIDs = 0x4449444D,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: CHARACTER DATA ------------------------
        // =======================================================================================
        /// <summary>
        /// [AGRA] - No research has been done on this format
        /// </summary>
        AnimationGraph = 0x41524741,

        /// <summary>
        /// [PDMD] - No research has been done on this format
        /// </summary>
        PedestrianMeshData = 0x444D4450,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: CITY LOCKING DATA ---------------------
        // =======================================================================================
        /// <summary>
        /// [CTYE] - No research has been done on this format
        /// </summary>
        CityLockingEntry = 0x45595443,

        /// <summary>
        /// [CTYS] - No research has been done on this format
        /// </summary>
        CityLockingSplines = 0x53595443,

        /// <summary>
        /// [CTYR] - No research has been done on this format
        /// </summary>
        CityLockingRoads = 0x52595443,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: CITY PACKAGE DATA ---------------------
        // =======================================================================================
        /// <summary>
        /// [AMBV] - No research has been done on this format
        /// </summary>
        AmbientVolumeData = 0x56424D41,

        /// <summary>
        /// [BILL] - No research has been done on this format
        /// </summary>
        BillboardInfo = 0x4C4C4942,

        /// <summary>
        /// [HINF] - No research has been done on this format
        /// </summary>
        HeightInfo = 0x464E4948,

        /// <summary>
        /// [ADVH] - No research has been done on this format
        /// </summary>
        AdvertisingHeader = 0x48564441,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: COMMENTARY DATA -----------------------
        // =======================================================================================
        /// <summary>
        /// [CMRT] - No research has been done on this format
        /// </summary>
        CommentaryRoot = 0x54524D43,

        /// <summary>
        /// [CMCC] - No research has been done on this format
        /// </summary>
        CommentaryCommonContainer = 0x43434D43,

        /// <summary>
        /// [CMMS] - No research has been done on this format
        /// </summary>
        CommentaryMissionData = 0x534D4D43,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: CUTSCENE DATA -------------------------
        // =======================================================================================
        /// <summary>
        /// [CUTR] - No research has been done on this format
        /// </summary>
        CutsceneRoot = 0x52545543,

        /// <summary>
        /// [CUTS] - No research has been done on this format
        /// </summary>
        CutsceneData = 0x53545543,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: SCRIPT DATA ---------------------------
        // =======================================================================================
        /// <summary>
        /// [SCRH] - Contains script package information.
        /// </summary>
        ScriptPackageHeader = 0x48524353,

        /// <summary>
        /// [SCRC] - Contains script package lookup information.
        /// </summary>
        ScriptPackageLookup         = 0x43524353,

        /// <summary>
        /// [SCRR] - Compiled script package data.
        /// </summary>
        ScriptPackageRoot           = 0x52524353,

        /// <summary>
        /// [SCRS] - Contains compiled Lua script data.
        /// </summary>
        ScriptPackageCompiledScript = 0x53524353,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: SHADER DATA ---------------------------
        // =======================================================================================
        /// <summary>
        /// [SPSR] - No research has been done on this format
        /// </summary>
        ShaderParamSetRoot = 0x52535053,

        /// <summary>
        /// [SPSS] - No research has been done on this format
        /// </summary>
        ShaderParamSet = 0x53535053, //SPSS

        /// <summary>
        /// [SPSP] - No research has been done on this format
        /// </summary>
        ShaderParamSetParams = 0x50535053, //SPSP

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: VEHICLE PACKAGE DATA ------------------
        // =======================================================================================
        /// <summary>
        /// [VSRT] - No research has been done on this format
        /// </summary>
        VehicleSoundRoot = 0x54525356,

        /// <summary>
        /// [VSCH] - No research has been done on this format
        /// </summary>
        VehicleCommonSoundHeader = 0x48435356,

        /// <summary>
        /// [VSVH] - No research has been done on this format
        /// </summary>
        VehicleSoundHeader = 0x48565356,

        /// <summary>
        /// [SBAK] - No research has been done on this format
        /// </summary>
        VehicleSoundBank = 0x4B414253,

        /// <summary>
        /// [GRBK] - No research has been done on this format
        /// </summary>
        VehicleGranularData = 0x4B425247,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: UNKNOWN DATA --------------------------
        // =======================================================================================
        /// <summary>
        /// [DMWK] - This format may be leftover from development. It serves an unknown purpose in 'dngvehicles.sp' in Driver: San Francisco.
        /// </summary>
        DM_WMK_Info = 0x4B574D44,
    }
}
