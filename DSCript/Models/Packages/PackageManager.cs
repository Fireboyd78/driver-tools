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

using System.Xml;
using System.Xml.Linq;

using DSCript.Spooling;

namespace DSCript.Models
{
    public static class PackageManager
    {
        static readonly Dictionary<int, IMaterialPackage> Packages;

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
        
        public static bool IsRegistered(IMaterialPackage package)
        {
            if (package == null)
                return false;

            var uid = package.UID;
            
            return ReferenceEquals(package, Find(uid));
        }

        public static bool IsRegisterable(IMaterialPackage package)
        {
            if (package == null)
                return false;

            var uid = package.UID;

            if (IsValidUID(uid))
                return !Packages.ContainsKey(uid);

            return false;
        }

        public static IMaterialPackage Find(int uid)
        {
            IMaterialPackage package = null;

            if (IsValidUID(uid))
                Packages.TryGetValue(uid, out package);
            
            return package;
        }

        public static void Load(IMaterialPackage package)
        {
            SpoolableResourceFactory.Load(package);

            var uid = package.UID;

            if (IsValidUID(uid))
            {
                if (!Packages.ContainsKey(uid))
                    Packages.Add(uid, package);
            }
        }

        public static bool Register(IMaterialPackage package, int uid)
        {
            if (Packages.ContainsKey(uid))
                return false;

            Packages.Add(uid, package);
            return true;
        }

        public static bool Register(IMaterialPackage package)
        {
            var uid = package.UID;

            if (IsValidUID(uid))
            {
                if (Register(package, uid))
                    return true;

                throw new InvalidOperationException("Cannot register a package more than once.");
            }

            return false;
        }

        public static bool UnRegister(int uid)
        {
            if (Packages.ContainsKey(uid))
            {
                Packages.Remove(uid);
                return true;
            }

            return false;
        }

        public static bool UnRegister(IMaterialPackage package)
        {
            var uid = package.UID;

            if (IsValidUID(uid))
                return UnRegister(uid);

            return false;
        }

        public static IEnumerable<IMaterialPackage> EnumerateAll()
        {
            foreach (var package in Packages)
                yield return package.Value;

            yield break;
        }
        
        static PackageManager()
        {
            Packages = new Dictionary<int, IMaterialPackage>();
        }
    }
}
