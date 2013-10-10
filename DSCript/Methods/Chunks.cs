using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Methods
{
    public static class Chunks
    {
        public static string Magic2Str(uint magic)
        {
            return (magic > 255)
                ? new string(new char[]{
                    (char)(magic & 0x000000FF),
                    (char)((magic & 0x0000FF00) >> 8),
                    (char)((magic & 0x00FF0000) >> 16),
                    (char)((magic & 0xFF000000) >> 24)})
                    : (magic == 0)
                        ? "Unified Packager"
                        : magic.ToString("X");
        }
    }
}
