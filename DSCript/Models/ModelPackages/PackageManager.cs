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
using System.Windows.Media.Media3D;

using System.Xml;
using System.Xml.Linq;

using DSCript.Spooling;

namespace DSCript.Models
{
    public static class PackageManager
    {
        static readonly Dictionary<int, ModelPackage> Packages;

        //
        // TODO:
        //  - Configure an upper limit? (Max 64 in Driv3r)
        //
        static bool IsValidUID(int uid)
        {
            return (uid != 0xFF);
        }

        public static void Clear()
        {
            //
            // TODO: Dependencies?
            //
            Packages.Clear();
        }

        public static bool IsRegistered(ModelPackage package)
        {
            if (package == null)
                return false;

            var uid = package.UID;
            
            return ReferenceEquals(package, Find(uid));
        }
        
        public static ModelPackage Find(int uid)
        {
            ModelPackage package = null;

            if (IsValidUID(uid))
                Packages.TryGetValue(uid, out package);
            
            return package;
        }

        public static void Load(ModelPackage package)
        {
            SpoolableResourceFactory.Load(package);

            var uid = package.UID;

            if (IsValidUID(uid))
            {
                if (!Packages.ContainsKey(uid))
                    Packages.Add(uid, package);
            }
        }
        
        public static bool Register(ModelPackage package)
        {
            var uid = package.UID;

            if (IsValidUID(uid))
            {
                if (Packages.ContainsKey(uid))
                    throw new InvalidOperationException("Cannot register a package more than once.");

                Packages.Add(uid, package);
                return true;
            }

            return false;
        }
        
        public static bool UnRegister(ModelPackage package)
        {
            var uid = package.UID;

            if (IsValidUID(uid))
            {
                if (Packages.ContainsKey(uid))
                    Packages.Remove(uid);
            }

            return false;
        }
        
        static PackageManager()
        {
            Packages = new Dictionary<int, ModelPackage>();
        }
    }
}
