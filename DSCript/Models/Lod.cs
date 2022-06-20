using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DSCript.Models
{
    [Flags]
    public enum LodMask
    {
        Zero 				= 0,

        // Lods
	    Low 				= 1,    // VL
	    High 				= 2,    // H
	    Lod1 				= 4,    // M
	    Lod2 				= 8,    // L
	    Lod3 				= 16,   // Unused?
	    Night 				= 1024, // hmm, lod only visible at night maybe?
	    NoShadow 			= Zero, // odd...

        // Flags
	    Shadow 				= 32,
        FLAG_64             = 64, // 'hwat is this?!

        // Internal game flags?
	    Alpha 				= 128,
	    NoVisibilityTest 	= 256,
	    Crossfade 			= 512,
	    CameraDistanceSet 	= 2048,

        // Valid bit masks
	    Lods 			    = Low | High | Lod1 | Lod2 | Lod3 | Night | NoShadow,
        Flags               = Shadow | FLAG_64,
    }

    public class Lod : ICopyCat<Lod>
    {
        bool ICopyCat<Lod>.CanCopy(CopyClassType copyType)              => true;
        bool ICopyCat<Lod>.CanCopyTo(Lod obj, CopyClassType copyType)   => true;

        bool ICopyCat<Lod>.IsCopyOf(Lod obj, CopyClassType copyType)
        {
            throw new NotImplementedException();
        }

        Lod ICopyClass<Lod>.Copy(CopyClassType copyType)
        {
            var lod = new Lod();

            CopyTo(lod, copyType);

            return lod;
        }

        void ICopyClassTo<Lod>.CopyTo(Lod obj, CopyClassType copyType)
        {
            CopyTo(obj, copyType);
        }

        protected void CopyTo(Lod obj, CopyClassType copyType)
        {
            obj.Type = Type;
            obj.Flags = Flags;
            obj.Mask = Mask;

            var instances = new List<LodInstance>();

            if (copyType == CopyClassType.DeepCopy)
            {
                foreach (var _instance in Instances)
                {
                    var instance = new LodInstance()
                    {
                        // parent to NEW lod
                        // allows copy operation to reparent for us
                        Parent = obj,
                    };

                    // DEEP COPY: all new instances down the line
                    CopyCatFactory.CopyToB(_instance, instance, CopyClassType.DeepCopy);

                    // add new lod instance
                    instances.Add(instance);
                }
            }
            else
            {
                // reuse the parent
                obj.Parent = Parent;

                // reuse the lod instance references
                obj.Instances.AddRange(Instances);
            }

            obj.Instances = instances;
        }

        public int Type { get; set; }

        public Model Parent { get; set; }
        public List<LodInstance> Instances { get; set; }

        public int Flags { get; set; }

        public int Mask { get; set; }

        public int ExtraData { get; set; }

        public static string GetLodTypeName(int lodType)
        {
            switch (lodType)
            {
            case 0: return "HIGH";
            case 1: return "LOD1";
            case 2: return "LOD2";
            case 3: return "LOD3";
            case 4: return "LOW";
            case 5: return "SHADOW1";
            case 6: return "SHADOW2";
            }

            throw new Exception($"FATAL: Unknown Lod type '{lodType}' -- cannot resolve name!");
        }

        public static int GetLodNameType(string lodName)
        {
            switch (lodName)
            {
            case "LOD0":
            case "HIGH":
                return 0;

            case "LOD1":
                return 1;

            case "LOD2":
                return 2;

            case "LOD3":
                return 3;

            case "LOD4":
            case "LOW":
                return 4;

            case "LOD5":
            case "SHADOW1":
                return 5;

            case "LOD6":
            case "SHADOW2":
                return 6;
            }

            return -1;
        }

        public static int GetLodTypeMask(int lodType, int version = 6)
        {
            switch (version)
            {
            case 1:
                switch (lodType)
                {
                case 0: return 2;
                case 1: return 4;
                case 2: return 8;
                case 3: return 16;
                case 4: return 0;
                case 5: return 0;
                case 6: return 0;
                }
                break;
            case 6:
                switch (lodType)
                {
                                                    // old names reference...
                                                    // :
                case 0: return (2 | 4 | 8 | 16);    // H (0x1E)
                case 1: return (4 | 8 | 16);        // M (0x1C)
                case 2: return (8 | 16);            // L (0x18)
                case 3: return 16;                  // - (0x10) -- welp, I never paid attention to this
                case 4: return 1;                   // VL       -- BUGFIX: type was incorrect!
                case 5: return 0;                   // SHADOW1
                case 6: return 0;                   // SHADOW2 !!! TODO !!!
                }
                break;
            }
            
            return -1;
        }

        public static int GuessLodTypeByMask(int lodMask)
        {
            switch (lodMask)
            {
            case 0x1E:  return 0;
            case 0x1C:  return 1;
            case 0x18:  return 2;
            case 0x10:  return 3;
            case 1:     return 4;
            case 0:     return 5;
            }

            return -1;
        }

        protected Lod()
        {
            Instances = new List<LodInstance>();
        }

        public Lod(int type)
            : this()
        {
            Type = type;
        }
    }
}
