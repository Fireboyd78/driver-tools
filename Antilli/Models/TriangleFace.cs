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

namespace Antilli.Models
{
    public class TriangleFace
    {
        public int P1 { get; private set; }
        public int P2 { get; private set; }
        public int P3 { get; private set; }

        public override string ToString()
        {
            return String.Format("[{0}, {1}, {2}]", P1, P2, P3);
        }

        public TriangleFace(int p1, int p2, int p3)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

        public TriangleFace(int p1, int p2, int p3, int minIndex)
        {
            P1 = p1 - minIndex;
            P2 = p2 - minIndex;
            P3 = p3 - minIndex;
        }
    }
}
