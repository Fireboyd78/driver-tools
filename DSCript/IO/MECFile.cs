using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public class MenuPackageData : SpoolableResource<SpoolablePackage>
    {

        protected override void Load()
        {
            throw new NotImplementedException();
        }

        protected override void Save()
        {
            throw new NotImplementedException();
        }
    }

    public class MECFile : FileChunker
    {

    }
}
