using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using DSCript.Spooling;

namespace DSCript.Models
{
    public class ModelFile : FileChunker
    {
        public List<ModelPackage> Models { get; set; }

        public ModelPackage GetModelPackage(int uid)
        {
            return (HasModels) ? Models.FirstOrDefault((m) => m.UID == uid) : null;
        }

        public virtual bool HasModels
        {
            get { return (Models != null && Models.Count > 0); }
        }

        public override bool CanSave
        {
            get { return HasModels && base.CanSave; }
        }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            switch ((ChunkType)sender.Context)
            {
            case ChunkType.ModelPackagePC:
            case ChunkType.ModelPackagePC_X:
                var modelPackage = SpoolableResourceFactory.Create<ModelPackage>(sender);
                modelPackage.ModelFile = this;

                Models.Add(modelPackage);
                break;
            }

            base.OnSpoolerLoaded(sender, e);
        }

        protected override void OnFileLoadBegin()
        {
            Models = new List<ModelPackage>();

            base.OnFileLoadBegin();
        }

        protected override void OnFileSaveBegin()
        {
            if (HasModels)
            {
                foreach (var model in Models)
                    model.CommitChanges();
            }

            base.OnFileSaveBegin();
        }

        public ModelFile() { }
        public ModelFile(string filename) : base(filename) { }
    }
}
