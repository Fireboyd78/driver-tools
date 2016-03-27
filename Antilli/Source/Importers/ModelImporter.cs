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
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

using FreeImageAPI;

using DSCript;

namespace Antilli
{
    public abstract class ModelImporter : IDisposable
    {
        public virtual void Dispose()
        {
            if (BaseStream != null)
                BaseStream.Dispose();
        }

        public Stream BaseStream { get; protected set; }

        public string FileName { get; protected set; }
        public string Name { get; set; }

        public Dictionary<string, Material> Materials { get; protected set; }

        public virtual Model3DCollection Models
        {
            get { return null; }
        }

        public virtual int Load()
        {
            throw new NotImplementedException();
        }

        public ModelImporter(string filename) : this(File.OpenRead(filename))
        {
            FileName = filename;
            Name = Path.GetFileNameWithoutExtension(filename);
        }

        public ModelImporter(Stream stream)
        {
            BaseStream = stream;
        }
    }
}
