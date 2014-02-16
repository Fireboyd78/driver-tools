using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Antilli
{
    public class DriverModelGroup
    {
        public List<DriverModelVisual3D> Models { get; set; }

        public DriverModelGroup()
        {
            Models = new List<DriverModelVisual3D>();
        }
    }
}
