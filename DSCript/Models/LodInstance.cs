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
    public class LodInstance : ICopyCat<LodInstance>
    {
        bool ICopyCat<LodInstance>.CanCopy(CopyClassType copyType)                      => true;
        bool ICopyCat<LodInstance>.CanCopyTo(LodInstance obj, CopyClassType copyType)   => true;

        bool ICopyCat<LodInstance>.IsCopyOf(LodInstance obj, CopyClassType copyType)
        {
            throw new NotImplementedException();
        }

        LodInstance ICopyClass<LodInstance>.Copy(CopyClassType copyType)
        {
            var instance = new LodInstance();

            CopyTo(instance, copyType);

            return instance;
        }

        void ICopyClassTo<LodInstance>.CopyTo(LodInstance obj, CopyClassType copyType)
        {
            CopyTo(obj, copyType);
        }

        protected void CopyTo(LodInstance obj, CopyClassType copyType)
        {
            obj.Transform = Transform;
            obj.UseTransform = UseTransform;
            obj.Reserved = Reserved;
            obj.Handle = Handle;

            var submodels = new List<SubModel>();

            if (copyType == CopyClassType.DeepCopy)
            {
                // in case we can reparent submodels
                var parent = obj.Parent;

                foreach (var _submodel in SubModels)
                {
                    // SOFT COPY: reuse all back references (we'll fix them after)
                    var submodel = CopyCatFactory.GetCopy(_submodel, CopyClassType.SoftCopy);

                    // reparent to NEW instance
                    submodel.LodInstance = obj;

                    // can reparent to new model?
                    if (parent != null)
                        submodel.Model = parent.Parent ?? null;
                    
                    //
                    // **** OH FUCK: someone needs to fix the model package it's attached to now! ****
                    //

                    // add new submodel
                    submodels.Add(submodel);
                }
            }
            else
            {
                // reuse the parent
                obj.Parent = Parent;

                // reuse submodel references
                submodels.AddRange(SubModels);
            }

            obj.SubModels = submodels;
        }

        public Lod Parent { get; set; }

        public List<SubModel> SubModels { get; set; }

        public Matrix44 Transform { get; set; }

        public bool UseTransform { get; set; }
        
        // likely unused, but I'm tired of chasing after bugs
        public int Reserved { get; set; }

        public int Handle { get; set; }

        public LodInstance()
        {
            SubModels = new List<SubModel>();
            Transform = Matrix44.Identity;
        }
    }
}
