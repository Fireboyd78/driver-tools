using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DSCript.Spoolers
{
    public static class SpoolerExtensions
    {
        public static List<Spooler> GetAllSpoolers(this SpoolableChunk @this)
        {
            var spoolerList = @this.Spoolers;

            if (spoolerList != null)
            {
                var spoolers = new List<Spooler>(spoolerList);

                foreach (Spooler s in spoolerList)
                    if (s is SpoolableChunk)
                        spoolers.AddRange(((SpoolableChunk)s).GetAllSpoolers());

                return spoolers;
            }
            else
            {
                return new List<Spooler>();
            }
        }
    }
}
