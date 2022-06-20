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
    
    public sealed class MaterialDataPC : MaterialDataWrapper<SubstanceDataPC>, ICopyCat<MaterialDataPC>
    {
        public MaterialDataPC() : base() { }

        bool ICopyCat<MaterialDataPC>.CanCopy(CopyClassType copyType)                       => true;
        bool ICopyCat<MaterialDataPC>.CanCopyTo(MaterialDataPC obj, CopyClassType copyType) => true;

        bool ICopyCat<MaterialDataPC>.IsCopyOf(MaterialDataPC obj, CopyClassType copyType)
        {
            throw new NotImplementedException();
        }

        MaterialDataPC ICopyClass<MaterialDataPC>.Copy(CopyClassType copyType)
        {
            var material = new MaterialDataPC();

            CopyTo(material, copyType);

            return material;
        }

        public void CopyTo(MaterialDataPC obj, CopyClassType copyType)
        {
            obj.Type = Type;
            obj.AnimationSpeed = AnimationSpeed;

            var substances = new List<SubstanceDataPC>();

            if (copyType == CopyClassType.DeepCopy)
            {
                // copy substances
                foreach (var _substance in Substances)
                {
                    // DEEP COPY: all new instances down the line
                    var substance = CopyCatFactory.GetCopy(_substance, CopyClassType.DeepCopy);

                    // add to new substances
                    substances.Add(substance);
                }
            }
            else
            {
                // reuse substance references
                substances.AddRange(Substances);
            }

            obj.Substances = substances;
        }
    }
}
