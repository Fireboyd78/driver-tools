using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.IO
{
    /// <summary>
    /// An enumeration for holding values of every possible type within a CHUNK file
    /// </summary>
    public enum CTypes : uint
    {

        /* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
         * 
         * WARNING:
         * 
         * THIS PORTION OF CODE IS EASY TO GET LOST IN, DUE TO THE SHEER AMOUNT OF 'TYPES' USED.
         * IF YOU HAVE ANY BETTER SUGGESTIONS FOR STORING THIS TYPE OF DATA, PLEASE LET ME KNOW!
         * 
         * PROCEED WITH CAUTION AND TRY NOT TO READ TOO FAST!
         * - CarLuver69
         * 
         * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

        // =======================================================================================
        // ----------------------------------------GLOBAL-----------------------------------------
        // =======================================================================================
        /// <summary>
        /// A type representing an unknown value (Note: This is used by tools only!)
        /// </summary>
        UNKNOWN = 0xFFFFFFFF,

        /// <summary>
        /// [CHNK] - The header for a CHUNK block
        /// </summary>
        CHUNK = 0x4B4E4843,
        
        /// <summary>
        /// [MDPC] - A block that contains various models and textures
        /// </summary>
        MODEL_PACKAGE_PC = 0x4350444D,
        
        /// <summary>
        /// [MDXN] - A block that contains various models and textures (new format found in DPL and DSF)
        /// </summary>
        MODEL_PACKAGE_PC_X = 0x4E58444D,
        
        /// <summary>
        /// [0x0] - A block that acts as a unifiable package for various data types
        /// </summary>
        UNIFIED_PACKAGE = 0x00000000,
        
        /// <summary>
        /// [UPVH] - A block that contains vehicle hierarchy information
        /// </summary>
        VEHICLE_HIERARCHY = 0x48565055,
        
        /// <summary>
        /// [UPST] - A block that contains standalone texture IDs
        /// </summary>
        STANDALONE_TEXTURES = 0x54535055,
        
        /// <summary>
        /// [BINF] - A block that contains developer export information
        /// </summary>
        BUILD_INFO = 0x464E4942,

        /// <summary>
        /// [MODL] - No research has been done on this format
        /// </summary>
        MODEL_CONTAINER = 0x4C444F4D,

        /// <summary>
        /// [GPUS] - No research has been done on this format
        /// </summary>
        GPU_SHADER = 0x53555047,

        //Unknown in DPL *_missions.sp
        /// <summary>
        /// [MRRD] - No research has been done on this format (some sort of flags table?)
        /// </summary>
        DPL_MISSION_UNKNOWN_MRRD = 0x4452524D,

        /// <summary>
        /// [EPMR] - No research has been done on this format
        /// </summary>
        EXPORTED_MISSION_ROOT = 0x524D5045,

        // =======================================================================================
        // ----------------------------------- SPOOLING SYSTEM -----------------------------------
        // =======================================================================================
        /// <summary>
        /// [SSIC] - No research has been done on this format
        /// </summary>
        SPOOL_SYSTEM_INIT_CHUNKER = 0x43495353,

        /// <summary>
        /// [SSLP] - No research has been done on this format
        /// </summary>
        SPOOL_SYSTEM_LOOKUP = 0x504C5353,
        
        /// <summary>
        /// [VSCT] - No research has been done on this format (may not be for strings?)
        /// </summary>
        SPOOLED_VEHICLE_STRING_COLLECTION_TABLE = 0x54435356,
        
        /// <summary>
        /// [CSCT] - No research has been done on this format (may not be for strings?)
        /// </summary>
        SPOOLED_CHARACTER_STRING_COLLECTION = 0x54435343,

        /// <summary>
        /// [SSBK] - No research has been done on this format
        /// </summary>
        SPOOLED_SOUND_BANK = 0x4B425353,

        /// <summary>
        /// [ANMC] - No research has been done on this format
        /// </summary>
        SPOOLED_ANIMATION_CHUNK = 0x434D4E41,

        /// <summary>
        /// [SGSC] - No research has been done on this format
        /// </summary>
        SPOOLED_GAME_SOUND_CHUNK = 0x43534753,

        /// <summary>
        /// [SGSB] - No research has been done on this format
        /// </summary>
        SPOOLED_GAME_SOUND_BANK = 0x42534753,

        /// <summary>
        /// [CHAC] - No research has been done on this format
        /// </summary>
        SPOOLED_CHARACTER_CHUNK = 0x43414843,

        /// <summary>
        /// [LOCC] - No research has been done on this format
        /// </summary>
        SPOOLED_LOCALE_CHUNK = 0x43434F4C,

        /// <summary>
        /// [LOCT] - No research has been done on this format
        /// </summary>
        SPOOLED_LOCALE_TEXT = 0x54434F4C,

        /// <summary>
        /// [LTCK] - No research has been done on this format
        /// </summary>
        SPOOLED_LITTER_CHUNK = 0x4B43544C,

        /// <summary>
        /// [LTDT] - No research has been done on this format
        /// </summary>
        SPOOLED_LITTER_DATA = 0x5444544C,

        /// <summary>
        /// [PTCK] - No research has been done on this format
        /// </summary>
        SPOOLED_PARTICLE_CHUNK = 0x4B435450,
        /// <summary>
        /// [PTDT] - No research has been done on this format
        /// </summary>
        SPOOLED_PARTICLE_DATA = 0x54444550,

        /// <summary>
        /// [SPCK] - No research has been done on this format
        /// </summary>
        SPOOLED_SKY_CLOUD_CHUNK = 0x4B435053,

        /// <summary>
        /// [SPDT] - No research has been done on this format
        /// </summary>
        SPOOLED_SKY_CLOUD_DATA = 0x54445053,

        /// <summary>
        /// [VEHC] - No research has been done on this format
        /// </summary>
        SPOOLED_VEHICLE_CHUNK = 0x43484556,

        /// <summary>
        /// [VO3D] - No research has been done on this format
        /// </summary>
        VO3_HANDLING_DATA = 0x44334F56,

        /// <summary>
        /// [CHAR] - No research has been done on this format
        /// </summary>
        CHARACTER_SPOOL_CHUNK = 0x52414843,

        /// <summary>
        /// [CHAI] - No research has been done on this format
        /// </summary>
        CHARACTER_SPOOL_INDEX = 0x49414843,

        /// <summary>
        /// [NONR] - No research has been done on this format
        /// </summary>
        NON_RENDERER_DATA = 0x524E4F4E,

        /// <summary>
        /// [PCKG] - No research has been done on this format
        /// </summary>
        RESOURCE_PACKAGE = 0x474B4350,

        /// <summary>
        /// [UIDL] - No research has been done on this format
        /// </summary>
        UID_LOOKUP = 0x4C444955,

        /// <summary>
        /// [SPOI] - No research has been done on this format
        /// </summary>
        SPOOLABLE_ITEM = 0x494F5053,

        /// <summary>
        /// [SPOL] - No research has been done on this format
        /// </summary>
        SPOOLABLE_ITEM_LOOKUP = 0x4C4F5053,

        /// <summary>
        /// [SDFD] - No research has been done on this format
        /// </summary>
        SPOOLABLE_DATA_TAKEARIDE = 0x44464453,

        // =======================================================================================
        // ------------------------------------- *.AN4 FILES -------------------------------------
        // =======================================================================================
        /// <summary>
        /// [AIDX] - No research has been done on this format
        /// </summary>
        ANIMATION_INDEX = 0x58444941,

        /// <summary>
        /// [ACLP] - No research has been done on this format
        /// </summary>
        ANIMATION_CLIPS = 0x504C4341,

        // =======================================================================================
        // ------------------------------------- *.D3C FILES -------------------------------------
        // =======================================================================================
        /// <summary>
        /// [GESR] - No research has been done on this format
        /// </summary>
        GLOBAL_SUPER_REGION = 0x52534547,

        /// <summary>
        /// [GEPM] - No research has been done on this format
        /// </summary>
        PHYSICS_MODEL_DATA = 0x4D504547,

        /// <summary>
        /// [PMLD] - No research has been done on this format
        /// </summary>
        PHYSICS_MODEL_LOOKUP_DATA = 0x444C4D50,

        /// <summary>
        /// [GWTR] - No research has been done on this format
        /// </summary>
        WATER_INSTANCE_DATA = 0x52545747,

        /// <summary>
        /// [GEBI] - No research has been done on this format
        /// </summary>
        BUILDING_INSTANCE_DATA = 0x49424547,

        /// <summary>
        /// [GERD] - No research has been done on this format
        /// </summary>
        SPLINE_ROAD_DATA = 0x44524547,

        /// <summary>
        /// [AIEX] - No research has been done on this format
        /// </summary>
        AI_EXPORT = 0x58454941,

        /// <summary>
        /// [ANOD] - No research has been done on this format
        /// </summary>
        ANIMATED_OBJECT_DATA = 0x444F4E41,

        /// <summary>
        /// [GEPD] - No research has been done on this format
        /// </summary>
        PROP_DATA_CONTAINER = 0x44504547,

        /// <summary>
        /// [HRDT] - No research has been done on this format
        /// </summary>
        PROP_HIERARCHY_INFORMATION = 0x54445248,

        /// <summary>
        /// [GEIR] - No research has been done on this format
        /// </summary>
        REGION = 0x52494547,

        /// <summary>
        /// [GWRD] - No research has been done on this format
        /// </summary>
        WATER_REGION_DATA = 0x44525747,

        /// <summary>
        /// [GRTD] - No research has been done on this format
        /// </summary>
        REGION_TERRAIN_DATA = 0x44545247,

        /// <summary>
        /// [GEBR] - No research has been done on this format
        /// </summary>
        BUILDING_ROUTEFIND_DATA = 0x52424547,

        /// <summary>
        /// [JNR!] - No research has been done on this format
        /// </summary>
        BROADPHASE_DATA = 0x21524E4A,

        /// <summary>
        /// [ATRD] - No research has been done on this format
        /// </summary>
        ATTRACTOR_DATA = 0x44525441,

        /// <summary>
        /// [SCND] - No research has been done on this format
        /// </summary>
        REGION_SCENEDATA_DATA = 0x444E4353,

        /// <summary>
        /// [OGCD] - No research has been done on this format
        /// </summary>
        OCCLUDER_GAME_DATA = 0x444743F4,

        /// <summary>
        /// [IRCT] - No research has been done on this format
        /// </summary>
        EXTERNAL_FILE_LINK_INTERNAL = 0x54435249,

        /// <summary>
        /// [ISHM] - No research has been done on this format
        /// </summary>
        INTERIOR_LIGHTING_DATA = 0x4D485349,

        /// <summary>
        /// [PINF] - No research has been done on this format
        /// </summary>
        MISSION_EDITOR_MODEL_INFO = 0x464E4950,

        /// <summary>
        /// [PIND] - No research has been done on this format
        /// </summary>
        MISSION_EDITOR_MODEL_DATA = 0x444E4950,

        /// <summary>
        /// [PINS] - No research has been done on this format
        /// </summary>
        MISSION_EDITOR_STRINGS = 0x534E4950,

        /// <summary>
        /// [GEGL] - No research has been done on this format
        /// </summary>
        GRANDIOSE_LOOKUP = 0x4C474547,

        /// <summary>
        /// [GESI] - No research has been done on this format
        /// </summary>
        SUPER_REGION_INFO = 0x49534547,

        /// <summary>
        /// [GILH] - No research has been done on this format
        /// </summary>
        LOOKUP_HEADER = 0x484C4947,

        /// <summary>
        /// [GSIL] - No research has been done on this format
        /// </summary>
        SUPER_REGION_LOOKUP = 0x4C495347,

        /// <summary>
        /// [GRIL] - No research has been done on this format
        /// </summary>
        REGION_LOOKUP = 0x4C495247,

        /// <summary>
        /// [IRLU] - No research has been done on this format
        /// </summary>
        INTERIOR_LOOKUP_INFO = 0x554C5249,

        /// <summary>
        /// [PCSL] - No research has been done on this format
        /// </summary>
        REGIONAL_AREA = 0x4C534350,

        // =======================================================================================
        // ------------------------------------- *.D4C FILES -------------------------------------
        // =======================================================================================
        /// <summary>
        /// [GEGR] - No research has been done on this format
        /// </summary>
        GLOBAL_REGION = 0x52474547,

        /// <summary>
        /// [IRDT] - No research has been done on this format
        /// </summary>
        SCENE_DATA = 0x54445249,

        /// <summary>
        /// [GLUT] - No research has been done on this format
        /// </summary>
        GADGET_LOOKUP = 0x54554C47,

        /// <summary>
        /// [WORM] - No research has been done on this format
        /// </summary>
        WORMS_DATA = 0x4D524F57,

        /// <summary>
        /// [PROP] - No research has been done on this format
        /// </summary>
        PROP_DATA = 0x504F5250,

        /// <summary>
        /// [ASRD] - No research has been done on this format
        /// </summary>
        ATTRACTOR_SUPER_REGION_DATA = 0x44525341,

        // =======================================================================================
        // ------------------------------------- *.MPC FILES -------------------------------------
        // =======================================================================================
        /// <summary>
        /// [EM__] - A block that contains relevant mission data
        /// </summary>
        EXPORTED_MISSION = 0x5F5F4D45,

        /// <summary>
        /// [EMMS] - A block that contains data in which the game should handle this mission, such as what city to play in (SPECIAL THANKS: Janujeq)
        /// </summary>
        MISSION_SUMMARY = 0x534D4D45,

        /// <summary>
        /// [EMOB] - A block used to place objects in the mission (SPECIAL THANKS: Janujeq)
        /// </summary>
        EXPORTED_MISSION_OBJECTS = 0x424F4D45,

        /// <summary>
        /// [EMPR] - No research has been done on this format
        /// </summary>
        EXPORTED_MISSION_PROP_HANDLES = 0x52504D45,

        // ===================================
        // --------- MISSION LOGIC -----------
        // ===================================
        /// <summary>
        /// [LELD] - No research has been done on this format
        /// </summary>
        LOGIC_EXPORT_DATA = 0x444C454C,

        /// <summary>
        /// [LESC] - A block containing strings for mission script data
        /// </summary>
        LOGIC_EXPORT_STRING_COLLECTION = 0x4353454C,

        /// <summary>
        /// [LESB] - A list of sound IDs to use specific sounds in the mission
        /// </summary>
        LOGIC_EXPORT_SOUND_BANK = 0x4253454C,

        /// <summary>
        /// [LEAC] - No research has been done on this format
        /// </summary>
        LOGIC_EXPORT_ACTORS_CHUNK = 0x4341454C,

        /// <summary>
        /// [LEAD] - No research has been done on this format
        /// </summary>
        LOGIC_EXPORT_ACTOR_DEFINITIONS = 0x4441454C,

        /// <summary>
        /// [LENC] - No research has been done on this format
        /// </summary>
        LOGIC_EXPORT_NODES_CHUNK = 0x434E454C,

        /// <summary>
        /// [LEND] - No research has been done on this format
        /// </summary>
        LOGIC_EXPORT_NODE_DEFINITIONS_TABLE = 0x444E454C,

        /// <summary>
        /// [LEPR] - No research has been done on this format
        /// </summary>
        LOGIC_EXPORT_PROPERTIES_TABLE = 0x5250454C,

        /// <summary>
        /// [LEAS] - No research has been done on this format
        /// </summary>
        LOGIC_EXPORT_ACTOR_SET_TABLE = 0x5341454C,

        /// <summary>
        /// [LEWC] - No research has been done on this format
        /// </summary>
        LOGIC_EXPORT_WIRE_COLLECTIONS = 0x4357454C,

        /// <summary>
        /// [LECO] - No research has been done on this format
        /// </summary>
        LOGIC_EXPORT_SCRIPT_COUNTERS = 0x4F43454C,

        // ===================================
        // -- Unknown purposes (leftovers?) --
        // ===================================
        /// <summary>
        /// [LEPS] - No research has been done on this format
        /// </summary>
        LOGIC_EXPORT_UNKNOWN_LEPS = 0x5350454C,
        
        /// <summary>
        /// [LENE] - No research has been done on this format
        /// </summary>
        LOGIC_EXPORT_UNKNOWN_LENE = 0x454E454C,
        
        /// <summary>
        /// [FING] - No research has been done on this format
        /// </summary>
        LOGIC_EXPORT_UNKNOWN_FING = 0x474E4946,

        /// <summary>
        /// [LEAT] - No research has been done on this format
        /// </summary>
        LOGIC_EXPORT_UNKNOWN_LEAT = 0x5441454C,

        // =======================================================================================
        // ------------------------------------- *.DAM FILES -------------------------------------
        // =======================================================================================
        /// <summary>
        /// [UPXD] - A block that contains character model and texture data.
        /// </summary>
        EXTRA_CHARACTER_DATA_PACKAGE = 0x44585055,

        /// <summary>
        /// [UPCB] - A block that contains character skeletons for the male and female models
        /// </summary>
        SKELETON_PACKAGE = 0x42435055,

        // =======================================================================================
        // ------------------------------------- *.MEC FILES -------------------------------------
        // =======================================================================================
        /// <summary>
        /// [RMDP] - A block that holds data pertaining to the GUI (main menu, pause menu, etc)
        /// </summary>
        REFLECTIONS_MENU_DATA_PACKAGE = 0x50444D52,

        /// <summary>
        /// [RMDL] - A block that contains data for handling all GUI-related data
        /// </summary>
        REFLECTIONS_MENU_DATA_CHUNK = 0x4C444D52,

        // =======================================================================================
        // ------------------------------------- *.BNK FILES -------------------------------------
        // =======================================================================================
        /// <summary>
        /// [FONT] - A block that holds data pertaining to the fonts
        /// </summary>
        FONT_CONTAINER = 0x544E4F46,

        /// <summary>
        /// [FBNK] - A block that contains textures used for the fonts
        /// </summary>
        FONT_DATA = 0x4B4E4246,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: RESOURCE PACKAGES ---------------------
        // =======================================================================================
        /// <summary>
        /// [DRSP] - No research has been done on this format
        /// </summary>
        RESOURCE_CHUNK = 0x50535244,

        /// <summary>
        /// [TEXC] - No research has been done on this format
        /// </summary>
        RESOURCE_TEXTURE = 0x43584554,

        /// <summary>
        /// [UIDC] - No research has been done on this format
        /// </summary>
        RESOURCE_TEXTURE_UID = 0x43444955,

        /// <summary>
        /// [DXTC] - No research has been done on this format
        /// </summary>
        RESOURCE_TEXTURE_DDS = 0x43545844,

        /// <summary>
        /// [MACO] - No research has been done on this format
        /// </summary>
        RESOURCE_MATERIAL_CONTAINER = 0x4F43414D,

        /// <summary>
        /// [MAHE] - No research has been done on this format
        /// </summary>
        RESOURCE_MATERIAL_HEADER = 0x4548414D,

        /// <summary>
        /// [MATX] - No research has been done on this format
        /// </summary>
        RESOURCE_MATERIAL_XML = 0x5844414D,

        /// <summary>
        /// [MARH] - No research has been done on this format
        /// </summary>
        RESOURCE_MATERIAL_RENDER_METHOD_HEADER = 0x4852414D,

        /// <summary>
        /// [MARP] - No research has been done on this format
        /// </summary>
        RESOURCE_MATERIAL_RENDER_METHOD_PARAMETER = 0x5052414D,

        /// <summary>
        /// [SBMC] - No research has been done on this format
        /// </summary>
        SUBMODEL_CHUNK = 0x434D4253,

        /// <summary>
        /// [SBBH] - No research has been done on this format
        /// </summary>
        SUBMODEL_HEADER = 0x48424253,

        /// <summary>
        /// [VXCK] - No research has been done on this format
        /// </summary>
        POOLED_VERTEX_CHUNK = 0x4B435856,

        /// <summary>
        /// [VXHD] - No research has been done on this format
        /// </summary>
        POOLED_VERTEX_HEADER = 0x44485856,

        /// <summary>
        /// [VXDP] - No research has been done on this format
        /// </summary>
        POOLED_VERTEX_DEPENDENCIES = 0x50445856,

        /// <summary>
        /// [VXDT] - No research has been done on this format
        /// </summary>
        POOLED_VERTEX_BUFFER = 0x54445856,

        /// <summary>
        /// [VCDL] - No research has been done on this format
        /// </summary>
        POOLED_VERTEX_DECLARATION = 0x4C434456,

        /// <summary>
        /// [IXCK] - No research has been done on this format
        /// </summary>
        POOLED_INDEX_CHUNK = 0x4B435849,

        /// <summary>
        /// [IXHD] - No research has been done on this format
        /// </summary>
        POOLED_INDEX_HEADER = 0x44485849,

        /// <summary>
        /// [IXDP] - No research has been done on this format
        /// </summary>
        POOLED_INDEX_DEPENDENCIES = 0x50445849,

        /// <summary>
        /// [IXDT] - No research has been done on this format
        /// </summary>
        POOLED_INDEX_BUFFER = 0x54445849,

        /// <summary>
        /// [MDID] - No research has been done on this format
        /// </summary>
        RENDERER_DATA_UIDS = 0x4449444D,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: CHARACTER DATA ------------------------
        // =======================================================================================
        /// <summary>
        /// [AGRA] - No research has been done on this format
        /// </summary>
        ANIMATION_GRAPH = 0x41524741,

        /// <summary>
        /// [PDMD] - No research has been done on this format
        /// </summary>
        PEDESTRIAN_MESH_DATA = 0x444D4450,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: CITY LOCKING DATA ---------------------
        // =======================================================================================
        /// <summary>
        /// [CTYE] - No research has been done on this format
        /// </summary>
        CITY_LOCKING_ENTRY = 0x45595443,

        /// <summary>
        /// [CTYS] - No research has been done on this format
        /// </summary>
        CITY_LOCKING_SPLINES = 0x53595443,

        /// <summary>
        /// [CTYR] - No research has been done on this format
        /// </summary>
        CITY_LOCKING_ROADS = 0x52595443,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: CITY PACKAGE DATA ---------------------
        // =======================================================================================
        /// <summary>
        /// [AMBV] - No research has been done on this format
        /// </summary>
        AMBIENT_VOLUME_DATA = 0x56424D41,

        /// <summary>
        /// [BILL] - No research has been done on this format
        /// </summary>
        BILLBOARD_INFO = 0x4C4C4942,

        /// <summary>
        /// [HINF] - No research has been done on this format
        /// </summary>
        HEIGHT_INFO = 0x464E4948,

        /// <summary>
        /// [ADVH] - No research has been done on this format
        /// </summary>
        ADVERTISING_HEADER = 0x48564441,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: COMMENTARY DATA -----------------------
        // =======================================================================================
        /// <summary>
        /// [CMRT] - No research has been done on this format
        /// </summary>
        COMMENTARY_ROOT = 0x54524D43,

        /// <summary>
        /// [CMCC] - No research has been done on this format
        /// </summary>
        COMMENTARY_COMMON_CONTAINER = 0x43434D43,

        /// <summary>
        /// [CMMS] - No research has been done on this format
        /// </summary>
        COMMENTARY_MISSION_DATA = 0x534D4D43,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: CUTSCENE DATA -------------------------
        // =======================================================================================
        /// <summary>
        /// [CUTR] - No research has been done on this format
        /// </summary>
        CUTSCENES_ROOT = 0x52545543,

        /// <summary>
        /// [CUTS] - No research has been done on this format
        /// </summary>
        CUTSCENES_DATA = 0x53545543,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: SHADER DATA ---------------------------
        // =======================================================================================
        /// <summary>
        /// [SPSR] - No research has been done on this format
        /// </summary>
        SHADER_PARAM_SET_ROOT = 0x52535053,

        /// <summary>
        /// [SPSS] - No research has been done on this format
        /// </summary>
        SHADER_PARAM_SET = 0x53535053, //SPSS

        /// <summary>
        /// [SPSP] - No research has been done on this format
        /// </summary>
        SHADER_PARAM_SET_PARAMS = 0x50535053, //SPSP

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: VEHICLE PACKAGE DATA ------------------
        // =======================================================================================
        /// <summary>
        /// [VSRT] - No research has been done on this format
        /// </summary>
        VEHICLE_SOUND_ROOT = 0x54525356,

        /// <summary>
        /// [VSCH] - No research has been done on this format
        /// </summary>
        VEHICLE_COMMON_SOUND_HEADER = 0x48435356,

        /// <summary>
        /// [VSVH] - No research has been done on this format
        /// </summary>
        VEHICLE_SOUND_HEADER = 0x48565356,

        /// <summary>
        /// [SBAK] - No research has been done on this format
        /// </summary>
        VEHICLE_SOUND_BANK = 0x4B414253,

        /// <summary>
        /// [GRBK] - No research has been done on this format
        /// </summary>
        VEHICLE_GRANULAR_BANK = 0x4B425247,

        // =======================================================================================
        // ---------------------- DRIVER: SAN FRANCISCO :: UNKNOWN DATA --------------------------
        // =======================================================================================
        /// <summary>
        /// [DMWK] - This format may be leftover from development. It serves an unknown purpose in 'dngvehicles.sp' in Driver: San Francisco.
        /// </summary>
        DM_WMK_INFO = 0x4B574D44,
    }
}
