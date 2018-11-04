using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

using FreeImageAPI;

namespace DSCript.Models
{
    public enum MaterialType
    {
        Group,
        Animated,
    }

    public interface IMaterialData
    {
        MaterialType Type { get; set; }

        float AnimationSpeed { get; set; }

        IEnumerable<ISubstanceData> Substances { get; }

        ISubstanceData GetSubstance(int index);
    }

    public abstract class MaterialDataWrapper<TSubstanceData> : IMaterialData
        where TSubstanceData : ISubstanceData
    {
        IEnumerable<ISubstanceData> IMaterialData.Substances
        {
            get { return (IEnumerable<ISubstanceData>)Substances; }
        }

        ISubstanceData IMaterialData.GetSubstance(int index)
        {
            return Substances[index];
        }

        public MaterialType Type { get; set; }

        public float AnimationSpeed { get; set; }

        public List<TSubstanceData> Substances { get; set; }
        
        public MaterialDataWrapper()
        {
            Substances = new List<TSubstanceData>();
        }
    }
    
    public sealed class MaterialDataPC : MaterialDataWrapper<SubstanceDataPC>
    {
        public MaterialDataPC() : base() { }
    }
}
