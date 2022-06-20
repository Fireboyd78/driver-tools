using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace DSCript.Models
{
    public static class MaterialManager
    {
        //
        // Return codes:
        //  1: OK
        //  0: not found
        //
        // -1: missing material
        // -2: default material
        // -3: null material
        // -4: local material
        //
        // -128: undefined result
        //
        public static int Find(IMaterialPackage package, MaterialHandle material, out IMaterialData result)
        {
            result = null;

            //
            // Null Material
            //
            if (material == 0xCCCCCCCC)
                return -3;

            // material is local to its model package?
            var isLocalMaterial = false;

            switch (material.UID)
            {
            //
            // Null Material
            //
            case 0xCCCC:
                return -3;
            //
            // Local Material
            //
            case 0xFFFD:
                isLocalMaterial = true;
                break;
            //
            // Global Material
            //
            case 0xF00D:
            case 0xFFFC:
            case 0xFFFE:
                //
                // TODO:
                //  - Implement material package manager
                //  - Material packages are model packages with no models (e.g. menu files, overlays, etc)
                //  - Enumerate over the loaded material packages for the material
                //
                return -128;
            //
            // Default Material
            //
            case 0xFFFF:
                //
                // TODO:
                //  - Return a default material from material package manager
                //  - Handle = 0, UID = 0xFFFF
                //
                return -2;
            }

            if (isLocalMaterial)
            {
                // can't get local materials without a reference
                if (package == null)
                    return -4;
            }
            else
            {
                // get the model package if needed
                if ((package == null) || (package.UID != material.UID))
                    package = PackageManager.Find(material.UID);
            }
            
            if (package == null)
            {
                DSC.Log($"WARNING: Material {material.Handle} requested from missing package {material.UID}!");
                return 0;
            }

            // try to get the material from the model package
            return package.FindMaterial(material, out result);
        }

        public static int Find(MaterialHandle material, out IMaterialData result)
        {
            return Find(null, material, out result);
        }

        public static int Find<TMaterialData>(IMaterialPackage package, MaterialHandle material, out TMaterialData result)
            where TMaterialData : class, IMaterialData
        {
            IMaterialData mtl = null;

            var type = Find(package, material, out mtl);

            result = (mtl as TMaterialData);
            return type;
        }
        
        public static int Find<TMaterialData>(MaterialHandle material, out TMaterialData result)
            where TMaterialData : class, IMaterialData
        {
            return Find(null, material, out result);
        }
    }
}
