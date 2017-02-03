using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace LocSF
{
    public class SpooledLocalisationFile : FileChunker
    {
        public List<LocalisationPackage> LocalisationPackages { get; set; }

        public SpoolSystem SpoolSystem { get; set; }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            var context = (SpoolerContext)sender.Context;

            switch (context.ToString())
            {
            case "SLRR":
                SpoolableResourceFactory.Create<LocalisationPackage>(sender, true);
                break;
            }

            base.OnSpoolerLoaded(sender, e);
        }

        protected override void OnFileLoadEnd()
        {
            SpoolSystem = new SpoolSystem();
            SpoolSystem.Load(this);

            // TESTING
            SpoolSystem.Save($"{Path.Combine(Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(FileName))}.SSIC");

            base.OnFileLoadBegin();
        }

        public SpooledLocalisationFile() : base() { }
        public SpooledLocalisationFile(string filename) : base(filename) { }
    }
}
