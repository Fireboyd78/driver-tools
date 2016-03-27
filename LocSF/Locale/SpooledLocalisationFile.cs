using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace LocSF
{
    public class SpooledLocalisationFile : FileChunker
    {
        public List<LocalisationPackage> LocalisationPackages { get; set; }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            base.OnSpoolerLoaded(sender, e);
        }
    }
}
